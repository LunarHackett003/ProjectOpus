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
    [Rpc(SendTo.ClientsAndHost, DeferLocal = true)]
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
    bool CheckWeaponManager()
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
                if (playFireAnimationOnDelay)
                    networkAnimator.SetTrigger("Fire");
            }
            else
            {
                rofLimited = true;
                Invoke(nameof(ResetROFLimit), timeBetweenRounds / GameplayManager.Instance.fireRateMultiplier.Value);
                if (!playFireAnimationOnDelay)
                    networkAnimator.SetTrigger("Fire");
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
    public IEnumerator DelayFire()
    {
        delayDone = false;
        if (playFireAnimationOnDelay)
        {
            wm.pc.netAnimator.SetTrigger("Fire");
        }
        yield return new WaitForSeconds(delayBeforeFire / GameplayManager.Instance.fireRateMultiplier.Value);
        if (IsServer)
        {
            FireWeaponOnServer(wm ? wm.NetworkObject : NetworkObject);
        }
        if (IsOwner || IsServer)
        {
            rofLimited = true;
        }
        Invoke(nameof(ResetROFLimit), timeBetweenRounds / GameplayManager.Instance.fireRateMultiplier.Value);
        delayDone = true;
    }
    public virtual void FireWeapon(Vector3 end)
    {
        print("fired weapon locally");
        fireEvents?.Invoke();
        if(wm && wm.pc && wm.pc.netAnimator && !(delayBeforeFire > 0 && playFireAnimationOnDelay))
        {
            wm.pc.netAnimator.SetTrigger("Fire");
        }

        if(wm && wm.pc)
        {
            wm.ReceiveRecoil();
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
