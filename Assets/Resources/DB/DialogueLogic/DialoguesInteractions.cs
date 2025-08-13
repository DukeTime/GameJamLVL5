using System;
using System.Collections.Generic;
using UnityEngine;


public class DialoguesInteractions: MonoBehaviour
{
    public DialogueSceneChange dialogueSceneChange;
    public DialogueRuneChoice dialogueRuneChoice;
    public Dictionary<string, IDialogueInteraction> All = new Dictionary<string, IDialogueInteraction>();

    public void Init()
    {
        All["NextScene"] = dialogueSceneChange;
        All["RuneChoice"] = dialogueRuneChoice;
    }
}