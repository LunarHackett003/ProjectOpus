using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class ONetBehaviour : NetworkBehaviour, IOpusScript
    {


        private void Start()
        {
            OManager.OScripts.Add(this);
        }
        public override void OnDestroy()
        {
            OManager.OScripts.Remove(this);
        }

        public virtual void OFixedUpdate()
        {

        }

        public virtual void OLateUpdate()
        {

        }

        public virtual void OUpdate()
        {

        }
    }
}
