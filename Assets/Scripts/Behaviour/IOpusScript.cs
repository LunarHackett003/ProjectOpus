using UnityEngine;

namespace Opus
{
    public interface IOpusScript
    {

        public abstract void OUpdate();
        public abstract void OFixedUpdate();
        public abstract void OLateUpdate();
    }
}
