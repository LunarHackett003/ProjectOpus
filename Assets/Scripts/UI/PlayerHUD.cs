using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerHUD : NetworkBehaviour
    {
        public PlayerManager manager;
        public PlayerController controller;
        public WeaponController wc;


        public int hudUpdateInterval;
        int updateTicks;

        public Image mechReadinessImage;
        public TMP_Text mechReadinessText;

        public TMP_Text ammoCountText;

        public bool playerAlive;
        int cachedAmmoCount;

        public CanvasGroup deadUI;
        public CanvasGroup aliveUI;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        private void Start()
        {
            if (manager == null)
            {
                manager = GetComponent<PlayerManager>();
            }
        }
        public void InitialiseHUD()
        {
            if(manager != null)
            {
                controller = manager.LivingPlayer;
                wc = manager.LivingPlayer.wc;
            }
        }

        private void Update()
        {
            if (manager == null || !IsOwner)
                return;

            playerAlive = manager.LivingPlayer != null && manager.LivingPlayer.currentHealth.Value > 0;

            updateTicks++;
            if(updateTicks > hudUpdateInterval)
            {
                updateTicks = 0;
                UpdateHUD();
            }

            if(wc != null)
            {
                if(wc.GetCurrentEquipment() is RangedWeapon w)
                {
                    if(w.CurrentAmmo != cachedAmmoCount)
                        UpdateAmmoCount(w);
                }
                else
                {
                    if (ammoCountText.gameObject.activeInHierarchy)
                    {
                        ammoCountText.gameObject.SetActive(false);
                    }
                }
            }
        }
        void UpdateAmmoCount(RangedWeapon w)
        {
            if (ammoCountText)
            {
                if (!ammoCountText.gameObject.activeInHierarchy)
                {
                    ammoCountText.gameObject.SetActive(true);
                }
                ammoCountText.text = $"{w.CurrentAmmo}/{w.maxAmmo}";
            }
            cachedAmmoCount = w.CurrentAmmo;
        }
        void UpdateHUD()
        {
            if (manager == null)
                return;

            deadUI.alpha = playerAlive ? 0 : 1;
            deadUI.blocksRaycasts = deadUI.interactable = !playerAlive;
            aliveUI.alpha = playerAlive ? 1 : 0;

            if (mechReadinessImage != null && mechReadinessText != null && !manager.mechDeployed.Value)
            {
                mechReadinessImage.fillAmount = manager.specialPercentage_noSync;
                mechReadinessText.text = $"{manager.specialPercentage_noSync * 100:0}%";
            }
        }
    }
}
