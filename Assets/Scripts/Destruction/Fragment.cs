using UnityEngine;

namespace Opus
{
    public class Fragment : MonoBehaviour
    {
        public float health;
        public float maxHealth = 25;
        public FragmentController controller;
        new public MeshRenderer renderer;
        new public MeshCollider collider;
        private void Awake()
        {
            health = maxHealth;
            renderer = GetComponent<MeshRenderer>();
            collider = GetComponent<MeshCollider>();
            renderer.enabled = false;
        }
        public void HitFragment(float damage)
        {
            health -= damage;
            if(health <= 0)
            {
                renderer.enabled = false;
                collider.enabled = false;
                controller.FragmentDamaged();
            }
        }
    }
}
