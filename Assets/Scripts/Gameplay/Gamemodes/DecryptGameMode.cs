using System.Collections;
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

        [Tooltip("After this time has elapsed, spawn a new Decrypt Station.")]
        public float timeToSpawnNewStation = 15;


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

        public void DecryptionCompleted(uint teamCompleted, NetworkObject decryptStation)
        {
            MatchManager.Instance.SetScoreForTeam(teamCompleted, scoreOnDecrypt, true, 0, 1000);

            objectives.Remove(decryptStation.gameObject);
            decryptStation.Despawn(true);
            StartCoroutine(SpawnNewDecryptStation());
        }
        public IEnumerator SpawnNewDecryptStation()
        { 
            yield return new WaitForSeconds(timeToSpawnNewStation);
            Vector3 pos, rot;
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
            objectives.Add(newStation.gameObject);
        }
    }
}
