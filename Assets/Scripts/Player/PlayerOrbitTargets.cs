using System.Collections.Generic;
using UnityEngine;

/// Вращающееся кольцо слотов вокруг игрока (как в видео).
/// Враги занимают слот и идут к нему (не напрямую к игроку).
[DisallowMultipleComponent]
public class PlayerOrbitTargets : MonoBehaviour
{
    public static PlayerOrbitTargets Instance { get; private set; }

    [Header("Ring")]
    [Min(2)] public int slots = 12;
    public float radius = 3.0f;
    public float spinDegPerSec = 60f;
    public float startAngleDeg = 0f;
    public bool randomizePhasePerSlot = false;

    [Header("Bias (опционально)")]
    [Range(0f, 360f)] public float behindSectorWidth = 180f;
    [Range(0f, 2f)] public float behindBias = 1.2f;

    [Header("Gizmos")]
    public bool drawRing = true;
    public bool drawSlots = true;
    public float gizmoSize = 0.12f;

    readonly List<Transform> _points = new();
    readonly List<float> _baseAngles = new();
    int?[] _owners;

    Vector3 _prevPos;
    Vector2 _heading = Vector2.right;
    float _phase;

    void Awake()
    {
        Instance = this;

        slots = Mathf.Max(2, slots);
        _owners = new int?[slots];

        for (int i = 0; i < slots; i++)
        {
            var t = new GameObject($"OrbitSlot_{i}").transform;
            t.SetParent(transform);
            _points.Add(t);

            float a = startAngleDeg + (360f / slots) * i;
            if (randomizePhasePerSlot) a += Random.Range(0f, 360f);
            _baseAngles.Add(a);
        }

        _prevPos = transform.position;
        UpdateSlotPositions();
    }

    void Update()
    {
        var pos = transform.position;
        var v = pos - _prevPos;
        if (v.sqrMagnitude > 0.0001f) _heading = ((Vector2)v).normalized;
        _prevPos = pos;

        _phase = Mathf.Repeat(_phase + spinDegPerSec * Time.deltaTime, 360f);
        UpdateSlotPositions();
    }

    void UpdateSlotPositions()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            float ang = Mathf.Repeat(_baseAngles[i] + _phase, 360f) * Mathf.Deg2Rad;
            var off = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
            _points[i].position = transform.position + off;
        }
    }

    // ===== API =====

    public int ClaimSlot(int requesterInstanceId, bool preferBehind = true)
    {
        for (int i = 0; i < _owners.Length; i++)
            if (_owners[i].HasValue && _owners[i].Value == requesterInstanceId)
                return i;

        List<int> free = new();
        for (int i = 0; i < _owners.Length; i++)
            if (!_owners[i].HasValue) free.Add(i);

        if (free.Count == 0) return 0;

        int best = free[Random.Range(0, free.Count)];

        if (preferBehind && behindSectorWidth > 1f && behindBias > 1.01f)
        {
            float head = Mathf.Atan2(_heading.y, _heading.x) * Mathf.Rad2Deg;
            float behindCenter = Mathf.Repeat(head + 180f, 360f);

            float bestScore = float.PositiveInfinity;
            foreach (var i in free)
            {
                float slotAng = Mathf.Repeat(_baseAngles[i] + _phase, 360f);
                float d = Mathf.Abs(Mathf.DeltaAngle(slotAng, behindCenter));
                float inside = Mathf.InverseLerp(behindSectorWidth * 0.5f, 0f, d);
                float score = d / Mathf.Max(0.001f, (1f + inside * (behindBias - 1f)));
                if (score < bestScore) { bestScore = score; best = i; }
            }
        }

        _owners[best] = requesterInstanceId;
        return best;
    }

    public void ReleaseSlot(int slotIndex, int requesterInstanceId)
    {
        if (slotIndex < 0 || slotIndex >= _owners.Length) return;
        if (_owners[slotIndex].HasValue && _owners[slotIndex].Value == requesterInstanceId)
            _owners[slotIndex] = null;
    }

    public Transform GetSlotTransform(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _points.Count) return null;
        return _points[slotIndex];
    }

    void OnDrawGizmos()
    {
        if (drawRing)
        {
            Gizmos.color = new Color(0.25f, 0.9f, 1f, 0.35f);
            const int segs = 64;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= segs; i++)
            {
                float a = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                if (i > 0) Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        if (!drawSlots) return;
        Gizmos.color = Color.cyan;
        int N = Mathf.Max(2, slots);
        for (int i = 0; i < N; i++)
        {
            float a = startAngleDeg + (360f / N) * i;
            Vector3 p = transform.position + Quaternion.Euler(0, 0, a) * Vector3.right * radius;
            Gizmos.DrawSphere(p, gizmoSize);
        }
    }
}
