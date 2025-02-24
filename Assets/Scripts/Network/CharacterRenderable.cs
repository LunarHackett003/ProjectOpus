using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class CharacterRenderable : NetworkBehaviour
    {
        public Renderer[] renderers;
        public Renderer[] viewmodelRenderer;
        public Renderer[] hideOnHostRenderers;

        public Outline outlineComponent;

        public int localRender = 6;

        PlayerManager owningPlayer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public void InitialiseViewable()
        {
            owningPlayer = PlayerManager.playersByID[OwnerClientId];
            if (IsOwner)
            {

                foreach (var item in hideOnHostRenderers)
                {
                    if (item != null)
                    {
                        item.enabled = false;
                    }
                }
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                    renderer.material.color = owningPlayer.myTeamColour;
            }
            
            if (IsOwner)
            {
                foreach (var item in hideOnHostRenderers)
                {
                    if(item != null)
                        item.enabled = false;
                }
                foreach (var item in viewmodelRenderer)
                {
                    print("Changed layer for local renderer");
                    item.gameObject.layer = localRender;
                }

                if (outlineComponent != null)
                {
                    outlineComponent.enabled = false;
                }
            }
            else
            {
                if(outlineComponent != null)
                {
                    outlineComponent.OutlineColor = PlayerSettings.Instance.teamColours[owningPlayer.teamIndex.Value];
                    outlineComponent.OutlineMode = owningPlayer.teamIndex.Value != PlayerManager.playersByID[NetworkManager.LocalClientId].teamIndex.Value ? Outline.Mode.OutlineHidden : Outline.Mode.OutlineVisible;
                }
            }
        }
    }
}
