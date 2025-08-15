using System;
using System.Collections;
using UnityEngine;

public class EnemyData: MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;

    public Action OnDie;

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDie?.Invoke();
        //Destroy(gameObject);
    }
}