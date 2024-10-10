using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;

namespace Opus
{
    public struct HitscanTracer
    {
        public Transform tracer;
        public float lerp;
        public float distancePerTick;
        public Vector3 start, end;
    }


    public class HitscanWeapon : RangedWeapon
    {
        public int hitscanIterations = 10;
        List<HitscanTracer> tracers = new();
        public float tracerSpeed;
        protected override void FixedUpdate()
        {
            UpdateTracers();
            base.FixedUpdate();
        }

        void UpdateTracers()
        {
            if (tracers.Count == 0)
                return;

            for (int i = tracers.Count - 1; i >= 0; i--)
            {
                HitscanTracer t = tracers[i];
                t.lerp += Time.fixedDeltaTime * t.distancePerTick;
                if (t.lerp <1)
                {
                    t.tracer.position = Vector3.Lerp(t.start, t.end, t.lerp);
                }
                else
                {
                    t.tracer.position = t.end;
                    StartCoroutine(ReturnObjectToNetworkPool(t.tracer.GetComponent<NetworkObject>()));
                    tracers.RemoveAt(i);
                    return;
                }
                tracers[i] = t;
            }
        }

        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);
            HitscanTracer[] tracers = new HitscanTracer[projectileModule.projectilesPerShot];
            for (int i = 0; i < tracers.Length; i++)
            {
                
                Vector3 fireVector = manager.attackOrigin.TransformDirection((Vector3.forward * projectileModule.projectileFireSpeed) + (Vector3)(Random.insideUnitCircle * projectileModule.baseProjectileDeviation));
                float fv_magnitude = fireVector.magnitude;
                HitscanTracer t = new()
                {
                    start = projectileOrigin.position,
                    tracer = NetworkObject.InstantiateAndSpawn(networkPrefab: projectileModule.projectilePrefab, networkManager: NetworkManager,
                    ownerClientId: OwnerClientId, position: projectileOrigin.position).transform,
                };
                if(Hitscan(manager.attackOrigin.position, fireVector.normalized, fv_magnitude, out RaycastHit hit, out Damageable d))
                {
                    if (d != null)
                    {
                        float dmg = Mathf.Lerp(projectileModule.damageAtRange.x, projectileModule.damageAtRange.y, Mathf.InverseLerp(projectileModule.range.x, projectileModule.range.y, hit.distance));
                        d.TakeDamage(dmg);
                    }
                    t.distancePerTick = tracerSpeed / Vector3.Distance(manager.attackOrigin.position, hit.point);
                    t.end = hit.point;
                }
                else
                {
                    t.end = manager.attackOrigin.position + fireVector;
                    t.distancePerTick = tracerSpeed / Vector3.Distance(manager.attackOrigin.position, t.end);
                }
                tracers[i] = t;
            }
            this.tracers.AddRange(tracers);
        }
        public override void AttackClient(bool secondaryAttack = false)
        {
            base.AttackClient();
        }
        public bool Hitscan(Vector3 origin, Vector3 directionNormalised, float distance, out RaycastHit hit, out Damageable d)
        {
            RaycastHit[] hits = new RaycastHit[hitscanIterations];
            if (projectileModule.projectileRadius > 0)
            {
                Physics.SphereCastNonAlloc(origin, projectileModule.projectileRadius, directionNormalised, hits, distance, MatchController.Instance.damageLayermask);
            }
            else
            {
                Physics.RaycastNonAlloc(origin, directionNormalised, hits, distance, MatchController.Instance.damageLayermask);
            }
            int closestIndex = -1;
            float closestDistance = 9999999;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].distance == 0)
                {
                    if(doLogging)
                        print("invalid hit, continuing");
                    continue;
                }
                if (hits[i].distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = hits[i].distance;
                }
            }
            if (closestIndex != -1)
            {
                hit = hits[closestIndex];
                if (hit.collider.TryGetComponent(out d) && d.NetworkObject == manager.NetworkObject)
                {
                    //we hit ourself, likely a hitbox or something
                    if (doLogging)
                    {
                        Debug.Log("Hit self", this);
                        Debug.DrawLine(origin, hit.point, Color.yellow, 1f, false);
                    }
                    return false;
                }
                if (doLogging)
                {
                    Debug.DrawLine(origin, hit.point, Color.green, 1f, false);
                    Debug.Log("Hit other", this);
                }
                return true;
            }
            else
            {
                if (doLogging)
                {
                    Debug.Log("hit nothing", this);
                    Debug.DrawLine(origin, origin + directionNormalised * distance, Color.red, 1f, false);
                }
                hit = new();
                d = null;
                return false;
            }
        }
    }
}
