using UnityEngine;

public class MapSceneData : MonoBehaviour
{
    public Transform[] alphaTeamSpawns, bravoTeamSpawns;
    public Transform[] randomSpawns;
    public Transform GetSpawnPoint(bool isAlphaTeam, bool useTeam)
    {
        int random;
        if (useTeam)
        {
            random = Random.Range(0, isAlphaTeam ? alphaTeamSpawns.Length : bravoTeamSpawns.Length);
            return isAlphaTeam ? alphaTeamSpawns[random] : bravoTeamSpawns[random];
        }
        else
        {
            random = Random.Range(0, randomSpawns.Length);
            return randomSpawns[random];
        }
    }
}
