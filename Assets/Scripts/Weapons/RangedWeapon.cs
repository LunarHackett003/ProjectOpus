using Netcode.Extensions;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Opus
{

    public class RangedWeapon : BaseWeapon
    {
        public string primaryAnimatorKey;
        protected int primaryAnimatorHash;
        public string secondaryAnimatorKey;
        protected int secondaryAnimatorHash;
        public string aimAmountKey;
        protected int aimAmountHash;
        public string reloadKey;
        protected int reloadHash;
        public VisualEffect muzzleFlash;


        protected bool fireCooldown;
        protected bool firePressed;
        [SerializeField] protected bool canAutoFire;
        [SerializeField] protected bool recockAfterShots;
        [SerializeField] protected int recockShotsRequired;
        [SerializeField] protected int shotsFiredForRecock;
        [SerializeField] protected float timeBetweenShots;
        public bool UseAmmo => maxAmmo != 0;
        public NetworkVariable<int> currentAmmunition = new(writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] protected int maxAmmo;
        public int MaxAmmo => maxAmmo;
        public ProjectileModule projectileModule;
        [SerializeField] protected Transform projectileOrigin;
        bool playingRecockAnimation;
        public bool useCountedReload;
        public bool useRecockAnimation;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                currentAmmunition.Value = MaxAmmo;
            }


            primaryAnimatorHash = Animator.StringToHash(primaryAnimatorKey);
            secondaryAnimatorHash = Animator.StringToHash(secondaryAnimatorKey);
            reloadHash = Animator.StringToHash(reloadKey);
            aimAmountHash = Animator.StringToHash(aimAmountKey);
        }
        public virtual void ReloadWeapon()
        {
            if (IsServer)
            {   
                currentAmmunition.Value = maxAmmo;
            }
            RecockWeapon();
        }
        public override void SwitchToWeapon()
        {
            base.SwitchToWeapon();

            if(playingRecockAnimation && shotsFiredForRecock != 0 && manager is PlayerWeaponManager p)
            {
                p.Animator.SetTrigger("Recock");
                animator.SetTrigger("Recock");
            }
        }
        public void RecockWeapon()
        {
            shotsFiredForRecock = 0;
            playingRecockAnimation = false;
        }
        protected virtual void FixedUpdate()
        {
            if (PrimaryInput)
            {
                if (!fireCooldown && (canAutoFire || !firePressed) && (!recockAfterShots || shotsFiredForRecock < recockShotsRequired) && (!UseAmmo || currentAmmunition.Value > 0))
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
                    shotsFiredForRecock++;
                    firePressed = true;
                }
            }
            else
            {
                firePressed = false;
            }
        }
        protected IEnumerator ReturnObjectToNetworkPool(NetworkObject n)
        {
            yield return new WaitForSecondsRealtime(projectileModule.projectileExpireDestroyTime);
            n.Despawn(false);

            if (n.TryGetComponent(out TrailRenderer t))
            {
                t.Clear();
            }
            NetworkObjectPool.Singleton.ReturnNetworkObject(n, projectileModule.projectilePrefab);
        }
        protected IEnumerator SetFireCooldown()
        {
            fireCooldown = true;
            yield return new WaitForSeconds(timeBetweenShots);
            fireCooldown = false;
            yield break;
        }

        public override void AttackServer(float damage)
        {
            base.AttackServer(damage);
            currentAmmunition.Value--;
        }
        [Rpc(SendTo.NotOwner)]
        protected void ClientAttack_RPC()
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
        [Rpc(SendTo.Everyone)]
        /// <summary>
        /// Presumably, where sounds will be played from.
        /// </summary>
        /// <param name="vec"></param>
        public virtual void HitEffectsAtPosition_RPC(Vector3 vec)
        {

        }
    }
}
