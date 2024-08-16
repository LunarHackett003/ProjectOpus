using Netcode.Extensions;
using opus.Weapons;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


namespace opus.utility
{
    public class PlayerAnimationHelper : NetworkBehaviour
    {
        public Animator animator;
        public ClientNetworkAnimator networkAnimator;

        public WeaponManager weaponManager;
        public PlayerCharacter playerCharacter;

        [SerializeField] internal AnimatorOverrideController overrideController;
        [Rpc(SendTo.Everyone)]
        public void UpdateAnimationsFromEquipment_RPC(NetworkBehaviourReference equipment)
        {
            if(equipment.TryGet(out BaseEquipment e))
            {
                UpdateAnimationClips(e);
            }
            else
            {
                Debug.LogWarning("Failed to get Equipment! Are you sure you assigned the reference correctly?");
            }
        }
        protected AnimationClipOverrides clipOverrides;
        public void UpdateAnimationClips(BaseEquipment equipment)
        {
            if(overrideController == null)
            {
                overrideController = new(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = overrideController;

                clipOverrides = new(overrideController.overridesCount);
                overrideController.GetOverrides(clipOverrides);
            }
            for (int i = 0; i < equipment.playerAnimations.overrides.Length; i++)
            {
                OverridePair pair = equipment.playerAnimations.overrides[i];
                clipOverrides[pair.name] = pair.clip;
            }
            overrideController.ApplyOverrides(clipOverrides);
        }
    }



    public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    {
        public AnimationClipOverrides(int capacity) : base(capacity) { }

        public AnimationClip this[string name]
        {
            get { return this.Find(x => x.Key.name.Equals(name)).Value; }
            set
            {
                int index = this.FindIndex(x => x.Key.name.Equals(name));
                if (index != -1)
                    this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
            }
        }
    }
}