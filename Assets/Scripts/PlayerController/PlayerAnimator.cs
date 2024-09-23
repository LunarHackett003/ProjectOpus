using Netcode.Extensions;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class PlayerAnimator : NetworkBehaviour
    {
        public Rigidbody rb;
        public InputCollector ic;
        public PlayerMotor pm;
        public Animator animator;
        public NetworkAnimator networkAnimator;
        public PlayerWeaponManager pwm;

        int horizontalMoveID, verticalMoveID, jumpTriggerID, landTriggerID, primaryMeleeID, secondaryMeleeID, quickMeleeID, inspectID;

        public delegate void OnPrimaryWeaponHit(int increment);
        public delegate void OnSecondaryWeaponHit(int increment);

        public OnPrimaryWeaponHit onPrimaryWeaponHit;
        public OnSecondaryWeaponHit onSecondaryWeaponHit;


        AnimatorOverrideController aoc;
        AnimationClipOverrides clipOverrides;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                rb = rb != null ? rb : GetComponentInParent<Rigidbody>();
                pm = pm != null ? pm : GetComponentInParent<PlayerMotor>();
                ic = ic != null ? ic : pm.ic;
                pwm = pwm != null ? pwm : GetComponentInParent<PlayerWeaponManager>();

                horizontalMoveID = Animator.StringToHash("Horizontal");
                verticalMoveID = Animator.StringToHash("Vertical");
                jumpTriggerID = Animator.StringToHash("Jump");
                landTriggerID = Animator.StringToHash("Land");
                primaryMeleeID = Animator.StringToHash("PrimaryMelee");
                secondaryMeleeID = Animator.StringToHash("SecondaryMelee");
                quickMeleeID = Animator.StringToHash("QuickMelee");
                inspectID = Animator.StringToHash("Animator");
            }
            if(pwm.primaryWeapon != null)
                UpdateAnimations(pwm.primaryWeapon);
        }
        public void UpdateAnimations(NetworkBehaviourReference netref)
        {
            if(!netref.TryGet(out BaseWeapon b))
            {
                return;
            }
            if (b == null || b.animationModule == null || b.animationModule.animationOverrides.Length == 0)
                return;
            print("Weapon: " + b.name);

            if(aoc == null)
            {
                aoc = new(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = aoc;
            }
            clipOverrides = new(aoc.overridesCount);
            aoc.GetOverrides(clipOverrides);


            foreach (var item in b.animationModule.animationOverrides)
            {
                if (!string.IsNullOrEmpty(item.name))
                {
                    print($"overriding {item.name} with {item.clip.name}");
                    clipOverrides[item.name] = item.clip;
                }
            }
            aoc.ApplyOverrides(clipOverrides);
            animator.Rebind();
            
            switch (b)
            {
                case MeleeWeapon t1:
                    SetLayerWeights(1, 0, 0);
                    break;
                case RangedWeapon t2:
                    SetLayerWeights(0, 1, 0);
                    break;
                case DualWieldWeapon t3:
                    SetLayerWeights(0, 0, 1);
                    break;
                default:
                    break;
            }
        }
        void SetLayerWeights(float meleeWeight, float twoHandWeight, float dualWeight)
        {
            print($"Set weapon layer weights: {meleeWeight}, {twoHandWeight}, {dualWeight}");


            animator.SetLayerWeight(1, meleeWeight);
            animator.SetLayerWeight(2, twoHandWeight);
            animator.SetLayerWeight(3, dualWeight);
            animator.SetLayerWeight(4, dualWeight);
        }
        private void FixedUpdate()
        {
            //Rigidbody or Input Collector is not assigned, and we therefore probably shouldn't be doing stuff with this character.
            //If we don't own this player character, we'll also ignore this too.
            if (!IsOwner || rb == null || ic == null || pm == null || pwm == null) return;
            animator.SetFloat(horizontalMoveID, pm.movementVector.x);
            animator.SetFloat(verticalMoveID, pm.movementVector.y);
        }
        public void JumpTrigger()
        {
            animator.SetTrigger(jumpTriggerID);
        }
        public void LandTrigger()
        {
            animator.SetTrigger(landTriggerID);
        }
        public void QuickMeleeTrigger()
        {
            animator.SetTrigger(quickMeleeID);
        }
        public void PrimaryMeleeSet(bool value)
        {
            animator.SetBool(primaryMeleeID, value);
        }
        public void SecondaryMeleeSet(bool value)
        {
            animator.SetBool(secondaryMeleeID, value);
        }
        public void PrimaryMeleeHit(int increment)
        {
            onPrimaryWeaponHit?.Invoke(increment);
        }
        public void SecondaryMeleeHit(int increment)
        {
            onSecondaryWeaponHit?.Invoke(increment);
        }
        public void ReloadLeftWeapon()
        {
            if (IsServer && pwm.equipmentDict[pwm.currentSlot.Value] is DualWieldWeapon d)
            {
                pwm.ClearReloadFlag();
                d.weaponOne.ReloadWeapon();
            }
        }
        public void ReloadRightweapon()
        {
            if (IsServer && pwm.equipmentDict[pwm.currentSlot.Value] is DualWieldWeapon d)
            {
                pwm.ClearReloadFlag();
                d.weaponTwo.ReloadWeapon();
            }
        }
        public void ReloadWeapon()
        {
            if(IsServer && pwm.equipmentDict[pwm.currentSlot.Value] is RangedWeapon r)
            {
                r.ReloadWeapon();
                pwm.ClearReloadFlag();
            }
        }
        public void RecockWeapon()
        {
            if(pwm.equipmentDict[pwm.currentSlot.Value] is RangedWeapon r)
            {
                r.RecockWeapon();
            }
        }
        public void RoundToWeapon(int rounds, RangedWeapon w)
        {
            if(IsServer)
                w.currentAmmunition.Value += rounds;
        }
        public void CheckAmmo()
        {
            if (pwm.equipmentDict[pwm.currentSlot.Value] is RangedWeapon w)
            {
                if (w.currentAmmunition.Value == (w.MaxAmmo - 1))
                {
                    w.animator.SetTrigger("CountedReloadFinish");
                    networkAnimator.SetTrigger("CountedReloadFinish");
                }
                else if(w.currentAmmunition.Value == (w.MaxAmmo - 2))
                {
                    w.animator.SetTrigger("CountedReloadSingle");
                    networkAnimator.SetTrigger("CountedReloadSingle");
                }
                else
                {
                    w.animator.SetTrigger("CountedReloadDouble");
                    networkAnimator.SetTrigger("CountedReloadDouble");
                }
            }
        }
        public void RoundToLeftWeapon(int rounds)
        {
            RoundToWeapon(rounds, (pwm.equipmentDict[pwm.currentSlot.Value] as DualWieldWeapon).weaponTwo);
        }
        public void RoundToRightWeapon(int rounds)
        {
            RoundToWeapon(rounds, (pwm.equipmentDict[pwm.currentSlot.Value] as DualWieldWeapon).weaponOne);
        }
        public void RoundToRegularWeapon(int rounds)
        {
            RoundToWeapon(rounds, pwm.equipmentDict[pwm.currentSlot.Value] as RangedWeapon);
        }
    }
}
