using System.Collections.Generic;
using UnityEngine;
using Netcode;
using Netcode.Extensions;
using System.Collections;
using Unity.Netcode;
namespace Opus
{
    public struct PretendProjectile
    {
        public Transform projectile;
        public float timeAlive;
        public Vector3 velocity;
        public Vector3 lastPosition;
        public float speed;
        public int bouncesLeft;
    }


    public class ProjectileWeapon : RangedWeapon
    {
        List<PretendProjectile> projectiles = new();
        public int projectileCheckIterations = 4;

        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);

            PretendProjectile[] ps = new PretendProjectile[projectileModule.projectilesPerShot];
            for (int i = 0; i < ps.Length; i++)
            {
                Vector3 fireVector = manager.attackOrigin.TransformDirection((Vector3.forward * projectileModule.projectileFireSpeed) + (Vector3)(Random.insideUnitCircle * projectileModule.baseProjectileDeviation));
                PretendProjectile p = new()
                {
                    projectile = NetworkObject.InstantiateAndSpawn(projectileModule.projectilePrefab, NetworkManager, OwnerClientId, position: projectileOrigin.position).transform,
                    lastPosition = projectileOrigin.position,
                    timeAlive = 0,
                    velocity = fireVector,
                    speed = fireVector.magnitude
                };
                ps[i] = p;
            }
            projectiles.AddRange(ps);
        }
        protected bool CanBounce(Vector3 directionIn, Vector3 normal)
        {
            return Vector3.Dot(directionIn.normalized, normal) > projectileModule.minBounceAlignment;
        }
        protected bool TryBounce(ref PretendProjectile p, RaycastHit hit, bool damageable)
        {
            bool bounced = false;
            if (hit.collider.TryGetComponent(out Damageable d))
            {
                d.TakeDamage(Mathf.Lerp(projectileModule.damageAtRange.x, projectileModule.damageAtRange.y, Mathf.InverseLerp(projectileModule.range.x, projectileModule.range.y, p.timeAlive)));
            }
            if (p.bouncesLeft > 0 && (!damageable || projectileModule.bounceOnDamageable) && CanBounce(p.velocity, hit.normal))
            {
                p.bouncesLeft--;
                p.velocity = Vector3.Reflect(p.velocity, hit.normal) * projectileModule.bounciness;
                bounced = true;
            }
            else
            {
                if (projectileModule.projectileHitPrefab != null)
                {
                    StartCoroutine(ReturnObjectToNetworkPool(NetworkObject.InstantiateAndSpawn(projectileModule.projectileHitPrefab, NetworkManager, OwnerClientId, position: hit.point)));
                }
                p.timeAlive = projectileModule.maxProjectileLifetime;
            }
            p.projectile.position = hit.point;
            return bounced;
        }

        protected void ProjectileSimulate()
        {
            for (int i = projectiles.Count - 1; i > -1; i--)
            {
                PretendProjectile p = projectiles[i];

                if (p.projectile != null && p.timeAlive >= projectileModule.maxProjectileLifetime)
                {
                    StartCoroutine(ReturnObjectToNetworkPool(p.projectile.GetComponent<NetworkObject>()));
                    projectiles.RemoveAt(i);
                }
                else
                {
                    p.velocity = NextVelocity(p);
                    int projectileResult = ProjectileCheck(out RaycastHit hit, p);
                    if(doLogging)
                        print("Projectile result = " + projectileResult);
                    if (projectileResult == -1)
                    {
                        p.projectile.position += p.velocity * Time.fixedDeltaTime;
                        p.timeAlive += Time.fixedDeltaTime;
                    }
                    else
                    {
                        TryBounce(ref p, hit, projectileResult == 1);
                        p.projectile.position = hit.point;
                    }
                    p.lastPosition = p.projectile.position;
                    projectiles[i] = p;
                }
            }
        }
        protected Vector3 NextVelocity(PretendProjectile p)
        {
            return Vector3.ClampMagnitude((p.velocity + (0.5f * projectileModule.projectileGravityModifier * Time.fixedDeltaTime * Physics.gravity))
                   / (1 + (projectileModule.projectileDrag * Time.fixedDeltaTime)), p.speed);
        }
        /// <summary>
        /// performs a SpherecastAll to check for objects that we might hit.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns>-1 if no hit<br></br>0 if we hit non-damageable<br></br>1 if hit fragment<br></br>2 if hit entity</returns>
        public int ProjectileCheck(out RaycastHit hit, PretendProjectile p)
        {
            RaycastHit[] hits = new RaycastHit[projectileCheckIterations];
            if (projectileModule.projectileRadius > 0)
            {
                Physics.SphereCastNonAlloc(p.lastPosition, projectileModule.projectileRadius, p.velocity, hits, p.speed * Time.fixedDeltaTime, MatchController.Instance.damageLayermask);
            }
            else
            {
                Physics.RaycastNonAlloc(p.lastPosition, p.velocity, hits, p.speed * Time.fixedDeltaTime, MatchController.Instance.damageLayermask);
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
            if(closestIndex != -1)
            {
                hit = hits[closestIndex];
                if (hit.collider.TryGetComponent(out Damageable d) && d.NetworkObject == manager.NetworkObject)
                {
                    //we hit ourself, likely a hitbox or something
                    if (doLogging)
                    {
                        Debug.Log("Hit self", p.projectile);
                        Debug.DrawLine(p.lastPosition, hit.point, Color.yellow, 1f, false);
                    }
                        return -1;
                    }
                if (doLogging)
                {
                    Debug.Log("Hit other", p.projectile);
                    Debug.DrawLine(p.lastPosition, hit.point, Color.green, 1f, false);
                }
                return d != null ? 1 : 0;
            }
            else
            {
                if (doLogging)
                {
                    Debug.Log("hit nothing", p.projectile);
                    Debug.DrawLine(p.lastPosition, p.lastPosition + p.velocity, Color.red, 1f, false);
                }
                hit = new();
                return -1;
            }
        }
        protected override void FixedUpdate()
        {
            //Simulate projectiles before adding new ones
            if (IsServer)
                ProjectileSimulate();
            base.FixedUpdate();

        }
    }
}
