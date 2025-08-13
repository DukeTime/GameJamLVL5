using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogPhrase
{
    public string characterId;
    public string emotion;
    [TextArea(3, 10)] public string text;
    public string[] encounters;
}


[System.Serializable]
public class DialogData
{
    public List<DialogPhrase> phrases;
}