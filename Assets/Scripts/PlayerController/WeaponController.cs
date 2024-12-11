using Netcode.Extensions;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public enum Slot
    {
        primary = 0,
        gadget1 = 1,
        gadget2 = 2,
        gadget3 = 3,
        special = 4
    }

    public class WeaponController : NetworkBehaviour
    {
        PlayerController pc;
        public Slot currentSlot;
        public BaseEquipment weapon;
        public BaseEquipment gadget1, gadget2, gadget3, special;
        public NetworkVariable<NetworkBehaviourReference> weaponRef = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> gadget1Ref = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> gadget2Ref = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> gadget3Ref = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<NetworkBehaviourReference> specialRef = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        public ClientNetworkAnimator networkAnimator;
        void SetUpWeaponSlot(Slot slot, BaseEquipment be)
        {
            switch (slot)
            {
                case Slot.primary:
                    weapon = be;
                    break;
                case Slot.gadget1:
                    gadget1 = be;
                    break;
                case Slot.gadget2:
                    gadget2 = be;
                    break;
                case Slot.gadget3:
                    gadget3 = be;
                    break;
                case Slot.special:
                    special = be;
                    break;
                default:
                    break;
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(pc == null)
            {
                pc = GetComponent<PlayerController>();
            }
            weaponRef.OnValueChanged += WeaponUpdated;
            gadget1Ref.OnValueChanged += Gadget1Updated;
            gadget2Ref.OnValueChanged += Gadget2Updated;
            gadget3Ref.OnValueChanged += Gadget3Updated;
            specialRef.OnValueChanged += SpecialUpdated;

            if (weapon)
            {
                weapon.myController = this;
            }

            if(weaponRef.Value.TryGet(out BaseEquipment e))
            {
                SetUpWeaponSlot(Slot.primary, e);
            }
            if(gadget1Ref.Value.TryGet(out e))
            {
                SetUpWeaponSlot(Slot.gadget1, e);
            }
            if (gadget2Ref.Value.TryGet(out e))
            {
                SetUpWeaponSlot(Slot.gadget2, e);
            }
            if (gadget3Ref.Value.TryGet(out e))
            {
                SetUpWeaponSlot(Slot.gadget3, e);
            }
            if (specialRef.Value.TryGet(out e))
            {
                SetUpWeaponSlot(Slot.special, e);
            }
        }

        public void WeaponUpdated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
                SetUpWeaponSlot(Slot.primary, e);
        }
        public void Gadget1Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
                SetUpWeaponSlot(Slot.gadget1, e);
        }
        public void Gadget2Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
                SetUpWeaponSlot(Slot.gadget2, e);
        }
        public void Gadget3Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
                SetUpWeaponSlot(Slot.gadget3, e);
        }
        public void SpecialUpdated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
                SetUpWeaponSlot(Slot.special, e);
        }

        private void FixedUpdate()
        {
            if(weapon != null)
            {
                SetInputs(weapon, Slot.primary);
            }
            if(gadget1 != null)
            {
                SetInputs(gadget1, Slot.gadget1);
            }
            if(gadget2 != null)
            {
                SetInputs(gadget2, Slot.gadget2);
            }
            if(gadget3 != null)
            {
                SetInputs(gadget3, Slot.gadget3);
            }
            if(special != null)
            {
                SetInputs(special, Slot.special);
            }
        }
        void SetInputs(BaseEquipment be, Slot slot)
        {
            if(be != null)
            {
                be.fireInput = pc.fireInput && currentSlot == slot;
                be.secondaryInput = pc.secondaryInput && currentSlot == slot;
            }
        }
        public void ReceiveShot()
        {
            if(networkAnimator != null)
            {
                networkAnimator.SetTrigger("Fire");
            }
        }
    }
}
