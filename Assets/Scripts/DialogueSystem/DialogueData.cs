namespace DefaultNamespace.DialogueSystem
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class DialogPhrase
    {
        public string characterId;
        public string emotion;
        [TextArea(3, 10)] public string text;
        public string textStyle;
    }

    
    [System.Serializable]
    public class DialogData
    {
        public List<DialogPhrase> phrases;
    }

    
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue System/Character Data")]
    public class CharacterData : ScriptableObject
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
}