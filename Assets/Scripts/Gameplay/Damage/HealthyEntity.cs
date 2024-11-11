using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class HealthyEntity : Entity
    {

        [SerializeField] float maxHealth;
        /// <summary>
        /// Current Health is only modifiable by the server.
        /// </summary>
        public NetworkVariable<float> currentHealth = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public enum ScoreAwardingBehaviour
        {
            /// <summary>
            /// Damaging this entity does NOT award score.
            /// </summary>
            none = 0,
            /// <summary>
            /// Damaging this entity awards combat score to the damager - think a player shooting somebody. Things that heal should NOT use ReceiveDamage.
            /// </summary>
            sourceCombat = 1,
            /// <summary>
            /// Damaging this entity awards support score to the damager - shooting an enemy device to disable it 
            /// </summary>
            sourceSupport = 2,
            /// <summary>
            /// In some cases, shooting something might award the owner combat score. Not sure what any of these cases ARE, but it wouldn't hurt to have this anyway.
            /// </summary>
            ownerCombat = 3,
            /// <summary>
            /// Objects such as shields or barricades might want to award the owner with support score when using them.
            /// </summary>
            ownerSupport = 4,
        }
        public ScoreAwardingBehaviour scoreBehaviour;
        public bool healable;
        public override void ReceiveDamage(float damageIn)
        {
            base.ReceiveDamage(damageIn);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID)
        {
            base.ReceiveDamage(damageIn, sourceClientID);
            currentHealth.Value -= damageIn;

            if (scoreBehaviour != 0)
            {
                uint value = (uint)Mathf.RoundToInt(damageIn * 10);
                PlayerManager source;
                switch (scoreBehaviour)
                {
                    case ScoreAwardingBehaviour.sourceCombat:
                        if(PlayerManager.playersByID.TryGetValue(sourceClientID, out source))
                        {
                            source.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} combat points to {source.name}//{sourceClientID}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.sourceSupport:
                        if (PlayerManager.playersByID.TryGetValue(sourceClientID, out source))
                        {
                            source.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} support points to {source.name}//{sourceClientID}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.ownerCombat:
                        if (PlayerManager.playersByID.TryGetValue(OwnerClientId, out source))
                        {
                            source.combatPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} combat points to {source.name}//{OwnerClientId}");
                            return;
                        }
                        else
                        {

                        }
                        break;
                    case ScoreAwardingBehaviour.ownerSupport:
                        if (PlayerManager.playersByID.TryGetValue(OwnerClientId, out source))
                        {
                            source.supportPoints.Value += (uint)Mathf.RoundToInt(value);
                            print($"Awarded {value} support points to {source.name}//{OwnerClientId}");
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
