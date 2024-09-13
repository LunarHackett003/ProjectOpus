using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Opus
{
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
            if(PrimaryInput)
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
                else
                {
                    firePressed = false;
                }
            }
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
