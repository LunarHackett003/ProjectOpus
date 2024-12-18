using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "New Equipment List", menuName = "Scriptable Objects/Equipment List")]
    public class EquipmentList : ScriptableObject
    {
        public EquipmentContainerSO[] equipment;
    }
}
