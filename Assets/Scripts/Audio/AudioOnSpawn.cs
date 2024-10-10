using FMODUnity;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class AudioOnSpawn : NetworkBehaviour
    {
        public EventReference soundbite;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            RuntimeManager.PlayOneShot(soundbite, transform.position);
        }
    }
}
