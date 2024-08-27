using UnityEngine;

public class Explosive : Grenade
{
    [SerializeField] float maxExplosionDamage, minExplosionDamage;
    [SerializeField] float damageDropOffStart, damageMaxRange;

    [SerializeField, Tooltip("How damage falls off between Drop Off Start and Max Range, after which the grenade will deal zero damage.")] 
    AnimationCurve damageDropOffCurve;
    [SerializeField] LayerMask damageableLayerMask, losLayerMask;
    protected override void ExplodeServerSide()
    {
        if (IsServer)
        {
            print("Exploded Server Side");
            Explode_ClientRPC();



            Collider[] overlap = Physics.OverlapSphere(transform.position, damageMaxRange, damageableLayerMask);
            bool[] hitThisFrame = new bool[overlap.Length];
            print($"checking {overlap.Length} entries on grenade");
            if(overlap != null && overlap.Length > 0)
            {
                for (int i = 0; i < overlap.Length; i++)
                {
                    Collider c = overlap[i];
                    Vector3 closest = c.ClosestPoint(transform.position);
                    Ray r = new(transform.position, closest - transform.position);
                    Debug.DrawRay(r.origin, r.direction, Color.cyan, 5f);
                    if (Physics.Raycast(r,out RaycastHit hit, damageMaxRange, losLayerMask))
                    {
                        //If we hit this collider with the raycast
                        if(c.TryGetComponent(out IDamageable d) && hit.collider == c && !hitThisFrame[i])
                        {
                            //Lerps from the maximum to the minimum damage
                            float damage = Mathf.Lerp(minExplosionDamage, maxExplosionDamage, 
                                damageDropOffCurve.Evaluate(Mathf.InverseLerp(damageDropOffStart, damageMaxRange, hit.distance)));
                            d.TakeDamage(damage);
                            hitThisFrame[i] = true;
                        }
                    }


                }
            }
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }

        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, damageMaxRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, Mathf.Lerp(damageDropOffStart, damageMaxRange, damageDropOffCurve.Evaluate(0.5f)));
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, damageDropOffStart);
    }
}
