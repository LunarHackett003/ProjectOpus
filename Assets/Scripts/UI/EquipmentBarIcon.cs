using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class EquipmentBarIcon : OBehaviour
    {

        public PlayerHUD HUD;

        BaseEquipment be;
        EquipmentContainerSO ecso;
        public TMP_Text counter;
        public Image baseImage, fillImage, selectionImage;
        public bool usesAmmo, usesCharge;
        public void AssignIcon(BaseEquipment be)
        {
            ecso = be.equipmentContainer;
            this.be = be;
            usesAmmo = be is RangedWeapon bw && bw.maxAmmo > 0;
            usesCharge = be.HasLimitedCharges;

            fillImage.enabled = usesCharge;
            counter.gameObject.SetActive(usesAmmo || usesCharge);

            baseImage.sprite = fillImage.sprite = selectionImage.sprite = ecso.hotbarIcon;

        }

        private void Reset()
        {
            HUD = GetComponentInParent<PlayerHUD>();
        }

        public void UpdateIcon()
        {
            if (be != null)
            {
                selectionImage.enabled = HUD.wc.slots[HUD.wc.weaponIndex.Value] == be || (HUD.wc.usingSpecial.Value && HUD.wc.specialEquipment == be);

                if (usesCharge)
                {
                    counter.text = be.currentCharges.ToString();
                    fillImage.fillAmount = be.currentRechargeTime < be.rechargeTime ? Mathf.InverseLerp(0, be.rechargeTime, be.currentRechargeTime) : 0;
                }
                else if (usesAmmo)
                {
                    RangedWeapon rw = be as RangedWeapon;
                    counter.text = rw.CurrentAmmo.ToString();
                }
            }
        }
    }
}
