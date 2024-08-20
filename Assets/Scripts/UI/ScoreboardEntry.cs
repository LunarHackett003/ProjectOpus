using opus.utility;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardEntry : MonoBehaviour
{
    public ulong steamID;
    public string playerName;
    public RawImage steamImage;
    public TMP_Text text;

    private async void Start()
    {
        text.text = playerName;

        var avatar = UtilityMethods.GetAvatar();
        await Task.WhenAll(avatar);
        steamImage.texture = avatar.Result?.Convert();
    }
}
