using Unity.Netcode;
using UnityEngine;

public class BarrelManager : NetworkBehaviour
{
    public Transform[] barrelSpawnpoints;
    public NetworkObject[] barrels;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            SpawnBarrels();
        }
    }
    void SpawnBarrels()
    {
        for (int i = 0; i < barrelSpawnpoints.Length; i++)
        {
            int random = Random.Range(0, barrels.Length);
            NetworkObject n = NetworkManager.SpawnManager.InstantiateAndSpawn(barrels[random], position: barrelSpawnpoints[i].position, rotation: barrelSpawnpoints[i].rotation);
        }
    }
}
