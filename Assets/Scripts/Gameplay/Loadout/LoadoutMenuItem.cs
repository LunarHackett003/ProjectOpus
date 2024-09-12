using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class LoadoutMenuItem : MonoBehaviour
    {
        public Image iconDisplay;
        public TMP_Text nameDisplay;

        public LoadoutItem associatedItem;

        public Button button;
        public void UpdateUI(LoadoutItem item = null, Sprite defaultSprite = null)
        {
            if(item == null)
            {
                associatedItem = null;
                iconDisplay.sprite = defaultSprite;
                nameDisplay.text = "NULL";
            }
            else
            {
                associatedItem = item;
                iconDisplay.sprite = item.icon;
                nameDisplay.text = item.displayName;
            }
        }
    }
}
