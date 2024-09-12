using System.Linq;
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
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            pm = PlayerManager.playerManagers.First(x => x.OwnerClientId == OwnerClientId);

            IC = pm.InputCollector;
            Animator = GetComponentInChildren<OpusNetworkAnimator>();
            PlayerAnimator = GetComponentInChildren<PlayerAnimator>();
            primaryWeapon.manager = this;
        }
        private void FixedUpdate()
        {

            if (!IsOwner)
                return;
            primaryInput = IC.primaryInput && !cannotFire;
            secondaryInput = IC.secondaryInput && !cannotFire;

            switch (currentSlot)
            {
                case EquipmentSlot.primary:
                    if(primaryWeapon != null)
                    {
                        primaryWeapon.primaryInput.Value = primaryInput;
                        primaryWeapon.secondaryInput.Value = secondaryInput;
                    }
                    break;
                case EquipmentSlot.secondary:
                    if(secondaryWeapon != null)
                    {
                        secondaryWeapon.primaryInput.Value = primaryInput;
                        secondaryWeapon.secondaryInput.Value = secondaryInput;
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
