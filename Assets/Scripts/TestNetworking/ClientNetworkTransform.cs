using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
