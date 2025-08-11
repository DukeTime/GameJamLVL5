using System.Collections.Generic;
using UnityEngine;

/// Кольцо слотов вокруг игрока, как в рефе, + "дыхание" радиуса:
/// слоты плавно двигаются ближе/дальше, каждый — со своей фазой.
/// Враги занимают слоты и идут к НИМ, а не прямо к игроку.
[DisallowMultipleComponent]
public class PlayerOrbitTargets : MonoBehaviour
{
    public static PlayerOrbitTargets Instance { get; private set; }

    [Header("Ring (угол/вращение)")]
    [Min(2)] public int slots = 12;
    public float spinDegPerSec = 60f;
    public float startAngleDeg = 0f;

    [Header("Радиус")]
    [Tooltip("Средний радиус кольца.")]
    public float radius = 3.0f;
    [Tooltip("Амплитуда дыхания: фактический радиус = radius ± radialAmplitude.")]
    [Min(0f)] public float radialAmplitude = 1.0f;
    [Tooltip("Скорость дыхания (циклов в секунду).")]
    [Min(0f)] public float radialBreathHz = 0.6f;
    [Tooltip("Разброс фазы дыхания между слотами, в градусах (0 = синхронно).")]
    [Range(0f, 360f)] public float perSlotPhaseJitterDeg = 180f;
    [Tooltip("Случайная поправка амплитуды для каждого слота (±%).")]
    [Range(0f, 1f)] public float perSlotAmplitudeJitter = 0.25f;
    [Tooltip("Минимальный радиус, чтобы точки не залетали прямо в игрока.")]
    public float minSafeRadius = 0.8f;

    [Header("Смещение от направления движения игрока (как в видео)")]
    [Range(0f, 360f)] public float behindSectorWidth = 180f;
    [Range(0f, 2f)] public float behindBias = 1.2f;

    [Header("Gizmos")]
    public bool drawRing = true;
    public bool drawSlots = true;
    public float gizmoSize = 0.12f;
    public Color ringColor = new Color(0.25f, 0.9f, 1f, 0.35f);
    public Color slotColor = Color.cyan;

    // runtime
    readonly List<Transform> _points = new();
    readonly List<float> _baseAngles = new();     // базовые углы слотов (без фазы вращения)
    readonly List<float> _phaseJitter = new();    // фаза дыхания слота (рад)
    readonly List<float> _ampMul = new();         // множитель амплитуды для слота
    int?[] _owners;

    Vector3 _prevPos;
    Vector2 _heading = Vector2.right;
    float _spinPhaseDeg;   // фаза вращения (градусы)

    void Awake()
    {
        Instance = this;

        slots = Mathf.Max(2, slots);
        _owners = new int?[slots];

        _points.Clear(); _baseAngles.Clear(); _phaseJitter.Clear(); _ampMul.Clear();

        var pr = Mathf.Clamp01(perSlotAmplitudeJitter);
        for (int i = 0; i < slots; i++)
        {
            var t = new GameObject($"OrbitSlot_{i}").transform;
            t.SetParent(transform);
            _points.Add(t);

            float a = startAngleDeg + (360f / slots) * i;
            _baseAngles.Add(a);

            // случайная фаза (в радианах) и амплитуда на слот
            float phaseOff = Random.Range(-perSlotPhaseJitterDeg * 0.5f, perSlotPhaseJitterDeg * 0.5f) * Mathf.Deg2Rad;
            _phaseJitter.Add(phaseOff);

            float ampMul = 1f + Random.Range(-pr, pr);
            _ampMul.Add(ampMul);
        }

        _prevPos = transform.position;
        UpdateSlotPositions(0f);
    }

    void Update()
    {
        // направление движения игрока — для «захода сзади» (bias)
        var pos = transform.position;
        var v = pos - _prevPos;
        if (v.sqrMagnitude > 0.0001f) _heading = ((Vector2)v).normalized;
        _prevPos = pos;

        // фазы
        _spinPhaseDeg = Mathf.Repeat(_spinPhaseDeg + spinDegPerSec * Time.deltaTime, 360f);
        float t = Time.time; // для дыхания

        UpdateSlotPositions(t);
    }

    void UpdateSlotPositions(float t)
    {
        // дышащий радиус
        float w = Mathf.PI * 2f * Mathf.Max(0f, radialBreathHz);

        for (int i = 0; i < _points.Count; i++)
        {
            // угол с вращением
            float angDeg = Mathf.Repeat(_baseAngles[i] + _spinPhaseDeg, 360f);
            float angRad = angDeg * Mathf.Deg2Rad;

            // дыхание этого слота
            float breath = Mathf.Sin(w * t + _phaseJitter[i]);
            float r = radius + radialAmplitude * _ampMul[i] * breath;
            r = Mathf.Max(minSafeRadius, r);

            Vector3 off = new Vector3(Mathf.Cos(angRad), Mathf.Sin(angRad), 0f) * r;
            _points[i].position = transform.position + off;
        }
    }

    // ======== API для врагов ========

    /// Закрепить слот за врагом. Возвращает индекс слота.
    public int ClaimSlot(int requesterInstanceId, bool preferBehind = true)
    {
        // уже есть слот?
        for (int i = 0; i < _owners.Length; i++)
            if (_owners[i].HasValue && _owners[i].Value == requesterInstanceId)
                return i;

        // свободные
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
            foreach (var idx in free)
            {
                float slotAng = Mathf.Repeat(_baseAngles[idx] + _spinPhaseDeg, 360f);
                float d = Mathf.Abs(Mathf.DeltaAngle(slotAng, behindCenter));
                float inside = Mathf.InverseLerp(behindSectorWidth * 0.5f, 0f, d);
                float score = d / Mathf.Max(0.001f, (1f + inside * (behindBias - 1f)));
                if (score < bestScore) { bestScore = score; best = idx; }
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (drawRing)
        {
            Gizmos.color = ringColor;
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

        if (!drawSlots || !Application.isPlaying) return;
        Gizmos.color = slotColor;
        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i]) Gizmos.DrawSphere(_points[i].position, gizmoSize);
        }
    }
#endif
}
