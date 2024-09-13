using Netcode.Extensions;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Opus
{
    public struct PretendProjectile
    {
        public Transform projectile;
        public float timeAlive;
        public Vector3 velocity;
        public Vector3 lastPosition;
        public int bouncesLeft;
    }
    public class RangedWeapon : BaseWeapon
    {
        public string primaryAnimatorKey;
        int primaryAnimatorHash;
        public string secondaryAnimatorKey;
        int secondaryAnimatorHash;
        public string aimAmountKey;
        int aimAmountHash;
        public string reloadKey;
        int reloadHash;
        public VisualEffect muzzleFlash;

        bool fireCooldown;
        bool firePressed;
        [SerializeField] protected bool canAutoFire;
        [SerializeField] protected float timeBetweenShots;



        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected GameObject projectileHitPrefab;
        [SerializeField] protected float hitPrefabDespawnTime;
        [SerializeField] protected float projectileExpireDestroyTime = 3;
        [SerializeField] protected bool useProjectileDamage;
        [SerializeField] protected float maxProjectileLifetime = 5;
        [SerializeField] protected bool spawnHitPrefabOnExpire;
        [SerializeField] protected float projectileFireSpeed;
        [SerializeField] protected int maxBounces;
        [SerializeField] protected float minBounceAlignment;
        [SerializeField] protected float projectileDrag = 0;
        [SerializeField] protected float projectileGravityModifier = 1;
        [SerializeField] protected bool bounceOnDamageable = false;
        [SerializeField] protected float bounciness = 0.7f;
        [SerializeField] protected Transform projectileOrigin;
        [SerializeField, Tooltip("X is minimum range (Dropoff start) and Y is Maximum range (dropoff end)")] protected Vector2 range, damageAtRange;
        [SerializeField, Tooltip("Sometimes, shooting downwards can cause the player to hit their own feet/legs." +
            "\nTo counter this problem, we use SpherecastAll, checking that what we hit was NOT our player.")]
        protected int maxCastEntries = 10;
        [SerializeField, Tooltip("How thick is the faux bullet, in metres")] protected float projectileRadius = 0.0025f;


        List<PretendProjectile> projectiles = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            primaryAnimatorHash = Animator.StringToHash(primaryAnimatorKey);
            secondaryAnimatorHash = Animator.StringToHash(secondaryAnimatorKey);
            reloadHash = Animator.StringToHash(reloadKey);
            aimAmountHash = Animator.StringToHash(aimAmountKey);
        }
        private void FixedUpdate()
        {

            //Simulate projectiles before we add any new ones.
            if (IsServer)
                ProjectileSimulate();
            if (PrimaryInput)
            {
                if (!fireCooldown && (canAutoFire || !firePressed))
                {
                    if (IsServer)
                    {
                        AttackServer(0);
                        ClientAttack_RPC();
                    }
                    if (IsOwner)
                    {
                        AttackClient();
                        if(manager is PlayerWeaponManager p)
                        {
                            p.Animator.SetTrigger(primaryAnimatorHash);
                        }
                    }
                    StartCoroutine(SetFireCooldown());
                    firePressed = true;
                }
            }
            else
            {
                firePressed = false;
            }

        }
        bool CanBounce(Vector3 directionIn, Vector3 normal)
        {
            return Vector3.Dot(directionIn.normalized, normal) > minBounceAlignment;
        }
        PretendProjectile TryBounce(PretendProjectile p, RaycastHit hit, bool damageable)
        {
            if (p.bouncesLeft > 0 && (!damageable || bounceOnDamageable) && CanBounce(p.velocity, hit.normal))
            {
                p.bouncesLeft--;
                p.velocity = Vector3.Reflect(p.velocity, hit.normal) * bounciness;
            }
            else
            {
                p.timeAlive = maxProjectileLifetime;
                if(projectileHitPrefab != null)
                {
                    StartCoroutine(ReturnObjectToNetworkPool(NetworkObject.InstantiateAndSpawn(projectileHitPrefab, NetworkManager, OwnerClientId, position: hit.point)));
                }
            }
            if(hit.collider.TryGetComponent(out Damageable d))
            {
                d.TakeDamage(Mathf.Lerp(damageAtRange.x, damageAtRange.y, Mathf.InverseLerp(range.x, range.y, p.timeAlive)));
            }
            p.projectile.position = hit.point;
            return p;
        }
        IEnumerator ReturnObjectToNetworkPool(NetworkObject n)
        {
            yield return new WaitForSecondsRealtime(projectileExpireDestroyTime);
            n.Despawn(false);

            if (n.TryGetComponent(out TrailRenderer t))
            {
                t.Clear();
            }
            NetworkObjectPool.Singleton.ReturnNetworkObject(n, projectilePrefab);
        }
        void ProjectileSimulate()
        {
            for (int i = projectiles.Count -1; i > -1; i--)
            {
                PretendProjectile p = projectiles[i];

                if(p.projectile != null && p.timeAlive >= maxProjectileLifetime)
                {
                    StartCoroutine(ReturnObjectToNetworkPool(p.projectile.GetComponent<NetworkObject>()));
                    projectiles.RemoveAt(i);
                    continue;
                }
                else
                {
                    p.velocity = NextVelocity(p);
                    int projectileResult = ProjectileCheck(out RaycastHit hit, p.lastPosition, p.velocity * Time.fixedDeltaTime);
                    if (projectileResult != -1)
                    {
                        p = TryBounce(p, hit, projectileResult == 1);
                    }
                    else
                    {
                        p.projectile.position += p.velocity * Time.fixedDeltaTime;
                        p.timeAlive += Time.fixedDeltaTime;
                    }
                    p.lastPosition = p.projectile.position;
                    projectiles[i] = p;
                }
            }
        }
        Vector3 NextVelocity(PretendProjectile p)
        {
            return (p.velocity + (0.5f * projectileGravityModifier * Time.fixedDeltaTime * Physics.gravity)) / (1 + (projectileDrag * Time.fixedDeltaTime));
        }
        /// <summary>
        /// performs a SpherecastAll to check for objects that we might hit.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns>-1 if no hit<br></br>0 if we hit non-damageable<br></br>1 if hit fragment<br></br>2 if hit entity</returns>
        public int ProjectileCheck(out RaycastHit hit, Vector3 origin, Vector3 direction)
        {
            RaycastHit[] hits = new RaycastHit[maxCastEntries];
            if (Physics.SphereCastNonAlloc(origin, projectileRadius, direction.normalized, hits, direction.magnitude, MatchController.Instance.damageLayermask) > 0)
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
                if (closestIndex == -1)
                {
                    Debug.Log($"did not hit any valid targets", this);
                    hit = new();
                    return -1;
                }
                Collider hitCollider = hits[closestIndex].collider;
                hit = hits[closestIndex];
                Debug.DrawLine(origin, hit.point, Color.green, 1f, false);
                return hitCollider.TryGetComponent(out Damageable d) ? 1 : 0;
            }
            else
            {
                //compile pls
                Debug.DrawRay(origin, direction, Color.red, 1f, false);
                Debug.Log($"did not hit anything", this);
                hit = new();
            }
            return -1;
        }
        IEnumerator SetFireCooldown()
        {
            fireCooldown = true;
            yield return new WaitForSeconds(timeBetweenShots);
            fireCooldown = false;
            yield break;
        }

        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);
            PretendProjectile p = new()
            {
                projectile = NetworkObject.InstantiateAndSpawn(projectilePrefab, NetworkManager, OwnerClientId, position: projectileOrigin.position).transform,
                lastPosition = projectileOrigin.position,
                timeAlive = 0,
                velocity = manager.attackOrigin.forward * projectileFireSpeed
            };
            projectiles.Add(p);

        }
        [Rpc(SendTo.NotOwner)]
        void ClientAttack_RPC()
        {

            AttackClient();
        }
        public override void AttackClient()
        {
            base.AttackClient();
            if (muzzleFlash != null)
            {
                muzzleFlash.SendEvent("Fire");
            }
        }
    }
}
