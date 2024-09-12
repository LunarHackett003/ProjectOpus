using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class Entity : NetworkBehaviour
    {
        public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
        public NetworkVariable<bool> isAlive = new NetworkVariable<bool>(true);
        public float maxHealth = 250;

        public Dictionary<ulong, float> attackers = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            currentHealth.OnValueChanged += HealthChanged;
            isAlive.OnValueChanged += AliveStateChanged;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            currentHealth.OnValueChanged -= HealthChanged;
            isAlive.OnValueChanged -= AliveStateChanged;
        }
        protected virtual void HealthChanged(float previous, float current)
        {
            if(IsServer && current <= 0)
            {
                isAlive.Value = false;
            }
            if(currentHealth.Value >= maxHealth)
            {
                attackers.Clear();
            }
        }
        protected virtual void AliveStateChanged(bool previous, bool current)
        {
            if (IsServer && current)
            {
                currentHealth.Value = maxHealth;
            }
        }
    }
}
