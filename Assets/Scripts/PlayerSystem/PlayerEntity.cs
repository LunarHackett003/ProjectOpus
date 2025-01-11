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
        public CinemachineCamera viewCineCam, worldCineCam;
        public Camera viewmodelCamera;

        public Outline outlineComponent;
        public CharacterRenderable cr;

        public NetworkVariable<bool> stunned = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> burning = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool Alive => CurrentHealth > 0;

        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
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

            cr.InitialiseViewable(this);
        }
    }
}
