using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 40;
    [SerializeField] private int currentHP;

    [Header("Hit feedback (optional)")]
    public float invulAfterHit = 0.05f;
    public SpriteRenderer flashRenderer;
    public Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);
    public float flashTime = 0.06f;

    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onDeath;

    bool _invul;
    Color _origColor;

    void Awake()
    {
        currentHP = Mathf.Max(1, maxHP);
        if (!flashRenderer) flashRenderer = GetComponentInChildren<SpriteRenderer>();
        if (flashRenderer) _origColor = flashRenderer.color;
    }

    // ВАЖНО: именно этот метод вызывают удары/пули (как у обычного врага)
    public void TakeDamage(int amount)
    {
        if (_invul) return;
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        currentHP -= amount;
        onHit?.Invoke();

        if (flashRenderer) StartCoroutine(FlashCo());
        if (invulAfterHit > 0f) StartCoroutine(InvulCo(invulAfterHit));

        if (currentHP <= 0)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    System.Collections.IEnumerator InvulCo(float t)
    {
        _invul = true;
        yield return new WaitForSeconds(t);
        _invul = false;
    }

    System.Collections.IEnumerator FlashCo()
    {
        if (!flashRenderer) yield break;
        flashRenderer.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        flashRenderer.color = _origColor;
    }
}
