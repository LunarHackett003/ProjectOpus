using Netcode.Extensions;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
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
        [Tooltip("How much the first shot is delayed by")]
        public float delayBeforeFire;
        [Tooltip("How many times the weapon fires in a burst")]
        public int burstShotCount;
        [Tooltip("How long, in seconds, the weapon waits between bursts before allowing to fire again")]
        public float burstCooldown;
        public bool UseBurst => burstShotCount > 0;
        public bool UseFireDelay => delayBeforeFire > 0;
        bool delayDone;
        bool burstFiring;
        int currentBurstCount;
        public bool useAim;
        public bool canAimReload;
        public float aimSpeed;
        public Vector3 aimViewPosition, aimLocalWeaponPos, aimRemoteWeaponPos;
        public Quaternion localAimOffsetRotation = Quaternion.identity, remoteAimOffsetRotation = Quaternion.identity;
        public float aimAmount;
        [Tooltip("How much of the ")] public float aimedRotationInfluence;

        [SerializeField] protected ParticleSystem roundEjectSystem;
        
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
        public virtual void EjectRound()
        {
            if(roundEjectSystem != null)
                roundEjectSystem.Play();
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
                    PreFire();
                }
            }
            else
            {
                delayDone = false;
                firePressed = false;
            }

            if (useAim)
            {
                bool canAim = useAim;
                if(manager is PlayerWeaponManager p)
                {
                    canAim &= (p.PlayingReloadAnimation && canAimReload) || !p.PlayingReloadAnimation;
                }
                if (canAim && SecondaryInput)
                {
                    aimAmount = aimAmount >= 1 ? 1 : Mathf.Clamp(aimAmount + (Time.fixedDeltaTime * aimSpeed), 0, 1);
                }
                else 
                {

                    aimAmount = aimAmount <= 0 ? 0 : Mathf.Clamp(aimAmount - (Time.fixedDeltaTime * aimSpeed), 0, 1);
                }
            }
        }
        protected virtual void PreFire()
        {
            if (!UseFireDelay || delayDone)
            {
                CheckBurst();
            }
            else
            {
                StartCoroutine(FireDelay());
            }
            StartCoroutine(SetFireCooldown());
            shotsFiredForRecock++;
            firePressed = true;
        }
        IEnumerator FireDelay()
        {
            delayDone = true;
            yield return new WaitForSeconds(delayBeforeFire);
            if (UseBurst)
            {
                CheckBurst();
            }
        }
        void CheckBurst()
        {
            if (!UseBurst)
            {
                PostFire();
            }
            else if (!burstFiring)
            {
                StartCoroutine(BurstFire());
            }
        }
        IEnumerator BurstFire()
        {
            burstFiring = true;
            while (currentBurstCount < burstShotCount && currentAmmunition.Value > 0)
            {
                PostFire();
                currentBurstCount++;
                if(currentBurstCount == burstShotCount - 1)
                {
                    yield return new WaitForSeconds(burstCooldown);
                }
                else
                {
                    yield return new WaitForSeconds(timeBetweenShots);
                }
            }
            if (!canAutoFire)
            {
                while (PrimaryInput)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            burstFiring = false;
            currentBurstCount = 0;
            yield break;
        }
        public virtual void PostFire()
        {
            if (IsServer)
            {
                AttackServer(0);
                ClientAttack_RPC();
            }
            if (IsOwner)
            {
                AttackClient();
                if (manager is PlayerWeaponManager p)
                {
                    p.Animator.SetTrigger(primaryAnimatorHash);
                    animator.SetTrigger(primaryAnimatorHash);
                }
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
        public override void AttackClient(bool secondaryAttack = false)
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
