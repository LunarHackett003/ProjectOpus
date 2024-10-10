using System.Linq;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace Opus
{
    public class ScoreboardEntry : MonoBehaviour
    {
        public ulong ID;
        public TMP_Text playerName, kills, deaths, assists, revives, amountHealed;
        public UnityEngine.UI.Image background;
        public int UpdateEntry(ulong newID = 999)
        {
            //If we're assigning this entry an ID, we need to set it. Otherwise, we can ignore it.
            //We (hopefully) won't ever have 999 clients in a server, so we can use 999 as a placeholder.
            //This would need to be changed if I ever decided to create 1000+ player lobbies (why??)
            if(newID == 999)
            {
                playerName.text = "-";
                kills.text = "-";
                deaths.text = "-";
                assists.text = "-";
                revives.text = "-";
                amountHealed.text = "-";
                background.color = new(Color.grey.r, Color.grey.g, Color.grey.b, 0.3f);
                return -1;
            }
            else
            {
                ID = newID;
                playerName.text = PlayerManager.playerManagers.First(x => x.OwnerClientId == ID).playerName.Value.ToString();
                MatchController.TeamMember t = MatchController.Instance.teamMembers.Value.Find(x => x.playerID == ID);
                //We'll do more stuff with this later.
                kills.text = t.kills.ToString();
                deaths.text = t.deaths.ToString();
                assists.text = t.assists.ToString();
                revives.text = t.revives.ToString();
                amountHealed.text = t.supportScore.ToString();
                return t.team;
            }
        }
        public int UpdateEntry(MatchController.TeamMember member)
        {
            playerName.text = PlayerManager.playerManagers.First(x => x.OwnerClientId == ID).playerName.Value.ToString();
            //We'll do more stuff with this later.
            kills.text = member.kills.ToString();
            deaths.text = member.deaths.ToString();
            assists.text = member.assists.ToString();
            revives.text = member.revives.ToString();
            amountHealed.text = member.supportScore.ToString();
            return member.team;
        }
    }
}
