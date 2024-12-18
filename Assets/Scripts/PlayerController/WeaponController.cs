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

        public AnimatorCustomParamProxy acpp;

        public Vector3 linearMoveBob, angularMoveBob;
        float moveBobTime, dampedMove, vdampedmove, swaytime;

        bool reloading;

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
            be.myController = this;
            be.cr.InitialiseViewable(pc);
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
                SetUpWeaponSlot(Slot.primary, weapon);
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
        public BaseEquipment GetCurrentEquipment()
        {
            return currentSlot switch
            {
                Slot.primary => weapon,
                Slot.gadget1 => gadget1,
                Slot.gadget2 => gadget2,
                Slot.gadget3 => gadget3,
                Slot.special => special,
                _ => null,
            };
        }
        public void TryReload()
        {
            if (GetCurrentEquipment() is RangedWeapon w)
            {
                if (w.CurrentAmmo == w.maxAmmo)
                    return;
                if(w.CurrentAmmo > 0)
                {
                    networkAnimator.SetTrigger("TacReload");
                    w.netAnimator.SetTrigger("TacReload");
                }
                else
                {
                    networkAnimator.SetTrigger("EmptyReload");
                    w.netAnimator.SetTrigger("EmptyReload");
                }
            }
        }
        void SwitchWeapon(int target)
        {
            currentSlot = (Slot)target;
            BaseEquipment w = GetCurrentEquipment();
            switch (w)
            {
                case RangedWeapon:
                    networkAnimator.Animator.SetLayerWeight(0,1);
                    networkAnimator.Animator.SetLayerWeight(1, 0);
                    networkAnimator.Animator.SetLayerWeight(2, 0);
                    break;
                case MeleeWeapon:
                    networkAnimator.Animator.SetLayerWeight(0, 0);
                    networkAnimator.Animator.SetLayerWeight(1, 1);
                    networkAnimator.Animator.SetLayerWeight(2, 0);
                    break;
                default:
                    break;
            }
        }

        public void WeaponUpdated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
            {
                if(IsServer && old.TryGet(out BaseEquipment w))
                {
                    w.NetworkObject.Despawn(true);
                }
                SetUpWeaponSlot(Slot.primary, e);
            }
        }
        public void Gadget1Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
            {
                if (IsServer && old.TryGet(out BaseEquipment w))
                {
                    w.NetworkObject.Despawn(true);
                }
                SetUpWeaponSlot(Slot.gadget1, e);
            }
        }
        public void Gadget2Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
            {
                if (IsServer && old.TryGet(out BaseEquipment w))
                {
                    w.NetworkObject.Despawn(true);
                }
                SetUpWeaponSlot(Slot.gadget2, e);
            }
        }
        public void Gadget3Updated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
            {
                if (IsServer && old.TryGet(out BaseEquipment w))
                {
                    w.NetworkObject.Despawn(true);
                }
                SetUpWeaponSlot(Slot.gadget3, e);
            }
        }
        public void SpecialUpdated(NetworkBehaviourReference old, NetworkBehaviourReference next)
        {
            if (next.TryGet(out BaseEquipment e))
            {
                if (IsServer && old.TryGet(out BaseEquipment w))
                {
                    w.NetworkObject.Despawn(true);
                }
                SetUpWeaponSlot(Slot.special, e);
            }
        }

        private void FixedUpdate()
        {
            if(weapon != null)
            {
                UpdateActiveEquipment(weapon, Slot.primary);
            }
            if(gadget1 != null)
            {
                UpdateActiveEquipment(gadget1, Slot.gadget1);
            }
            if(gadget2 != null)
            {
                UpdateActiveEquipment(gadget2, Slot.gadget2);
            }
            if(gadget3 != null)
            {
                UpdateActiveEquipment(gadget3, Slot.gadget3);
            }
            if(special != null)
            {
                UpdateActiveEquipment(special, Slot.special);
            }
        }
        private void LateUpdate()
        {
            if (weapon != null)
            {
                UpdateEquipmentPosition(weapon, Slot.primary);
            }
            if (gadget1 != null)
            {
                UpdateEquipmentPosition(gadget1, Slot.gadget1);
            }
            if (gadget2 != null)
            {
                UpdateEquipmentPosition(gadget2, Slot.gadget2);
            }
            if (gadget3 != null)
            {
                UpdateEquipmentPosition(gadget3, Slot.gadget3);
            }
            if (special != null)
            {
                UpdateEquipmentPosition(special, Slot.special);
            }
        }
        void UpdateEquipmentPosition(BaseEquipment be, Slot slot)
        {
            if (slot == currentSlot)
            {
                if (be.transform.localScale == Vector3.zero)
                {
                    be.transform.localScale = Vector3.one;
                }
            }
            else
            {
                if (be.transform.localScale == Vector3.one)
                {
                    be.transform.localScale = Vector3.zero;
                }
            }
            be.transform.SetPositionAndRotation(pc.weaponOffset.position, pc.weaponOffset.rotation);

        }
        void UpdateActiveEquipment(BaseEquipment be, Slot slot)
        {
            if (be != null)
            {
                be.fireInput = pc.fireInput && currentSlot == slot && !be.acpp.customParams[0].boolValue;
                be.secondaryInput = pc.secondaryInput && currentSlot == slot && !be.acpp.customParams[0].boolValue;
                if (slot == currentSlot)
                {
                    dampedMove = Mathf.SmoothDamp(dampedMove, pc.moveInput.sqrMagnitude * (pc.isGrounded ? 1 : 0)
                        * (pc.sprintInput ? be.swayContainer.sprintMultiplier : 1)
                        * (pc.crouchInput ? .5f : 1), ref vdampedmove, pc.moveSwayPosDampTime);
                    swaytime += (dampedMove * Time.fixedDeltaTime * be.swayContainer.speed);
                    moveBobTime = (swaytime % 1f) ;
                    linearMoveBob = be.swayContainer.linearMoveBob.Evaluate(moveBobTime).ScaleReturn(be.swayContainer.linearMoveScale) * dampedMove;
                    angularMoveBob = be.swayContainer.angularMoveBob.Evaluate(moveBobTime).ScaleReturn(be.swayContainer.angularMoveScale) * dampedMove;
                }
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
