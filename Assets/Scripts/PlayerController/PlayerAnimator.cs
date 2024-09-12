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
                ic = ic != null ? ic : GetComponentInParent<InputCollector>();
                pm = pm != null ? pm : GetComponentInParent<PlayerMotor>();
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

            aoc = new(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = aoc;

            clipOverrides = new(aoc.overridesCount);
            aoc.GetOverrides(clipOverrides);
            UpdateAnimations(pwm.primaryWeapon);
        }
        public void UpdateAnimations(NetworkBehaviourReference netref)
        {
            if(!netref.TryGet(out BaseWeapon b))
            {
                return;
            }

            foreach (var item in b.animationModule.animationOverrides)
            {
                clipOverrides[item.name] = item.clip;
            }
            aoc.ApplyOverrides(clipOverrides);
            animator.Rebind();
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

    }
}
