using UnityEngine;

public class EnemyAttackZone : MonoBehaviour
{
    [HideInInspector] public int damage = 10;
    [HideInInspector] public EnemyController enemy;

    [Header("Damage")]
    [Min(0.05f)] public float tickInterval = 0.5f;

    private bool playerInside = false;
    private PlayerHealth playerHealth = null;
    private float nextTickTime = 0f;

    public void SetEnemyReference(EnemyController controller) => enemy = controller;
    public void SetTickInterval(float interval) => tickInterval = Mathf.Max(0.05f, interval);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerHealth = other.GetComponent<PlayerHealth>();
        playerInside = true;

        DealDamage(); // first hit on enter
        nextTickTime = Time.time + tickInterval;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!playerInside || playerHealth == null) return;
        if (!other.CompareTag("Player")) return;

        if (Time.time >= nextTickTime)
        {
            DealDamage();
            nextTickTime = Time.time + tickInterval;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        playerHealth = null;
    }

    private void DealDamage()
    {
        if (playerInside && playerHealth != null)
            playerHealth.TakeDamage(damage);
    }
}
