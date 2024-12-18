using UnityEngine;

namespace Opus
{
    public class MeleeWeapon : BaseWeapon
    {
        public float attackResetTime;
        bool primaryPressed;
        bool secondaryPressed;
        bool resettingAttack;
        float currentAttackResetTime;

        public float secondaryChargeTime;
        float currentSecondaryCharge;

        public MeleeBehaviour primaryBehaviour, secondaryBehaviour;
        public bool releaseChargeWhenFull;
        public Vector3 meleeSweepBounds, meleeSweepOffset;
        public Transform meleeSweepOrigin;

        Vector3 lastSweepPos, currentSweepPos;
        int currentSweepTicks;
        bool attackSweeping;
        int currentAttackIndex;
        public LayerMask meleeLayermask;
        public int[] attackDamages = new int[2] {15, 45};


        public const string PrimaryKey = "PrimaryAttack", SecondaryKey = "SecondaryAttack";

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fireInput)
            {
                TryPrimary();
            }
            else
            {
                currentAttackResetTime += Time.fixedDeltaTime;
                if(currentAttackResetTime >= attackResetTime)
                {
                    resettingAttack = false;
                    netAnimator.ResetTrigger(PrimaryKey);
                }
                else
                {
                    resettingAttack = true;
                }
                primaryPressed = false;
            }
            
            if (secondaryInput)
            {
                TrySecondary();
            }
            else
            {
                currentSecondaryCharge -= Time.fixedDeltaTime;
                currentSecondaryCharge = Mathf.Clamp(currentSecondaryCharge, 0, secondaryChargeTime);
                float charge = Mathf.InverseLerp(0, secondaryChargeTime, currentSecondaryCharge);
                netAnimator.Animator.SetFloat(SecondaryKey, charge);
                myController.networkAnimator.Animator.SetFloat(SecondaryKey, charge);
            }
            if (attackSweeping)
            {
                RaycastHit[] hits = Physics.BoxCastAll(meleeSweepOrigin.TransformPoint(meleeSweepOffset), meleeSweepBounds/2, lastSweepPos - currentSweepPos, meleeSweepOrigin.rotation, 1, meleeLayermask);
                if(hits.Length > 0)
                {
                    for(int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit hit = hits[i];
                        if (hit.rigidbody)
                        {
                            if(hit.rigidbody.TryGetComponent(out Entity ent))
                            {
                                ent.ReceiveDamage(attackDamages[currentAttackIndex], OwnerClientId);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void TryPrimary()
        {
            switch (primaryBehaviour)
            {
                case MeleeBehaviour.none:

                    break;
                case MeleeBehaviour.pressAttack:
                    if (!primaryPressed)
                    {

                        primaryPressed = true;
                    }
                    break;
                case MeleeBehaviour.holdAttack:
                    netAnimator.SetTrigger("PrimaryAttack");
                    myController.networkAnimator.SetTrigger("PrimaryAttack");
                    break;
                case MeleeBehaviour.chargeAttack:
                    //Ignore this one. There are currently no charged primary attacks.
                    break;
                default:
                    break;
            }
        }
        protected virtual void TrySecondary()
        {
            if (secondaryBehaviour == MeleeBehaviour.chargeAttack)
            {
                if (currentSecondaryCharge >= secondaryChargeTime)
                {
                    netAnimator.SetTrigger(SecondaryKey+"Fire");
                }
            }
        }
        public void TryAttack()
        {
            if (!IsServer)
                return;

        }
        public void TrySecondaryAttack()
        {
            if (!IsServer)
                return;
        }

        public void StartAttackSweep(int attackIndex)
        {
            lastSweepPos = meleeSweepOrigin.position;
            currentSweepPos = lastSweepPos;
            attackSweeping = true;
        }
        public void EndAttackSweep()
        {
            attackSweeping = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (meleeSweepOrigin)
            {
                Gizmos.matrix = meleeSweepOrigin.localToWorldMatrix;
                Gizmos.color = new(.5f, .3f, 0.1f, .4f);
                Gizmos.DrawCube(meleeSweepOffset, meleeSweepBounds);
            }
        }
    }
}
