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
                            if(!rw.reloading || (accumulatedReloadTime >= rw.reloadCancelTime))
                            {
                                cancellingReload = true;
                            }
                        }
                        e.fireInput = pm.fireInput;
                        e.secondaryInput = pm.secondaryInput;
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
            weapon.RefillAmmo();
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
