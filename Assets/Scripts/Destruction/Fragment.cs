using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class Fragment : Damageable
    {
        public NetworkVariable<float> health = new();
        public float maxHealth = 25;
        public FragmentController controller;
        new public MeshRenderer renderer;
        new public MeshCollider collider;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            health.Value = maxHealth;
            renderer = GetComponent<MeshRenderer>();
            collider = GetComponent<MeshCollider>();
            renderer.enabled = controller.hasBeenHit.Value;
        }
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            health.Value -= damage;
            if(health.Value <= 0)
            {
                renderer.enabled = false;
                collider.enabled = false;
                controller.FragmentDamaged();
            }
        }
    }
}
