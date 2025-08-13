using System;
using UnityEngine;

public class PlayerAttackController : MonoBehaviour
{
    public Action OnAttack;
    
    [SerializeField] private PlayerConfig _attackData;
    [SerializeField] private Transform _weaponPivot; // Точка, от которой идёт атака (обычно чуть перед игроком)
    
    private float _lastAttackTime;
    private bool _isAttacking;

    private bool CanAttack()
    {
        return Time.time >= _lastAttackTime + _attackData.Cooldown && !_isAttacking;
    }

    public void Attack()
    {
        if (!CanAttack())
            return;
        
        OnAttack?.Invoke();
        
        _lastAttackTime = Time.time;
        _isAttacking = true;
        
        // Получаем направление на курсор мыши
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;
        
        // Запускаем визуализацию атаки
        if (PlayerAttackVisualizer.Instance != null)
        {
            PlayerAttackVisualizer.Instance.PlaySlashEffect(
                _weaponPivot.position,
                direction
                // _attackData.SwingDuration,
                // _attackData.SwingArc,
                // _attackData.SwingDistance
            );
        }
        
        // Наносим урон всем врагам в зоне атаки
        DetectAndDamageEnemies(_weaponPivot.position, direction);
        
        Invoke(nameof(EndAttack), _attackData.SwingDuration);
    }

    private void DetectAndDamageEnemies(Vector2 attackOrigin, Vector2 direction)
    {
        // Определяем зону атаки (сектор)
        float halfArc = _attackData.SwingArc * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, _attackData.SwingDistance);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector2 toEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector2.Angle(direction, toEnemy);
                
                if (angle <= halfArc)
                {
                    // Наносим урон
                    if (hit.TryGetComponent<EnemyData>(out var health))
                    {
                        health.TakeDamage(_attackData.Damage);
                    }
                }
            }
        }
    }

    private void EndAttack()
    {
        _isAttacking = false;
    }
}