using UnityEngine;

namespace Opus
{
    public class Hitbox : Entity
    {
        [SerializeField] Entity parentEntity;

        public override void ReceiveDamage(float damageIn)
        {
            if (parentEntity)
            {
                print($"blocked {damageIn} damage for {parentEntity.name}");
            }
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID)
        {
            if (parentEntity)
            {
                print($"blocked {damageIn} damage from player {sourceClientID} for {parentEntity.name}");
            }
        }
    }
}
