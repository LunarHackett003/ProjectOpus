using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class RangedWeaponAnimatorEvents : NetworkBehaviour
    {
        RangedWeapon be;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(be == null)
            {
                be = GetComponentInParent<RangedWeapon>();
            }
        }
        public void ReloadWeapon()
        {
            if (be != null && IsServer)
            {
                be.RefillAmmo();
            }
        }
        public void GiveWeaponAmmo(int ammo)
        {
            if (be != null && IsServer)
            {
                be.AddAmmo(ammo);
            }
        }
    }
}
