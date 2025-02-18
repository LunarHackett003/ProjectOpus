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
        public override void OFixedUpdate()
        {
            base.OFixedUpdate();

        }

        protected virtual void TryPrimary()
        {

        }
        protected virtual void TrySecondary()
        {

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
