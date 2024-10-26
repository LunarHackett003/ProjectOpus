using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerManager : NetworkBehaviour
    {
        public Renderer[] outlineRenderers;
        public NetworkVariable<uint> teamIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> kills = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> deaths = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> assists = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> revives = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> supportPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Outline outlineComponent;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            teamIndex.OnValueChanged += UpdateTeamIndex;
            UpdateTeamIndex(0, teamIndex.Value);
        }
        public void UpdateTeamIndex(uint previous, uint current)
        {
            foreach (Renderer item in outlineRenderers)
            {
                if(item == null)
                {
                    print("no renderer!");
                    continue;
                }
                foreach (var item1 in item.materials)
                {
                    if(item1 == null)
                    {
                        print("no instanced material!");
                        continue;
                    }
                    item1.color = PlayerSettings.Instance.teamColours[current];
                }
            }
            if(MatchManager.Instance != null)
            {
                if(NetworkManager.LocalClientId != OwnerClientId)
                {
                    if(teamIndex.Value == MatchManager.Instance.clientsOnTeams.Value[NetworkManager.LocalClientId])
                    {
                        outlineComponent.enabled = true;
                    }
                }
            }
            outlineComponent.OutlineColor = PlayerSettings.Instance.teamColours[current];
        }
    }
}
