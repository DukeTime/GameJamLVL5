using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogSystem : MonoBehaviour
{
    public static DialogSystem Instance { get; private set; }
    //public bool dialogueEnable;
    
    [SerializeField] private DialoguesInteractions dialogueInteractions;
    [SerializeField] private string dialogsFolder = "Dialogs";
    [SerializeField] private string charactersFolder = "Characters";
    [SerializeField] private float textSpeed = 20f; // Characters per second
    
    private Dictionary<string, DialogData> loadedDialogs = new Dictionary<string, DialogData>();
    private Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();
    
    private DialogData currentDialog;
    private int currentPhraseIndex;
    public bool isTyping = false;
    private Coroutine typingCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllCharacters();
        dialogueInteractions.Init();
    }

    public void DialogueDisable()
    {
        StopCoroutine(DialogueCor());
        DialogView.Instance.Hide();
    }
    
    public void DialogueEnable()
    {
        DialogView.Instance.Show();
        StartCoroutine(DialogueCor());
    }
    
    private void LoadAllCharacters()
    {
        CharacterData[] loadedCharacters = Resources.LoadAll<CharacterData>(charactersFolder);
        foreach (var character in loadedCharacters)
        {
            characters[character.characterId] = character;
        }
    }

    public void LoadDialog(string dialogName)
    {
        LoadFiles(dialogName);
        
        GlobalGameController.Instance.CutsceneFreeze();
        
        currentPhraseIndex = 0;
        StartCoroutine(DialogueCor());
    }

    private IEnumerator DialogueCor()
    {
        while (currentPhraseIndex < currentDialog.phrases.Count)
        {
            ShowCurrentPhrase();

            // Wait for typing to complete or skip
            while (isTyping)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (typingCoroutine != null)
                    {
                        StopCoroutine(typingCoroutine);
                        typingCoroutine = null;
                    }
                    DialogView.Instance.CompleteTextImmediately();
                    isTyping = false;
                }
                yield return null;
            }

            // Wait for click to continue
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            yield return null;

            if (currentDialog.phrases[currentPhraseIndex].encounters != null)
            {
                foreach (string encounter in currentDialog.phrases[currentPhraseIndex].encounters)
                {
                    yield return StartCoroutine(dialogueInteractions
                        .All[encounter]
                        .Activate());
                }
            }
            
            currentPhraseIndex++;
        }
        EndDialog();
    }
    
    public void LoadFiles(string dialogName)
    {
        if (loadedDialogs.TryGetValue(dialogName, out currentDialog))
        {
            return;
        }

        string resourcePath = $"{dialogsFolder}/{dialogName}";

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        if (jsonFile == null)
        {
            Debug.LogError($"Dialog file {dialogName} not found at path: {resourcePath}");
            var allDialogs = Resources.LoadAll<TextAsset>(dialogsFolder);
            Debug.Log($"Available dialogs in {dialogsFolder}:");
            foreach (var dialog in allDialogs)
            {
                Debug.Log(dialog.name);
            }
            return;
        }

        currentDialog = JsonUtility.FromJson<DialogData>(jsonFile.text);
        loadedDialogs[dialogName] = currentDialog;
    }
    
    private void ShowCurrentPhrase()
    {
        var currentPhrase = currentDialog.phrases[currentPhraseIndex];
        typingCoroutine = StartCoroutine(DialogView.Instance.ShowPhraseAnimated(currentPhrase, textSpeed));
        isTyping = true;
    }
    
    private void EndDialog()
    {
        DialogView.Instance.Hide();
        currentDialog = null;
        
        GlobalGameController.Instance.CutsceneUnfreeze();
    }
    
    public CharacterData GetCharacterData(string characterId)
    {
        if (characters.TryGetValue(characterId, out var character))
        {
            return character;
        }
        return null;
    }
}