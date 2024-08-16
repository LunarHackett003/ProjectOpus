using System.Collections.Generic;
using opus.Gameplay;
using opus.utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

        public bool PrimaryInput => PlayerManager.Instance.fireInput && !PlayerManager.Instance.pc.Dead.Value && !fireBlocked && !weaponBlocked;
        public bool SecondaryInput => PlayerManager.Instance.aimInput && !PlayerManager.Instance.pc.Dead.Value && !fireBlocked && !weaponBlocked;
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




        [SerializeField] internal List<EquipmentImage> equipmentImages;
        [SerializeField] internal GameObject baseEquipmentImage;
        [SerializeField] internal Transform equipmentImageRoot;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            
            equipment.OnListChanged += Equipment_OnListChanged;
            if (IsOwner)
            {
                animHelperNetRef.Value = animHelper;
            }
        }

        private void Equipment_OnListChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
        {
            equipmentList.Clear();
            for (int i = equipmentImages.Count -1; i > -1; i--)
            {
                Destroy(equipmentImages[i].gameObject);
            }
            equipmentImages.Clear();
            foreach (var item in equipment)
            {
                if (item.TryGet(out BaseEquipment e))
                {
                    if (equipmentList.Contains(e))
                        equipmentList[equipmentList.FindIndex(x => x == e)] = e;
                    else
                        equipmentList.Add(e);
                }
                var im = Instantiate(baseEquipmentImage, equipmentImageRoot).GetComponent<EquipmentImage>();
                im.equipment = e;
                im.useTextDisplay = e.isWeapon || e.storedUses > 0;
                equipmentImages.Add(im);
                
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            equipment.OnListChanged -= Equipment_OnListChanged;
        }

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
                for (int i = 0; i < equipmentList.Count; i++)
                {
                    BaseEquipment e = equipmentList[i];
                    if (i == equipmentIndex)
                    {
                        e.SetPrimaryInput(PrimaryInput);
                        e.SetSecondaryInput(SecondaryInput);

                        if (e is BaseWeapon b)
                        {
                            aimAmount = Mathf.Clamp01(aimAmount + (e.secondaryInput.Value ? Time.deltaTime : -Time.deltaTime) * aimSpeed);
                            PlayerManager.Instance.viewFOV = Mathf.Lerp(PlayerManager.Instance.baseViewFOV, b.aimedViewFOV, aimAmount);
                            PlayerManager.Instance.worldFOV = Mathf.Lerp(PlayerManager.Instance.baseWorldFOV, b.aimedWorldFOV, aimAmount);
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
            }

            UpdateRecoil_Frame();
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(baseCrosshairSize.x, maxCrosshairSize.x, accumulatedSpread));
            crosshair.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(baseCrosshairSize.y, maxCrosshairSize.y, accumulatedSpread));
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
        }
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
                aimTarget.localPosition = weapon.aimTransform.localPosition;
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
    }
}