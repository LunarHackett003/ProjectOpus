using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class ProjectileRangedWeapon : RangedWeapon
    {
        public NetworkObject projectile;
        
        public override void FireOnServer(Vector3 direction, Vector3 origin)
        {
            base.FireOnServer(direction, origin);
            Projectile projectile = NetworkManager.SpawnManager.InstantiateAndSpawn(this.projectile, OwnerClientId, false, false, false, origin, Quaternion.LookRotation(direction, Vector3.up))
                .GetComponent<Projectile>();
            
        }
    }
}
