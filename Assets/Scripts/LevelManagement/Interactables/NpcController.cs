using System.Collections;
using UnityEngine;


public class NpcController : Interactable
{
    // [Header("Settings")]
    // [SerializeField] private SpriteRenderer interactionSprite; // Спрайт с буквой "E"
    // [SerializeField] private float fadeSpeed = 5f; // Скорость анимации
    // [SerializeField] private KeyCode interactionKey = KeyCode.E; // Клавиша взаимодействия
    //
    // private bool isPlayerInRange = false;
    // private bool isInteractable = true;
    // private Color originalColor;
    public string dialogue = "TestDialogue";
    public bool interacted = false;
    
    public override void Interact()
    {
        if (interacted)
        {
            DialogSystem.Instance.LoadDialog(dialogue);
            interacted = true;
            // Ваша логика взаимодействия здесь
            
            // Пример: временно отключаем взаимодействие после использования
            // StartCoroutine(CooldownInteraction(1f));}}}
        }
    }
    //
    // private void Update()
    // {
    //     if (isPlayerInRange && isInteractable && Input.GetKeyDown(interactionKey))
    //     {
    //         Interact();
    //     }
    // }
    //
    // public void ShowInteraction()
    // {
    //     if (!isInteractable || interactionSprite == null) return;
    //
    //     isPlayerInRange = true;
    //     StopAllCoroutines();
    //     interactionSprite.gameObject.SetActive(true);
    //     StartCoroutine(FadeSprite(0f, originalColor.a));
    // }
    //
    // public void HideInteraction()
    // {
    //     if (interactionSprite == null) return;
    //
    //     isPlayerInRange = false;
    //     StopAllCoroutines();
    //     StartCoroutine(FadeSprite(interactionSprite.color.a, 0f, () => 
    //     {
    //         interactionSprite.gameObject.SetActive(false);
    //     }));
    // }
    //
    // private IEnumerator FadeSprite(float startAlpha, float endAlpha, System.Action onComplete = null)
    // {
    //     float progress = 0f;
    //     Color currentColor = interactionSprite.color;
    //
    //     while (progress < 1f)
    //     {
    //         Debug.Log(3);
    //         progress += Time.deltaTime * fadeSpeed;
    //         currentColor.a = Mathf.Lerp(startAlpha, endAlpha, progress);
    //         interactionSprite.color = currentColor;
    //         yield return null;
    //     }
    //
    //     currentColor.a = endAlpha;
    //     interactionSprite.color = currentColor;
    //     onComplete?.Invoke();
    // }
    //
    // public override void Interact()
    // {
    //     Debug.Log("Interacted with " + gameObject.name);
    //     // Ваша логика взаимодействия здесь
    //     
    //     // Пример: временно отключаем взаимодействие после использования
    //     // StartCoroutine(CooldownInteraction(1f));
    // }
    //
    // private IEnumerator CooldownInteraction(float duration)
    // {
    //     isInteractable = false;
    //     yield return new WaitForSeconds(duration);
    //     isInteractable = true;
    // }
    //
    // // Методы для триггеров
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     Debug.Log(1);
    //     if (other.CompareTag("Player"))
    //     {
    //         Debug.Log(2);
    //         ShowInteraction();
    //     }
    // }
    //
    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         HideInteraction();
    //     }
    // }
    //
    // private void Start()
    // {
    //     //DialogSystem.Instance.LoadDialog("TestDialogue");
    //     if (interactionSprite != null)
    //     {
    //         originalColor = interactionSprite.color;
    //         interactionSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    //         interactionSprite.gameObject.SetActive(false);
    //     }
    // }
}