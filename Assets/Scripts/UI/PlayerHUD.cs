using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerHUD : NetworkBehaviour
    {
        public PlayerManager manager;
        public PlayerEntity entity;
        public WeaponController wc;

        public Button readyButton;

        public int hudUpdateInterval;
        int updateTicks;

        public TMP_Text ammoCountText;

        public bool playerAlive;
        int cachedAmmoCount;

        public CanvasGroup deadUI;
        public CanvasGroup aliveUI;

        public TMP_Text respawnCounterText;

        public Slider healthBar;
        public TMP_Text healthValue;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                gameObject.SetActive(false);
                return;
            }
            else
            {
                if (manager == null)
                {
                    manager = GetComponent<PlayerManager>();
                }
                manager.timeUntilSpawn.OnValueChanged += UpdateRespawnTimer;
                UpdateRespawnTimer(0, manager.timeUntilSpawn.Value);
            }
        }

        public void UpdateRespawnTimer(int previous, int current)
        {
            if(current > 0)
            {
                respawnCounterText.text = $"Respawn in {current}";
            }
            else
            {
                respawnCounterText.text = $"Respawn Ready!";
            }
            readyButton.gameObject.SetActive(current <= 0);
        }


        public void InitialiseHUD()
        {
            if(manager != null)
            {
                entity = manager.Character;
                entity.currentHealth.OnValueChanged += HealthUpdated;
                healthBar.maxValue = entity.MaxHealth;
                healthBar.value = entity.CurrentHealth;
                //wc = manager.LivingPlayer.wc;
            }
        }
        public override void OnNetworkDespawn()
        {
            entity.currentHealth.OnValueChanged -= HealthUpdated;
            base.OnNetworkDespawn();
        }

        void HealthUpdated(float prev, float curr)
        {
            healthBar.value = curr;
            healthValue.text = $"{curr}/{entity.MaxHealth}";
        }


        private void Update()
        {
            if (manager == null || !IsOwner)
                return;

            playerAlive = manager.Character != null && manager.Character.currentHealth.Value > 0;

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

            
        }
    }
}
