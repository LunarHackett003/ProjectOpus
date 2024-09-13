using UnityEditor.Rendering;
using UnityEngine;

namespace Opus
{
    public class TestShooter : MonoBehaviour
    {
        public LineRenderer lastShotLine;
        public GameObject lastExplosionSphere;
        public float explosionRadius;
        public LayerMask shootmask;
        private void Start()
        {
            lastShotLine.enabled = false;
            lastExplosionSphere.SetActive(false);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Fire();
            }
            else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Explode();
            }
        }
        void Fire()
        {
            if (Physics.SphereCast(transform.position, 0.04f, transform.forward, out RaycastHit hit, 250, shootmask))
            {
                if (hit.collider.TryGetComponent(out Fragment fc))
                {
                    fc.TakeDamage(30);
                }
                lastShotLine.enabled = true;
                lastShotLine.useWorldSpace = true;
                lastShotLine.SetPosition(0, transform.position);
                lastShotLine.SetPosition(1, hit.point);
            }
        }
        void Explode()
        {
            lastExplosionSphere.SetActive(true);
            if(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 250, shootmask))
            {
                foreach (var item in Physics.OverlapSphere(hit.point, explosionRadius, shootmask))
                {
                    if(item.TryGetComponent(out Fragment fc))
                    {
                        float distance = Vector3.Distance(hit.point, item.ClosestPoint(hit.point));
                        fc.TakeDamage(30 / Mathf.Min(1, distance));
                    }
                    lastExplosionSphere.transform.position = hit.point;
                }
            }
        }
    }
}
