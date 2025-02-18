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

        void IOpusScript.OUpdate()
        {
            throw new System.NotImplementedException();
        }

        void IOpusScript.OFixedUpdate()
        {
            throw new System.NotImplementedException();
        }

        void IOpusScript.OLateUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}
