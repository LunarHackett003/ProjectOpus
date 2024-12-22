using Netcode.Extensions;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class BaseEquipment : NetworkBehaviour
    {
        public bool fireInput;
        public bool secondaryInput;

        public SwayContainerSO swayContainer;

        public WeaponController myController;

        public CharacterRenderable cr;

        public ClientNetworkAnimator netAnimator;

        public AnimatorCustomParamProxy acpp;


        public bool hasAnimations;
        public AnimationSetSO animationSet;


        protected bool lastFireInput;

        public virtual void TrySelect()
        {
            print($"Tried to select {gameObject.name}");
        }
    }
}
