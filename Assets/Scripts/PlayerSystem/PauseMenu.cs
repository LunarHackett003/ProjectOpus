using UnityEngine;

namespace Opus
{
    public class PauseMenu : OBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        public bool paused = false;
        public bool cursorFree = true;
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void QuitGame()
        {
            Application.Quit(15);
        }

        public override void OUpdate()
        {
            Cursor.lockState = paused || cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
