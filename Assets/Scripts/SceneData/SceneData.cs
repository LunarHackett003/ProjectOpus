using UnityEngine;

namespace Opus
{
    public class SceneData : MonoBehaviour
    {
        [System.Serializable]
        public struct TeamData
        {
            public Transform[] spawnpoints;
        }
        public TeamData[] teamData;
        public Transform GetSpawnPoint(int team)
        {
            int random = Random.Range(0, teamData[team].spawnpoints.Length);
            return teamData[team].spawnpoints[random];
        }
    }
}
