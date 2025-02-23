using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class WeaponControllerV2 : ONetBehaviour
    {
        [Tooltip("The player's currently equipped weapon")]
        public NetworkVariable<int> weaponIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public List<BaseEquipment> slots = new List<BaseEquipment>();
        public NetworkList<NetworkBehaviourReference> netSlots = new(new List<NetworkBehaviourReference>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> netSpecial = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public BaseEquipment specialEquipment;
        public NetworkVariable<bool> usingSpecial = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public Transform weaponPoint;

        public PlayerManager pm;
        public NetworkAnimator networkAnimator;
        public PlayerMotorV2 Controller;
        public bool cancellingReload;
        public bool Reloading => slots[weaponIndex.Value] is RangedWeapon rw && rw.reloading;

        CharacterAnimationV2 charAnim;

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

        public Transform fireOrigin;

        public bool Grabbing => currentCarriable != null;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(Controller == null)
            {
                Controller = GetComponent<PlayerMotorV2>();
            }
            if(charAnim == null)
            {
                charAnim = GetComponent<CharacterAnimationV2>();
            }
            pm = PlayerManager.playersByID[OwnerClientId];
            if (IsOwner)
            {
                weaponIndex.Value = 0;
            }
            netSlots.OnListChanged += NetSlots_OnListChanged;
            StartCoroutine(DelayInitialise());
        }
        IEnumerator DelayInitialise()
        {
            yield return null;
            if (netSlots.Count > 0)
                NetSlots_OnListChanged(new() { });
            NetSpecialUpdated(null, netSpecial.Value);
        }

        private void NetSpecialUpdated(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            if(current.TryGet(out BaseEquipment be))
            {
                specialEquipment = be;
                pm.hud.specialIcon.AssignIcon(be);
            }
        }

        private void NetSlots_OnListChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
        {

            print(changeEvent.Type);

            for (int i = 0; i < netSlots.Count; i++)
            {
                if (netSlots[i].TryGet(out BaseEquipment be))
                {
                    pm.hud.equipmentBarIcons[i].AssignIcon(be);
                    if(i < slots.Count)
                    {
                        slots[i] = be;
                        Debug.Log($"Assigning object to existing slot {i}", be);
                    }
                    else
                    {
                        Debug.Log($"Creating slot {i} and assigning object to new slot {i}", be);
                        slots.Add(be);
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to get Network Behaviour. Behaviour is either not of type [BaseEquipment] OR it is not a valid Network Behaviour.");
                }
            }

            if(slots.Count > 0)
            {
                if(IsOwner)
                    charAnim.UpdateAnimations((Slot)weaponIndex.Value);

                while (slots.Count > netSlots.Count)
                {
                    int index = Mathf.Max(0, slots.Count - 1);
                    Debug.Log($"Removing redundant object from slot {index}", this);
                    slots.RemoveAt(index);
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SetEquipmentSlot_RPC(NetworkBehaviourReference nbr, int index)
        {
            if (nbr.TryGet(out BaseEquipment equip))
            {
                if(index == 4)
                {
                    specialEquipment = equip;
                }
                else
                {
                    if (index >= slots.Count)
                    {
                        slots.Add(equip);
                    }
                    else
                    {
                        slots[index] = equip;
                    }
                    equip.cr.InitialiseViewable();
                    if(index == weaponIndex.Value)
                    {
                        charAnim.UpdateAnimations((Slot)index);
                        pm.hud.equipmentBarHeader.AssignEquipment(equip);
                    }
                }
            }
        }
        public bool TrySwitchWeapon(int index, bool special = false)
        {
            index %= slots.Count;
            BaseEquipment be;
            if (slots[weaponIndex.Value] is RangedWeapon cw)
            {
                cw.reloading = false;
            }
            if (special)
            {
                be = specialEquipment;
            }
            else
            {
                be = slots[index];
            }

            if (be.HasLimitedCharges && be.currentCharges <= 0)
                return false;


            if (be != null)
            {

                if (be.hasAnimations)
                {
                    if(!special)
                        weaponIndex.Value = index;

                    if (charAnim != null)
                        charAnim.UpdateAnimations((Slot)index);

                    pm.hud.equipmentBarHeader.AssignEquipment(be);
                }
                else
                {
                    be.TrySelect();
                }


                return true;
            }
            return false;
        }

        public override void OLateUpdate()
        {
            UpdateEquipment(4, specialEquipment);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                    continue;
                BaseEquipment e = slots[i];
                UpdateEquipment(i, e);
            }
        }
        void UpdateEquipment(int i, BaseEquipment e)
        {
            if ((i == weaponIndex.Value && e != specialEquipment) || i == 4)
            {
                e.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                e.transform.localScale = pm.Character.Alive ? Vector3.one : Vector3.zero;
                if (IsOwner)
                {
                    if (e is RangedWeapon rw)
                    {
                        if (pm.reloadInput && !rw.reloading && rw.CurrentAmmo < rw.maxAmmo && currentCarriable == null && pm.Character.Alive)
                        {
                            pm.reloadInput = false;
                            TryReload(rw);
                        }
                        if ((pm.fireInput || pm.secondaryInput) && rw.reloading && (accumulatedReloadTime >= rw.reloadCancelTime) || currentCarriable != null)
                        {
                            cancellingReload = true;
                        }
                    }
                    e.fireInput = pm.fireInput && currentCarriable == null && pm.Character.Alive;
                    e.secondaryInput = pm.secondaryInput && currentCarriable == null && pm.Character.Alive;
                }
            }
            else
            {
                e.transform.localScale = Vector3.zero;
                if (IsOwner)
                {
                    e.fireInput = false;
                    e.secondaryInput = false;
                }
            }
        }
        public override void OFixedUpdate()
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

                        if (pm.pickupInput && pm.Character.Alive)
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
                if (pm.secondaryInput || !pm.Character.Alive)
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
            currentCarriable.transform.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);
            while (currentGrabLerpTime < 1 && currentCarriable != null && pm.Character.Alive)
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
            bool hitSomething = false;
            if (Physics.SphereCast(pm.Character.headTransform.position, interactRadius, pm.Character.headTransform.forward, out RaycastHit hit, interactDistance, interactLayermask, QueryTriggerInteraction.Ignore))
            {
                BaseInteractable b = hit.collider.GetComponentInParent<BaseInteractable>();
                if(b != null && b.CanInteract(OwnerClientId))
                {
                    hitSomething = true;
                    if (currentInteractableTargeted != null && currentInteractableTargeted != b)
                    {
                        currentInteractableTargeted.HoverOver(false);
                        currentInteractableTargeted = null;
                        b.HoverOver(true);
                    }
                    if(currentInteractableTargeted == null)
                    {
                        if (!string.IsNullOrWhiteSpace(b.interactText))
                        {
                            pm.hud.SetInteractText(b.interactText, true);
                        }
                        else
                        {
                            pm.hud.SetInteractText("E to Interact", true);
                        }
                    }



                    currentInteractableTargeted = b;

                    if (pm.interactInput)
                    {
                        if (!b.holdInteract)
                        {
                            pm.interactInput = false;
                        }
                        b.InteractStart_RPC(OwnerClientId);
                    }
                    else
                    {
                        if(b.holdInteract)
                            b.InteractEnd_RPC(OwnerClientId);
                    }
                }
            }
            if (!hitSomething)
            {
                if(pm.hud.interactCG.alpha > 0)
                {
                    pm.hud.SetInteractText("", false);
                }
                if(currentInteractableTargeted != null)
                {
                    currentInteractableTargeted.HoverOver(false);
                    if (currentInteractableTargeted.holdInteract)
                        currentInteractableTargeted.InteractEnd_RPC(OwnerClientId);
                    currentInteractableTargeted = null;
                }
            }
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
