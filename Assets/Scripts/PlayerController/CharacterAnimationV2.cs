using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class CharacterAnimationV2 : ONetBehaviour
    {

        public WeaponControllerV2 wc;
        public PlayerMotorV2 pm;
        public PlayerEntity pe;
        public NetworkAnimator netAnimator;
        public Animator animator;


        protected AnimationClipOverrides clipOverrides;
        protected AnimatorOverrideController aoc;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            UpdateAnimations((Slot)wc.weaponIndex.Value);
        }

        public void UpdateAnimations(Slot equipmentSlot)
        {
            if (IsOwner)
            {
                //We'll send this to the non-owners. This SHOULD be fine because the rpc is marked with SendTo.NotOwner. 
                //That'll call THIS method, and we'll do all of this shite here.
                UpdateAnimations_RPC(equipmentSlot);
            }

            if (aoc == null)
            {
                aoc = new(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = aoc;
            }
            BaseEquipment be = equipmentSlot == Slot.special ? wc.specialEquipment : wc.slots[Mathf.Clamp((int)equipmentSlot, 0, wc.slots.Count)];
            if (wc.slots.Count > 0 && (int)equipmentSlot < wc.slots.Count)
            {
                if (be == null || !be.hasAnimations)
                {
                    return;
                }

                clipOverrides = new(aoc.overridesCount);
                aoc.GetOverrides(clipOverrides);

                for (int i = 0; i < be.animationSet.animations.Length; i++)
                {
                    AnimationClipPair acp = be.animationSet.animations[i];
                    if (acp.clip != null && !string.IsNullOrWhiteSpace(acp.name))
                    {
                        clipOverrides[acp.name] = acp.clip;
                    }
                }
                aoc.ApplyOverrides(clipOverrides);
            }
        }
        [Rpc(SendTo.NotOwner)]
        public void UpdateAnimations_RPC(Slot equipmentSlot)
        {
            UpdateAnimations(equipmentSlot);
        }

        public override void OUpdate()
        {
            base.OUpdate();
        }
        public override void OFixedUpdate()
        {
            base.OFixedUpdate();
        }
        private void OnValidate()
        {
            if (wc == null)
                wc = GetComponent<WeaponControllerV2>();
            if (pm == null)
                pm = GetComponent<PlayerMotorV2>();
            if (pe == null)
                pe = GetComponent<PlayerEntity>();
            if(netAnimator == null)
                netAnimator = GetComponentInChildren<NetworkAnimator>();
            if(animator == null)
                animator = GetComponentInChildren<Animator>();
        }
    }
}
