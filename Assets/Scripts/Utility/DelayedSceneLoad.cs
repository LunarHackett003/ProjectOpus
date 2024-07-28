using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DelayedSceneLoad : MonoBehaviour
{
    public SceneReference sceneToLoad;
    public float delay;
    public bool loadOnStart;
    private void Start()
    {
        if(loadOnStart)
        Invoke(nameof(LoadScene), delay);
    }
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad.BuildIndex);
    }
}
