using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class LoadoutItemDisplay : MonoBehaviour
    {
        public TMP_Text nameDisplay;
        public TMP_Text descriptionDisplay;

        public EquipmentContainerSO equipmentSO;

        public Button button;

        public void Initialise(EquipmentContainerSO so)
        {
            equipmentSO = so;
            if(nameDisplay != null )
            {
                nameDisplay.text = so.name;
            }
            if(descriptionDisplay != null)
            {
                descriptionDisplay.text = so.description;
            }
        }
    }
}
