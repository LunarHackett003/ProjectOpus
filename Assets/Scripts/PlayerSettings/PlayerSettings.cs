using Newtonsoft.Json;
using UnityEngine;

namespace Opus
{
    public class PlayerSettings : MonoBehaviour
    {
        #region Initialisation
        public static PlayerSettings Instance { get; private set; }
        public Color[] teamColours = new Color[]
        {
            new(0.3f, 0.7f, 1),
            new(1, 0.184f, 0.184f),
            new(0.733f, 0.49f, 1),
            new(0.42f, 0.98f, 0.365f)
        };
        private void Start()
        {
            Instance = this;
            DontDestroyOnLoad(Instance.gameObject);
            Debug.Log("Created Player Settings object.");
            Instance.FileSavePath = Application.persistentDataPath + "/config.json";
            Instance.LoadFile();
        }
        #endregion


        #region Player Parameters

        [System.Serializable]
        public struct SettingsContainer
        {
            public SettingsContainer(float mouseX, float mouseY, float padX, float padY, bool mouseAccel, bool padAccel, bool crouchTog, bool sprintTog, bool aimTog)
            {
                mouseLookSpeedX = mouseX;
                mouseLookSpeedY = mouseY;
                padLookSpeedX = padX;
                padLookSpeedY = padY;
                mouseUseAcceleration = mouseAccel;
                padUseAcceleration = padAccel;
                crouchToggle = crouchTog;
                sprintToggle = sprintTog;
                aimToggle = aimTog;
            }

            public float mouseLookSpeedX, mouseLookSpeedY;
            public float padLookSpeedX, padLookSpeedY;
            public bool mouseUseAcceleration;
            public bool padUseAcceleration;


            public bool crouchToggle, sprintToggle, aimToggle;
        }
        public SettingsContainer settingsContainer;
        #endregion

        #region Save and Load
        public string FileSavePath;

        public void SaveFile()
        {
            Debug.Log($"Saved settings to {FileSavePath}.");
            
            System.IO.File.WriteAllText(FileSavePath, JsonConvert.SerializeObject(settingsContainer));
        }
        public void LoadFile()
        {
            if(!System.IO.File.Exists(FileSavePath))
            {
                print("Creating new settings file");
                settingsContainer = new()
                {
                    aimToggle = false,
                    crouchToggle = true,
                    sprintToggle = false,
                    mouseLookSpeedX = 50f,
                    mouseLookSpeedY = 50f,
                    mouseUseAcceleration = false,
                    padLookSpeedX = 5f,
                    padLookSpeedY = 5f,
                    padUseAcceleration = false,
                };
                SaveFile();
            }
            else
            {
                print("loading settings file");
                settingsContainer = JsonConvert.DeserializeObject<SettingsContainer>(System.IO.File.ReadAllText(FileSavePath));
            }
        }
        private void OnApplicationQuit()
        {
            SaveFile();
        }
        #endregion
    }
}
