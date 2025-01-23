using Opus;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


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


        public bool useWorldHealthBar;
        public bool hideBarOnOwner;
        public Slider worldHealthBar;
        public float worldHealthBarFade;
        public float worldHealthBarDisplayTime;
        [SerializeField] protected bool displayingBar;
        public CanvasGroup worldHealthBarCG;
        float currentBarFadeTime;

        public bool useDamageTypeOverride;
        public DamageType damageTypeOverride;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }

            if (useWorldHealthBar )
            {
                if(hideBarOnOwner && IsOwner)
                {
                    worldHealthBarCG.alpha = 0;
                }
                else
                {
                    worldHealthBar.maxValue = MaxHealth;
                }

            }
            currentHealth.OnValueChanged += HealthUpdated;
            HealthUpdated(0, currentHealth.Value);
        }

        protected virtual void HealthUpdated(float prev, float curr)
        {
            if (useWorldHealthBar)
            {
                displayingBar = true;
                currentBarFadeTime = worldHealthBarFade * 1.1f;
                worldHealthBar.value = curr;
            }
        }

        protected void DisplayHealthBar()
        {
            if (displayingBar)
            {
                currentBarFadeTime += Time.deltaTime;
                if (currentBarFadeTime < worldHealthBarFade)
                {
                    worldHealthBarCG.alpha = Mathf.InverseLerp(0, worldHealthBarFade, currentBarFadeTime);
                }
                else if (currentBarFadeTime >= worldHealthBarDisplayTime - worldHealthBarFade)
                {
                    worldHealthBarCG.alpha = Mathf.InverseLerp(worldHealthBarDisplayTime, worldHealthBarDisplayTime - worldHealthBarFade, currentBarFadeTime);
                }
                else
                {
                    worldHealthBarCG.alpha = 1;
                }

                if (currentBarFadeTime >= worldHealthBarDisplayTime)
                    displayingBar = false;
            }
        }

        private void Update()
        {
            if (useWorldHealthBar && (!hideBarOnOwner || !IsOwner))
            {
                DisplayHealthBar();
            }
        }


        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply, DamageType damageType = DamageType.Regular)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
            currentHealth.Value -= damageIn;

            PlayerManager.playersByID[sourceClientID].SendHitmarker_RPC(useDamageTypeOverride ? damageTypeOverride : damageType);          



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
