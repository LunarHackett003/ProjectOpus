using Netcode.Extensions;
using System.Collections;
using Unity.Collections;
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
        public PlayerController Controller { get; private set; }
        public Slot currentSlot;
        public NetworkVariable<Slot> syncedSlot = new(Slot.primary, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
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

        protected AnimationClipOverrides clipOverrides;
        protected AnimatorOverrideController aoc;

        bool playingGesture;
        public float gestureLerpTime;
        float gestureWeight;

        public void SetPlayingGesture(bool value)
        {
            playingGesture = value;
            if (IsOwner)
            {
                LerpGestureWeight_RPC(value);
            }
        }

        public bool QuerySlot(Slot slot)
        {
            return slot switch
            {
                Slot.primary => weapon,
                Slot.gadget1 => gadget1,
                Slot.gadget2 => gadget2,
                Slot.gadget3 => gadget3,
                Slot.special => special,
                _ => null,
            } != null;
        }


        public ClientNetworkAnimator networkAnimator;
        void SetUpWeaponSlot(Slot slot, BaseEquipment be)
        {
            switch (slot)
            {
                case Slot.primary:
                    weapon = be;
                    if (IsOwner)
                    {
                        SwitchWeapon_RPC(0);
                    }
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
            //be.cr.InitialiseViewable(Controller);
        }

        public void TrySwitchWeapon(int input)
        {
            if (IsOwner)
            {
                int target = input;
                BaseEquipment be = GetEquipment((Slot)target);
                if(be != null)
                {
                    if (MatchManager.Instance != null && !MatchManager.Instance.lockedSlots[target])
                    {
                        if (be.hasAnimations)
                        {
                            SwitchWeapon_RPC(target);
                        }
                        else
                        {
                            print($"Slot {target} has no animations, triggering this equipment's effect...");
                            be.TrySelect();
                        }
                    }
                    else
                    {
                        print($"Slot {target} is locked!");
                    }
                }
                else
                {
                    print($"Slot {target} is not filled! This could be a result of it being locked. In either case, it should not be equippable.");
                }


            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(Controller == null)
            {
                Controller = GetComponent<PlayerController>();
            }
            weaponRef.OnValueChanged += WeaponUpdated;
            WeaponUpdated(null, weaponRef.Value);
            gadget1Ref.OnValueChanged += Gadget1Updated;
            Gadget1Updated(null, gadget1Ref.Value);
            gadget2Ref.OnValueChanged += Gadget2Updated;
            Gadget2Updated(null, gadget2Ref.Value);
            gadget3Ref.OnValueChanged += Gadget3Updated;
            Gadget3Updated(null, gadget3Ref.Value);
            specialRef.OnValueChanged += SpecialUpdated;
            SpecialUpdated(null, specialRef.Value);

            currentSlot = syncedSlot.Value;
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

        public BaseEquipment GetCurrentEquipmentByReference()
        {
            NetworkBehaviourReference nbr = currentSlot switch
            {
                Slot.primary => weaponRef.Value,
                Slot.gadget1 => gadget1Ref.Value,
                Slot.gadget2 => gadget2Ref.Value,
                Slot.gadget3 => gadget3Ref.Value,
                Slot.special => specialRef.Value,
                _ => null,
            };
            if(nbr.TryGet(out BaseEquipment be))
            {
                return be;
            }
            else
            {
                return null;
            }
        }

        public BaseEquipment GetEquipment(Slot slot)
        {
            return slot switch
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
            if (Controller.Alive && GetCurrentEquipment() is RangedWeapon w)
            {
                if (w.CurrentAmmo == w.maxAmmo)
                    return;
                if(w.CurrentAmmo > 0)
                {
                    networkAnimator.SetTrigger("TacReload");
                    if(w.netAnimator != null)
                        w.netAnimator.SetTrigger("TacReload");
                }
                else
                {
                    networkAnimator.SetTrigger("EmptyReload");
                    if (w.netAnimator != null)
                        w.netAnimator.SetTrigger("EmptyReload");
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        void SwitchWeapon_RPC(int targetSlot)
        {
            currentSlot = (Slot)targetSlot;
            if (IsOwner)
            {
                syncedSlot.Value = currentSlot;
            }
            BaseEquipment w = GetCurrentEquipment();
            switch (w)
            {
                case RangedWeapon:
                    networkAnimator.Animator.SetInteger("Type",0);
                    break;
                case MeleeWeapon:
                    networkAnimator.Animator.SetInteger("Type", 1);
                    break;
                default:
                    networkAnimator.Animator.SetInteger("Type", 2);
                    break;
            }
            if (IsOwner)
            {
                SetUpAnimations_RPC(w);
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SetUpAnimations_RPC(NetworkBehaviourReference nbr)
        {
            print($"overriding animations for {OwnerClientId}'s {currentSlot} slot.");
            if(aoc == null)
            {
                aoc = new(networkAnimator.Animator.runtimeAnimatorController);
                networkAnimator.Animator.runtimeAnimatorController = aoc;
            }

            clipOverrides = new(aoc.overridesCount);
            aoc.GetOverrides(clipOverrides);

            BaseEquipment be = GetCurrentEquipment();
            if(be == null)
            {
                if(!nbr.TryGet(out be))
                {
                    Debug.LogWarning($"Failed to get weapon or weapon reference in slot {currentSlot} for client {OwnerClientId}!");
                    return;
                }
                switch (currentSlot)
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
            for (int i = 0; i < be.animationSet.animations.Length; i++)
            {
                AnimationClipPair acp = be.animationSet.animations[i];
                if(acp.clip != null && !string.IsNullOrWhiteSpace(acp.name))
                {
                    clipOverrides[acp.name] = acp.clip;
                }
            }
            aoc.ApplyOverrides(clipOverrides);
            networkAnimator.Animator.Rebind();

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
            else
            {
                print("failed to update primary");
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
            else
            {
                print("failed to update gadget 1");
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
            else
            {
                print("failed to update gadget 2");
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
            else
            {
                print("failed to update gadget 3");
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
            else
            {
                print("failed to update special");
            }
        }

        private void FixedUpdate()
        {
            if (!Controller.Alive)
                return;
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
            be.transform.SetPositionAndRotation(Controller.weaponOffset.position, Controller.weaponOffset.rotation);

        }
        void UpdateActiveEquipment(BaseEquipment be, Slot slot)
        {
            if (be != null && IsOwner)
            {
                be.fireInput = Controller.MyPlayerManager.fireInput && currentSlot == slot && !be.acpp.customParams[0].boolValue && !playingGesture && Controller.Alive;
                be.secondaryInput = Controller.MyPlayerManager.secondaryInput && currentSlot == slot && !be.acpp.customParams[0].boolValue && !playingGesture && Controller.Alive;
                if (Controller.Alive && slot == currentSlot)
                {
                    dampedMove = Mathf.SmoothDamp(dampedMove, Controller.MyPlayerManager.moveInput.sqrMagnitude * (Controller.isGrounded ? 1 : 0)
                        * (Controller.MyPlayerManager.sprintInput ? be.swayContainer.sprintMultiplier : 1)
                        * (Controller.MyPlayerManager.crouchInput ? .5f : 1), ref vdampedmove, Controller.moveSwayPosDampTime);
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

        public void PlayGesture(string triggerName)
        {
            if(!playingGesture)
                PlayGesture_RPC(triggerName);
        }

        [Rpc(SendTo.Everyone)]
        void PlayGesture_RPC(FixedString32Bytes value)
        {
            networkAnimator.SetTrigger(value.ToString());
            StartCoroutine(LerpLayerWeight());
        }
        [Rpc(SendTo.Everyone)]
        void LerpGestureWeight_RPC(bool gesturing = true)
        {
            StartCoroutine(LerpLayerWeight(gesturing));
        }
        public void LerpGestureWeight(bool gesturing = true)
        {
            StartCoroutine(LerpLayerWeight(gesturing));
        }
        IEnumerator LerpLayerWeight(bool gesturing = true)
        {
            if (gesturing)
            {
                while (gestureWeight < 1)
                {
                    gestureWeight = Mathf.Clamp01(gestureWeight + (Time.fixedDeltaTime / gestureLerpTime));
                    networkAnimator.Animator.SetLayerWeight(1, gestureWeight);
                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                while (gestureWeight > 0)
                {
                    gestureWeight = Mathf.Clamp01(gestureWeight - (Time.fixedDeltaTime / gestureLerpTime));
                    networkAnimator.Animator.SetLayerWeight(1, gestureWeight);
                    yield return new WaitForFixedUpdate();
                    playingGesture = false;
                }
            }
        }
    }
}
