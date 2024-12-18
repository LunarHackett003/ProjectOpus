using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Scriptable Objects/Equipment Container")]
    public class EquipmentContainerSO : ScriptableObject
    {
        public NetworkObject equipmentPrefab;
        public string displayName;
        [TextArea(1, 5)]
        public string description;
        [SerializeField, HideInInspector]
        BaseEquipment be;
        private void OnValidate()
        {
            if(equipmentPrefab != null)
            {
                if(be != null && be.gameObject.name != equipmentPrefab.gameObject.name)
                {
                    be = null;
                }
                if(be == null)
                {
                    if(equipmentPrefab.TryGetComponent(out be))
                    {
                        Debug.Log("Found and assigned a weapon");
                    }
                    else
                    {
                        equipmentPrefab = null;
                        Debug.LogWarning("Invalidated prefab - the assigned prefab did not have an equipment component!");
                    }
                }
            }
        }
    }
}
