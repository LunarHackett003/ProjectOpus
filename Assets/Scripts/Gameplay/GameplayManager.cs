using opus.SteamIntegration;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
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
        healthRegenPerSec = new(50),
        healthRegenDelay = new(10),
        shieldRegenDelay = new(5)
        ;
    public NetworkVariable<bool> infiniteTime = new(false), headshotsOnly = new(false), friendlyFire = new(false), regenHealth = new(true);

    public NetworkVariable<float> teamAlphaScore = new(0), teamBetaScore = new(0);
    public NetworkVariable<uint> timeLeft = new(600);
    public NetworkVariable<bool> pregameLobby = new(true);

    [SerializeField] internal LayerMask bulletLayermask;
    public bool IsBravoTeam(ulong ID)
    {
        return teamMembers.Find(x => x.steamID == ID).BravoTeam;
    }
    /// <summary>
    /// Checks if two players are on another team.
    /// </summary>
    /// <param name="id_A">The ID of the first player</param>
    /// <param name="id_B">The ID of the second player</param>
    /// <returns></returns>
    public bool IsOppositeTeam(ulong id_A, ulong id_B)
    {
        return teamMembers.Find(x => x.steamID == id_A).BravoTeam != teamMembers.Find(x => x.steamID == id_B).BravoTeam;
    }
    public NetworkVariable<List<TeamMember>> TeamMembers = new();
    [System.Serializable]
    public struct TeamMember
    {
        public ulong steamID;
        public string Name;
        public bool BravoTeam;
        public NetworkObjectReference player;
    }
    public List<TeamMember> teamMembers = new();

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
    /// How high the player jumps. Not a measure of the actual jump height itself, but the force applied, in newtons/KG, when the player jumps.
    /// </summary>
    public static float BaseJumpSpeed { get; private set; } = 45;
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

    public static float MaxHealth => BaseHealth * instance.healthMultiplier.Value;

    public NetworkVariable<bool> allowRespawns = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> respawnTime = new(writePerm: NetworkVariableWritePermission.Server);
    public List<GameObject> grenadeTypes;
    public LayerMask fireMask;
    public float fireTickTime = 0.5f, fireDamagePerTick = 10f;
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
        BaseJumpSpeed = playerJumpHeight;
        BaseFireDamage = fireDamage;
        if (NetworkManager.IsServer && !IsSpawned)
        {
            NetworkObject.Spawn(gameObject);
        }

        InvokeRepeating(nameof(TickTimer), 1, 1);
    }
    void TeamMembersChanged(List<TeamMember> previous, List<TeamMember> current)
    {
        teamMembers = current;
        SteamLobbyManager.Instance.scoreboard.TeamsUpdated(previous, current);
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
        TeamMember t = TeamMembers.Value.Find(x => x.steamID == arg2.Id);
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
            steamID = f.Id,
            Name = f.Name,
            BravoTeam = CheckTeamNumbers(),
            
        };
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
