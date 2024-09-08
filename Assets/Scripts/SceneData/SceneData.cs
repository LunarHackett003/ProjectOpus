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
            print($"Finding spawnpoint for player on team {team + 1}");
            int random = Random.Range(0, teamData[team].spawnpoints.Length);
            return teamData[team].spawnpoints[random];
        }
    }
}
