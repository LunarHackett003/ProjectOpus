using FMODUnity;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseWeapon : NetworkBehaviour
    {
        /*The base weapon script should not be overloaded with features.
         * It should be the most extensible and configurable class with regards to the weapon system.
         * 
         * Weapons should make liberal use of inheritance, as well. for example - a raycast machine gun could be
         * a rapid-fire weapon that inherits raycast weapon that inherits ammo-constrained weapon, as a rough example.
         * This example might not be followed in practice. Do NOT refer to this specific example unless implemented.
         * 
         * Weapons should also make liberal use of Scriptable Object-driven configuration
         */

        public bool doLogging = false;
        
        public OpusNetworkAnimator animator;
        public WeaponManager manager;
        public bool PrimaryInput { get; internal set; }
        public bool SecondaryInput { get; internal set; }

        public delegate void OnKilledEntity(Entity entityKilled);
        public OnKilledEntity onKilledEntity;

        public WeaponAnimationModule animationModule;
        public bool attackOnPress, attackOnRelease;

        public EventReference primaryEventRef, secondaryEventRef;

        [Rpc(SendTo.Server)]
        public void SetPrimaryInput_RPC(bool input)
        {
            PrimaryInput = input;
        }
        [Rpc(SendTo.Server)]
        public void SetSecondaryInput_RPC(bool input)
        {
            SecondaryInput = input;
        }

        public virtual void SwitchToWeapon()
        {

        }
        public bool isPlayerWeapon;
        public bool isCurrentWeapon;
        public float hitEffectOffsetMultiplier = 0.1f; 
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (isPlayerWeapon)
            {
                manager = PlayerManager.playerManagers.First(x => x.OwnerClientId == OwnerClientId).weaponManager; 
            }
        }
        /// <summary>
        /// Where everything that clients see when somebody attacks is performed
        /// </summary>
        public virtual void AttackClient(bool secondaryAttack = false)
        {
            RuntimeManager.PlayOneShot(secondaryAttack ? secondaryEventRef : primaryEventRef, manager.attackOrigin.position);
        }
        /// <summary>
        /// Where the attack itself executes.
        /// </summary>
        public virtual void AttackServer(float damage)
        {
            
        }

    }
}
