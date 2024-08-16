using UnityEngine;

[CreateAssetMenu(fileName = "RecoilProfile", menuName = "Scriptable Objects/RecoilProfile")]
public class RecoilProfile : ScriptableObject
{

    [Tooltip("How quickly to decay the aim pitch additive")]
    public float aimPitchDecaySpeed;

    [Tooltip("How much angular recoil is applied to the camera per shot to make the recoil more impactful.\nThe value on the x axis is permanent, while the y and z axis are temporary.")]
    public Vector3 minCamRecoilEuler, maxCamRecoilEuler;
    [Tooltip("How much linear recoil is applied to the camera per shot to make the recoil more impactful.")]
    public Vector3 minCamRecoilLinear, maxCamRecoilLinear;
    [Tooltip("How much angular recoil is applied to the weapon per shot.")]
    public Vector3 minWeaponRecoilEuler, maxWeaponRecoilEuler;
    [Tooltip("How much linear recoil is applied to the weapon per shot")]
    public Vector3 minWeaponRecoilLinear, maxWeaponRecoilLinear;
    [Tooltip("How slowly the player's camera moves towards the recoil vector.\nHigher values mean slower recoil.")]
    public float camRecoilSmoothness, camAngularRecoilDecay, weaponRecoilSmoothness, weaponAngularRecoilDecay;
    [Tooltip("How quickly the recoil vector returns to zero")]
    public float cameraRecoilDecay, weaponRecoilDecay;
    [Tooltip("How much the weapon's accuracy is affected by firing")]
    public float spreadAdditivePerShot;
    [Tooltip("How quickly the accumulated spread decays")]
    public float spreadDecay;
    
}
