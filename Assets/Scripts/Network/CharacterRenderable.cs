using FMODUnity;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class CharacterRenderable : NetworkBehaviour
    {
        public Renderer[] renderers;
        public Renderer[] viewmodelRenderer;
        public Renderer[] hideOnHostRenderers;
        
        public int localRender;

        PlayerManager owningPlayer;

        public void InitialiseViewable(PlayerEntity fromThis)
        {
            owningPlayer = fromThis.playerManager;
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
                foreach (var item in viewmodelRenderer)
                {
                    print("Changed layer for local renderer");
                    item.gameObject.layer = localRender;
                }
            }

            if (fromThis.outlineComponent)
            {
                if (!IsOwner)
                {
                    if (owningPlayer.teamIndex.Value != PlayerManager.MyTeam)
                    {
                        fromThis.outlineComponent.enabled = true;
                        fromThis.outlineComponent.OutlineMode = Outline.Mode.OutlineVisible;
                        fromThis.outlineComponent.OutlineColor = PlayerSettings.Instance.teamColours[owningPlayer.teamIndex.Value];
                    }
                    else
                    {
                        fromThis.outlineComponent.enabled = true;
                        fromThis.outlineComponent.OutlineMode = Outline.Mode.OutlineAll;
                        fromThis.outlineComponent.OutlineColor = owningPlayer.myTeamColour;
                    }
                }
            }
        }
    }
}
