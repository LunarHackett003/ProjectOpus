using UnityEngine;

namespace Opus
{
    public class MainMenu : MonoBehaviour
    {
        public void StartClient()
        {
            SessionManager.Instance.StartClient();
        }
        public void StartHost()
        {
            SessionManager.Instance.StartHost();
        }
        
    }
}
