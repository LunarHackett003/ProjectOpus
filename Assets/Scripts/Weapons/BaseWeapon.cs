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

        protected virtual void FixedUpdate()
        {

        }

        public ParticleSystem fireParticleSystem;
        public VisualEffect fireVFX;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
    }
}
