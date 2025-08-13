using UnityEngine;
using UnityEngine.SceneManagement;


public class DialogueSceneChange : MonoBehaviour, IDialogueInteraction
{
    public void Activate()
    {
        StartCoroutine(GlobalGameController.Instance.LoadScene(SceneManager.GetActiveScene().buildIndex + 1));
    }
}