using System.Collections.Generic;
using UnityEngine;

/// ���������� ��������: ������������ ����� ������, ������� ����� ��������� ������������.
public class AttackCoordinator2D : MonoBehaviour
{
    public static AttackCoordinator2D Instance { get; private set; }

    [Tooltip("������� ������ ������������ ����� ���������� � ��������� Attack.")]
    [Min(1)] public int maxSimultaneousAttackers = 2;

    // ��� ������ ������� (�� InstanceID)
    private readonly HashSet<int> _active = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ����������� �� ���������� �� �������� ����: DontDestroyOnLoad(gameObject);
    }

    public bool TryAcquire(int enemyId)
    {
        if (_active.Contains(enemyId)) return true;                // ��� ������ ����
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
