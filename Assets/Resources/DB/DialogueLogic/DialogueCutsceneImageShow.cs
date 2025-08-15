using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DialogueCutsceneImageShow : MonoBehaviour, IDialogueInteraction
{
    public IEnumerator Activate()
    {
        List<Rune>newRunes = RuneManager.GetAvailableRunes(3);
        
        DialogSystem.Instance.DialogueDisable();
        yield return StartCoroutine(ViewManager.Instance.StartRuneChoice(newRunes));
        DialogSystem.Instance.DialogueEnable();
    }
}