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
    private void Start()
    {
        if(equipment is BaseWeapon b)
        {
            b.currentAmmo.OnValueChanged += TextUpdate;
            _numUses = b.currentAmmo.Value;
        }
        else
        {
            equipment.currentStoredUses.OnValueChanged += TextUpdate;
            _numUses = equipment.currentStoredUses.Value;
        }
        TextUpdate(0, _numUses);
    }
    public void TextUpdate(int previous, int current)
    {
        if (useTextDisplay)
        {
            if (!textDisplay.isActiveAndEnabled)
            {
                textDisplay.enabled = true;
                textDisplay.gameObject.SetActive(true);
            }

            if (equipment is BaseWeapon b)
            {
                textDisplay.text = $"{b.currentAmmo.Value}/{b.maxAmmo}";
            }
            else
            {
                textDisplay.text = $"{equipment.currentStoredUses.Value}/{equipment.storedUses}";
            }

        }
    }
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
        image.fillAmount = fill;
    }
}
