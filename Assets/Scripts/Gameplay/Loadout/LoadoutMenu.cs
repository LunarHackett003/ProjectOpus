using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class LoadoutMenu : MonoBehaviour
    {
        public Sprite invalidSelectionSprite;
        public LoadoutMenuItem primary, secondary, gadgetOne, gadgetTwo, special;
        public EquipmentSlot currentSelection = 0;

        public GameObject loadoutMenuItemPrefab;
        public Transform selectionOptionsParent;
        public List<LoadoutMenuItem> selectionOptions;
        public GameObject selectionDisplayRoot;

        private void Start()
        {
            primary.UpdateUI(null, invalidSelectionSprite);
            secondary.UpdateUI(null, invalidSelectionSprite);
            gadgetOne.UpdateUI(null, invalidSelectionSprite);
            gadgetTwo.UpdateUI(null, invalidSelectionSprite);
            special.UpdateUI(null, invalidSelectionSprite);
            if (LoadoutManager.Instance != null)
            {
                if(LoadoutManager.Instance.primaryIndex != -1)
                    primary.UpdateUI(LoadoutManager.Instance.ValidLoadoutItemContainer.primary[LoadoutManager.Instance.primaryIndex]);
                if(LoadoutManager.Instance.secondaryIndex != -1)
                    secondary.UpdateUI(LoadoutManager.Instance.ValidLoadoutItemContainer.secondary[LoadoutManager.Instance.secondaryIndex]);
                if (LoadoutManager.Instance.gadget1Index != -1)
                    gadgetOne.UpdateUI(LoadoutManager.Instance.ValidLoadoutItemContainer.gadget[LoadoutManager.Instance.gadget1Index]);
                if (LoadoutManager.Instance.gadget2Index != -1)
                    gadgetTwo.UpdateUI(LoadoutManager.Instance.ValidLoadoutItemContainer.gadget[LoadoutManager.Instance.gadget2Index]);
                if (LoadoutManager.Instance.specialIndex != -1)
                    special.UpdateUI(LoadoutManager.Instance.ValidLoadoutItemContainer.special[LoadoutManager.Instance.specialIndex]);
            }
            primary.button.onClick.AddListener(() => CheckSlotAndDoStuff(EquipmentSlot.primary));
            secondary.button.onClick.AddListener(() => CheckSlotAndDoStuff(EquipmentSlot.secondary));
            gadgetOne.button.onClick.AddListener(() => CheckSlotAndDoStuff(EquipmentSlot.gadget1));
            gadgetTwo.button.onClick.AddListener(() => CheckSlotAndDoStuff(EquipmentSlot.gadget2));
            special.button.onClick.AddListener(() => CheckSlotAndDoStuff(EquipmentSlot.special));

            selectionDisplayRoot.gameObject.SetActive(false);

        }
        public void CheckSlotAndDoStuff(EquipmentSlot slot)
        {
            if (currentSelection == EquipmentSlot.none)
            {
                selectionDisplayRoot.SetActive(BuildCurrentSelection(slot));
            }
            else
            {
                print("Deselected current slot");
                currentSelection = EquipmentSlot.none;
                DestroyCurrentSelectionList();
                selectionDisplayRoot.SetActive(false);
            }
        }

        public bool BuildCurrentSelection(EquipmentSlot slot)
        {
            currentSelection = slot;
            DestroyCurrentSelectionList();

            List<LoadoutItem> validItems = slot switch
            {
                EquipmentSlot.none => null,
                EquipmentSlot.primary => new(LoadoutManager.Instance.ValidLoadoutItemContainer.primary),
                EquipmentSlot.secondary => new(LoadoutManager.Instance.ValidLoadoutItemContainer.secondary),
                EquipmentSlot.gadget1 => new(LoadoutManager.Instance.ValidLoadoutItemContainer.gadget),
                EquipmentSlot.gadget2 => new(LoadoutManager.Instance.ValidLoadoutItemContainer.gadget),
                EquipmentSlot.special => new(LoadoutManager.Instance.ValidLoadoutItemContainer.special),
                _ => null,
            };
            if (validItems == null || validItems.Count == 0)
                return false;

            for (int i = 0; i < validItems.Count; i++)
            {
                LoadoutMenuItem item = Instantiate(loadoutMenuItemPrefab, selectionOptionsParent).GetComponent<LoadoutMenuItem>();
                item.UpdateUI(validItems[i], invalidSelectionSprite);
                item.button.onClick.AddListener(() => AssignCurrentItem(item.associatedItem));
                selectionOptions.Add(item);
            }
            return true;
        }
        public void DestroyCurrentSelectionList()
        {
            if (selectionOptions.Count > 0)
            {
                for (int i = 0; i < selectionOptions.Count; i++)
                {
                    Destroy(selectionOptions[i].gameObject);
                }
                selectionOptions.Clear();
            }
        }
        public void AssignCurrentItem(LoadoutItem item)
        {
            switch (item.slot)
            {
                case EquipmentSlot.none:
                    print("Invalid slot!");
                    break;
                case EquipmentSlot.primary:
                    LoadoutManager.Instance.primaryIndex = LoadoutManager.Instance.ValidLoadoutItemContainer.primary.IndexOf(item);
                    primary.UpdateUI(item);
                    break;
                case EquipmentSlot.secondary:
                    LoadoutManager.Instance.secondaryIndex = LoadoutManager.Instance.ValidLoadoutItemContainer.secondary.IndexOf(item);
                    secondary.UpdateUI(item);
                    break;
                case EquipmentSlot.gadget1:
                    LoadoutManager.Instance.gadget1Index = LoadoutManager.Instance.ValidLoadoutItemContainer.gadget.IndexOf(item);
                    gadgetOne.UpdateUI(item);
                    break;
                case EquipmentSlot.gadget2:
                    LoadoutManager.Instance.gadget2Index = LoadoutManager.Instance.ValidLoadoutItemContainer.gadget.IndexOf(item);
                    gadgetTwo.UpdateUI(item);
                    break;
                case EquipmentSlot.special:
                    LoadoutManager.Instance.specialIndex = LoadoutManager.Instance.ValidLoadoutItemContainer.special.IndexOf(item);
                    special.UpdateUI(item);
                    break;
                default:
                    print("Invalid slot!");
                    break;
            }
            LoadoutManager.Instance.UpdateLoadoutNumbers();
            selectionDisplayRoot.gameObject.SetActive(false);
            currentSelection = EquipmentSlot.none;
        }
    }
}
