using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class HealthyEntity : Entity
    {

        [SerializeField] float maxHealth;
        public float MaxHealth => maxHealth;
        /// <summary>
        /// Current Health is only modifiable by the server.
        /// </summary>
        public NetworkVariable<float> currentHealth = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public float CurrentHealth => currentHealth.Value;
        public bool healable;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }
            
        }

        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
            currentHealth.Value -= damageIn;

            if (scoreBehaviour != 0)
            {
                uint value = (uint)Mathf.RoundToInt(damageIn * 10);
                PlayerManager target;
                switch (scoreBehaviour)
                {
                    case ScoreAwardingBehaviour.sourceCombat:
                        if(PlayerManager.playersByID.TryGetValue(sourceClientID, out target))
                        {
                            target.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} combat points to {target.name}//{sourceClientID}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.sourceSupport:
                        if (PlayerManager.playersByID.TryGetValue(sourceClientID, out target))
                        {
                            target.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} support points to {target.name}//{sourceClientID}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.ownerCombat:
                        if (PlayerManager.playersByID.TryGetValue(OwnerClientId, out target))
                        {
                            target.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} combat points to {target.name}//{OwnerClientId}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.ownerSupport:
                        if (PlayerManager.playersByID.TryGetValue(OwnerClientId, out target))
                        {
                            target.supportPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} support points to {target.name}//{OwnerClientId}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Restoring health to an entity will ALWAYS reward the one who performed the action with support points.
        /// </summary>
        /// <param name="healthIn"></param>
        /// <param name="sourceClientID"></param>
        public virtual void RestoreHealth(float healthIn, ulong sourceClientID)
        {
            currentHealth.Value += healthIn;
            uint value = (uint)Mathf.RoundToInt(healthIn * 10);
            if (PlayerManager.playersByID.TryGetValue(sourceClientID, out PlayerManager source))
            {
                source.supportPoints.Value += (uint)Mathf.RoundToInt(value);
                print($"Awarded {value} support points to {source.name}//{OwnerClientId}");
                return;
            }
        }
    }
}
