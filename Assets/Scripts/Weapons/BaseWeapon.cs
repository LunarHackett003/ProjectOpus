using Opus;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.VFX;

namespace Opus
{
    /// <summary>
    /// How weapons react to the fire input
    /// </summary>
    public enum WeaponFireType
    {
        /// <summary>
        /// The weapon will fire immediately when fire is pressed
        /// </summary>
        onPress = 0,
        /// <summary>
        /// The weapon will fire immediately when fire is released
        /// <br></br> Example use case: Gadget Deployment (yes, gadgets are classified as weapons here)
        /// </summary>
        onReleaseInstant = 1,
        /// <summary>
        /// The weapon will fire with animations - useful for things like grenade throws.
        /// <Br></Br>Example use case: Grenades/Mines
        /// </summary>
        onReleaseAnimated = 2,
        /// <summary>
        /// The weapon will charge up when pressed, and then fire. Charge will decay if not holding fire.
        /// <br></br>Example use case: Laser weapon
        /// </summary>
        chargeHold = 4,
        /// <summary>
        /// The weapon will charge up fully when pressed, even if fire is released, and then fire when full.
        /// <br></br>Example use case: Open bolt weapons
        /// </summary>
        chargePress = 8,
        /// <summary>
        /// The weapon will charge up, and then fire when released. This automatically sets charge to zero after firing.<br></br>
        /// Example use case: Bows
        /// </summary>
        chargeRelease = 16,
        /// <summary>
        /// Fires when you press AND release the trigger.
        /// </summary>
        binaryFire = 32,
    }
    /// <summary>
    /// How the weapon behaves when firing
    /// </summary>
    public enum WeaponFireBehaviour
    {
        /// <summary>
        /// The weapon uses the time between rounds to reset its ability to fire. The fire input is ignored after firing once.
        /// </summary>
        semiAutomatic = 0,
        /// <summary>
        /// The weapon uses the time between rounds to reset its ability to fire. The fire input is preserved after firing.
        /// </summary>
        fullyAutomatic = 1,
        /// <summary>
        /// The weapon fires multiple times, with TimeBetweenRounds between them, delays for TimeBetweenBursts, and consumes the fire input.
        /// </summary>
        burstFireOnce = 2,
        /// <summary>
        /// The weapon fires multiple times, with TimeBetweenRounds between them, delays for TimeBetweenBursts, and loops while the fire input is held.
        /// </summary>
        burstFireLoop = 4,
        /// <summary>
        /// The fire input is consumed when firing, and the weapon cannot be fired again until an animation has played to reset this.<br></br>
        /// This is typical of bolt-action/pump action firearms.
        /// </summary>
        animatedReset = 8,


    }

    public class BaseWeapon : BaseEquipment
    {

        public override void OFixedUpdate()
        {

        }
        protected PlayerManager owningPlayer;
        public ParticleSystem fireParticleSystem;
        public VisualEffect fireVFX;
        public WeaponFireType weaponFireType;
        public WeaponFireBehaviour fireBehaviour;
        protected bool fireInputPressed;
        protected bool fired;
        protected bool charging;


        public LayerMask damageLayermask;
        public bool useDamageRanging;
        public float maxRange;
        public float minRange;
        public float maxRangeDamage;
        public float minRangeDamage;
        public AnimationCurve damageFalloff;
        public float critMultiplier = 1;
        public int fireIterations = 1;
        
        protected bool burstFiring;
        public Vector3 recoilPos;
        public Vector3 recoilEuler;

        public float roundsPerMinute;
        protected float timeBetweenRounds;
        [Tooltip("How long, in seconds, the weapon waits after firing a burst before allowing the player to fire again")]
        public float timeBetweenBursts;
        [Tooltip("How quickly the weapon charges up when firing.")]
        public float chargeSpeed;
        [Tooltip("How quickly the weapon \"Cools down\"")]
        public float chargeDecay;
        public bool clearChargeOnShot;
        
