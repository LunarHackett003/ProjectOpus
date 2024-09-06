using System.Linq;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class Scoreboard : MonoBehaviour
    {
        public static Scoreboard Instance { get; private set; }
        public InputCollector ic;
        bool sbActive;
        public GameObject scoreboard;
        List<ScoreboardEntry> allEntries = new();
        [System.Serializable]
        public struct TeamEntry
        {
            public ScoreboardEntry[] scoreboardEntries;
        }
        public TeamEntry[] teamEntries;
        private void Start()
        {
            Instance = this;
            scoreboard.SetActive(false);
            for (int i = 0; i < teamEntries.Length; i++)
            {
                allEntries.AddRange(teamEntries[i].scoreboardEntries);
            }
        }
        private void Update()
        {
            if(sbActive != ic.scoreboardInput)
            {
                ActivateScoreboard();
            }
        }
        void ActivateScoreboard()
        {
            sbActive = ic.scoreboardInput;
            scoreboard.SetActive(sbActive);
        }
    }
}
