using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerHUD : MonoBehaviour
    {
        public PlayerManager manager;
        public PlayerController controller;

        public int hudUpdateInterval;
        int updateTicks;

        public Image mechReadinessImage;
        public TMP_Text mechReadinessText;

        public void InitialiseHUD()
        {
            controller = GetComponentInParent<PlayerController>();
            //force a recompile?
            manager = controller.MyPlayerManager;
        }

        private void Update()
        {
            updateTicks++;
            if(updateTicks > hudUpdateInterval)
            {
                updateTicks = 0;
                UpdateHUD();
            }
        }
        void UpdateHUD()
        {
            if (manager == null)
                return;
            if (!manager.mechDeployed.Value)
            {
                mechReadinessImage.fillAmount = manager.specialPercentage_noSync;
                mechReadinessText.text = $"{manager.specialPercentage_noSync * 100:0}%";
            }
        }
    }
}
