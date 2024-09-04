
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class DemoPiece : NetworkBehaviour
    {
        [SerializeField] protected readonly NetworkVariable<float> integrity = new();
        [SerializeField] protected float _integrity;
        [SerializeField] protected float maxIntegrity = 100;
        public float Integrity { get { return integrity.Value; } }
        public delegate void OnDamageReceived();
        public OnDamageReceived onDamageReceived;

        public readonly NetworkVariable<bool> active = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsHost)
            {
                integrity.Value = maxIntegrity;
                integrity.OnValueChanged += IntegrityChanged;
            }
        }
        protected virtual void Start()
        {

        }

        protected virtual void IntegrityChanged(float prev, float next)
        {
            _integrity = next;
        }

        public virtual void DealDamage(float damage)
        {
            integrity.Value -= damage;
            if (Integrity <= 0)
                active.Value = false;

            onDamageReceived?.Invoke();
        }
    }
}
