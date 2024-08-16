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
            NetworkObject.Despawn();



            Collider[] overlap = Physics.OverlapSphere(transform.position, damageMaxRange, damageableLayerMask);
            bool[] hitThisFrame = new bool[overlap.Length];
            if(overlap != null && overlap.Length > 0)
            {
                for (int i = 0; i < overlap.Length; i++)
                {
                    Collider c = overlap[i];
                    Vector3 closest = c.ClosestPoint(transform.position);
                    if (Physics.Linecast(transform.position, closest ,out RaycastHit hit, losLayerMask))
                    {
                        //If we hit this collider with the raycast
                        if(c.TryGetComponent(out Damageable d) && hit.collider == c && !hitThisFrame[i])
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

        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, damageMaxRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, damageDropOffCurve.Evaluate(0.5f) * damageMaxRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, damageDropOffCurve.Evaluate(1) * damageMaxRange);
    }
}
