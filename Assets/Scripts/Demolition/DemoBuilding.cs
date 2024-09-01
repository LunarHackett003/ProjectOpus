using Opus.Demolition;
using UnityEngine;

namespace Opus
{
    public class DemoBuilding : MonoBehaviour
    {
        [Tooltip("Floors MUST be in descending height order (That's from top to bottom, if you didn't know) to work correctly!")]
        public DemoCluster[] floors;
        private void Start()
        {
            for (int i = floors.Length -1; i > -1; i--)
            {
                DemoCluster floor = floors[i];
                floor.onDamageReceived += CheckFloors;
                if(i < floors.Length - 1)
                {
                    floor.bottomJoint = floor.gameObject.AddComponent<FixedJoint>();
                    floor.bottomJoint.connectedBody = floors[i + 1].rb;
                }
            }
        }
        void CheckFloors()
        {
            print("Checking all floors");
            //Iterate from bottom to top
            for (int i = floors.Length - 1; i >= 0; i--)
            {
                var floor = floors[i];

                //We ignore any already-collapsed floors.
                if (floor.collapsed)
                    continue;
                if(floor.Integrity <= floor.collapseIntegrity * 100)
                {
                    print($"Floor {i} should collapse");
                    if(!floor.connectedBottom && floor.bottomJoint)
                    {
                        Destroy(floor.bottomJoint);
                    }
                    floor.collapsed = true;
                    floor.rb.isKinematic = false;
                }
                if (i < floors.Length - 1 && !floors[i + 1].rb.isKinematic)
                {
                    floor.rb.isKinematic = false;
                }
            }
        }
    }
}
