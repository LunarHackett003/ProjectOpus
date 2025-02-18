using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class OManager : MonoBehaviour
    {
        public static HashSet<IOpusScript> OScripts = new();

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }


        private void Update()
        {
            foreach (var item in OScripts)
            {
                item.OUpdate();
            }
        }
        private void FixedUpdate()
        {
            foreach (var item in OScripts)
            {
                item.OFixedUpdate();
            }
        }
        private void LateUpdate()
        {
            foreach(var item in OScripts)
            {
                item.OLateUpdate();
            }
        }
    }
}
