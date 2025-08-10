using System.Collections.Generic;
using UnityEngine;

/// Ќавигатор: строит A* по NavGrid2D и сам идЄт по вейпоинтам.
/// ѕлюс локальное "разъезжание" (separation), чтобы враги не стопили друг друга.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathMover : MonoBehaviour
{
    [Header("Target & Speed")]
    public Transform target;
    public float speed = 3f;

    [Header("Tilemap Navigation")]
    public NavGrid2D nav;                // если null Ч найдЄм в Awake
    public LayerMask obstacleMask;       // если nav!=null Ч возьмЄм из nav

    [Header("Path planning")]
    public float repathInterval = 0.35f;
    public float repathWhenTargetMoved = 0.5f;
    public float pathStickTime = 0.35f;
    public float directSwitchDistance = 1.0f;
    public float targetSampleInterval = 0.20f;

    [Header("Movement")]
    public float waypointTolerance = 0.15f;

    [Header("Local Avoidance")]
    [Tooltip("—лой союзников (обычно слой Enemy).")]
    public LayerMask allyMask;
    [Tooltip("–адиус, в пределах которого разводим союзников (м).")]
    public float separationRadius = 0.6f;
    [Tooltip("Ќасколько сильно учитывать отталкивание (0..2).")]
    [Range(0f, 2f)] public float separationWeight = 0.7f;
    [Tooltip("ћаксимум учитываемых соседей (дл€ стабильности).")]
    public int separationMaxNeighbors = 6;

    // runtime
    readonly List<Vector2> path = new();
    int pathIndex = 0;
    float nextRepathTime = 0f;
    float stickPathUntil = 0f;

    Rigidbody2D rb;
    Vector2 pendingDir;
    Vector2 sampledTargetPos;
    float nextTargetSampleTime = 0f;
    Vector2 lastPlannedTarget;

    public void SetTarget(Transform t) => target = t;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        if (nav == null) nav = FindObjectOfType<NavGrid2D>();
        if (nav != null) obstacleMask = nav.obstacleMask;

        // если слой союзников не задан Ч попробуем слой "Enemy"
        if (allyMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) allyMask = (1 << enemyLayer);
        }

        SampleTarget(true);
        lastPlannedTarget = sampledTargetPos;
    }

    void Update() { Plan(); }
    void FixedUpdate() { MoveStep(); }

    void SampleTarget(bool force = false)
    {
        if (target == null) return;
        if (force || Time.time >= nextTargetSampleTime)
        {
            sampledTargetPos = target.position;
            nextTargetSampleTime = Time.time + targetSampleInterval;
        }
    }

    void Plan()
    {
        if (target == null) { pendingDir = Vector2.zero; return; }

        SampleTarget();
        var targetPos = sampledTargetPos;

        bool los = (obstacleMask.value != 0) && !Physics2D.Linecast(transform.position, targetPos, obstacleMask);

        // 1) ≈сли путь есть Ч следуем ему
        if (path.Count > 0)
        {
            Vector2 dest = path[pathIndex];

            if (Vector2.Distance(transform.position, dest) <= waypointTolerance)
            {
                pathIndex++;
                if (pathIndex >= path.Count) { path.Clear(); }
                else { dest = path[pathIndex]; }
            }

            if (path.Count == 0 && los && Vector2.Distance(transform.position, targetPos) <= directSwitchDistance)
            {
                // близко и видно Ч пойдЄм пр€мо
            }
            else if (path.Count == 0 && !los)
            {
                // держим пустой путь до следующего пересчЄта
            }
            else
            {
                pendingDir = ((path.Count > 0 ? path[pathIndex] : targetPos) - (Vector2)transform.position).normalized;
                pendingDir = ApplySeparation(pendingDir);
                TryRepath(targetPos, preferKeepCurrent: true);
                return;
            }
        }

        // 2) ѕуть пуст Ч если видно, идЄм пр€мо
        if (los)
        {
            pendingDir = (targetPos - (Vector2)transform.position).normalized;
            pendingDir = ApplySeparation(pendingDir);
            TryRepath(targetPos, preferKeepCurrent: true);
            return;
        }

        // 3) LOS нет Ч нужен путь
        TryRepath(targetPos, preferKeepCurrent: false);

        if (path.Count == 0)
        {
            pendingDir = ApplySeparation(Vector2.zero); // можно слегка разойтись, даже если стоим
            return;
        }

        pendingDir = (path[pathIndex] - (Vector2)transform.position).normalized;
        pendingDir = ApplySeparation(pendingDir);
    }

    void TryRepath(Vector2 targetPos, bool preferKeepCurrent)
    {
        bool canRepathNow = Time.time >= nextRepathTime
                            && ((targetPos - lastPlannedTarget).sqrMagnitude >= repathWhenTargetMoved * repathWhenTargetMoved);

        if (!canRepathNow) return;

        if (preferKeepCurrent && Time.time < stickPathUntil && path.Count > 0)
            return;

        nextRepathTime = Time.time + repathInterval;
        lastPlannedTarget = targetPos;

        if (nav == null) return;

        var tmp = new List<Vector2>();
        if (PathFinder2D.FindPath(transform.position, targetPos, nav, tmp))
        {
            path.Clear();
            path.AddRange(tmp);
            pathIndex = 0;
            stickPathUntil = Time.time + pathStickTime;
        }
    }

    Vector2 ApplySeparation(Vector2 desiredDir)
    {
        // ЋЄгкое разъезжание союзников поблизости
        var hits = Physics2D.OverlapCircleAll((Vector2)transform.position, separationRadius, allyMask);
        if (hits == null || hits.Length == 0) return desiredDir;

        Vector2 push = Vector2.zero;
        int counted = 0;

        foreach (var h in hits)
        {
            if (h.attachedRigidbody == rb) continue; // себ€ пропускаем

            // FIX: приводим оба к Vector2, чтобы не было неоднозначности Vector2-Vector3
            Vector2 toMe = (Vector2)transform.position - (Vector2)h.transform.position;
            float d = toMe.magnitude;
            if (d < 1e-3f) continue;

            float w = 1f - Mathf.Clamp01(d / separationRadius); // чем ближе Ч тем сильнее
            push += (toMe / d) * w;                             // нормализованное отталкивание
            counted++;
            if (counted >= separationMaxNeighbors) break;
        }

        if (push.sqrMagnitude < 1e-6f) return desiredDir;

        Vector2 dir = desiredDir;
        Vector2 combined = (dir + push * separationWeight);
        if (combined.sqrMagnitude < 1e-6f) return dir;

        return combined.normalized;
    }

    void MoveStep()
    {
        if (pendingDir == Vector2.zero) return;

        float dt = Time.fixedDeltaTime;
        Vector2 step = pendingDir * speed * dt;
        rb.MovePosition(rb.position + step);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (path == null || path.Count == 0) return;
        Gizmos.color = Color.green;

        Vector3 prev = transform.position;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Vector3 p3 = new Vector3(path[i].x, path[i].y, prev.z);
            Gizmos.DrawLine(prev, p3);
            Gizmos.DrawSphere(p3, 0.06f);
            prev = p3;
        }

        // радиус разъезжани€
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}
