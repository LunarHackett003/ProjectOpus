using UnityEngine;

namespace Opus.Demolition
{
    public class DemoCluster : DemoPiece
    {
        [Tooltip("Ideally, assign fragments to this to use it as a floor")]
        public DemoCluster[] ownedDemoPieces;
        public bool collapsed;
        public float collapseIntegrity = .5f;
        [Tooltip("Alternatively, assign to this array to use it as a cluster. ")]
        public DemoPiece[] bottomPieces, topPieces;
        public bool connectedTop, connectedBottom;
        public Rigidbody rb;
        public FixedJoint bottomJoint;
        [Tooltip("Check this if this part should NOT influence connection between floors")]
        public bool ignoreConnection;
        protected override void Start()
        {
            base.Start();
            rb = rb != null ? rb : GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
            }
            if(ownedDemoPieces.Length > 0)
            {
                for (int i = 0; i < ownedDemoPieces.Length; i++)
                {
                    ownedDemoPieces[i].onDamageReceived += CheckFloor;
                }
            }
            if (topPieces.Length > 0)
            {
                for (int i = 0; i < topPieces.Length; i++)
                {
                    topPieces[i].onDamageReceived += CheckClusterValidity;
                }
            }
            if(bottomPieces.Length > 0)
            {
                for (int i = 0; i < bottomPieces.Length; i++)
                {
                    bottomPieces[i].onDamageReceived += CheckClusterValidity;
                }
            }
        }
        private void OnDestroy()
        {
            if(ownedDemoPieces.Length > 0)
            {
                for (int i = 0; i < ownedDemoPieces.Length; i++)
                {
                    ownedDemoPieces[i].onDamageReceived -= CheckFloor;
                }
            }

        }

        void CheckFloor()
        {
            Debug.Log("Checking floor validity", gameObject);
            connectedTop = true;
            connectedBottom = true;
            int disconnectedBottom = ownedDemoPieces.Length;
            integrity.Value = maxIntegrity;
            for (int i = 0; i < ownedDemoPieces.Length; i++)
            {
                if (ignoreConnection)
                    continue;

                connectedTop &= ownedDemoPieces[i].connectedTop;
                connectedBottom &= ownedDemoPieces[i].connectedBottom;
                if (!ownedDemoPieces[i].connectedBottom)
                {
                    integrity.Value -= maxIntegrity / ownedDemoPieces.Length;
                }

            }
            
            onDamageReceived?.Invoke();
        }
        void CheckClusterValidity()
        {
            connectedTop = true;
            float integ = 0;
            int pieces = 0;
            for (int i = 0; i < topPieces.Length; i++)
            {
                connectedTop &= topPieces[i].Integrity > 0;
                integ += topPieces[i].Integrity;
                pieces++;
            }
            for (int i = 0; i < bottomPieces.Length; i++)
            {
                connectedBottom &= bottomPieces[i].Integrity > 0;
                integ += bottomPieces[i].Integrity;
                pieces++;
            }
            if (pieces == 0)
                pieces++;
            integrity.Value = integ / pieces;
            onDamageReceived?.Invoke();
        }
    }
}
