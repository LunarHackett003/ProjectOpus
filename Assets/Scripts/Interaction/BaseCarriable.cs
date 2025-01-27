using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Opus
{
    public class BaseCarriable : BaseHoverable
    {
        public Material validDropMat, invalidDropMat;
        public bool canReleaseHere;
        bool canReleaseLast;
        public NetworkVariable<bool> grabbed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Vector3 overlapBoxSize;
        public Collider[] collidersOnObject;

        public Quaternion grabOffset = Quaternion.identity;

        public LayerMask releaseMask;

        public int grabbedLayer, defaultLayer;

        public Rigidbody rb;

        public override void HoverOver(bool hovered)
        {
            base.HoverOver(hovered);
            grabbedOutline.material = validDropMat;
        }

        private void FixedUpdate()
        {
            if (grabbed.Value)
            {
                canReleaseHere = !Physics.CheckBox(transform.position, overlapBoxSize/2, transform.rotation, releaseMask, QueryTriggerInteraction.Ignore);
                if(canReleaseLast != canReleaseHere)
                {
                    canReleaseLast = canReleaseHere;
                    grabbedOutline.material = canReleaseHere ? validDropMat : invalidDropMat;
                }
            }   
        }

        [Rpc(SendTo.Server)]
        public void OnGrab_RPC(uint clientID)
        {
            if (!grabbed.Value)
            {
                grabbed.Value = true;
                NetworkObject.ChangeOwnership(clientID);
                ReturnGrab_RPC(true, false, clientID);
            }
        }
        [Rpc(SendTo.Server)]
        public void Released_RPC(bool thrown)
        {
            if (thrown)
            {
                ThrowAction();
            }
            grabbed.Value = false;
            ReturnGrab_RPC(false, thrown, 0);
        }

        public virtual void ThrowAction()
        {

        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ReturnGrab_RPC(bool state, bool thrown = false, uint clientID = 0)
        {
            SetColliderLayer(state ? grabbedLayer : defaultLayer);
            rb.isKinematic = state;
            if(!state && thrown)
            {

            }
        }
        public void SetColliderLayer(int layer)
        {
            for (int i = 0; i < collidersOnObject.Length; i++)
            {
                collidersOnObject[i].gameObject.layer = layer;
            }
        }

        [ContextMenu("Get Colliders")]
        public void GetColliders()
        {
            collidersOnObject = GetComponentsInChildren<Collider>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(collision.impulse.magnitude > 25 && collision.rigidbody != null && collision.rigidbody != PlayerManager.playersByID[OwnerClientId].Character.wc.Controller.rb && collision.rigidbody.TryGetComponent(out Entity entity))
            {
                SendCarriableDamage_RPC(entity);
            }
        }
        [Rpc(SendTo.Server)]
        void SendCarriableDamage_RPC(NetworkBehaviourReference entityRef)
        {
            if(entityRef.TryGet(out Entity entity))
            {
                entity.ReceiveDamage(30, OwnerClientId, 1, DamageType.Regular);
            }
        }
    }
}
