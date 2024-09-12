using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PauseMenu : NetworkBehaviour
    {
        public static bool GamePaused {  get; private set; }
        public GameObject pauseMenuRoot;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                PauseGame(false);
            }
        }
        public void PauseGame(bool paused)
        {
            GamePaused = paused;
            pauseMenuRoot.SetActive(paused);
            Cursor.lockState = paused ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public void DisconnectFromGame()
        {
            Cursor.lockState = CursorLockMode.None;
            SteamTester.Instance.DisconnectFromServer();
        }
    }
}
