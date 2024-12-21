using UnityEngine;

namespace Opus
{
    public class AnimatorCustomParamProxy : MonoBehaviour
    {
        [System.Serializable]
        public struct CustomParam
        {
            public string stringValue;
            public float floatValue;
            public bool boolValue;
        }

        public CustomParam[] customParams;

        public void SetParam(AnimationEvent animEvent)
        {
            print($"{animEvent.intParameter}");
            if(customParams.Length > animEvent.intParameter)
            {
                customParams[animEvent.intParameter] = new CustomParam()
                {
                    stringValue = animEvent.stringParameter,
                    floatValue = animEvent.floatParameter,
                    boolValue = animEvent.stringParameter.ToLower() == "true"
                };
                    print("setting custom parameter");
            }
            else
            {
                print("Failed to set custom parameter!");
            }
        }
    }

}
