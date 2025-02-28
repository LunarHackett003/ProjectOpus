using UnityEngine;

namespace Opus
{
    public class AttenuationFactorSetter : MonoBehaviour
    {
        AkGameObj go;
        public float attenuationFactor;
        private void Start()
        {
            if(TryGetComponent(out go))
            {
                go.ScalingFactor = attenuationFactor;
                print("yahoo! setted the attenneration!");
            }
            else
            {
                print("uh oh! couldn't set attenuation!");
            }
        }
    }
}
