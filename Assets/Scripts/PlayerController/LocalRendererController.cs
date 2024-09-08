using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class LocalRendererController : NetworkBehaviour
    {
        public string localLayerName = "LocalPlayer", remoteLayerName = "Default";
        public GameObject[] renderers;
        public Renderer[] shadowOnlyRenderers;
        public bool overrideCameraLayers;
        public LayerMask cameraLayerMask;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                int localMask = LayerMask.NameToLayer(localLayerName);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].layer = localMask;
                }
                for (int i = 0; i < shadowOnlyRenderers.Length; i++)
                {
                    if (shadowOnlyRenderers[i] != null)
                        shadowOnlyRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }

                if (overrideCameraLayers)
                {
                    Camera.main.cullingMask = cameraLayerMask;
                }
            }
            else
            {
                int remoteMask = LayerMask.NameToLayer(remoteLayerName);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].layer = remoteMask;
                }
            }

        }
    }
}
