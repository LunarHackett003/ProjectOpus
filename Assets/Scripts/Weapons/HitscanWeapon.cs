using UnityEngine;

namespace Opus
{
    public class HitscanWeapon : RangedWeapon
    {
        [SerializeField, Tooltip("X is minimum range (Dropoff start) and Y is Maximum range (dropoff end)")] protected Vector2 range, damageAtRange;
        [SerializeField, Tooltip("Sometimes, shooting downwards can cause the player to hit their own feet/legs." +
            "\nTo counter this problem, we use SpherecastAll, checking that what we hit was NOT our player.")] protected int maxCastEntries = 10;
        [SerializeField, Tooltip("How thick is the faux bullet, in metres")] protected float bulletDiameter = 0.005f;
        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);
            RaycastHit[] hits = new RaycastHit[maxCastEntries];
            if (Physics.SphereCastNonAlloc(manager.attackOrigin.position, bulletDiameter, manager.attackOrigin.forward, hits, range.y, MatchController.Instance.damageLayermask, QueryTriggerInteraction.Ignore) > 0)
            {
                float closestItem = range.y + 1;
                int closestIndex = -1;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null)
                        continue;
                    

                    if (manager.transform != hits[i].transform.root && closestItem > hits[i].distance)
                    {
                        print("New closest found");
                        closestItem = hits[i].distance;
                        closestIndex = i;
                    }
                }
                if(closestIndex == -1)
                {
                    Debug.Log($"did not hit any valid targets", this);
                    return;
                }
                Collider hitCollider = hits[closestIndex].collider;
                float dmg = Mathf.Lerp(damageAtRange.x, damageAtRange.y, Mathf.InverseLerp(range.x, range.y, hits[closestIndex].distance));
                if(hitCollider.TryGetComponent(out Fragment f))
                {
                    f.HitFragment(dmg);
                    Debug.Log($"Hit fragment for {dmg} damage.");
                    Debug.DrawLine(manager.attackOrigin.position, hits[closestIndex].point , Color.blue, 1f);
                    return;
                }
                else if(hitCollider.TryGetComponent(out Entity e))
                {
                    Debug.Log($"hit entity {e.name} for {dmg} damage", this);
                    Debug.DrawLine(manager.attackOrigin.position, hits[closestIndex].point, Color.green, 1f);
                }
            }
            else
            {
                Debug.DrawRay(manager.attackOrigin.position, manager.attackOrigin.forward * range.y, Color.red, 1f);
                Debug.Log($"did not hit anything", this);
                return;
            }
        }
        public override void AttackClient()
        {
            base.AttackClient();
        }
    }
}
