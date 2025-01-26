using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseInteractable : NetworkBehaviour
    {
        public bool holdInteract;
        public string interactText;





        [Rpc(SendTo.Everyone)]
        public virtual void InteractStart_RPC(uint clientID = 0)
        {

        }
        [Rpc(SendTo.Everyone)]
        public virtual void InteractEnd_RPC(uint clientID = 0)
        {

        }
    }
}
