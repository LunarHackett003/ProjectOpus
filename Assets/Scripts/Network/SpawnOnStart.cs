using Unity.Netcode;
using UnityEngine;

public class SpawnOnStart : NetworkBehaviour
{
    private void Start()
    {
        if (IsSpawned)
            return;
        Spawn_ServerRPC();
    }
    [ServerRpc]
    void Spawn_ServerRPC(ServerRpcParams param = default)
    {
        NetworkObject.SpawnWithOwnership(param.Receive.SenderClientId);
    }
}
