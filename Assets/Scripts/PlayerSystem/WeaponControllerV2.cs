using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class WeaponControllerV2 : NetworkBehaviour
    {
        [Tooltip("The player's currently equipped weapon")]
        public NetworkVariable<int> weaponIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public BaseEquipment[] slots = new BaseEquipment[4];
        public BaseEquipment specialEquipment;
        public NetworkVariable<bool> usingSpecial = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public Transform weaponPoint;

        public PlayerManager pm;
        public NetworkAnimator networkAnimator;
        public PlayerMotorV2 Controller;
        public bool cancellingReload;
        public bool Reloading => slots[weaponIndex.Value] is RangedWeapon rw && rw.reloading;


        [Tooltip("The point where held items will hover")]
        public Transform grabPoint;
        public float grabLerpToHandSpeed;
        float currentGrabLerpTime;
        bool lerpingGrab;
        public LayerMask grabLayermask;
        [Tooltip("How far can the player reach to grab something?")]
        public float grabDistance;
        [Tooltip("The thickness of the ray used to try and grab things.")]
        public float grabRadius;
        public BaseCarriable currentCarriable;
        public BaseCarriable currentCarriableTargeted;
        public float carriableThrowForce;

        public LayerMask interactLayermask;
        public float interactDistance, interactRadius;
        public BaseInteractable currentInteractableTargeted;

        public bool Grabbing => currentCarriable != null;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(Controller == null)
            {
                Controller = GetComponent<PlayerMotorV2>();
            }
            pm = PlayerManager.playersByID[OwnerClientId];
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void SetEquipmentSlot_RPC(NetworkBehaviourReference nbr, int index)
        {
            if (nbr.TryGet(out BaseEquipment equip))
            {
                slots[index] = equip;
                equip.cr.InitialiseViewable(pm.Character);
            }
        }
        public bool TrySwitchWeapon(int index)
        {
            if (slots[index] != null)
            {
                weaponIndex.Value = index;
                return true;
            }
            return false;
        }

        private void Update()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                BaseEquipment e = slots[i];
                if(i == weaponIndex.Value)
                {
                    e.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                    e.transform.localScale = Vector3.one;
                    if (IsOwner)
                    {
                        if(e is RangedWeapon rw)
                        {
                            if (pm.reloadInput && !rw.reloading && rw.CurrentAmmo < rw.maxAmmo && currentCarriable == null)
                            {
                                pm.reloadInput = false;
                                TryReload(rw);
                            }
                            if ((pm.fireInput || pm.secondaryInput) && rw.reloading && (accumulatedReloadTime >= rw.reloadCancelTime) || currentCarriable != null)
                            {
                                cancellingReload = true;
                            }
                        }
                        e.fireInput = pm.fireInput && currentCarriable == null;
                        e.secondaryInput = pm.secondaryInput && currentCarriable == null;
                    }
                }
                else
                {
                    if (IsOwner)
                    {
                        e.transform.localScale = Vector3.zero;
                        e.fireInput = false;
                        e.secondaryInput = false;
                    }
                }
            }
        }
        private void FixedUpdate()
        {
            if (IsOwner)
            {
                CheckCarriable();
                CheckInteractable();
            }
        }

        void CheckCarriable()
        {
            if (currentCarriable == null)
            {
                bool hitSomething = false;
                if (Physics.SphereCast(pm.Character.headTransform.position, grabRadius, pm.Character.headTransform.forward, out RaycastHit hit, grabDistance, grabLayermask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.rigidbody && hit.rigidbody.TryGetComponent(out BaseCarriable b) && !b.grabbed.Value)
                    {
                        //This may be a carriable, check the rigidbody.
                        if (currentCarriableTargeted == null || b != currentCarriableTargeted)
                        {
                            if (currentCarriableTargeted != null)
                            {
                                currentCarriableTargeted.HoverOver(false);
                            }
                            currentCarriableTargeted = b;
                            b.HoverOver(true);
                        }

                        if (pm.pickupInput)
                        {
                            PickUpCarriable(b);   
                        }
                        hitSomething = true;
                    }
                }
                if (!hitSomething && currentCarriableTargeted != null)
                {
                    currentCarriableTargeted.HoverOver(false);
                    currentCarriableTargeted = null;
                }
            }
            else
            {
                if (!lerpingGrab)
                {
                    currentCarriable.rb.Move(grabPoint.position, grabPoint.rotation * currentCarriable.grabOffset);
                }
                if (currentCarriable.canReleaseHere)
                {
                    if (pm.fireInput)
                    {
                        currentCarriable.rb.isKinematic = false;
                        currentCarriable.rb.AddForce(grabPoint.forward * carriableThrowForce, ForceMode.Impulse);
                        currentCarriable.Released_RPC(true);
                        pm.fireInput = false;
                        currentCarriable = null;
                    }
                }
                if (pm.secondaryInput)
                {
                    currentCarriable.Released_RPC(false);
                    currentCarriable = null;
                    pm.secondaryInput = false;
                }
            }
        }
        void PickUpCarriable(BaseCarriable carriable)
        {
            currentCarriable = carriable;
            carriable.rb.isKinematic = true;
            carriable.OnGrab_RPC((uint)OwnerClientId);
            StartCoroutine(GrabLerp());
        }

        IEnumerator GrabLerp()
        {
            WaitForFixedUpdate wff = new();
            lerpingGrab = true;
            Vector3 startPos = currentCarriable.transform.position;
            Quaternion startRot = currentCarriable.transform.rotation;
            while (currentGrabLerpTime < 1 && currentCarriable != null)
            {
                currentGrabLerpTime += Time.fixedDeltaTime * grabLerpToHandSpeed;
                currentCarriable.rb.Move(Vector3.Lerp(startPos, grabPoint.position, currentGrabLerpTime), Quaternion.Slerp(startRot, grabPoint.rotation * currentCarriable.grabOffset, currentGrabLerpTime));
                yield return wff;
            }
            currentGrabLerpTime = 0;
            lerpingGrab = false;
        }
        void CheckInteractable()
        {

        }
        
        public void ReceiveShot()
        {

        }
        void TryReload(RangedWeapon weapon)
        {
            if(weapon.CurrentAmmo < weapon.maxAmmo && !weapon.reloading)
            {
                if (weapon.useSingleReload)
                {
                    StartCoroutine(ReloadWeaponSingle(weapon));
                }
                else
                {
                    StartCoroutine(ReloadWeaponFull(weapon));
                }
            }
        }
        float accumulatedReloadTime;
        IEnumerator ReloadWeaponFull(RangedWeapon weapon)
        {
            weapon.reloading = true;
            var wff = new WaitForFixedUpdate();
            float t = 0;
            accumulatedReloadTime = 0;
            while (t < weapon.reloadTime && !cancellingReload)
            {
                accumulatedReloadTime += Time.fixedDeltaTime;
                t += Time.fixedDeltaTime;
                yield return wff;
            }
            if(!cancellingReload)
                weapon.RefillAmmo_RPC();
            weapon.reloading = false;
            cancellingReload = false;

        }

        IEnumerator ReloadWeaponSingle(RangedWeapon weapon)
        {
            weapon.reloading = true;
            var wff = new WaitForFixedUpdate();
            float t = 0;
            while (t < weapon.firstReloadDelay && !cancellingReload)
            {
                accumulatedReloadTime  += Time.fixedDeltaTime;
                t += Time.fixedDeltaTime;
            }
            weapon.AddAmmo(1);
            while (weapon.CurrentAmmo < weapon.maxAmmo && !cancellingReload)
            {
                while (t < weapon.reloadTime && !cancellingReload)
                {
                    accumulatedReloadTime += Time.fixedDeltaTime;
                    t += Time.fixedDeltaTime;
                    yield return wff;
                }
                weapon.AddAmmo(1);
            }
            weapon.reloading = false;
            cancellingReload = false;
        }
    }
}
