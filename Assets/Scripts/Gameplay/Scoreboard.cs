using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class Scoreboard : MonoBehaviour
    {
        public static Scoreboard Instance { get; private set; }
        public List<ScoreboardEntry> scoreboardEntries;
        public GameObject scoreboardRoot;
        bool firstUpdateDone;
        private void Awake()
        {
            Instance = this;
            ShowScoreboard(false);
        }
        public TeamEntry[] teamEntries;
        public void UpdateScoreboard()
        {
            firstUpdateDone = true;
            int[] playersOnTeams = new int[MatchController.Instance.numberOfTeams];
            for (int i = 0; i < playersOnTeams.Length; i++)
            {
                TeamNameSO.TeamData t = MatchController.Instance.teamNames.teams[MatchController.Instance.teamDataNumbers.Value[i]];
                //Initialise the teams first
                teamEntries[i].teamNameDisplay.text = t.name;
                teamEntries[i].teamBackground.color = t.color;
            }
            for (int i = 0; i < teamEntries.Length; i++)
            {
                TeamEntry t = teamEntries[i];
                List<MatchController.TeamMember> members = MatchController.Instance.teamMembers.Value.FindAll(x => x.team == i);
                print($"found {members.Count} players on team {i}");
                for (int j = 0; j < t.scoreboardEntries.Length; j++)
                {
                    ScoreboardEntry s = t.scoreboardEntries[j];
                    if(members.Count > 0 && j < members.Count)
                        s.UpdateEntry(members[j]);
                    else
                        s.UpdateEntry(999);
                }  
            }
        }
        public void ShowScoreboard(bool show)
        {
            if (show && !firstUpdateDone)
            {
                UpdateScoreboard();
            }
            scoreboardRoot.SetActive(show);
        }
    }
}
