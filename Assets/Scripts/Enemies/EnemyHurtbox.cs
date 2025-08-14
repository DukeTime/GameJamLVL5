using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private EnemyData enemyData;

    private void Awake()
    {
        // Берём оба варианта, чтобы поддерживать и старых, и новых врагов
        enemyHealth = GetComponentInParent<EnemyHealth>();
        enemyData = GetComponentInParent<EnemyData>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что попали в оружие игрока

            // Если есть EnemyData — бьём через него
            if (enemyData != null)
            {
                enemyData.TakeDamage(20);
            }
            // Если есть EnemyHealth — бьём через него
            else if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(20);
            }
        
    }
}
