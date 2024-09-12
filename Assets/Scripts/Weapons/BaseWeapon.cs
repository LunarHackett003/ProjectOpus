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

        public OpusNetworkAnimator animator;
        public WeaponManager manager;
        public NetworkVariable<bool> primaryInput = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> secondaryInput = new(writePerm: NetworkVariableWritePermission.Owner);

        public delegate void OnKilledEntity(Entity entityKilled);
        public OnKilledEntity onKilledEntity;

        public WeaponAnimationModule animationModule;

        public bool isPlayerWeapon;
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
        public virtual void AttackClient()
        {

        }
        /// <summary>
        /// Where the attack itself executes.
        /// </summary>
        public virtual void AttackServer(float damage)
        {
            
        }

    }
}
