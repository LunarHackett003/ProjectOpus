using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseInteractable : BaseHoverable
    {
        public bool holdInteract;
        public string interactText;





        [Rpc(SendTo.Everyone)]
        public void InteractStart_RPC(uint clientID = 0)
        {
            InteractStart(clientID);
        }

        protected virtual void InteractStart(uint clientID = 0)
        {

        }

        [Rpc(SendTo.Everyone)]
        public void InteractEnd_RPC(uint clientID = 0)
        {
            InteractEnd(clientID);
        }

        protected virtual void InteractEnd(uint clientID = 0)
        {

        }
    }
}
