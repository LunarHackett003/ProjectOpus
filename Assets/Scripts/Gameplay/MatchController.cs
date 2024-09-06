
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Overlays;
using UnityEngine;

namespace Opus
{
    public class MatchController : NetworkBehaviour
    {
        [System.Serializable]
        public struct TeamMember
        {
            public ulong playerID;
            public int team, kills, deaths, assists, revives, amountHealed;
        }
        public NetworkVariable<List<TeamMember>> teamMembers = new();
        /// <summary>
        /// A dictionary containing the number of teams, and the number of players on those teams.<br></br>
        /// Key = Team Number<br></br>
        /// Value = Number of players on that team
        /// </summary>
        public NetworkVariable<Dictionary<int, int>> teamNumbers = new(new());
        public int numberOfTeams = 4;
        public void AssignPlayerToTeam(ulong ID)
        {
            int teamToAssign = 0;
            int smallestTeam = 999;

            for (int i = 0; i < teamNumbers.Value.Count; i++)
            {
                if (teamNumbers.Value[i] < smallestTeam)
                {
                    teamToAssign = i;
                }
            }
            TeamMember t = new()
            {
                playerID = ID,
                team = teamToAssign,

            };
            teamMembers.Value.Add(t);
            teamNumbers.Value.Add(teamToAssign, teamNumbers.Value[teamToAssign] + 1);
        }
        
        public static MatchController Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
            if (IsOwner)
            {
                for (int i = 0; i < numberOfTeams; i++)
                {
                    teamNumbers.Value.Add(i, 0);
                }
                NetworkManager.OnConnectionEvent += PlayerConnectionEvent;
            }
        }

        private void PlayerConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            if(arg2.EventType == ConnectionEvent.ClientConnected)
            {
                AssignPlayerToTeam(arg2.ClientId);
            }
            else if(arg2.EventType == ConnectionEvent.ClientDisconnected)
            {
                RemovePlayerFromTeam(arg2.ClientId);
            }
        }
        private void Start()
        {

        }
        void RemovePlayerFromTeam(ulong ID)
        {
            TeamMember t = teamMembers.Value.Find(x => x.playerID == ID);
            teamMembers.Value.RemoveAt(t.team);
            teamNumbers.Value.Add(t.team, teamNumbers.Value[t.team] - 1);
        }
    }
}
