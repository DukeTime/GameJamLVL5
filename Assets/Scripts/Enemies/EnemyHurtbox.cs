using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player")) // или твой тег удара
        {
            enemyHealth.TakeDamage(20); // или урон из оружия
        }
    }
}
