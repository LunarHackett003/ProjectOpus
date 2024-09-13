using UnityEngine;

namespace Opus
{
    public class MeleeWeapon : BaseWeapon
    {
        public override void AttackClient()
        {
            base.AttackClient();
        }
        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);

            Collider[] cols = new Collider[maxHits];
            int hits = Physics.OverlapSphereNonAlloc(manager.attackOrigin.TransformPoint(attackOffset), attackRadius, cols, MatchController.Instance.damageLayermask);
            if (hits > 0)
            {
                for (int i = 0; i < hits; i++)
                {
                    if (cols[i].TryGetComponent(out Damageable d))
                    {
                        d.TakeDamage(damage);
                    }
                }
            }
        }

        public Vector3 attackOffset;
        public int maxHits;
        int currentAttackIndex;
        public float attackRadius, primaryDamage, secondaryDamage;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(manager is PlayerWeaponManager p)
            {
                p.PlayerAnimator.onPrimaryWeaponHit += PrimaryHit;
                p.PlayerAnimator.onSecondaryWeaponHit += SecondaryHit;
            }
        }
        void PrimaryHit(int increment)
        {
            if (IsServer)
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
            if (IsServer)
            {
                AttackServer(secondaryDamage);
            }
            if (IsOwner)
            {
                currentAttackIndex = PrimaryInput ? (currentAttackIndex + increment) % animationModule.attackAnimationCount : 0;
            }
            AttackClient();
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
            Gizmos.DrawWireSphere(transform.position + attackOffset, attackRadius );
        }
    }
}
