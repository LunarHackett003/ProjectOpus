using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkLayerChange : NetworkBehaviour
{
    public const string remoteLayer = "RemotePlayer", localLayer = "LocalPlayer", localNoRenderLayer = "LocalPlayer_NoRender";
    public List<GameObject> renderersAffected;
    public List<GameObject> localHideLayer;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            foreach (var item in renderersAffected)
            {
                item.layer = LayerMask.NameToLayer(localLayer);
            }
            foreach (var item in localHideLayer)
            {
                item.layer = LayerMask.NameToLayer(localNoRenderLayer);
            }
        }
        else
        {
            foreach (var item in renderersAffected)
            {
                item.layer = LayerMask.NameToLayer(remoteLayer);
            }
        }
    }
}
