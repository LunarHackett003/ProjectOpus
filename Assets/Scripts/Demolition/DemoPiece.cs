using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Opus
{
    public class DemoPiece : NetworkBehaviour
    {
        [SerializeField] protected readonly SyncVar<float> integrity = new(new(WritePermission.ServerOnly));
        [SerializeField] protected float _integrity;
        [SerializeField] protected float maxIntegrity = 100;
        public float Integrity { get { return integrity.Value; } }
        public delegate void OnDamageReceived();
        public OnDamageReceived onDamageReceived;

        public readonly SyncVar<bool> active = new SyncVar<bool>(new()
        {
            WritePermission = WritePermission.ServerOnly
        });
        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            if (connection.IsHost)
            {
                integrity.Value = maxIntegrity;
                integrity.OnChange += IntegrityChanged;
            }
        }
        protected virtual void Start()
        {

        }

        protected virtual void IntegrityChanged(float prev, float next, bool asServer)
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
