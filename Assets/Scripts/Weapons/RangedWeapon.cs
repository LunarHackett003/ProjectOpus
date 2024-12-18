using Unity.Netcode;
using UnityEngine;
using System.Collections;
namespace Opus
{
    public class RangedWeapon : BaseWeapon
    {
        public float roundsPerMinute;
        float timeBetweenRounds;
        [Tooltip("How long, in seconds, the weapon waits after firing a burst before allowing the player to fire again")]
        public float timeBetweenBursts;
        [Tooltip("How quickly the weapon charges up when firing.")]
        public float chargeSpeed;
        [Tooltip("How quickly the weapon \"Cools down\"")]
        public float chargeDecay;
        [Tooltip("How much charge the weapon currently has")]
        public float CurrentCharge { get; private set; }
        [Tooltip("How many shots are fired in a burst")]
        public int burstFireRounds;
        bool burstFiring;
        public Vector3 recoilPos, recoilEuler;
        public readonly NetworkVariable<bool> fireInputSynced = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        bool fireInputPressed;
        bool fired;
        public WeaponFireType weaponFireType;
        public WeaponFireBehaviour fireBehaviour;

        public int maxAmmo;
        public int CurrentAmmo { get; private set; }
        public NetworkVariable<int> syncedAmmo = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool reloading;

        void UpdateAmmo()
        {
            SendAmmo_RPC(CurrentAmmo);
            syncedAmmo.Value = CurrentAmmo;
        }

        public void AddAmmo(int ammoToAdd)
        {
            if (IsServer)
            {
                CurrentAmmo += ammoToAdd;
                UpdateAmmo();
            }
        }
        public void RefillAmmo()
        {
            if (IsServer)
            {
                CurrentAmmo = maxAmmo;
                SendAmmo_RPC(CurrentAmmo);
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            fireInput = fireInputSynced.Value;
            if (syncedAmmo.Value != -1)
            {
                CurrentAmmo = syncedAmmo.Value;
            }
            else
            {
                CurrentAmmo = maxAmmo;
            }
            OnValidate();
        }
        

        protected override void FixedUpdate()
        {
            //if (fireInput)
            //{
            //    TryFire();
            //}
            //else
            //{
            //    fireInputPressed = false;
            //}
            reloading = acpp.customParams[0].boolValue;

            if (fired || reloading || (maxAmmo > 0 && CurrentAmmo <= 0))
                return;
            switch (weaponFireType)
            {
                case WeaponFireType.onPress:
                    if (fireInput)
                    {
                        TryFire();
                        fireInputPressed = true;
                    }
                    else
                    {
                        fireInputPressed = false;
                    }
                    break;
                case WeaponFireType.onReleaseInstant:
                    if (fireInput)
                    {
                        fireInputPressed = true;
                    }
                    if (!fireInput)
                    {

                    }
                    break;
                case WeaponFireType.onReleaseAnimated:
                    break;
                case WeaponFireType.chargeHold:
                    break;
                case WeaponFireType.chargePress:
                    break;
                default:
                    break;
            }
        }
        void TryFire()
        {
            switch (fireBehaviour)
            {
                case WeaponFireBehaviour.semiAutomatic:
                    if (!fireInputPressed)
                    {
                        Fire();
                    }
                    break;
                case WeaponFireBehaviour.fullyAutomatic:
                    Fire();
                    break;
                case WeaponFireBehaviour.burstFireOnce:
                    if (!fireInputPressed && !burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.burstFireLoop:
                    if (!burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.animatedReset:
                    Fire();
                    break;
                default:
                    break;
            }
        }
        [Rpc(SendTo.Everyone)]
        public void SendFireInput_RPC(bool input)
        {
            fireInput = input;

            if (IsServer)
            {
                fireInputSynced.Value = input;
            }
            Fire();
        }
        void Fire()
        {
            if (IsServer)
            {
                FireOnServer();
            }
            if (IsClient)
            {
                FireOnClient();
            }
            fired = true;
            StartCoroutine(ResetFire());
        }
        IEnumerator ResetFire()
        {
            yield return new WaitForSeconds(timeBetweenRounds);
            fired = false;
        }
        public IEnumerator ChargeToFull()
        {
            WaitForFixedUpdate wff = new();
            while (CurrentCharge < 1)
            {
                CurrentCharge += chargeSpeed * Time.fixedDeltaTime;
                yield return wff;
            }

        }
        public IEnumerator BurstFire()
        {
            WaitForSeconds wait = new(timeBetweenRounds);
            int fired = 0;
            while (fired < burstFireRounds)
            {
                Fire();
                fired++;
                if (fired != burstFireRounds - 1)
                    yield return wait;
            }
            yield return new WaitForSeconds(timeBetweenBursts);
            if (fireBehaviour == WeaponFireBehaviour.burstFireLoop)
            {
                fireInputPressed = false;
            }
        }

        public void FireOnServer()
        {
            //If a client somehow gets here, we need to make sure they don't get any further.
            if (!IsServer)
                return;
            CurrentAmmo--;
            UpdateAmmo();
            //SendTracer_RPC(Vector3.zero, Vector3.zero);
        }
        [Rpc(SendTo.Everyone)]
        public void SendTracer_RPC(Vector3 start, Vector3 end)
        {

        }
        [Rpc(SendTo.ClientsAndHost)]
        void SendAmmo_RPC(int ammoAmount)
        {
            CurrentAmmo = ammoAmount;
        }
        public void FireOnClient()
        {
            if (fireParticleSystem)
            {
                fireParticleSystem.Play();
            }
            if (fireVFX)
            {
                fireVFX.Play();
            }
            if (myController)
            {
                myController.ReceiveShot();
            }
        }
        private void OnValidate()
        {
            timeBetweenRounds = 1 / (roundsPerMinute / 60);
        }
    }
}
