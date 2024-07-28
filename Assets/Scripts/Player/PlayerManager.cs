using opus.SteamIntegration;
using UnityEngine;
using UnityEngine.InputSystem;
namespace opus.Gameplay {
    public class PlayerManager : MonoBehaviour
    {
        protected static PlayerManager instance;
        public static PlayerManager Instance { 
            get
            {
                if (!instance)
                {
                    return new GameObject("PlayerManager").AddComponent<PlayerManager>();
                }
                return instance;
            } 
        }

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [SerializeField] protected GameObject menuCanvas;

        [SerializeField] protected bool paused;
        public bool InGame => !paused && SteamLobbyManager.Instance.InLobby;


        public Vector2 lookSpeed;
        public bool invertLookY;


        public Vector2 moveInput, lookInput;
        public bool sprintInput, crouchInput, fireInput, aimInput, jumpInput;

        public void SetPause(bool input)
        {
            if (SteamLobbyManager.Instance.InLobby)
            {
                paused = input;
            }
            else
            {
                paused = true;
            }

            menuCanvas.SetActive(!InGame);
            Cursor.lockState = InGame ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !InGame;
    }
        public void TogglePause()
        {
            SetPause(!paused);
        }

        #region InputCallbacks
        public void GetMoveInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                moveInput = context.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }
        }
        public void GetLookInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                lookInput = context.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }
        }
        public void GetPauseInput(InputAction.CallbackContext context)
        {
            if (context.performed)
                TogglePause();
        }
        public void GetJumpInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {

            }
            else
            {

            }
        }
        public void GetSprintInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {

            }
            else
            {

            }
        }
        public void GetCrouchInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                
            }
            else
            {

            }
        }

        #endregion
    }
}