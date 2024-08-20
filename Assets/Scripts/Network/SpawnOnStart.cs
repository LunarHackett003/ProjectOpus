using Unity.Netcode;
using UnityEngine;

public class SpawnOnStart : NetworkBehaviour
{
    public bool withOwnership;
    private void Start()
    {
        if (IsSpawned)
            return;
        Spawn_ServerRPC();
    }
    [ServerRpc]
    void Spawn_ServerRPC(ServerRpcParams param = default)
    {
        if (withOwnership)
        {
            NetworkObject.SpawnWithOwnership(param.Receive.SenderClientId);
        }
        else
        {
            NetworkObject.Spawn();
        }
    }
}
