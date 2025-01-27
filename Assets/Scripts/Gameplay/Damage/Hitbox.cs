using UnityEngine;

namespace Opus
{
    public class Hitbox : Entity
    {
        [SerializeField] Entity parentEntity;
        [SerializeField] bool criticalBox = false;
        [SerializeField] bool transmitDamage = true;
        [SerializeField] float transmitDamageMultiplier = 1;
        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            if (parentEntity)
            {
                print($"blocked {damageIn} damage for {parentEntity.name}");
                if (transmitDamage)
                {
                    parentEntity.ReceiveDamage(damageIn * (criticalBox ? incomingCritMultiply : 1) * transmitDamageMultiplier, OwnerClientId, 1);
                }
            }
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply, DamageType damageType = DamageType.Regular)
        {
            if (parentEntity)
            {
                print($"blocked {damageIn} damage from player {sourceClientID} for {parentEntity.name}");
                if (transmitDamage)
                {
                    if(damageType == DamageType.Regular && criticalBox)
                        damageType = DamageType.Critical;

                    parentEntity.ReceiveDamage(damageIn * (criticalBox ? incomingCritMultiply : 1) * transmitDamageMultiplier, sourceClientID, 1, damageType);
                }
            }
        }
    }
}
