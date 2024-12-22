using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class LoadoutUI : MonoBehaviour
    {
        public PlayerManager pm;

        public static LoadoutUI Instance {  get; private set; }

        public RectTransform contentRoot;
        Slot currentSelectedSlot;

        public EquipmentList weapons;
        public EquipmentList gadgets;

        public EquipmentList defaultWeapons, defaultGadgets;

        public GameObject equipmentItemPrefab;
        List<LoadoutItemDisplay> displayItems = new();

        private void Start()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                return;
            }
        }


        public void SetUpSlotSelection(Slot slot)
        {
            if(MatchManager.Instance != null)
            {
                weapons = MatchManager.Instance.weapons;
                gadgets = MatchManager.Instance.gadgets;
            }
            else
            {
                weapons = defaultWeapons;
                gadgets = defaultGadgets;
            }

            if (displayItems.Count > 0)
            {
                for (int i = displayItems.Count - 1; i >= 0; i--)
                {
                    Destroy(displayItems[i].gameObject);
                }
                displayItems.Clear();
            }
            if (currentSelectedSlot == slot)
            {

            }
            else {
                if (slot == Slot.primary)
                {
                    IterateAndCreateEquipmentButtons(weapons.equipment, slot);
                }
                else
                {
                    IterateAndCreateEquipmentButtons(gadgets.equipment, slot);
                }
            }
        }
        void IterateAndCreateEquipmentButtons(EquipmentContainerSO[] equipment, Slot slot)
        {
            for (int i = 0; i < equipment.Length; i++)
            {
                EquipmentContainerSO e = equipment[i];
                displayItems.Add(Instantiate(equipmentItemPrefab, contentRoot).GetComponent<LoadoutItemDisplay>());
                displayItems[i].Initialise(e);

                displayItems[i].button.onClick.AddListener(() =>
                {
                    SetEquipmentInSlot(Slot.primary, i);
                });
            }
        }

        void SetEquipmentInSlot(Slot slot, int equipmentIndex)
        {
            switch (slot)
            {
                case Slot.primary:
                    pm.primaryWeaponIndex = equipmentIndex;
                    break;
                case Slot.gadget1:
                    pm.gadget1Index = equipmentIndex;
                    break;
                case Slot.gadget2:
                    pm.gadget2Index = equipmentIndex;
                    break;
                case Slot.gadget3:
                    pm.gadget3Index = equipmentIndex;
                    break;
                case Slot.special:
                    pm.specialIndex = equipmentIndex;
                    break;
                default:
                    break;
            }
        }
    }
}
