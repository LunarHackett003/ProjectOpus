using System.Collections.Generic;
using opus.Gameplay;
using opus.utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace opus.Weapons
{
    public class WeaponManager : NetworkBehaviour
    {

        [SerializeField] Transform weaponPoint, holsterPoint;
        [SerializeField] internal Transform fireDirectionReference;

        [SerializeField] internal PlayerCharacter pc;
        [SerializeField] internal PlayerAnimationHelper animHelper;

        public NetworkList<NetworkBehaviourReference> equipment = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> animHelperNetRef = new(writePerm: NetworkVariableWritePermission.Owner);
        [SerializeField] internal List<BaseEquipment> equipmentList;
        [SerializeField] internal int equipmentIndex;
        public NetworkObject[] equipmentPrefabs;

        public bool PrimaryInput => PlayerManager.Instance.fireInput && !PlayerManager.Instance.pc.Dead.Value;
        public bool SecondaryInput => PlayerManager.Instance.aimInput && !PlayerManager.Instance.pc.Dead.Value;
        public bool CanUseWeapon => !fireBlocked && !weaponBlocked && !carrying.Value && !interacting.Value;
        [SerializeField] internal bool fireBlocked;

        [SerializeField] Vector2 baseCrosshairSize;
        [SerializeField] Vector2 maxCrosshairSize;

        [SerializeField] RectTransform crosshair;
        public RecoilProfile GetRecoilProfile
        {
            get
            {
                if (equipmentList != null && equipmentList.Count > equipmentIndex && equipmentList[equipmentIndex] != null && equipmentList[equipmentIndex] is BaseWeapon w)
                {
                    return w.recoilProfile;
                }
                return null;
            } 
        }

        [SerializeField] internal float accumulatedSpread;

        [SerializeField] internal bool weaponBlocked;
        [SerializeField] internal LayerMask weaponBlockMask;
        [SerializeField] internal float aimAmount;
        [SerializeField] internal float aimSpeed;

        [SerializeField] internal Transform aimTarget;
        [SerializeField] internal Transform aimTransform;


        bool swappedAndFirePressed;

        [SerializeField] internal List<EquipmentImage> equipmentImages;
        [SerializeField] internal GameObject baseEquipmentImage;
        [SerializeField] internal Transform equipmentImageRoot;


        public CanvasGroup hitmarker;
        public Image hitmarkerImage;
        public Color regularHitColor = Color.white, headshotColour = Color.yellow;


        [SerializeField] internal Transform carryPoint;

        [SerializeField] internal LayerMask interactMask, carryMask;
        [SerializeField] internal NetworkVariable<bool> interacting = new(writePerm:NetworkVariableWritePermission.Server);
        [SerializeField] internal NetworkVariable<bool> carrying = new(writePerm:NetworkVariableWritePermission.Server);
        [SerializeField] internal NetworkVariable<bool> carryInput = new(writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] internal NetworkVariable<bool> interactInput = new(writePerm: NetworkVariableWritePermission.Owner);
        #region Network Callbacks

        [SerializeField] internal Quaternion recoilPointInitialRotation;

        [SerializeField] internal float carriablePositionTime = 0.1f, carriableRotationSpeed;
        [SerializeField] internal float carriableThrowForce = 25;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            
            equipment.OnListChanged += Equipment_OnListChanged;
            if (IsOwner)
            {
                animHelperNetRef.Value = animHelper;
                currentCarriableRef.OnValueChanged += CarriableChanged;
            }
        }
        void CarriableChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            current.TryGet(out currentCarriable);
        }

        private void Equipment_OnListChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
        {
            
            if(changeEvent.Type == NetworkListEvent<NetworkBehaviourReference>.EventType.Clear)
            {
                equipmentList.Clear();
                for (int i = equipmentImages.Count -1; i > -1; i--)
                {
                    Destroy(equipmentImages[i].gameObject);
                }
                equipmentImages.Clear();
            }
            if(changeEvent.Type == NetworkListEvent<NetworkBehaviourReference>.EventType.Add)
            {
                if (changeEvent.Value.TryGet(out BaseEquipment e))
                {
                    if (equipmentList.Contains(e))
                        equipmentList[equipmentList.FindIndex(x => x == e)] = e;
                    else
                        equipmentList.Add(e);
                }
                if (e != null)
                {

                    var im = Instantiate(baseEquipmentImage, equipmentImageRoot).GetComponent<EquipmentImage>();
                    im.equipment = e;
                    im.useTextDisplay = e.isWeapon || e.storedUses > 0;
                    equipmentImages.Add(im);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            equipment.OnListChanged -= Equipment_OnListChanged;
        }
        #endregion
        internal void TryWeaponBlock()
        {
            if(equipmentList.Count > equipmentIndex && equipmentList[equipmentIndex] != null)
            {
                BaseEquipment e = equipmentList[equipmentIndex];
                Debug.DrawRay(fireDirectionReference.position, fireDirectionReference.forward, Color.cyan);
                if (pc.moveState.Value == PlayerCharacter.MoveState.ladder || pc.moveState.Value == PlayerCharacter.MoveState.vaulting || pc.moveState.Value == PlayerCharacter.MoveState.mounted
                    || Physics.CheckBox(fireDirectionReference.TransformPoint(e.blockingCheckOffset), e.blockingCheckSize/2, fireDirectionReference.rotation, weaponBlockMask, QueryTriggerInteraction.Ignore))
                {
                    weaponBlocked = true;
                }
                else
                {
                    weaponBlocked = false;
                }
            }
        }
        #region Unity Messages
        private void Update()
        {
            if (!animHelper)
            {
                if(animHelperNetRef.Value.TryGet(out PlayerAnimationHelper e))
                {
                    animHelper = e;
                }
            }
            if (IsOwner)
            {
                carryInput.Value = PlayerManager.Instance.carryInput;
                interactInput.Value = PlayerManager.Instance.interactInput;


                for (int i = 0; i < equipmentList.Count; i++)
                {
                    BaseEquipment e = equipmentList[i];
                    if (i == equipmentIndex)
                    {
                        e.SetPrimaryInput(PrimaryInput && !swappedAndFirePressed && CanUseWeapon && reloadCoroutine == null);
                        e.SetSecondaryInput(SecondaryInput && CanUseWeapon && reloadCoroutine == null);

                        if (e is BaseWeapon b)
                        {
                            aimAmount = Mathf.Clamp01(aimAmount + (e.secondaryInput.Value ? Time.deltaTime : -Time.deltaTime) * aimSpeed);
                            PlayerManager.Instance.viewFOV = Mathf.Lerp(PlayerManager.Instance.baseViewFOV, b.aimedViewFOV, aimAmount);
                            PlayerManager.Instance.worldFOV = Mathf.Lerp(PlayerManager.Instance.baseWorldFOV, b.aimedWorldFOV, aimAmount);
                            aimTarget.localPosition = b.aimPosition;


                            if(PlayerManager.Instance.reloadInput && b.useAmmo && b.CurrentAmmo < b.maxAmmo)
                            {
                                StartReload(b);
                            }
                        }
                        else
                            aimAmount = 0;
                        aimTransform.position = Vector3.Lerp(aimTransform.parent.position, aimTarget.position, aimAmount);
                    }
                    else
                    {
                        e.SetPrimaryInput(false);
                        e.SetSecondaryInput(false);
                    }
                }
                if (!PrimaryInput)
                    swappedAndFirePressed = false;
            }

            UpdateRecoil_Frame();
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(baseCrosshairSize.x, maxCrosshairSize.x, accumulatedSpread));
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(baseCrosshairSize.y, maxCrosshairSize.y, accumulatedSpread));
        }
        public void CancelReload()
        {
            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
        }
        void StartReload(BaseWeapon b)
        {
            if (reloadCoroutine == null)
            {
                b.networkAnimator.SetTrigger("Reload");
                pc.netAnimator.SetTrigger("Reload");
                reloadCoroutine = StartCoroutine(ReloadTimer(b));
            }
        }
        Coroutine reloadCoroutine;
        IEnumerator ReloadTimer(BaseWeapon b)
        {
            yield return new WaitForSeconds(b.reloadTime);
            ReloadWeapon_ServerRPC(b);
            reloadCoroutine = null;
        }
        [ServerRpc]
        void ReloadWeapon_ServerRPC(NetworkBehaviourReference weapon)
        {
            if(weapon.TryGet(out BaseWeapon b))
            {
                b.currentAmmo.Value = b.maxAmmo;
            }
        }

        private void LateUpdate()
        {
            if(IsOwner && weaponPoint != null)
            {
                for (int i = 0; i < equipmentList.Count; i++)
                {
                    BaseEquipment e = equipmentList[i];
                    if (i == equipmentIndex)
                    {
                        e.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                    }
                    else
                    {
                        e.transform.SetPositionAndRotation(holsterPoint.position, holsterPoint.rotation);
                    }
                    e.currentGear.Value = equipmentIndex == i;
                }
            }
        }
        private void FixedUpdate()
        {
            UpdateRecoil_Fixed();
            TryWeaponBlock();
            TryInteract();
        }
        #endregion
        #region Interaction
        public float interactionRadius;
        public LayerMask interactLineOfSightMask;
        internal NetworkVariable<NetworkBehaviourReference> currentCarriableRef = new(writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] internal Carriable currentCarriable;
        internal NetworkVariable<NetworkBehaviourReference> currentInteractableRef = new(writePerm: NetworkVariableWritePermission.Server);
        internal 
        bool carryPressed;
        bool interactPressed;
        void TryInteract()
        {
            if (carryInput.Value)
            {
                if (!carrying.Value && !carryPressed)
                {
                    //If we're not carrying something,we want to find something to pick up
                    Collider[] cols = new Collider[5];
                    Carriable targeted = null;
                    if (Physics.OverlapSphereNonAlloc(fireDirectionReference.position, interactionRadius, cols, carryMask, QueryTriggerInteraction.Ignore) > 0)
                    {
                        for (int i = 0; i < cols.Length; i++)
                        {
                            if (cols[i] == null)
                                continue;
                            Collider col = cols[i];
                            if (Physics.Linecast(fireDirectionReference.position, col.transform.position, out RaycastHit hit, interactLineOfSightMask, QueryTriggerInteraction.Ignore) && hit.collider == col)
                            {
                                targeted = hit.collider.GetComponentInParent<Carriable>();
                            }
                        }
                    }
                    if (targeted)
                    {
                        currentCarriableRef.Value = targeted;
                        currentCarriable = targeted;
                        carrying.Value = true;
                        targeted.OnPickup(this);
                        carryPressed = true;

                    }
                }
            }
            else
            {
                carryPressed = false;
            }

            if (currentCarriable != null && ((!carryPressed && carryInput.Value) || PrimaryInput))
            {
                currentCarriable.OnThrow(PrimaryInput);
                currentCarriable = null;
                currentCarriableRef.Value = null;
                carrying.Value = false;
                swappedAndFirePressed = true;
                carryPressed = true;
            }

            if (interactInput.Value && !carrying.Value)
            {

            }
            interactPressed = interactInput.Value;
        }
        #endregion
        [ServerRpc()]
        internal void Respawn_ServerRPC(ServerRpcParams param = default)
        {
            if(equipment.Count > 0)
            {
                foreach (var item in equipment)
                {
                    if(item.TryGet(out NetworkBehaviour b))
                    {
                        b.NetworkObject.Despawn();
                    }
                }
            }
            equipment.Clear();
            List<NetworkBehaviourReference> e = new List<NetworkBehaviourReference>();
            for (int i = 0; i < equipmentPrefabs.Length; i++)
            {
                var n = NetworkManager.SpawnManager.InstantiateAndSpawn(equipmentPrefabs[i], param.Receive.SenderClientId, position: transform.position, rotation: transform.rotation);
                var b = n.GetComponent<BaseEquipment>();
                if (b is BaseWeapon weapon)
                {
                    weapon.ownerWeaponManager.Value = this;
                }
                e.Add(b);
            }
            foreach (var item in e)
            {
                equipment.Add(item);
            }
            Player_UpdateAnimations_RPC();
        }
        internal int nextEquipmentIndex;
        internal int lastEqupimentIndex;
        internal void SwitchWeaponDirectly(int switchWeapon)
        {
            //If we are ACTUALLY switching weapons
            if (switchWeapon != nextEquipmentIndex)
            {
                lastEqupimentIndex = equipmentIndex;
                nextEquipmentIndex = switchWeapon;
                if (animHelper)
                {
                    if (nextEquipmentIndex == equipmentIndex)
                        animHelper.networkAnimator.SetTrigger("CancelSwitch");
                    else
                        animHelper.networkAnimator.SetTrigger("Switch");
                }
                equipmentIndex = nextEquipmentIndex;
                FinishWeaponSwitch();
                swappedAndFirePressed = PrimaryInput;
            }
        }
        [Rpc(SendTo.Owner)]
        void Player_UpdateAnimations_RPC()
        {
            FinishWeaponSwitch();
        }
        internal void FinishWeaponSwitch()
        {
            if (animHelper)
            {
                animHelper.UpdateAnimationsFromEquipment_RPC(equipmentList[nextEquipmentIndex]);
            }
            equipmentIndex = nextEquipmentIndex;
            if (equipmentList[equipmentIndex] is BaseWeapon weapon)
            {
                aimTarget.localPosition = weapon.aimPosition;
            }
        }
        internal void ScrollWeapon(bool up)
        {
            if (up)
            {
                equipmentIndex++;
                equipmentIndex %= equipmentList.Count;
            }
            else
            {
                equipmentIndex--;
                equipmentIndex %= equipmentList.Count;
            }
        }
        void UpdateRecoil_Frame()
        {

        }
        void UpdateRecoil_Fixed()
        {
            if(GetRecoilProfile != null)
            {
                accumulatedSpread = Mathf.Clamp01(accumulatedSpread - (GetRecoilProfile.spreadDecay * Time.fixedDeltaTime));
            }
        }
        internal void ReceiveRecoil()
        {
            accumulatedSpread += GetRecoilProfile.spreadAdditivePerShot;
            pc.ReceiveRecoil();
        }

        #region Hit Feedback
        Coroutine hitfeedbackCoroutine;
        [SerializeField] float hitmarkerDisplayTime;
        [Rpc(SendTo.Owner)]
        public void HitFeedback_RPC(bool headshot)
        {
            if (hitfeedbackCoroutine != null)
                StopCoroutine(hitfeedbackCoroutine);
            hitfeedbackCoroutine = StartCoroutine(HitmarkerDisplay());
            hitmarkerImage.color = headshot ? headshotColour : regularHitColor;
        }
        IEnumerator HitmarkerDisplay()
        {
            float t = 0;
            while (t < hitmarkerDisplayTime)
            {
                t += Time.deltaTime;
                hitmarker.alpha = Mathf.InverseLerp(hitmarkerDisplayTime, 0, t);
                yield return new WaitForEndOfFrame();
            }
            hitmarker.alpha = 0;

        }
        #endregion
    }
}