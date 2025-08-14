using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DialogueSceneChange : MonoBehaviour, IDialogueInteraction
{
    public IEnumerator Activate()
    {
        StartCoroutine(GlobalGameController.Instance.LoadScene(SceneManager.GetActiveScene().buildIndex + 1));
        yield return null;
    }
}