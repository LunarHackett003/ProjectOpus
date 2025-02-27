using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class Entity : ONetBehaviour
    {
        public ScoreAwardingBehaviour scoreBehaviour;

        public virtual void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            print($"Received {damageIn} damage from empty source");
        }

        public virtual void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply, DamageType damageType = DamageType.Regular)
        {
            print($"Received {damageIn} damage from client {sourceClientID}");
        }

        public virtual void ApplyDebuff(ulong sourceClientID, float duration, DebuffToApply debuff)
        {

        }
    }
}
