using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "New Projectile Module", menuName = "Projectile Module")]
    public class ProjectileModule : ScriptableObject
    {
        public GameObject projectilePrefab;
        public GameObject projectileHitPrefab;
        public float hitPrefabDespawnTime;
        public float projectileExpireDestroyTime = 3;
        public bool useProjectileDamage;
        public float maxProjectileLifetime = 5;
        public bool spawnHitPrefabOnExpire;
        public float projectileFireSpeed;
        public int maxBounces;
        public float minBounceAlignment;
        public float projectileDrag = 0;
        public float projectileGravityModifier = 1;
        public bool bounceOnDamageable = false;
        public float bounciness = 0.7f;
        [Tooltip("X is minimum range (Dropoff start) and Y is Maximum range (dropoff end)")] public Vector2 range, damageAtRange;
        [Tooltip("How thick is the faux projectile, in metres")] public float projectileRadius = 0.0025f;
        [Tooltip("How many projectiles to fire"), Range(1, 30)] public int projectilesPerShot = 1;
        [Tooltip("How much projectiles should deviate at base")] public Vector2 baseProjectileDeviation;
        [Tooltip("How much projectiles should deviate at maximum spread")] public Vector2 maxProjectileDeviation;
    }
}
