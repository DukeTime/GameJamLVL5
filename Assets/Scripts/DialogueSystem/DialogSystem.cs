using System.Collections;

namespace DefaultNamespace.DialogueSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public class DialogSystem : MonoBehaviour
    {
        public static DialogSystem Instance { get; private set; }
        
        [SerializeField] private string dialogsFolder = "Dialogs";
        [SerializeField] private string charactersFolder = "Characters";
        
        private Dictionary<string, DialogData> loadedDialogs = new Dictionary<string, DialogData>();
        private Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();
        
        private DialogData currentDialog;
        private int currentPhraseIndex;
        
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
            
            GlobalGameController.CutsceneFreeze();
            
            currentPhraseIndex = 0;
            StartCoroutine(DialogueCor());
        }

        private IEnumerator DialogueCor()
        {
            while (currentPhraseIndex < currentDialog.phrases.Count)
            {
                ShowCurrentPhrase();

                yield return null;

                while (!Input.GetMouseButtonDown(0))
                {
                    yield return null;
                }

                currentPhraseIndex++;
            }
            EndDialog();
        }
        
        public void LoadFiles(string dialogName)
        {
            // Пытаемся найти уже загруженный диалог
            if (loadedDialogs.TryGetValue(dialogName, out currentDialog))
            {
                //StartDialog();
                return;
            }

            // Формируем путь относительно папки Resources
            string resourcePath = $"{dialogsFolder}/{dialogName}";
    
            TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
            if (jsonFile == null)
            {
                Debug.LogError($"Dialog file {dialogName} not found at path: {resourcePath}");
                // Дополнительная диагностика - выведем все доступные файлы
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
            DialogView.Instance.ShowPhrase(currentPhrase);
        }
        
        private void EndDialog()
        {
            DialogView.Instance.Hide();
            currentDialog = null;
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
}