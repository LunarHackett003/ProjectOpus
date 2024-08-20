using opus.SteamIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySettingsSetter : MonoBehaviour
{

    public Slider moveSpeedSlider, gravitySlider, reloadSpeedSlider, fireRateSlider, airControlSlider,
        recoilSlider, inaccuracySlider, damageSlider, fireDamageSlider, healthSlider, healthRegenDelay, healthRegenSpeed;
    public Toggle infiniteTimeToggle, headshotToggle, friendlyFireToggle, shieldRegenToggle, healthRegenToggle;
    public TMP_Text moveSpeedText, gravityText, reloadSpeedText, fireRateText, airControlText, recoilText,
        inaccuracyText, damageText, fireDamageText, healthText,
        healthRegenDelayText, healthRegenSpeedText;

    private void OnEnable()
    {
        if (GameplayManager.Instance)
        {
            moveSpeedSlider.value = GameplayManager.Instance.moveSpeedMultiplier.Value;
            gravitySlider.value = GameplayManager.Instance.gravityMultiplier.Value;
            reloadSpeedSlider.value = GameplayManager.Instance.reloadSpeedMultiplier.Value;
            fireRateSlider.value = GameplayManager.Instance.fireRateMultiplier.Value;
            airControlSlider.value = GameplayManager.Instance.airControlMultiplier.Value;
            recoilSlider.value = GameplayManager.Instance.recoilMultiplier.Value;
            inaccuracySlider.value = GameplayManager.Instance.inaccuracyMultiplier.Value;
            damageSlider.value = GameplayManager.Instance.damageMultiplier.Value;
            fireDamageSlider.value = GameplayManager.Instance.fireDamageMultiplier.Value;
            healthSlider.value = GameplayManager.Instance.healthMultiplier.Value;
            healthRegenDelay.value = GameplayManager.Instance.healthRegenDelay.Value;
            healthRegenSpeed.value = GameplayManager.Instance.healthRegenPerSec.Value;

            infiniteTimeToggle.isOn = GameplayManager.Instance.infiniteTime.Value;
            headshotToggle.isOn = GameplayManager.Instance.headshotsOnly.Value;
            friendlyFireToggle.isOn = GameplayManager.Instance.friendlyFire.Value;
            healthRegenToggle.isOn = GameplayManager.Instance.regenHealth.Value;
        }    
    }
    public void ResetSlider(Slider s)
    {
        s.value = 1;
        s.onValueChanged.Invoke(s.value);
    }


    float ClampAmount(float amount)
    {
        return Mathf.Max(amount, 0);
    }
    public void SetMoveSpeedMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;

        GameplayManager.Instance.moveSpeedMultiplier.Value = ClampAmount(amount);
        moveSpeedText.text = $"Move Speed: x{amount:0.00}";
    }
    public void SetGravityMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.gravityMultiplier.Value = ClampAmount(amount);
        gravityText.text = $"Gravity: x{amount:0.00}";
    }
    public void SetReloadSpeedMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.reloadSpeedMultiplier.Value = ClampAmount(amount);
        reloadSpeedText.text = $"Reload Speed: x{amount:0.00}";

    }
    public void SetFireRateMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.fireRateMultiplier.Value = ClampAmount(amount);
        fireRateText.text = $"Fire Rate: x{amount:0.00}";
    }
    public void SetRecoilMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.recoilMultiplier.Value = ClampAmount(amount);
        recoilText.text = $"Recoil: x{amount:0.00}";
    }
    public void SetInaccuracyMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.inaccuracyMultiplier.Value = ClampAmount(amount);
        inaccuracyText.text = $"Inaccuracy : x{amount:0.00}";
    }
    public void SetDamageMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.damageMultiplier.Value = ClampAmount(amount);
        damageText.text = $"Weapon Damage: x{amount:0.00}";
    }
    public void SetFireDamageMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.fireDamageMultiplier.Value = ClampAmount(amount);
        fireDamageText.text = $"Fire Damage: x{amount:0.00}";
    }
    public void SetHealthMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.healthMultiplier.Value = ClampAmount(amount);
        healthText.text = $"Max Health: x{amount:0.00}";

    }
    public void SetAirControlMultiplier(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.airControlMultiplier.Value = ClampAmount(amount);
        airControlText.text = $"Air Control: x{amount:0.00}";
    }
    public void SetHealthRegenDelay(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.healthRegenDelay.Value = ClampAmount(amount);
        healthRegenDelayText.text = $"Health Regen Delay: {amount}s";
    }
    public void SetHealthRegenSpeed(float amount)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.healthRegenPerSec.Value = ClampAmount(amount);
        healthRegenSpeedText.text = $"Health Regen: {amount:0.00}HP/sec";
    }

    public void SetInfiniteTime(bool value)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.infiniteTime.Value = value;
    }
    public void SetHeadshotsOnly(bool value)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.headshotsOnly.Value = value;
    }
    public void SetFriendlyFire(bool value)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.friendlyFire.Value = value;
    }
    public void SetHealthRegen(bool value)
    {
        if (!SteamLobbyManager.Instance.IsHost)
            return;
        GameplayManager.Instance.regenHealth.Value = value;
    }
}
