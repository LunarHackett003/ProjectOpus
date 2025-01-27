using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class NetworkObjectSpawner : NetworkBehaviour
    {
        [System.Serializable]
        public struct PrefabSpawns
        {
            public NetworkObject[] prefabToSpawn;
            public Transform[] placesToSpawn;
        }
        public PrefabSpawns[] prefabSpawns;

        protected override void OnInSceneObjectsSpawned()
        {
            base.OnInSceneObjectsSpawned();

            if (IsServer)
            {
                for (int i = 0; i < prefabSpawns.Length; i++)
                {
                    TrySpawn(prefabSpawns[i]);
                }
            }
        }

        protected virtual void TrySpawn(PrefabSpawns prefabSpawn)
        {
            for (int i = 0; i < prefabSpawn.placesToSpawn.Length; i++)
            {
                int random = Random.Range(0, prefabSpawn.prefabToSpawn.Length);
                NetworkManager.SpawnManager.InstantiateAndSpawn(prefabSpawn.prefabToSpawn[random], 0, false, false, false, prefabSpawn.placesToSpawn[i].position, prefabSpawn.placesToSpawn[i].rotation);
            }
        }
    }
}
