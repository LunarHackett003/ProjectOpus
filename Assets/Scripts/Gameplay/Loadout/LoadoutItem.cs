using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "LoadoutItem", menuName = "Scriptable Objects/LoadoutItem")]
    public class LoadoutItem : ScriptableObject
    {
        public EquipmentSlot slot;
        public string displayName;
        public Sprite icon;
        public NetworkObject prefab;
    }

}
