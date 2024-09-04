
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
        }
        
        public static MatchController Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
        }
        private void Start()
        {
            if (IsOwner)
            {
                SetMyTeam_RPC(OwnerClientId);
            }
        }
        [Rpc(SendTo.Server)]
        void SetMyTeam_RPC(ulong ID)
        {
            AssignPlayerToTeam(ID);
        }
    }
}
