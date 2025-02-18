using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseInteractable : BaseHoverable
    {
        public bool holdInteract;
        public string interactText;



        public virtual bool CanInteract(ulong clientID)
        {
            return true;
        }

        [Rpc(SendTo.Everyone)]
        public void InteractStart_RPC(ulong clientID = 0)
        {
            InteractStart(clientID);
        }

        protected virtual void InteractStart(ulong clientID = 0)
        {

        }

        [Rpc(SendTo.Everyone)]
        public void InteractEnd_RPC(ulong clientID = 0)
        {
            InteractEnd(clientID);
        }

        protected virtual void InteractEnd(ulong clientID = 0)
        {

        }
    }
}
