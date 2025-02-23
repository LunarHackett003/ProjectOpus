using UnityEngine;

namespace Opus
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        public bool gamePaused;
        public bool IsPaused => gamePaused;
        bool pausedLast;
        public bool IsPausedValue;

        public bool cursorFreed;
        bool cursorFreedLast;
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

        private void Update()
        {
            IsPausedValue = IsPaused;
            if(IsPausedValue != pausedLast)
            {
                pausedLast = IsPausedValue;
                PauseGame(IsPausedValue);
            }
            if (cursorFreed != cursorFreedLast)
            {
                cursorFreedLast = cursorFreed;
                FreeCursor(cursorFreed);
            }
        }

        public void PauseGame(bool input)
        {
            FreeCursor(input);
        }
        public void FreeCursor(bool input)
        {
            cursorFreed = input || MatchManager.Instance == null;
            Cursor.lockState = input || MatchManager.Instance == null ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = input || MatchManager.Instance == null;
        }
    }
}
