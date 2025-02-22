using Netcode.Extensions;
using Opus;
using Opus;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.VFX;

namespace Opus
{
    public class BaseEquipment : ONetBehaviour
    {
        public bool fireInput;
        public bool secondaryInput;

        public SwayContainerSO swayContainer;

        public EquipmentContainerSO equipmentContainer;

        public WeaponControllerV2 myController;

        public CharacterRenderable cr;

        public ClientNetworkAnimator netAnimator;

        public AnimatorCustomParamProxy acpp;


        public bool hasAnimations;
        public AnimationSetSO animationSet;

        public int maxCharges, currentCharges;
        public bool HasLimitedCharges => maxCharges > 0;
        public float rechargeTime;
        public float currentRechargeTime { get; private set; }

        public virtual bool FireBlocked => HasLimitedCharges && currentCharges == 0;

        public ConsumeCharge whenToConsumeCharge = ConsumeCharge.fired;


        protected bool lastFireInput;

        public override void OFixedUpdate()
        {
            base.OFixedUpdate();
            if (IsOwner && HasLimitedCharges)
            {
                if (currentCharges < maxCharges)
                {
                    currentRechargeTime += Time.fixedDeltaTime;
                    if(currentRechargeTime >= rechargeTime)
                    {
                        currentRechargeTime = 0;
                        currentCharges++;
                    }
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            StartCoroutine(DelayInitialise());
            currentCharges = maxCharges;
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
