using FishNet;
using FishNet.Object;
using UnityEngine;

namespace Opus.Demolition.Testing
{
    public class TestBallShooter : MonoBehaviour
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

            if (InstanceFinder.IsHostStarted)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var b = Instantiate(ball, transform.position, Quaternion.identity);
                    InstanceFinder.ServerManager.Spawn(b, ownerConnection: InstanceFinder.ClientManager.Connection);
                    b.GetComponent<Rigidbody>().AddForce(transform.forward * launchForce, ForceMode.Impulse);
                    Destroy(b, 8);
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                paused = !paused;
                Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }
    }
}
