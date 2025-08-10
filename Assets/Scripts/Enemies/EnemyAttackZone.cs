using UnityEngine;

public class EnemyAttackZone : MonoBehaviour
{
    [HideInInspector] public int damage = 10;
    [HideInInspector] public EnemyController enemy;

    private bool playerInside = false;
    private PlayerHealth playerHealth = null;

    // Привязка врага (на всякий случай)
    public void SetEnemyReference(EnemyController controller)
    {
        enemy = controller;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerHealth = other.GetComponent<PlayerHealth>();
            playerInside = true;
            DealDamage();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            playerHealth = null;
        }
    }

    void DealDamage()
    {
        if (playerInside && playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            // Если нужен кулдаун урона — реализуй таймер тут
        }
    }
}
