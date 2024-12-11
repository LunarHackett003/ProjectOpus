using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
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
        /// </summary>
        onReleaseInstant = 1,
        /// <summary>
        /// The weapon will fire with animations - useful for things like grenade throws.
        /// </summary>
        onReleaseAnimated = 2,
        /// <summary>
        /// The weapon will charge up when pressed, and then fire. Charge will decay if not holding fire.
        /// </summary>
        chargeHold = 4,
        /// <summary>
        /// The weapon will charge up fully when pressed, even if fire is released, and then fire when full.
        /// </summary>
        chargePress = 8
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
        public float roundsPerMinute;
        float timeBetweenRounds;
        [Tooltip("How long, in seconds, the weapon waits after firing a burst before allowing the player to fire again")]
        public float timeBetweenBursts;
        [Tooltip("How quickly the weapon charges up when firing.")]
        public float chargeSpeed;
        [Tooltip("How quickly the weapon \"Cools down\"")]
        public float chargeDecay;
        [Tooltip("How much charge the weapon currently has")]
        public float CurrentCharge { get; private set; }
        [Tooltip("How many shots are fired in a burst")]
        public int burstFireRounds;
        bool burstFiring;
        public Vector3 recoilPos, recoilEuler;
        public readonly NetworkVariable<bool> fireInputSynced = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        bool fireInputPressed;
        bool fired;
        public WeaponFireType weaponFireType;
        public WeaponFireBehaviour fireBehaviour;

        public ParticleSystem fireParticleSystem;
        public VisualEffect fireVFX;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            fireInput = fireInputSynced.Value;

            OnValidate();
        }
        private void FixedUpdate()
        {
            //if (fireInput)
            //{
            //    TryFire();
            //}
            //else
            //{
            //    fireInputPressed = false;
            //}
            if (fired)
                return;
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
                    if (!fireInput)
                    {

                    }
                    break;
                case WeaponFireType.onReleaseAnimated:
                    break;
                case WeaponFireType.chargeHold:
                    break;
                case WeaponFireType.chargePress:
                    break;
                default:
                    break;
            }
        }
        void TryFire()
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
        }
        [Rpc(SendTo.Everyone)]
        public void SendFireInput_RPC(bool input)
        {
            fireInput = input;

            if (IsServer)
            {
                fireInputSynced.Value = input;
            }
            Fire();
        }
        void Fire()
        {
            if (IsServer)
            {
                FireOnServer();
            }
            if (IsClient)
            {
                FireOnClient();
            }
            fired = true;
            StartCoroutine(ResetFire());
        }
        IEnumerator ResetFire()
        {
            yield return new WaitForSeconds(timeBetweenRounds);
            fired = false;
        }
        public IEnumerator ChargeToFull()
        {
            WaitForFixedUpdate wff = new();
            while (CurrentCharge < 1)
            {
                CurrentCharge += chargeSpeed * Time.fixedDeltaTime;
                yield return wff;
            }

        }
        public IEnumerator BurstFire()
        {
            WaitForSeconds wait = new(timeBetweenRounds);
            int fired = 0;
            while (fired < burstFireRounds)
            {
                Fire();
                fired++;
                if(fired != burstFireRounds-1)
                    yield return wait;
            }
            yield return new WaitForSeconds(timeBetweenBursts);
            if(fireBehaviour == WeaponFireBehaviour.burstFireLoop)
            {
                fireInputPressed = false;
            }
        }

        public void FireOnServer()
        {
            //If a client somehow gets here, we need to make sure they don't get any further.
            if (!IsServer)
                return;
            //SendTracer_RPC(Vector3.zero, Vector3.zero);
        }
        [Rpc(SendTo.Everyone)]
        public void SendTracer_RPC(Vector3 start, Vector3 end)
        {

        }
        public void FireOnClient()
        {
            if (fireParticleSystem)
            {
                fireParticleSystem.Play();
            }
            if (fireVFX)
            {
                fireVFX.Play();
            }
            if (myController)
            {
                myController.ReceiveShot();
            }
        }

        private void OnValidate()
        {
            timeBetweenRounds = 1 / (roundsPerMinute / 60);
        }
    }
}
