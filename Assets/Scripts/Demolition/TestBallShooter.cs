
using Unity.Netcode;
using UnityEngine;

namespace Opus.Demolition.Testing
{
    public class TestBallShooter : NetworkBehaviour
    {
        public GameObject ball;
        public float launchForce;
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        public bool paused;
        public void Update()
        {

        }
    }
}
