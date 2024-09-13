using UnityEngine;

namespace Opus
{
    public class DualWieldWeapon : BaseWeapon
    {
        public BaseWeapon weaponOne, weaponTwo;
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
