using TMPro;
using UnityEngine;

namespace Opus
{
    public class EquipmentBarHeader : OBehaviour
    {
        public TMP_Text counter, equipmentName;
        BaseEquipment equipment;
        public void AssignEquipment(BaseEquipment be)
        {
            equipment = be;
            equipmentName.text = equipment.equipmentContainer.displayName;
            if (equipment is RangedWeapon rw)
                UpdateEquipment($"{rw.CurrentAmmo}/{rw.maxAmmo}");
            else if(equipment.HasLimitedCharges)
                UpdateEquipment(equipment.currentCharges.ToString());
        }

        public void UpdateEquipment(string counterValue)
        {
            counter.text = counterValue;
        }
    }
}
