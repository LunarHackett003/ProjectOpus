
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class RemoteComponentController : NetworkBehaviour
    {
        public Behaviour[] remoteDisable;
        public Behaviour[] ownerDisable;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                foreach (var item in remoteDisable)
                {
                    item.enabled = false;
                }
            }
            else
            {
                foreach (var item in ownerDisable)
                {
                    item.enabled = false;
                }
            }
        }
    }
}
