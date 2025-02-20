using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    /// <summary>
    /// <b>DECRYPT</b><br></br>
    /// SECURE. DECRYPT. DEFEND.<br></br>
    /// In Decrypt, players must make their way to a static objective
    /// </summary>
    public class DecryptGameMode : GameMode
    {
        public float timeToDecrypt, timeToSteal;
        public uint scoreOnDecrypt = 10000;
        public uint scoreToWin = 20000;

        public List<DecryptStation> objectives = new();

        [Tooltip("After this time has elapsed, spawn a new Decrypt Station.")]
        public float timeToSpawnNewStation = 15;

        public int objectiveIndex;

        DecryptSpawnPoint[] decryptSpawns = new DecryptSpawnPoint[0];

        public NetworkObject decryptStationPrefab;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            decryptSpawns = FindObjectsByType<DecryptSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (IsServer)
            {
                StartCoroutine(SpawnNewDecryptStation());
            }
        }
        public override bool GetWinningTeam(out uint team)
        {
            team = 0;
            foreach (KeyValuePair<uint, uint> v in MatchManager.Instance.teamScores.Value)
            {
                if(v.Value >= scoreToWin)
                {
                    team = v.Key;
                    return true;
                }
            }
            return false;
        }
        public void DecryptionCompleted(uint teamCompleted, DecryptStation decryptStation)
        {
            MatchManager.Instance.SetScoreForTeam(teamCompleted, scoreOnDecrypt, true, 0, 1000);

            objectives.Remove(decryptStation);
            decryptStation.NetworkObject.Despawn(true);
            StartCoroutine(SpawnNewDecryptStation());
        }
        public IEnumerator SpawnNewDecryptStation()
        { 
            yield return new WaitForSeconds(timeToSpawnNewStation);
            Vector3 pos, rot;
            objectiveIndex++;
            if(decryptSpawns.Length > 0)
            {
                int rand = Random.Range(0, decryptSpawns.Length);
                Transform t = decryptSpawns[rand].transform;
                pos = t.position;
                rot = t.eulerAngles;
            }
            else
            {
                print("Failed to find spawn for decrypt station!");
                pos = new(0, 50, 0);
                rot = Vector3.zero;
            }
            NetworkObject newStation = NetworkManager.SpawnManager.InstantiateAndSpawn(decryptStationPrefab, 0, false, false, false, pos, Quaternion.Euler(rot));
            objectives.Add(newStation.GetComponent<DecryptStation>());
        }
        public override void OFixedUpdate()
        {
            base.OFixedUpdate();

            if(MatchManager.Instance.GameInProgress.Value && GetWinningTeam(out uint teamWon))
            {
                print($"{teamWon} won the game!");
                if (IsServer)
                {
                    MatchManager.Instance.GameInProgress.Value = false;
                }
                StartCoroutine(EndGameSequence());
            }
        }
        public override void EndGameActions()
        {
            base.EndGameActions();

        }
    }
}
