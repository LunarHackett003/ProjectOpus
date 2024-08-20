using opus.Gameplay;
using UnityEngine;

public class ScreenEffectsController : MonoBehaviour
{
    [SerializeField] Material m;
    [SerializeField] FullScreenPassRendererFeature mFeature;
    [SerializeField] float noiseStrengthPerHP, pixelateStrengthPerHP, edgePixelStrengthPerHP;
    [SerializeField] float noiseStrengthDecay, edgePixelStrengthDecay, pixelateDecay;
    [SerializeField] float corruptedPixelate;
    float noiseStrengthAcc, pixelAcc, edgePixelAcc, colourSepAcc;
    [SerializeField] AnimationCurve colourSeparationHealthCurve;
    private void Update()
    {
        mFeature.SetActive(PlayerManager.Instance.pc);
        if (GameplayManager.Instance)
        {
            ProcessEffects();
        }
    }
    public void TakeDamage(float damageReceived)
    {
        edgePixelAcc += edgePixelStrengthPerHP * damageReceived;
    }
    public void ProcessEffects()
    {
        //Update edge pixelation
        colourSepAcc = Mathf.Max(0, colourSeparationHealthCurve.Evaluate(Mathf.InverseLerp(GameplayManager.MaxHealth, 0, PlayerManager.Instance.pc.currentHealth.Value)));
        edgePixelAcc = Mathf.Max(edgePixelAcc - Time.deltaTime * edgePixelStrengthDecay, 0);
        float corruptValue = PlayerManager.Instance.pc.corrupted.Value ? corruptedPixelate : pixelAcc - Time.deltaTime * pixelateDecay;
        pixelAcc = Mathf.Max(0, corruptValue);
        noiseStrengthAcc = Mathf.Max(0, corruptValue);
        m.SetFloat(Shader.PropertyToID("_EdgePixelationStrength"), edgePixelAcc);
        m.SetFloat(Shader.PropertyToID("_ColourSeparationPower"), colourSepAcc);
        m.SetFloat(Shader.PropertyToID("_PixelationStrength"), pixelAcc);
        m.SetFloat(Shader.PropertyToID("_HorizontalNoiseStrength"), noiseStrengthAcc);
    }
    
}
