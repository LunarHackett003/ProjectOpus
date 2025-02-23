using UnityEngine;

namespace Opus
{
    public class OBehaviour : MonoBehaviour, IOpusScript
    {


        private void Start()
        {
            OManager.OScripts.Add(this);
        }
        public void OnDestroy()
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
