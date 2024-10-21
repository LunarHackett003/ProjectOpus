using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{

    public enum EquipmentSlot
    {
        none = 0,
        primary = 1,
        secondary = 2,
        gadget1 = 3,
        gadget2 = 4,
        special = 5
    }

    public class PlayerWeaponManager : WeaponManager
    {
        PlayerManager pm;


        [SerializeField] protected bool cannotFire;
        public BaseWeapon primaryWeapon, secondaryWeapon;
        public NetworkVariable<EquipmentSlot> currentSlot = new(writePerm: NetworkVariableWritePermission.Owner);
        public InputCollector IC { get; private set; }
        public OpusNetworkAnimator Animator { get; private set; }
        public PlayerAnimator PlayerAnimator { get; private set; }

        public NetworkVariable<NetworkBehaviourReference> primaryWeaponRef = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> secondaryWeaponRef = new(writePerm: NetworkVariableWritePermission.Server);

        public Dictionary<EquipmentSlot, BaseWeapon> equipmentDict = new();

        public Transform weaponPoint, primaryHolster, secondaryHolster, gadget1Holster, gadget2Holster, specialHolster;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            pm = PlayerManager.playerManagers.First(x => x.OwnerClientId == OwnerClientId);

            IC = pm.InputCollector;
            
            pm.weaponManager = this;
            
            Animator = GetComponentInChildren<OpusNetworkAnimator>();
            PlayerAnimator = GetComponentInChildren<PlayerAnimator>();

            primaryWeaponRef.OnValueChanged += PrimaryWeaponChanged;
            secondaryWeaponRef.OnValueChanged += SecondaryWeaponChanged;

            if (IsServer)
            {
                //Callback to the server to give this player weapons
                print("give the player weapons");
                pm.BestowWeapons();
            }
        }
        [Rpc(SendTo.Everyone)]
        public void UpdateWeapons_RPC()
        {
            print("updating weapons");
            if (primaryWeapon != null)
            {
                UpdateEquipmentDictionary(primaryWeapon, EquipmentSlot.primary);
            }
            if(secondaryWeapon != null)
            {
                UpdateEquipmentDictionary(secondaryWeapon, EquipmentSlot.secondary);
            }

        }
        void PrimaryWeaponChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            if(current.TryGet(out BaseWeapon weapon))
            {
                primaryWeapon = weapon;
                primaryWeapon.manager = this;
                if(currentSlot.Value == 0)
                {
                    currentSlot.Value = EquipmentSlot.primary;
                }
                if (currentSlot.Value == EquipmentSlot.primary)
                {
                    PlayerAnimator.UpdateAnimations(current);
                }
                pendingSlot = currentSlot.Value;
            }
            UpdateEquipmentDictionary(weapon, EquipmentSlot.primary);
        }
        void SecondaryWeaponChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            if(current.TryGet(out BaseWeapon weapon))
            {
                secondaryWeapon = weapon;
                secondaryWeapon.manager = this;
            }
            UpdateEquipmentDictionary(weapon, EquipmentSlot.secondary);
        }
        void UpdateEquipmentDictionary(BaseWeapon weapon, EquipmentSlot slot)
        {
            print("Updating equipment dictionary");
            if (weapon != null)
            {
                if (equipmentDict.TryGetValue(slot, out BaseWeapon b))
                {
                    if (IsServer && weapon != b)
                    {
                        //we have either updated the weapon via a testing loadout menu, or we've respawned and haven't correctly disposed of the previous weapon.
                        b.NetworkObject.Despawn();
                    }
                    equipmentDict[slot] = weapon;
                }
                else
                {
                    Debug.Log("Added weapon to equipment dictionary", weapon);
                    equipmentDict.Add(slot, weapon);
                }
            }
            else
            {
                if (equipmentDict.ContainsKey(slot))
                {
                    equipmentDict.Remove(slot);
                }
            }
        }

        private void LateUpdate()
        {
            /*This approach removes the need for a network transform on the weapons, cutting down on the bandwidth the game requires at the cost of each client having to do a little more work.
            *This will look better in poor network conditions, however.
            *switch (currentSlot)
            *{
            *    case EquipmentSlot.none:
            *        if(primaryWeapon != null)
            *        primaryWeapon.transform.SetPositionAndRotation(primaryHolster.position, primaryHolster.rotation);
            *        if (secondaryWeapon != null)
            *        secondaryWeapon.transform.SetPositionAndRotation(secondaryHolster.position, secondaryHolster.rotation);
            *        break;
            *    case EquipmentSlot.primary:
            *        if (primaryWeapon != null)
            *        primaryWeapon.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
            *        if (secondaryWeapon != null)
            *            secondaryWeapon.transform.SetPositionAndRotation(secondaryHolster.position, secondaryHolster.rotation);
            *        break;
            *    case EquipmentSlot.secondary:
            *        if (primaryWeapon != null)
            *        primaryWeapon.transform.SetPositionAndRotation(primaryHolster.position, primaryHolster.rotation);
            *        if (secondaryWeapon != null)
            *        secondaryWeapon.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
            *        break;
            *    case EquipmentSlot.gadget1:
            *        break;
            *    case EquipmentSlot.gadget2:
            *        break;
            *    case EquipmentSlot.special:
            *        break;
            *    default:
            *        break;
            }
            */
            foreach (var item in equipmentDict)
            {
                if (item.Value == null)
                    continue;
                item.Value.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                if(item.Key == currentSlot.Value)
                {
                    item.Value.transform.localScale = Vector3.one;

                }
                else
                {
                    item.Value.transform.localScale = Vector3.zero;
                }
            }
            if (IC.reloadInput && !playingReloadAnimation)
            {
                TryReloadWeapon();
            }
        }
        [SerializeField] bool playingReloadAnimation;
        public bool PlayingReloadAnimation => playingReloadAnimation;
        public void ClearReloadFlag()
        {
            playingReloadAnimation = false;
        }
        void TryReloadWeapon()
        {
            switch (equipmentDict[currentSlot.Value])
            {
                case DualWieldWeapon d:
                    if (d.weaponOne.UseAmmo && d.weaponOne.currentAmmunition.Value < d.weaponOne.MaxAmmo)
                    {
                        Animator.SetTrigger(d.weaponOne.currentAmmunition.Value <= 0 ? "ReloadRightEmpty" : "ReloadRight");
                        d.animator.ResetTrigger("RecockRight");
                        playingReloadAnimation = true;
                    }
                    if(d.weaponTwo.UseAmmo && d.weaponTwo.currentAmmunition.Value < d.weaponTwo.MaxAmmo)
                    {
                        Animator.SetTrigger(d.weaponTwo.currentAmmunition.Value <= 0 ? "ReloadLeftEmpty" : "ReloadLeft");
                        d.animator.ResetTrigger("RecockLeft");
                        playingReloadAnimation = true;
                    }
                    break;
                case RangedWeapon r:
                    if (r.UseAmmo && r.currentAmmunition.Value < r.MaxAmmo)
                    {
                        playingReloadAnimation = true;
                        if (r.useCountedReload)
                        {
                            string trigger = r.currentAmmunition.Value == 0 ? "CountedReload" : "CountedReload";
                            r.animator.SetTrigger(trigger);
                            Animator.SetTrigger(trigger);
                        }
                        else
                        {
                            string trigger = r.currentAmmunition.Value == 0 ? "ReloadEmpty" : "ReloadQuick";
                            r.animator.SetTrigger(trigger);
                            Animator.SetTrigger(trigger);
                        }
                        r.animator.ResetTrigger("Recock");
                        Animator.ResetTrigger("Recock");

                    }
                    break;
                default:
                    break;
            }
        }



        [SerializeField] EquipmentSlot pendingSlot;
        [SerializeField] bool switchingWeapons;
        public void SwitchWeapon(EquipmentSlot targetSlot)
        {
            pendingSlot = targetSlot;
            if (IsOwner && equipmentDict.ContainsKey(targetSlot) && equipmentDict[targetSlot] != null && 
                !switchingWeapons && currentSlot.Value != targetSlot)
            {
                switchingWeapons = true;

                Animator.SetTrigger("SwitchWeapon");
                if (equipmentDict[currentSlot.Value].animator != null)
                {
                    equipmentDict[currentSlot.Value].animator.SetTrigger("SwitchWeapon");
                }
            }
            else
            {
                print($"{IsOwner}, {equipmentDict.ContainsKey(targetSlot)}, {equipmentDict[targetSlot] != null}");
            }
        }
        public void ConfirmWeaponSwitch()
        {
            if (IsOwner)
            {
                currentSlot.Value = pendingSlot;
                switchingWeapons = false;
            }
            PlayerAnimator.UpdateAnimations(equipmentDict[currentSlot.Value]);
        }
        private void FixedUpdate()
        {
            if (!IsOwner || IC == null)
                return;
            primaryInput = !PauseMenu.Instance.GamePaused && IC.primaryInput && !cannotFire && !playingReloadAnimation && !switchingWeapons;
            secondaryInput = !PauseMenu.Instance.GamePaused && IC.secondaryInput && !cannotFire && !playingReloadAnimation && !switchingWeapons;
            foreach (var item in equipmentDict)
            {
                if (item.Value == null)
                    continue;
                if (item.Key == currentSlot.Value)
                {
                    item.Value.transform.localScale = Vector3.one;
                    if (item.Value.PrimaryInput != primaryInput)
                        item.Value.SetPrimaryInput_RPC(primaryInput);
                    if(item.Value.SecondaryInput != secondaryInput)
                        item.Value.SetSecondaryInput_RPC(secondaryInput);
                    item.Value.isCurrentWeapon = true;
                }
                else
                {
                    item.Value.transform.localScale = Vector3.zero;
                    if (item.Value.PrimaryInput != false)
                        item.Value.SetPrimaryInput_RPC(false);
                    if (item.Value.SecondaryInput != false)
                        item.Value.SetSecondaryInput_RPC(false);
                    item.Value.isCurrentWeapon = false;
                }
            }
        }
    }
}
