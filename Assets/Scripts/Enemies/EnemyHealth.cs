using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 40;
    [SerializeField] int currentHP;
    public UnityEvent onHit;
    public UnityEvent onDeath;

    void Awake() { currentHP = Mathf.Max(1, maxHP); }

    // סמגלוסעטל ס SendMessage("TakeDamage", int)
    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        currentHP -= amount;
        onHit?.Invoke();

        if (currentHP <= 0)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
