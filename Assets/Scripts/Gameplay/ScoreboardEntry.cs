using System.Linq;
using TMPro;
using UnityEngine;

namespace Opus
{
    public class ScoreboardEntry : MonoBehaviour
    {
        public ulong ID;
        public TMP_Text playerName, kills, deaths, assists, revives, amountHealed;
        public int UpdateEntry(ulong newID = 999)
        {
            //If we're assigning this entry an ID, we need to set it. Otherwise, we can ignore it.
            //We (hopefully) won't ever have 999 clients in a server, so we can use 999 as a placeholder.
            //This would need to be changed if I ever decided to create 1000+ player lobbies (why??)
            if(newID != 999)
                ID = newID;
            
            playerName.text = PlayerManager.playerManagers.First(x => x.OwnerClientId == ID).playerName.Value;
            MatchController.TeamMember t = MatchController.Instance.teamMembers.Value.Find(x => x.playerID == ID);
            //We'll do more stuff with this later.

            return t.team;
        }
    }
}