        [Tooltip("How many shots are fired in a burst")]
        public int burstFireRounds;
        public readonly NetworkVariable<bool> fireInputSynced = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [Tooltip("How much charge the weapon currently has")]
        public float CurrentCharge { get; private set; }




        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            owningPlayer = PlayerManager.playersByID[OwnerClientId];
        }
        protected virtual void TryFire()
        {
            switch (fireBehaviour)
            {
                case WeaponFireBehaviour.semiAutomatic:
                    if (!fireInputPressed)
                    {
                        Fire();
                    }
                    break;
                case WeaponFireBehaviour.fullyAutomatic:
                    Fire();
                    break;
                case WeaponFireBehaviour.burstFireOnce:
                    if (!fireInputPressed && !burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.burstFireLoop:
                    if (!burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.animatedReset:
                    Fire();
                    break;
                default:
                    break;
            }
            if (clearChargeOnShot)
                CurrentCharge = 0;
        }
        protected virtual void Fire()
        {
            if (IsOwner)
            {
                SendFireToServer_RPC(myController.fireOrigin.forward, myController.fireOrigin.position);
            }
            if (IsClient)
            {
                FireOnClient();
            }
            fired = true;
            StartCoroutine(ResetFire());
        }
        public virtual IEnumerator BurstFire()
        {
            WaitForSeconds wait = new(timeBetweenRounds);
            int fired = 0;
            while (fired < burstFireRounds)
            {
                Fire();
                fired++;
                if (fired != burstFireRounds - 1)
                    yield return wait;
            }
            yield return new WaitForSeconds(timeBetweenBursts);
            if (fireBehaviour == WeaponFireBehaviour.burstFireLoop)
            {
                fireInputPressed = false;
            }
        }
        public virtual IEnumerator ChargeToFull()
        {
            charging = true;
            WaitForFixedUpdate wff = new();
            while (CurrentCharge < 1)
            {
                CurrentCharge += chargeSpeed * Time.fixedDeltaTime;
                yield return wff;
            }
            charging = false;
            yield break;
        }

        protected virtual IEnumerator ResetFire()
        {
            yield return new WaitForSeconds(timeBetweenRounds);
            fired = false;
        }

        public virtual bool FireBlocked => fired;

        protected virtual void ProcessFire()
        {
            if (FireBlocked)
            {
                fireInputPressed = false;
                return;
            }
            switch (weaponFireType)
            {
                case WeaponFireType.onPress:
                    if (fireInput)
                    {
                        TryFire();
                        fireInputPressed = true;
                    }
                    else
                    {
                        fireInputPressed = false;
                    }
                    break;
                case WeaponFireType.onReleaseInstant:
                    if (fireInput)
                    {
                        fireInputPressed = true;
                    }
                    if (!fireInput && fireInputPressed)
                    {
                        fireInputPressed = false;
                        TryFire();
                    }
                    break;
                case WeaponFireType.onReleaseAnimated:
                    if (fireInput)
                    {
                        fireInputPressed = true;
                    }
                    if (!fireInput && fireInputPressed)
                    {
                        fireInputPressed = false;
                    }
                    break;
                case WeaponFireType.chargeHold:
                    if (fireInput)
                    {
                        CurrentCharge = Mathf.Min(1, CurrentCharge + (Time.fixedDeltaTime * chargeSpeed));
                    }
                    else if (CurrentCharge > 0)
                    {
                        CurrentCharge = Mathf.Max(0, CurrentCharge - (Time.fixedDeltaTime * chargeDecay));
                    }

                    if (CurrentCharge >= 1)
                    {
                        TryFire();
                    }
                    break;
                case WeaponFireType.chargePress:
                    if (fireInput)
                    {
                        if (CurrentCharge <= 0)
                            StartCoroutine(ChargeToFull());
                    }
                    else if (!charging)
                    {
                        CurrentCharge = 0;
                    }
                    
                    if (CurrentCharge >= 1)
                    {
                        TryFire();
                    }
                    break;
                case WeaponFireType.binaryFire:
                    if (fireInput)
                    {
                        TryFire();
                        fireInputPressed = true;
                    }
                    if (!fireInput && fireInputPressed)
                    {
                        fireInputPressed = false;
                        TryFire();
                    }
                    break;
                default:
                    break;
            }
        }
        public virtual void FireOnClient()
        {

        }

        [Rpc(SendTo.Server)]
        public void SendFireToServer_RPC(Vector3 direction, Vector3 origin)
        {
            FireOnServer(direction, origin);
        }
        
        public virtual void FireOnServer(Vector3 direction, Vector3 origin)
        {

        }
    }
}
