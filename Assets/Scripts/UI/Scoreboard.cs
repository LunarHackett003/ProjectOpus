using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
public class Scoreboard : MonoBehaviour
{
    

    bool show;
    [SerializeField] CanvasGroup scoreboard;
    public bool registeredCallbacks;
    public Transform alphaTeamRoot, bravoTeamRoot;
    public GameObject displayPrefab;
    public Dictionary<ulong, ScoreboardEntry> scoreboardTeams = new();
    private void OnEnable()
    {
        RegisterTeamUpdateCallbacks();
    }
    private void OnDisable()
    {
        if (GameplayManager.Instance)
        {
            GameplayManager.Instance.TeamMembers.OnValueChanged -= TeamsUpdated;
            registeredCallbacks = false;
        }
    }
    public void ShowScoreboard(InputAction.CallbackContext context)
    {
        show = context.ReadValueAsButton() && GameplayManager.Instance;
    }
    public void Update()
    {
        if (!registeredCallbacks)
        {
            RegisterTeamUpdateCallbacks();
        }
        scoreboard.alpha = show ? 1 : 0;
    }
    void RegisterTeamUpdateCallbacks()
    {
        if (GameplayManager.Instance)
        {
            GameplayManager.Instance.TeamMembers.OnValueChanged += TeamsUpdated;
            TeamsUpdated(new List<GameplayManager.TeamMember>(), GameplayManager.Instance.TeamMembers.Value);
            registeredCallbacks = true;
        }
    }
    public void TeamsUpdated(List<GameplayManager.TeamMember> previous, List<GameplayManager.TeamMember> current)
    {
        if (current.Count >= previous.Count)
        {
            print("player added, updating scoredboard");
            for (int i = 0; i < current.Count; i++)
            {
                if (previous.Exists(x => x.steamID == current[i].steamID))
                {
                    //This player already exists, and so we can check the team and move on
                    scoreboardTeams[current[i].steamID].transform.SetParent(current[i].BravoTeam ? bravoTeamRoot : alphaTeamRoot, false);
                }
                else
                {
                    var s = Instantiate(displayPrefab, current[i].BravoTeam ? bravoTeamRoot : alphaTeamRoot).GetComponent<ScoreboardEntry>();
                    s.steamID = current[i].steamID;
                    s.playerName = current[i].Name;
                    scoreboardTeams.Add(current[i].steamID, s);
                }
            }
        }
        else
        {
            print("player removed, updating scoreboard");
            for (int i = 0; i < previous.Count; i++)
            {
                if(current.Exists(x => x.steamID == previous[i].steamID))
                {
                    //This player did not leave, we can ignore them
                    scoreboardTeams[previous[i].steamID].transform.SetParent(previous[i].BravoTeam ? bravoTeamRoot : alphaTeamRoot, false);
                }
                else
                {
                    Destroy(scoreboardTeams[previous[i].steamID].gameObject);
                    scoreboardTeams.Remove(previous[i].steamID);
                }
            }
        }
    }
}
