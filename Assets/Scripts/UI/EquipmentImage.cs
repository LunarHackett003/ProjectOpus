using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentImage : MonoBehaviour
{
    public float fill;
    public Image image;
    public bool useTextDisplay;
    public TMP_Text textDisplay;
    public BaseEquipment equipment;
    int _numUses;
    private void LateUpdate()
    {
        if (equipment.hasCooldown)
        {
            fill = Mathf.InverseLerp(equipment.cooldownDuration, 0, equipment.localCooldown);
        }
        else
        {
            fill = 1;
        }

        if(useTextDisplay)
        {
            if (!textDisplay.isActiveAndEnabled)
            {
                textDisplay.enabled = true;
                textDisplay.gameObject.SetActive(true);
            }

            if(_numUses != equipment.currentStoredUses.Value)
            {
                _numUses = equipment.currentStoredUses.Value;
                if (equipment.isWeapon && equipment is BaseWeapon w)
                {
                    textDisplay.text = $"{w.CurrentAmmo}/{w.maxAmmo}";
                }
                else
                {
                    textDisplay.text = $"{equipment.currentStoredUses.Value}/{equipment.storedUses}";
                }
            }
        }
        image.fillAmount = fill;
    }
}
