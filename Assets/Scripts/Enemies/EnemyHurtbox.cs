using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private EnemyData enemyData;

    private void Awake()
    {
        // ���� ��� ��������, ����� ������������ � ������, � ����� ������
        enemyHealth = GetComponentInParent<EnemyHealth>();
        enemyData = GetComponentInParent<EnemyData>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ���������, ��� ������ � ������ ������

            // ���� ���� EnemyData � ���� ����� ����
            if (enemyData != null)
            {
                enemyData.TakeDamage(20);
            }
            // ���� ���� EnemyHealth � ���� ����� ����
            else if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(20);
            }
        
    }
}
