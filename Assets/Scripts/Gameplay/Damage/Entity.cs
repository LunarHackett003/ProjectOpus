using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class Entity : NetworkBehaviour
    {
        public ScoreAwardingBehaviour scoreBehaviour;

        public virtual void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            print($"Received {damageIn} damage from empty source");
        }

        public virtual void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            print($"Received {damageIn} damage from client {sourceClientID}");
        }
    }
}
