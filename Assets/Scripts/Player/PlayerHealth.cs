using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100;
    
    public Action OnHpIsGone;
    public Action OnHurt;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            OnHpIsGone?.Invoke();
        }
        else
        {
            Debug.Log(1);
            OnHurt?.Invoke();
        }
        // ��� ������ ������ ��� ��������
    }
}
