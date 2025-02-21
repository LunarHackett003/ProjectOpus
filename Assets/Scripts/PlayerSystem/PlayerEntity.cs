using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Opus
{
    public class PlayerEntity : HealthyEntity
    {
        public PlayerManager playerManager;
        public NetworkTransform netTransform;
        public Transform headTransform;
        public CinemachineCamera viewCineCam, worldCineCam;
        public Camera viewmodelCamera;

        public Outline outlineComponent;
        public CharacterRenderable cr;
        public WeaponControllerV2 wc;

        public Collider[] allColliders;
        public Renderer[] allRenderers;


        public Vector3 LastGroundedPosition;

        public bool Alive => CurrentHealth > 0;

        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply, DamageType damageType = DamageType.Regular)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply, damageType);
        }
        public override void RestoreHealth(float healthIn, ulong sourceClientID)
        {
            base.RestoreHealth(healthIn, sourceClientID);
        }
        [Rpc(SendTo.Owner)]
        public void Teleport_RPC(Vector3 pos, Quaternion rot)
        {
            netTransform.Teleport(pos, rot, Vector3.one);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            PlayerManager.playersByID.TryGetValue(OwnerClientId, out playerManager);
            playerManager.Character = this;

            if (IsOwner)
            {

                UniversalAdditionalCameraData uacd = Camera.main.GetUniversalAdditionalCameraData();

                uacd.cameraStack.Add(viewmodelCamera);
                uacd.renderPostProcessing = false;
            }
            else
            {
                worldCineCam.enabled = false;
                viewCineCam.enabled = false;
                viewmodelCamera.enabled = false;
            }
            StartCoroutine(DelayedInitialise());
        }
        protected virtual IEnumerator DelayedInitialise()
        {
            yield return new WaitForFixedUpdate();
            cr.InitialiseViewable();
        }
        protected override void HealthUpdated(float prev, float curr)
        {
            base.HealthUpdated(prev, curr);
            if(IsOwner)
            {
                wc.Controller.rb.isKinematic = curr <= 0;
                if(prev > 0 && curr <= 0)
                {
                    playerManager.ClientDied();
                }
            }
        }
        [Rpc(SendTo.Everyone)]
        public void SetCollidersEnabledState_RPC(bool isEnabled)
        {
            if (allColliders.Length > 0)
            {
                for (int i = 0; i < allColliders.Length; i++)
                {
                    allColliders[i].enabled = isEnabled;
                }
            }
        }
        [Rpc(SendTo.Everyone)]
        public void SetRenderersEnabledState_RPC(bool isEnabled)
        {
            if (allRenderers.Length > 0)
            {
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    allRenderers[i].enabled = isEnabled;
                }
            }
        }
    }
}
