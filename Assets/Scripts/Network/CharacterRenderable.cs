using FMODUnity;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class CharacterRenderable : NetworkBehaviour
    {
        public Renderer[] renderers;
        public Renderer[] hideOnHostRenderers;

        PlayerManager owningPlayer;

        public void InitialiseViewable(PlayerController fromThis)
        {
            owningPlayer = fromThis.MyPlayerManager;
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
