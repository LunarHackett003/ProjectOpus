using UnityEngine;

namespace Opus
{
    public class MainMenu : MonoBehaviour
    {
        public void StartGame()
        {
            SteamTester.Instance.StartHost();
        }
    }
}
