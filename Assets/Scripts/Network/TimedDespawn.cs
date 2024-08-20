using Unity.Netcode;
using UnityEngine;

public class TimedDespawn : NetworkBehaviour
{
    public float time;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Invoke(nameof(ByeBye),time);
        }
    }
    void ByeBye()
    {
        NetworkObject.Despawn();
    }
}
