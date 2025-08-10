namespace DefaultNamespace.DialogueSystem
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class DialogView : MonoBehaviour
    {
        public static DialogView Instance { get; private set; }
        
        [Header("UI Elements")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text dialogText;
        
        [Header("Styles")]
        [SerializeField] private TextStyle[] textStyles;
        
        [System.Serializable]
        public class TextStyle
        {
            public string styleId;
            public Color color;
            public TMP_FontAsset font;
            public float fontSize;
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            Hide();
        }
        
        public void ShowPhrase(DialogPhrase phrase)
        {
            var character = DialogSystem.Instance.GetCharacterData(phrase.characterId);
            
            if (character != null)
            {
                characterNameText.text = character.displayName;
                characterPortrait.sprite = character.GetPortraitForEmotion(phrase.emotion);
            }
            else
            {
                characterNameText.text = phrase.characterId;
                characterPortrait.sprite = null;
            }
            
            // ApplyTextStyle(dialogText, phrase.textStyle);
            dialogText.text = phrase.text;
            
            Show();
        }
        
        private void ApplyTextStyle(TMP_Text textElement, string styleId)
        {
            foreach (var style in textStyles)
            {
                if (style.styleId == styleId)
                {
                    textElement.color = style.color;
                    textElement.font = style.font;
                    textElement.fontSize = style.fontSize;
                    return;
                }
            }
            
            // Default style if not found
            var defaultStyle = textStyles[0];
            textElement.color = defaultStyle.color;
            textElement.font = defaultStyle.font;
            textElement.fontSize = defaultStyle.fontSize;
        }
        
        public void Show()
        {
            dialogPanel.SetActive(true);
        }
        
        public void Hide()
        {
            dialogPanel.SetActive(false);
        }
    }
}