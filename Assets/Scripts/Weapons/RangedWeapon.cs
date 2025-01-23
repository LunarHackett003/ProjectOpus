using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Pool;
using System.Collections.Generic;
using Unity.Cinemachine;
namespace Opus
{


    public class RangedWeapon : BaseWeapon
    {
        public int maxAmmo;
        public int CurrentAmmo { get; private set; }
        public NetworkVariable<int> syncedAmmo = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool reloading;

        public Transform muzzle;
        public bool useSingleReload;
        public float firstReloadDelay = 1.2f;
        public float reloadTime = 2f;

        public float reloadCancelTime = 0.5f;

        public override bool FireBlocked => base.FireBlocked || CurrentAmmo <= 0 || reloading;

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
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
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
            if(acpp != null)
            {
                reloading = acpp.customParams[0].boolValue;
            }

            if (IsOwner)
            {
                if (lastFireInput != fireInput)
                {
                    lastFireInput = fireInput;
                    SendFireInput_RPC(fireInput);
                }
            }
            ProcessFire();
        }

        [Rpc(SendTo.Everyone)]
        public void SendFireInput_RPC(bool input)
        {
            fireInput = input;

            if (IsServer)
            {
                fireInputSynced.Value = input;
            }
        }
        protected override void ProcessFire()
        {
            base.ProcessFire();
        }
        protected virtual void ServerFireLogic(Vector3 direction, Vector3 origin)
        {

        }
        
        [Rpc(SendTo.ClientsAndHost)]
        void SendAmmo_RPC(int ammoAmount)
        {
            CurrentAmmo = ammoAmount;
        }
        public override void FireOnClient()
        {
            base.FireOnClient();
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

        public override void FireOnServer(Vector3 direction, Vector3 origin)
        {
            base.FireOnServer(direction, origin);

            //If a client somehow gets here, we need to make sure they don't get any further.
            if (!IsServer)
                return;
            CurrentAmmo--;
            UpdateAmmo();

            ServerFireLogic(direction, origin);
            //SendTracer_RPC(Vector3.zero, Vector3.zero);
        }
        private void OnValidate()
        {
            timeBetweenRounds = 1 / (roundsPerMinute / 60);
        }
    }
}
