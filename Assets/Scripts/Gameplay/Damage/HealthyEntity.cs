using Opus;
using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
        public bool Burning => burnStacks.Value > 0;
        public float burnTime, stunTime;
        public NetworkVariable<int> burnStacks = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        public override void OUpdate()
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
                uint value = (uint)Mathf.RoundToInt(damageIn);
                PlayerManager target;
                switch (scoreBehaviour)
                {
                    case ScoreAwardingBehaviour.sourceCombat:
                        if(PlayerManager.playersByID.TryGetValue(sourceClientID, out target))
                        {
                            target.combatPoints.Value += value;
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
                            target.combatPoints.Value += value;
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
                            target.combatPoints.Value += value;
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
                            target.supportPoints.Value += value;
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


        Coroutine BurnCoroutine;

        public NetworkVariable<bool> stunned = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [Rpc(SendTo.Everyone)]
        public void ReceiveDebuff_RPC(ulong sourceClientID, float duration, DebuffToApply debuff)
        {
            if (IsServer)
            {
                switch (debuff)
                {
                    case DebuffToApply.none:
                        
                        break;
                    case DebuffToApply.burn:
                        if(burnStacks.Value < 8)
                        {
                            burnStacks.Value++;
                        }
                        if (Burning)
                        {
                            BurnCoroutine ??= StartCoroutine(BurnDamage());
                        }
                        break;
                    case DebuffToApply.stun:
                        
                        break;
                    default:
                        break;
                }
            }
        }

        public IEnumerator BurnDamage()
        {
            var wfs = new WaitForSeconds(MatchManager.Instance.burnDamageTickTime);
            while (burnTime > 0)
            {
                burnTime -= MatchManager.Instance.burnDamageTickTime;
                currentHealth.Value -= burnStacks.Value * MatchManager.Instance.burnDamagePerStack;
                yield return wfs;
            }
            burnStacks.Value = 0;
            BurnCoroutine = null;
        }
        public virtual IEnumerator Stunned()
        {
            yield break;
        }
    }
}
