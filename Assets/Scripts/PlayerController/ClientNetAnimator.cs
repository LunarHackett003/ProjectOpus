using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class ClientNetAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
