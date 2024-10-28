using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class PlayerNetworkTransform : AnticipatedNetworkTransform
    {

        private Vector3 lastPosition;
        private Quaternion lastRotation;

        public override void OnNetworkSpawn()
        {
            StaleDataHandling = StaleDataHandling.Reanticipate;
            base.OnNetworkSpawn();
        }
        public override void OnReanticipate(double lastRoundTripTime)
        {
            base.OnReanticipate(lastRoundTripTime);
        }
        private void FixedUpdate()
        {

        }
    }
}
