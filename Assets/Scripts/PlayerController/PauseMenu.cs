using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PauseMenu : NetworkBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public bool GamePaused;
        public GameObject pauseMenuRoot;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                Instance = this;
                PauseGame(false);
            }
        }
        public void PauseGame(bool paused)
        {
            GamePaused = paused;
            pauseMenuRoot.SetActive(paused);
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void DisconnectFromGame()
        {
            Cursor.lockState = CursorLockMode.None;
            SteamTester.Instance.DisconnectFromServer();
        }
    }
}
