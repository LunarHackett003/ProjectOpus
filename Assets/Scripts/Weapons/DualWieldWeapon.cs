using UnityEngine;

namespace Opus
{
    public class DualWieldWeapon : BaseWeapon
    {
        public RangedWeapon weaponOne, weaponTwo;
        public string reloadKey2;
        int reloadHash2;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            weaponOne.manager = manager;
            weaponTwo.manager = manager;
        }
        private void Update()
        {
            weaponOne.PrimaryInput = PrimaryInput;
            weaponTwo.PrimaryInput = SecondaryInput;
        }
    }
}
