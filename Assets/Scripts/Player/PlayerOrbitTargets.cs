// PlayerOrbitTargets.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerOrbitTargets : MonoBehaviour
{
    [Header("Orbit")]
    [Min(1)] public int pointsCount = 8;
    public float radius = 2.5f;
    public float angularSpeedDeg = 60f; // град/сек
    public float startAngleDeg = 0f;
    public bool randomizePhase = false;

    [Header("Debug")]
    public bool drawGizmos = true;
    public float gizmoSize = 0.12f;

    private readonly List<Transform> _points = new();
    private readonly List<float> _angles = new();
    private readonly Dictionary<Transform, int> _claimCounts = new(); // сколько врагов закреплено за точкой

    public static PlayerOrbitTargets Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        // Создаём дочерние пустышки-точки
        for (int i = 0; i < pointsCount; i++)
        {
            var t = new GameObject($"OrbitPoint_{i}").transform;
            t.SetParent(transform);
            _points.Add(t);

            float baseAngle = startAngleDeg + (360f / pointsCount) * i;
            if (randomizePhase) baseAngle += Random.Range(0f, 360f);
            _angles.Add(baseAngle);

            _claimCounts[t] = 0;
        }

        UpdatePointsPositions(0f);
    }

    void Update()
    {
        UpdatePointsPositions(Time.deltaTime);
    }

    private void UpdatePointsPositions(float dt)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            _angles[i] = Mathf.Repeat(_angles[i] + angularSpeedDeg * dt, 360f);
            float rad = _angles[i] * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            _points[i].position = (Vector2)transform.position + (Vector2)offset;
        }
    }

    /// Возвращает точку с наименьшей загрузкой (или случайную из свободных).
    public Transform ClaimBestPoint(bool preferLeastClaimed = true)
    {
        if (_points.Count == 0) return null;

        Transform best = null;

        if (preferLeastClaimed)
        {
            int bestCount = int.MaxValue;
            foreach (var p in _points)
            {
                int c = _claimCounts[p];
                if (c < bestCount) { bestCount = c; best = p; }
            }
        }
        else
        {
            best = _points[Random.Range(0, _points.Count)];
        }

        _claimCounts[best]++;
        return best;
    }

    /// Освободить точку (когда враг умирает/деспавнится).
    public void ReleasePoint(Transform point)
    {
        if (point == null) return;
        if (_claimCounts.ContainsKey(point))
        {
            _claimCounts[point] = Mathf.Max(0, _claimCounts[point] - 1);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Окружность орбиты
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        const int segs = 64;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // Примерные точки (в редакторе, до рантайма)
        Gizmos.color = Color.cyan;
        float step = (pointsCount > 0) ? 360f / pointsCount : 360f;
        for (int i = 0; i < pointsCount; i++)
        {
            float angle = startAngleDeg + step * i;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * radius;
            Gizmos.DrawSphere(p, gizmoSize);
        }
    }
}
