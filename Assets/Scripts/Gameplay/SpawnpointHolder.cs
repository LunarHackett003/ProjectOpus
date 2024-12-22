using UnityEngine;

namespace Opus
{
    public class SpawnpointHolder : MonoBehaviour
    {
        public Transform[] spawnpoints;
        public (Vector3 pos, Quaternion rot) FindSpawnpoint(uint teamID)
        {
            int random = Random.Range(0, spawnpoints.Length);
            return (spawnpoints[random].position, spawnpoints[random].rotation);
        }
    }
}
