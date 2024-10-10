using UnityEngine;

namespace Opus
{
    public class MeleeWeapon : BaseWeapon
    {
        public override void AttackClient(bool secondaryAttack = false)
        {
            base.AttackClient();
        }
        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);
            print("trying to hit...");
            Collider[] cols = new Collider[maxHits];
            int charactersHit = 0;
            int hits = Physics.OverlapSphereNonAlloc(manager.attackOrigin.TransformPoint(attackOffset), attackRadius, cols, MatchController.Instance.damageLayermask);
            if (hits > 0)
            {
                for (int i = 0; i < hits; i++)
                {
                    if (cols[i].TryGetComponent(out Damageable d) && d.NetworkObject != manager.NetworkObject)
                    {
                        if(d is Entity e && charactersHit < maxEntityHits)
                        {
                            print($"Hit entity {e.name} for {damage} dmg");
                            charactersHit++;
                            e.TakeDamage(damage);
                        }
                        else
                        {
                            print($"Hit damagable {d.name} for {damage} dmg");
                            d.TakeDamage(damage);
                        }
                    }
                }
            }
        }

        public Vector3 attackOffset;
        public int maxHits = 20;
        public int maxEntityHits = 3;
        int currentAttackIndex;
        public float attackRadius, primaryDamage, secondaryDamage;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(manager is PlayerWeaponManager p)
            {
                print("subscribing to animation events for melee");
                p.PlayerAnimator.onPrimaryWeaponHit += PrimaryHit;
                p.PlayerAnimator.onSecondaryWeaponHit += SecondaryHit;
            }
        }
        void PrimaryHit(int increment)
        {
            print($"primary hit with {name}");
            if (IsServer || IsHost)
            {
                AttackServer(primaryDamage);
            }
            if (IsOwner)
            {
                currentAttackIndex = PrimaryInput ? (currentAttackIndex + increment) % animationModule.attackAnimationCount : 0;
            }
            AttackClient();
            if (manager is PlayerWeaponManager p)
            {
                p.PlayerAnimator.animator.SetInteger("MeleeIndex", currentAttackIndex);
            }
        }
        void SecondaryHit(int increment)
        {
            print($"secondary hit with {name}");
            if (IsServer || IsHost)
            {
                AttackServer(secondaryDamage);
            }
            if (IsOwner)
            {
                currentAttackIndex = PrimaryInput ? (currentAttackIndex + increment) % animationModule.attackAnimationCount : 0;
            }
            AttackClient(true);
        }
        private void FixedUpdate()
        {
            if (IsOwner)
            {
                if(manager is PlayerWeaponManager p)
                {
                    p.PlayerAnimator.PrimaryMeleeSet(PrimaryInput);
                    p.PlayerAnimator.SecondaryMeleeSet(SecondaryInput);
                }
            }
        }
        private void OnDrawGizmos()
        {
            if (manager)
            {
                Gizmos.DrawWireSphere(manager.attackOrigin.TransformPoint(attackOffset), attackRadius);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position + attackOffset, attackRadius );
            }
        }
    }
}
