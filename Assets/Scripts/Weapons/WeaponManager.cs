using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class WeaponManager : NetworkBehaviour
    {
        [SerializeField] protected bool primaryInput, secondaryInput;
        public Transform attackOrigin;
        public Rigidbody rb;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            rb = GetComponent<Rigidbody>();
        }
    }
}
