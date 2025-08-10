using System.Collections.Generic;
using UnityEngine;

/// √лобальный менеджер: ограничивает число врагов, которые могут атаковать одновременно.
public class AttackCoordinator2D : MonoBehaviour
{
    public static AttackCoordinator2D Instance { get; private set; }

    [Tooltip("—колько врагов одновременно могут находитьс€ в состо€нии Attack.")]
    [Min(1)] public int maxSimultaneousAttackers = 2;

    // кто сейчас атакует (по InstanceID)
    private readonly HashSet<int> _active = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // опционально не уничтожать на загрузке сцен: DontDestroyOnLoad(gameObject);
    }

    public bool TryAcquire(int enemyId)
    {
        if (_active.Contains(enemyId)) return true;                // уже держит слот
        if (_active.Count >= maxSimultaneousAttackers) return false;
        _active.Add(enemyId);
        return true;
    }

    public void Release(int enemyId)
    {
        if (_active.Contains(enemyId)) _active.Remove(enemyId);
    }

    public bool IsHolding(int enemyId) => _active.Contains(enemyId);
}
