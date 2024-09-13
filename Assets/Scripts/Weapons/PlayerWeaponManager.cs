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


        protected bool cannotFire;
        public BaseWeapon primaryWeapon, secondaryWeapon;
        [SerializeField] EquipmentSlot currentSlot;
        public InputCollector IC { get; private set; }
        public OpusNetworkAnimator Animator { get; private set; }
        public PlayerAnimator PlayerAnimator { get; private set; }

        public NetworkVariable<NetworkBehaviourReference> primaryWeaponRef = new(writePerm: NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> secondaryWeaponRef = new(writePerm: NetworkVariableWritePermission.Server);


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
                pm.BestowWeapons();
            }
        }
        void PrimaryWeaponChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            if(current.TryGet(out BaseWeapon weapon))
            {
                primaryWeapon = weapon;
                primaryWeapon.manager = this;
                if(currentSlot == 0)
                {
                    currentSlot = EquipmentSlot.primary;
                }
                if (currentSlot == EquipmentSlot.primary)
                    PlayerAnimator.UpdateAnimations(current);
            }
        }
        void SecondaryWeaponChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            if(current.TryGet(out BaseWeapon weapon))
            {
                secondaryWeapon = weapon;
                secondaryWeapon.manager = this;
            }
        }

        private void LateUpdate()
        {
            //This approach removes the need for a network transform on the weapons, cutting down on the bandwidth the game requires at the cost of each client having to do a little more work.
            //This will look better in poor network conditions, however.
            switch (currentSlot)
            {
                case EquipmentSlot.none:
                    if(primaryWeapon != null)
                    primaryWeapon.transform.SetPositionAndRotation(primaryHolster.position, primaryHolster.rotation);
                    if (secondaryWeapon != null)
                    secondaryWeapon.transform.SetPositionAndRotation(secondaryHolster.position, secondaryHolster.rotation);
                    break;
                case EquipmentSlot.primary:
                    if (primaryWeapon != null)
                    primaryWeapon.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                    if (secondaryWeapon != null)
                        secondaryWeapon.transform.SetPositionAndRotation(secondaryHolster.position, secondaryHolster.rotation);
                    break;
                case EquipmentSlot.secondary:
                    if (primaryWeapon != null)
                    primaryWeapon.transform.SetPositionAndRotation(primaryHolster.position, primaryHolster.rotation);
                    if (secondaryWeapon != null)
                    secondaryWeapon.transform.SetPositionAndRotation(weaponPoint.position, weaponPoint.rotation);
                    break;
                case EquipmentSlot.gadget1:
                    break;
                case EquipmentSlot.gadget2:
                    break;
                case EquipmentSlot.special:
                    break;
                default:
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner || IC == null)
                return;
            primaryInput = IC.primaryInput && !cannotFire;
            secondaryInput = IC.secondaryInput && !cannotFire;

            switch (currentSlot)
            {
                case EquipmentSlot.primary:
                    if(primaryWeapon != null)
                    {
                        if(primaryWeapon.PrimaryInput != primaryInput)
                            primaryWeapon.SetPrimaryInput_RPC(primaryInput);
                        if(primaryWeapon.SecondaryInput != secondaryInput)
                            primaryWeapon.SetSecondaryInput_RPC(secondaryInput);
                    }
                    break;
                case EquipmentSlot.secondary:
                    if(secondaryWeapon != null)
                    {
                        if (secondaryWeapon.PrimaryInput != primaryInput)
                            secondaryWeapon.SetPrimaryInput_RPC(primaryInput);
                        if (secondaryWeapon.SecondaryInput != secondaryInput)
                            secondaryWeapon.SetSecondaryInput_RPC(secondaryInput);
                    }
                    break;
                case EquipmentSlot.gadget1:
                    break;
                case EquipmentSlot.gadget2:
                    break;
                case EquipmentSlot.special:
                    break;
                default:
                    break;
            }
        }
    }
}
