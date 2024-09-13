using UnityEngine;

namespace Opus
{
    public class HitscanWeapon : RangedWeapon
    {

        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);

        }
        public override void AttackClient()
        {
            base.AttackClient();
        }
    }
}
