using Netcode.Extensions;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class BaseEquipment : ONetBehaviour
    {
        public bool fireInput;
        public bool secondaryInput;

        public SwayContainerSO swayContainer;

        public WeaponControllerV2 myController;

        public CharacterRenderable cr;

        public ClientNetworkAnimator netAnimator;

        public AnimatorCustomParamProxy acpp;


        public bool hasAnimations;
        public AnimationSetSO animationSet;


        protected bool lastFireInput;

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            StartCoroutine(DelayInitialise());
        }
        protected virtual IEnumerator DelayInitialise()
        {
            yield return new WaitForFixedUpdate();
            myController = PlayerManager.playersByID[OwnerClientId].Character.wc;
        }
        public virtual void TrySelect()
        {
            print($"Tried to select {gameObject.name}");
        }
    }
}
