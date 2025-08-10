using System.Collections;
using DefaultNamespace.DialogueSystem;
using UnityEngine;


public class NpcController : Interactable
{
    [SerializeField] private GameObject interactionPanel; // Панель с буквой "E"
    [SerializeField] private float fadeDuration = 0.3f; // Длительность анимации

    private CanvasGroup canvasGroup;
    private bool isPlayerInRange = false;

    private void Awake()
    {
        // Настройка панели
        if (interactionPanel != null)
        {
            canvasGroup = interactionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = interactionPanel.AddComponent<CanvasGroup>();
            }
            
            interactionPanel.SetActive(false);
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        // Проверка нажатия E, когда игрок в зоне
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    // Вызывается, когда игрок входит в зону взаимодействия
    public void ShowInteractionPrompt()
    {
        isPlayerInRange = true;
        StopAllCoroutines();
        interactionPanel.SetActive(true);
        StartCoroutine(FadePanel(0f, 1f, fadeDuration));
    }

    // Вызывается, когда игрок выходит из зоны взаимодействия
    public void HideInteractionPrompt()
    {
        isPlayerInRange = false;
        StopAllCoroutines();
        StartCoroutine(FadePanel(1f, 0f, fadeDuration, () => 
        {
            interactionPanel.SetActive(false);
        }));
    }

    // Обработка взаимодействия
    public override void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
        // Здесь добавьте свою логику взаимодействия
    }

    // Анимация плавного появления/исчезновения
    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }

    // Методы для триггеров (можно использовать OnTriggerEnter/Exit в 3D или OnCollisionEnter2D/Exit2D в 2D)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowInteractionPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HideInteractionPrompt();
        }
    }

    private void Start()
    {
        DialogSystem.Instance.LoadDialog("TestDialogue");
    }
}