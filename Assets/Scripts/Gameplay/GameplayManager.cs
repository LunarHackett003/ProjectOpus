using opus.SteamIntegration;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameplayManager : NetworkBehaviour
{
    public NetworkVariable<float> moveSpeedMultiplier = new(1),
        gravityMultiplier = new(1),
        reloadSpeedMultiplier = new(1), 
        fireRateMultiplier = new(1),
        recoilMultiplier = new(1),
        inaccuracyMultiplier = new(1),
        damageMultiplier = new(1),
        fireDamageMultiplier = new(1),
        shieldsMultiplier = new(1),
        healthMultiplier = new(1),
        airControlMultiplier = new(1),
        healthRegenPerSec = new(1),
        shieldRegenPerSec = new(2),
        healthRegenDelay = new(10),
        shieldRegenDelay = new(5)
        ;
    public NetworkVariable<bool> infiniteTime = new(false), headshotsOnly = new(false), friendlyFire = new(false),
        regenShield = new(true), regenHealth = new(true);

    public NetworkVariable<float> teamAlphaScore = new(0), teamBetaScore = new(0);
    public NetworkVariable<uint> timeLeft = new(600);
    public NetworkVariable<bool> pregameLobby = new(true);


    public bool QueryTeam(ulong ID)
    {
        return teamMembers.Find(x => x.ID == ID).BravoTeam;
    }

    public NetworkVariable<List<TeamMember>> TeamMembers = new();
    [System.Serializable]
    public struct TeamMember
    {
        public ulong ID;
        public string Name;
        public bool BravoTeam;
    }
    public List<TeamMember> teamMembers;

    protected static GameplayManager instance;
    public static GameplayManager Instance { get
        {
            return instance;
        } 
    }
    /// <summary>
    /// How fast the player moves on ground, in metres per second.
    /// </summary>
    public static float BaseMoveSpeed { get; private set; } = 5;
    /// <summary>
    /// How much health the player has at max health
    /// </summary>
    public static float BaseHealth { get; private set; } = 100;
    /// <summary>
    /// How much shields the player has at max shields
    /// </summary>
    public static float BaseShields { get; private set; } = 50;
    /// <summary>
    /// How high the player jumps. Not a measure of the actual jump height itself, but the force applied, in newtons/KG, when the player jumps.
    /// </summary>
    public static float BaseJumpHeight { get; private set; } = 45;
    /// <summary>
    /// How much damage the player takes per second from fire
    /// </summary>
    public static float BaseFireDamage { get; private set; } = 20;
    /// <summary>
    /// How much, as a multiplier of the base movement speed, the player moves at
    /// </summary>
    public static float BaseAirControl { get; private set; } = 0.2f;
    public float playerSpeed, playerHealth, playerJumpHeight, fireDamage, playerShield;
    public uint gameTime = 600;
    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BaseMoveSpeed = playerSpeed;
        BaseHealth = playerHealth;
        BaseJumpHeight = playerJumpHeight;
        BaseFireDamage = fireDamage;
        BaseShields = playerShield;
        if (NetworkManager.IsServer && !IsSpawned)
        {
            NetworkObject.Spawn(gameObject);
        }

        InvokeRepeating(nameof(TickTimer), 1, 1);
    }
    void TeamMembersChanged(List<TeamMember> previous, List<TeamMember> current)
    {
        foreach (var item in current)
        {
            TeamMember t = new()
            {
                BravoTeam = item.BravoTeam,
                ID = item.ID
            };
            if (SteamLobbyManager.Instance.GetNamesOfTeamMembersAutomatically)
            {
                IEnumerable<Friend> friends = SteamLobbyManager.Instance.CurrentLobby.Value.Members;
                if (friends.Count() > 0)
                {
                    foreach (var friend in friends)
                    {
                        if (friend.Id.Value == item.ID)
                            t.Name = friend.Name;
                    }
                }
            }
            teamMembers.Add(t);
        }
    }
    private void OnEnable()
    {
        infiniteTime.OnValueChanged += InfiniteTimeChanged;
        TeamMembers.OnValueChanged += TeamMembersChanged;
        gravityMultiplier.OnValueChanged += GravityMultiplierChanged;

        SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected += LobbyMemberDisconnected;

    }

    private void LobbyMemberDisconnected(Steamworks.Data.Lobby arg1, Friend arg2)
    {
        TeamMember t = TeamMembers.Value.Find(x => x.ID == arg2.Id);
        TeamMembers.Value.Remove(t);
        print($"removed player {arg2.Name} from team {(t.BravoTeam ? "Bravo" : "Alpha")}");
    }
    private void LobbyMemberJoined(Steamworks.Data.Lobby arg1, Friend arg2)
    {
        AssignTeam(arg2);
    }
    bool CheckTeamNumbers()
    {

        int alphaMembers = teamMembers.Count(x => !x.BravoTeam);
        int bravoMembers = teamMembers.Count(x => x.BravoTeam);

        return bravoMembers < alphaMembers;
    }
    private void OnDisable()
    {
        infiniteTime.OnValueChanged -= InfiniteTimeChanged;
        TeamMembers.OnValueChanged -= TeamMembersChanged;
        gravityMultiplier.OnValueChanged -= GravityMultiplierChanged;

        SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected -= LobbyMemberDisconnected;
    }
    public void GravityMultiplierChanged(float previous, float current)
    {
        Physics.gravity = (Vector3.down * 9.81f) * current;
    }
    public void InfiniteTimeChanged(bool previous, bool current)
    {
        if (!current)
            timeLeft.Value = gameTime;
    }
    public void AssignTeam(Friend f)
    {
        TeamMember t = new()
        {
            ID = f.Id,
            Name = f.Name
        };
        t.BravoTeam = CheckTeamNumbers();
        TeamMembers.Value.Add(t);
        print($"Added player to {t.BravoTeam}");

    }
    void TickTimer()
    {
        if (!pregameLobby.Value && timeLeft.Value > 0)
        timeLeft.Value -= 1;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IEnumerable<Friend> lobbyMembers = SteamLobbyManager.Instance.CurrentLobby.Value.Members;
        foreach (var item in lobbyMembers)
        {
            AssignTeam(item);
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    void UpdateGameMode()
    {

    }
    
}
