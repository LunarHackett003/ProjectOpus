using FMODUnity;
using opus.Weapons;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class BaseWeapon : BaseEquipment
{
    public bool semiFired;
    public bool rofLimited;
    [SerializeField, Tooltip("How much ammo this weapon has by default")] internal int maxAmmo = 30;
    [SerializeField] internal NetworkVariable<int> currentAmmo = new(writePerm:NetworkVariableWritePermission.Server);
    [SerializeField, Tooltip("Does this weapon use ammo?")] internal bool useAmmo;

    [SerializeField] protected UnityEvent fireEvents;

    [SerializeField] internal RecoilProfile recoilProfile;

    [SerializeField] internal EventSequence reloadSequence;

    [SerializeField] EventReference gunshotReference;

    public enum FireMode
    {
        /// <summary>
        /// Fires once and must be reset by an animation
        /// </summary>
        single = 0,
        /// <summary>
        /// Fires once and is reset when releasing the fire button
        /// </summary>
        semi = 1,
        /// <summary>
        /// Fires a set number of rounds and then waits for the player to release the button, or for a configurable delay.
        /// </summary>
        burst = 2,
        /// <summary>
        /// Fires until the player releases the input
        /// </summary>
        auto = 3
    }
    public string FireAnimatorParamName = "Fire";
    [SerializeField, Tooltip("How many rounds are fired every minute")] protected float roundsPerMinute;
    [SerializeField, Tooltip("How many rounds are fired every second")] protected float roundsPerSecond;
    [SerializeField, Tooltip("The delay between each shot")] protected float timeBetweenRounds;
    [SerializeField] protected bool canFire;
    [SerializeField] protected FireMode[] allowedFireModes;
    [SerializeField] protected int fireModeIndex;
    [SerializeField] protected int roundsInBurst;
    [SerializeField] protected float delayAfterBurst;

    [SerializeField] protected float delayBeforeFire;
    [SerializeField] protected bool playFireAnimationOnDelay;
    [SerializeField] protected bool playSoundOnDelay;
    [SerializeField] protected Vector2 minHipSpread, maxHipSpread;
    [SerializeField] protected Vector2 minBaseSpread, maxBaseSpread;

    [SerializeField] internal float aimedWorldFOV = 70, aimedViewFOV = 30;

    [SerializeField] internal Vector3 aimPosition;

    [SerializeField] bool playerWeapon = true;
    public NetworkVariable<NetworkBehaviourReference> ownerWeaponManager;
    internal WeaponManager wm;
    public int CurrentAmmo => currentAmmo.Value;
    protected bool CanFireWeapon => (!useAmmo || (useAmmo && currentAmmo.Value > 0)) && !semiFired && !rofLimited;

    [SerializeField] internal float reloadTime = 2;
    //Sends the fire stuff to everyone
    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void FireWeapon_RPC(Vector3 end)
    {
        print("received remote fire from a client");
        FireWeapon(end);
    }
    void ResetROFLimit()
    {
        rofLimited = false;
    }
    private void OnValidate()
    {
        roundsPerSecond = roundsPerMinute / 60;
        timeBetweenRounds = 1 / roundsPerSecond;
    }
    protected virtual void FireWeaponOnServer(NetworkObject ownerObject)
    {
        if (useAmmo)
        {
            currentAmmo.Value--;
        }
        if (!CheckWeaponManager())
            return;
    }
    public override void OnSelected()
    {
        base.OnSelected();
        if(wm && wm.animHelper)
        {
            wm.animHelper.UpdateAnimationsFromEquipment_RPC(this);
        }
    }
    protected override void Start()
    {
        delayDone = true;
    }
    protected bool CheckWeaponManager()
    {
        if (!wm && playerWeapon)
        {
            if (ownerWeaponManager.Value.TryGet(out WeaponManager wm))
            {
                this.wm = wm;
            }
        }
        return wm || !playerWeapon;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!CheckWeaponManager())
            return;

        canFire = CanFireWeapon && (!useAmmo || currentAmmo.Value > 0);
        if (IsServer || IsOwner)
        {
            FireMode fm = allowedFireModes[fireModeIndex];
            switch (fm)
            {
                case FireMode.single:
                    PlayerFireLogic();
                    break;
                case FireMode.semi:
                    if (primaryInput.Value)
                    {
                        if (canFire)
                            PlayerFireLogic();
                        semiFired = true;
                    }
                    else
                    {
                        semiFired = false;
                    }
                    break;
                case FireMode.burst:
                    if (primaryInput.Value && canFire)
                        StartCoroutine(BurstFire());
                    break;
                case FireMode.auto:
                    if (primaryInput.Value && canFire)
                    {
                        PlayerFireLogic();
                    }
                    break;
                default:
                    break;
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    protected void PlayerFireLogic()
    {
        if (IsServer)
        {
            if(delayBeforeFire > 0)
            {
                if (delayDone)
                {
                    print("firing with delay");
                    StartCoroutine(DelayFire());
                }
            }
            else
            {
                if (wm)
                {
                    FireWeaponOnServer(wm.NetworkObject);
                }
                else
                {
                    FireWeaponOnServer(NetworkObject);
                }
            }
        }
        if (IsOwner)
        {
            wm.CancelReload();
            if(delayBeforeFire > 0)
            {
                if(delayDone)
                    StartCoroutine(DelayFire());
            }
            else
            {
                rofLimited = true;
                Invoke(nameof(ResetROFLimit), timeBetweenRounds / GameplayManager.Instance.fireRateMultiplier.Value);
                if (!playFireAnimationOnDelay)
                    networkAnimator.SetTrigger(FireAnimatorParamName);
            }
        }
    }
    public IEnumerator BurstFire()
    {
        int b = 0;
        semiFired = true;
        while (b < roundsInBurst && currentAmmo.Value > 0)
        {
            b++;
            if (IsServer)
                FireWeaponOnServer(wm? wm.NetworkObject : NetworkObject);
            yield return new WaitForSeconds(timeBetweenRounds);
        }
        yield return new WaitForSeconds(delayAfterBurst);
        semiFired = false;
    }
    bool delayDone;

        [SerializeField, Tooltip("How much damage this weapon does at the dropoff start")] protected float maxDamage = 20;

        [SerializeField, Tooltip("How much damage this weapon does at the dropoff end")] protected float minDamage = 1;

        [SerializeField, Tooltip("How much the damage is multiplied by on a headshot")] protected float headshotDamageMultiplier;

        [SerializeField, Tooltip("The distance in metres before which the weapon deals max damage")] protected float damageDropoffStart = 10;

        [SerializeField, Tooltip("The distance in metres after which the weapon deals min damage")] protected float damageDropoffEnd = 100;
    public IEnumerator DelayFire()
    {
        delayDone = false;
        if (playFireAnimationOnDelay && IsOwner)
        {
            wm.pc.netAnimator.SetTrigger(FireAnimatorParamName);
            networkAnimator.SetTrigger(FireAnimatorParamName);
        }
        if (playSoundOnDelay)
        {
            PlayGunshot(true);
        }
        yield return new WaitForSeconds(delayBeforeFire / GameplayManager.Instance.fireRateMultiplier.Value);
        if (IsServer)
        {
            FireWeaponOnServer(wm ? wm.NetworkObject : NetworkObject);
            rofLimited = true;
        }
        if (IsOwner)
        {
            if (!playFireAnimationOnDelay)
            {
                wm.pc.netAnimator.SetTrigger(FireAnimatorParamName);
                networkAnimator.SetTrigger(FireAnimatorParamName);
                rofLimited = true;
            }
        }
        Invoke(nameof(ResetROFLimit), timeBetweenRounds / GameplayManager.Instance.fireRateMultiplier.Value);
        delayDone = true;
    }
    void PlayGunshot(bool sendRPC)
    {
        if (gunshotReference.IsNull)
            return;

        RuntimeManager.PlayOneShot(gunshotReference, transform.position);
        if (sendRPC)
        {
            PlayGunshot_RPC();
        }
    }
    [Rpc(SendTo.NotMe)]
    void PlayGunshot_RPC()
    {
        PlayGunshot(false);
    }
    public virtual void FireWeapon(Vector3 end)
    {
        print("fired weapon locally");
        fireEvents?.Invoke();
        if (!playSoundOnDelay)
            PlayGunshot(false);
        if(IsOwner && wm && wm.pc)
        {
            if(wm.pc.netAnimator && !playFireAnimationOnDelay)
            {
                wm.pc.netAnimator.SetTrigger(FireAnimatorParamName);
            }
            wm.ReceiveRecoil();
            if(storedUses > 0)
            {
                currentStoredUses.Value--;
                CheckStillUsable();
            }
        }
    }
    protected Vector3 SpreadVector(Vector2 min, Vector2 max, float z)
    {
        Random.InitState(Time.frameCount);
        Vector2 randomCircle = Random.insideUnitCircle;
        return new()
        {
            x = Mathf.Lerp(min.x, max.x, randomCircle.x),
            y = Mathf.Lerp(min.y, max.y, randomCircle.y),
            z = z
        };
    }
        public void UpdateFireMode()
        {
            fireModeIndex++;
            fireModeIndex %= allowedFireModes.Length;
            SetFireModeIndex_ServerRPC(fireModeIndex);
        }
        

        [ServerRpc()]
        void SetFireModeIndex_ServerRPC(int fireModeIndex)
        {
            this.fireModeIndex = fireModeIndex;
        }
}
