using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue System/Character Data")]
public class CharacterData: ScriptableObject
{
    public string characterId;
    public string displayName;
    public Sprite defaultPortrait;

    [System.Serializable]
    public class EmotionPortrait
    {
        public string emotionId;
        public Sprite portrait;
    }

    public EmotionPortrait[] emotionPortraits;

    public Sprite GetPortraitForEmotion(string emotion)
    {
        foreach (var ep in emotionPortraits)
        {
            if (ep.emotionId == emotion) return ep.portrait;
        }
        return defaultPortrait;
    }
}