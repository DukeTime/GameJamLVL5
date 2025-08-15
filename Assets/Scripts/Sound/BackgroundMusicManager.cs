using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    private static BackgroundMusicManager instance;
    private AudioSource audioSource;
    public AudioClip defaultMusic; // Музыка по умолчанию для других сцен
    public AudioClip gameMusic; // Музыка для сцен Game, Game1, Game2, Game3
    public string[] gameSceneNames = { "Game", "Game1", "Game2", "Game3" }; // Массив имён сцен, где будет играть gameMusic

    void Awake()
    {
        Debug.Log("BackgroundMusicManager Awake called");
        // Если экземпляр уже существует, уничтожаем дубликат
        if (instance != null && instance != this)
        {
            Debug.Log($"Destroying duplicate BackgroundMusicManager. Existing instance: {instance.gameObject.name}, This instance: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // Сохраняем этот объект между сценами
        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"BackgroundMusicManager instance set, gameObject: {gameObject.name}, DontDestroyOnLoad applied");

        // Получаем компонент AudioSource и проверяем его
        audioSource = GetComponent<AudioSource>();
        Debug.Log($"AudioSource in Awake: {audioSource}");

        // Если AudioSource ещё нет, добавляем
        if (audioSource == null)
        {
            Debug.Log("Adding new AudioSource");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Подписываемся на событие загрузки сцены и проверяем
        try
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("Successfully subscribed to SceneManager.sceneLoaded");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to subscribe to SceneManager.sceneLoaded: {e.Message}");
        }
    }

    void Start()
    {
        Debug.Log("BackgroundMusicManager Start called");
        // Устанавливаем музыку по умолчанию, если она есть
        if (defaultMusic != null && audioSource != null)
        {
            Debug.Log($"Setting default music: {defaultMusic.name}");
            audioSource.clip = defaultMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Default music or AudioSource is null in Start");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"OnSceneLoaded called for scene: {scene.name}, Mode: {mode}, Path: {scene.path}");

        // Выводим все имена сцен из gameSceneNames для отладки
        string sceneNamesList = string.Join(", ", gameSceneNames);
        Debug.Log($"Game scene names: {sceneNamesList}");

        // Проверяем, загрузилась ли одна из сцен Game
        bool isGameScene = System.Array.Exists(gameSceneNames, name => name == scene.name);
        Debug.Log($"Is this a Game scene? {isGameScene} (Scene name: {scene.name})");

        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null in BackgroundMusicManager!");
            audioSource = gameObject.GetComponent<AudioSource>(); // Пытаемся восстановить
            if (audioSource == null)
            {
                Debug.Log("Adding new AudioSource in OnSceneLoaded");
                audioSource = gameObject.AddComponent<AudioSource>(); // Создаём новый, если не существует
            }
        }

        if (isGameScene && gameMusic != null)
        {
            Debug.Log($"Playing Game Music for scene: {scene.name}, Clip: {gameMusic.name}");
            // Останавливаем текущую музыку
            if (audioSource != null && audioSource.isPlaying)
            {
                Debug.Log("Stopping current music");
                audioSource.Stop();
            }

            // Устанавливаем и воспроизводим музыку для сцен Game
            if (audioSource != null)
            {
                audioSource.clip = gameMusic;
                audioSource.loop = true;
                audioSource.Play();
                Debug.Log($"Game music playing: {gameMusic.name}");
            }
            else
            {
                Debug.LogError("AudioSource is still null after recreation!");
            }
        }
        else if (defaultMusic != null)
        {
            Debug.Log($"Playing Default Music for scene: {scene.name}, Clip: {defaultMusic.name}");
            // Если это не сцена Game, воспроизводим музыку по умолчанию
            if (audioSource != null && (audioSource.clip != defaultMusic || !audioSource.isPlaying))
            {
                Debug.Log("Stopping current music for default");
                audioSource.Stop();
                audioSource.clip = defaultMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("Default music is null in OnSceneLoaded");
        }
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
            Debug.Log($"Setting volume to: {volume}");
        }
        else
        {
            Debug.LogError("AudioSource is null in SetVolume!");
        }
    }

    void OnDestroy()
    {
        Debug.Log("BackgroundMusicManager OnDestroy called");
        // Отписываемся от события, чтобы избежать ошибок при уничтожении
        try
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("Unsubscribed from SceneManager.sceneLoaded");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to unsubscribe from SceneManager.sceneLoaded: {e.Message}");
        }
    }
}