using System.Collections;
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
    
    private string currentFullText;
    private Coroutine typingCoroutine;
    
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
    
    public IEnumerator ShowPhraseAnimated(DialogPhrase phrase, float textSpeed)
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
        
        currentFullText = phrase.text;
        dialogText.text = "";
        
        Show();
        
        float delay = 1f / textSpeed;
        int totalCharacters = currentFullText.Length;
        int currentCharacter = 0;
        
        while (currentCharacter < totalCharacters)
        {
            currentCharacter++;
            dialogText.text = currentFullText.Substring(0, currentCharacter);
            yield return new WaitForSeconds(delay);
        }
        
        DialogSystem.Instance.isTyping = false;
    }
    
    public void CompleteTextImmediately()
    {
        if (dialogText.text != currentFullText)
        {
            dialogText.text = currentFullText;
        }
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