using FishNet;
using FishNet.Object;
using UnityEngine;

namespace Opus
{
    public class TestBall : MonoBehaviour
    {
        public float impulseThreshold;
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.sqrMagnitude > impulseThreshold && collision.collider.TryGetComponent(out DemoPiece d))
            {
                print("Damaged something!");
                d.DealDamage(25);
            }
        }
    }
}
