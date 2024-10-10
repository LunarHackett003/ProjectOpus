
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class MatchController : NetworkBehaviour
    {
        [System.Serializable, GenerateSerializationForGenericParameter(0)]
        public struct TeamMember : INetworkSerializable
        {
            public ulong playerID;
            public int team, kills, deaths, assists, revives, supportScore;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref playerID);
                serializer.SerializeValue(ref team);
                serializer.SerializeValue(ref kills);
                serializer.SerializeValue(ref deaths);
                serializer.SerializeValue(ref assists);
                serializer.SerializeValue(ref revives);
                serializer.SerializeValue(ref supportScore);
            }
        }
        public NetworkObject objectPoolPrefab;

        public TeamNameSO teamNames;
        public NetworkVariable<Dictionary<int, int>> teamDataNumbers = new(new());
        public NetworkVariable<List<TeamMember>> teamMembers = new();
        /// <summary>
        /// A dictionary containing the number of teams, and the number of players on those teams.<br></br>
        /// Key = Team Number<br></br>
        /// Value = Number of players on that team
        /// </summary>
        public NetworkVariable<Dictionary<int, int>> teamNumbers = new(new());
        public NetworkVariable<Dictionary<ulong, int>> kills = new(new());
        public List<TeamMember> localTeamMembers = new();
        public int numberOfTeams = 4;

        public LayerMask damageLayermask;

        public void AssignPlayerToTeam(ulong ID)
        {
            int teamToAssign = 0;
            int smallestTeam = 999;

            for (int i = 0; i < teamNumbers.Value.Count; i++)
            {
                if (teamNumbers.Value[i] < smallestTeam)
                {
                    print(teamNumbers.Value[i] + " players on team " + i);
                    smallestTeam = i;
                    teamToAssign = i;
                }
            }
            TeamMember t = new()
            {
                playerID = ID,
                team = teamToAssign,

            };
            print($"added Player {t.playerID} to team {teamToAssign}");
            teamMembers.Value.Add(t);
            teamNumbers.Value[teamToAssign] += 1;
            NetworkManager.ConnectedClients[t.playerID].PlayerObject.GetComponent<PlayerManager>().BestowPlayer();
        }
        
        public static MatchController Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
            teamMembers.OnValueChanged += TeamMembersUpdated;
            if (IsOwner)
            {
                NetworkManager.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
                for (int i = 0; i < numberOfTeams; i++)
                {
                    teamNumbers.Value.Add(i, 0);
                }
                InitialiseTeamNumbers();
                AssignPlayerToTeam(0);
                NetworkManager.OnConnectionEvent += PlayerConnectionEvent;
            }
        }

        private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            NetworkManager.SpawnManager.InstantiateAndSpawn(objectPoolPrefab);
        }

        void InitialiseTeamNumbers()
        {
            for (int i = 0; i < numberOfTeams; i++)
            {
                int random = UnityEngine.Random.Range(0, teamNames.teams.Count);
                if(i != 0 )
                {
                    while (teamDataNumbers.Value.ContainsValue(random))
                    {
                        random = UnityEngine.Random.Range(0, teamNames.teams.Count);
                    }
                }
                print($"using team data index {random} for team {i}");
                teamDataNumbers.Value.Add(i, random);
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner)
            {
                NetworkManager.OnConnectionEvent -= PlayerConnectionEvent;
            }
            teamMembers.OnValueChanged -= TeamMembersUpdated;
        }
        void TeamMembersUpdated(List<TeamMember> previous,  List<TeamMember> current)
        {
            localTeamMembers = current;
            if(Scoreboard.Instance != null)
            {
                Scoreboard.Instance.UpdateScoreboard();
            }
        }

        private void PlayerConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            if(arg2.EventType == ConnectionEvent.ClientConnected)
            {
                print("adding player to team");
                AssignPlayerToTeam(arg2.ClientId);
            }
            else if(arg2.EventType == ConnectionEvent.ClientDisconnected)
            {
                RemovePlayerFromTeam(arg2.ClientId);
            }
        }
        void RemovePlayerFromTeam(ulong ID)
        {
            TeamMember t = teamMembers.Value.Find(x => x.playerID == ID);
            teamMembers.Value.RemoveAt(t.team);
            teamNumbers.Value.Add(t.team, teamNumbers.Value[t.team] - 1);
        }
        public void UpdateScoreForPlayer(ulong ID, int killDelta = 0, int deathDelta = 0, int assistDelta = 0, int reviveDelta = 0, int healDelta = 0)
        {
            int index = teamMembers.Value.FindIndex(x => x.playerID == ID);
            TeamMember t = teamMembers.Value[index];
            teamMembers.Value[index] = new()
            {
                playerID = t.playerID,
                kills = t.kills + killDelta,
                deaths = t.deaths + deathDelta,
                assists = t.assists + assistDelta,
                revives = t.revives + reviveDelta,
                supportScore = t.supportScore + healDelta,
            };
        }
    }
}
