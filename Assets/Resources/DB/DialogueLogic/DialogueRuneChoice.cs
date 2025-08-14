using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DialogueRuneChoice : MonoBehaviour, IDialogueInteraction
{
    public void Activate()
    {
        List<Rune>newRunes = RuneManager.GetAvailableRunes(3);
        
        ViewManager.Instance.StartRuneChoice(newRunes);
        DialogSystem.Instance.DialogueDisable();
    }
}