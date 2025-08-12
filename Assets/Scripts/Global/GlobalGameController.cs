using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalGameController : MonoBehaviour
{
    public static GlobalGameController Instance;

    public int sceneProgress = 1;
    public bool Paused { get; private set; } = false;
    public bool CutsceneFreezed { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public IEnumerator LoadScene(string sceneName)
    {
        yield return StartCoroutine(ViewManager.Instance.FadeIn());
        SceneManager.LoadScene(sceneName);
    }

    public void Pause()
    {
        Paused = true;
    }
    
    public void Unpause()
    {
        Paused = false;
    }
    
    public void CutsceneFreeze()
    {
        CutsceneFreezed = true;
    }
    
    public void CutsceneUnfreeze()
    {
        CutsceneFreezed = false;
    }
}