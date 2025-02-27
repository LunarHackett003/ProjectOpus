﻿using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerHUD : ONetBehaviour
    {
        public PlayerManager manager;
        public PlayerEntity entity;
        public WeaponControllerV2 wc;

        public Button readyButton;

        public int hudUpdateInterval;
        int updateTicks;

        public bool playerAlive;
        int cachedAmmoCount;

        public CanvasGroup deadUI;
        public CanvasGroup aliveUI;

        public TMP_Text respawnCounterText;

        public Slider healthBar;
        public TMP_Text healthValue;

        public CanvasGroup chargeSliderCG;
        public Slider chargeSlider;

        bool[] playingDamageType = new bool[4];
        Coroutine[] damageTypeCoroutines = new Coroutine[4];

        public CanvasGroup[] hitmarkerCGs;
        public float hitmarkerFadeSpeed = 3;

        public CanvasGroup interactCG;
        public TMP_Text interactText;


        public EquipmentBarIcon[] equipmentBarIcons;
        public EquipmentBarIcon specialIcon;
        public EquipmentBarHeader equipmentBarHeader;
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
                wc = entity.wc;
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
            healthValue.text = $"{curr:0}/{entity.MaxHealth}";
        }


        public override void OUpdate()
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
                if(wc.slots[wc.weaponIndex.Value] is RangedWeapon w)
                {
                    UpdateWeaponHud(w);
                    equipmentBarHeader.UpdateEquipment($"{w.CurrentAmmo}/{w.maxAmmo}");
                }
                else
                {
                    equipmentBarHeader.UpdateEquipment(wc.slots[wc.weaponIndex.Value].HasLimitedCharges ? wc.slots[wc.weaponIndex.Value].currentCharges.ToString() : "∞");
                }
                for (int i = 0; i < equipmentBarIcons.Length; i++)
                {
                    equipmentBarIcons[i].UpdateIcon();
                }
                specialIcon.UpdateIcon();
            }
        }
        void UpdateWeaponHud(RangedWeapon w)
        {
            if (chargeSliderCG)
            {
                chargeSliderCG.alpha = w.chargeSpeed > 0 ? 1 : 0;
                chargeSlider.value = w.CurrentCharge;
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
        
        public void PlayHitmarker(DamageType damageType)
        {
            int dt = (int)damageType;
            if (playingDamageType[dt] && damageTypeCoroutines[dt] != null)
            {
                StopCoroutine(damageTypeCoroutines[dt]);
            }
            damageTypeCoroutines[dt] = StartCoroutine(Hitmarker(dt));
        }
        IEnumerator Hitmarker(int damageType)
        {
            float t = 1;
            while (t > 0)
            {
                t -= Time.fixedDeltaTime * hitmarkerFadeSpeed;
                hitmarkerCGs[damageType].alpha = t;
                yield return new WaitForFixedUpdate();
            }
        }

        public void SetInteractText(string value = "do a thing!", bool show = false)
        {
            interactCG.alpha = show ? 1 : 0;
            if (show)
            {
                interactText.text = value;
            }
        }
    }
}
