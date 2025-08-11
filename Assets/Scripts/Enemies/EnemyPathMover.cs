using System.Collections.Generic;
using UnityEngine;

/// Жёсткое следование по маршруту через фиксированные чекпоинты.
/// - A*: строим маршрут и дискретизируем его в N точек
/// - Двигаемся к checkpoints[0]; как только близко — RemoveAt(0), БЕЗ перестроек
/// - Перестраиваем путь ТОЛЬКО когда список точек закончился
/// - Если появился прямой LOS до цели — очищаем точки и идём прямо
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathMover : MonoBehaviour
{
    [Header("Navigation")]
    public NavGrid2D nav;                  // если не задано — найдём в Awake
    public LayerMask obstacleMask;         // возьмём из nav, если пустой

    [Header("Movement")]
    public float maxSpeed = 3.2f;
    public float acceleration = 25f;       // сглаживание (м/с^2)
    public float waypointTolerance = 0.12f;

    [Header("Checkpoints")]
    [Min(2)] public int checkpointCount = 10;      // Сколько точек на весь путь
    public float minCheckpointSpacing = 0.15f;     // Минимальный шаг между точками

    [Header("Direct mode")]
    [Tooltip("Если до цели открылась прямая видимость — идём напрямую, игнорируя точки.")]
    public bool enableDirectMode = true;

    // --- runtime ---
    Rigidbody2D rb;
    Vector2 velocity;

    // внешняя цель от контроллера
    Vector2 desiredTarget;      // «куда хотим прийти» (слот/игрок)
    bool hasTarget = false;

    // фиксированные точки текущего маршрута
    readonly List<Vector2> checkpoints = new();

    // API: контроллер сообщает текущую цель (можно звать хоть каждый кадр)
    public void SetDestination(Vector3 worldPos)
    {
        desiredTarget = (Vector2)worldPos;
        hasTarget = true;
        // ВАЖНО: не перестраиваем путь здесь! Перестраиваем ТОЛЬКО когда points закончились
        // (или при прямом LOS — тогда вообще без точек)
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (nav == null) nav = FindObjectOfType<NavGrid2D>();
        if (nav != null && obstacleMask.value == 0) obstacleMask = nav.obstacleMask;
    }

    void Update()
    {
        if (!hasTarget) return;

        // 1) Прямой режим — если путь свободен, идём прямо, очищаем точки
        bool hasLOS = (obstacleMask.value == 0)
            ? true
            : !Physics2D.Linecast(rb.position, desiredTarget, obstacleMask);

        if (enableDirectMode && hasLOS)
        {
            checkpoints.Clear();
            return; // движение в FixedUpdate будет напрямую к desiredTarget
        }

        // 2) Если списка точек нет — построй его ОДИН РАЗ
        if (checkpoints.Count == 0)
        {
            BuildCheckpoints(rb.position, desiredTarget);
            return;
        }

        // 3) Если подошли к текущему чекпоинту — удаляем его и идём к следующему
        if (Vector2.Distance(rb.position, checkpoints[0]) <= waypointTolerance)
        {
            checkpoints.RemoveAt(0);
            // Никаких перестроек! Только когда список пуст — снова BuildCheckpoints
        }
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        Vector2 targetPoint;

        // прямой режим: нет чекпоинтов и LOS открыт
        bool hasLOS = (obstacleMask.value == 0)
            ? true
            : !Physics2D.Linecast(rb.position, desiredTarget, obstacleMask);

        if (enableDirectMode && hasLOS)
        {
            targetPoint = desiredTarget;
        }
        else
        {
            if (checkpoints.Count == 0)
            {
                // Путь отсутствует (ещё не построился или закончился) — попробуем построить
                BuildCheckpoints(rb.position, desiredTarget);
                if (checkpoints.Count == 0) { SlowDown(); return; }
            }
            targetPoint = checkpoints[0];
        }

        // Плавное движение к целевой точке
        Vector2 dir = targetPoint - rb.position;
        if (dir.sqrMagnitude > 1e-6f) dir.Normalize();

        Vector2 desiredVel = dir * maxSpeed;
        velocity = Vector2.MoveTowards(velocity, desiredVel, acceleration * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void SlowDown()
    {
        if (velocity.sqrMagnitude > 1e-6f)
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
    }

    // --- построение ЖЁСТКОГО списка чекпоинтов ---
    void BuildCheckpoints(Vector2 start, Vector2 goal)
    {
        checkpoints.Clear();

        if (nav == null)
        {
            // без навсетки — fallback: хотя бы конечная точка
            checkpoints.Add(goal);
            return;
        }

        var nodes = new List<Vector2>();
        if (!PathFinder2D.FindPath(start, goal, nav, nodes))
        {
            // пути нет — fallback: конечная
            checkpoints.Add(goal);
            return;
        }

        GenerateEvenCheckpoints(start, nodes, goal, checkpointCount, minCheckpointSpacing, checkpoints);
    }

    // Равномерная дискретизация маршрута в N точек (включая goal)
    static void GenerateEvenCheckpoints(
        Vector2 start, List<Vector2> nodes, Vector2 goal,
        int targetCount, float minSpacing, List<Vector2> outPoints)
    {
        outPoints.Clear();
        if (targetCount < 2) targetCount = 2;

        // Полилиния: start -> nodes -> goal
        var poly = new List<Vector2>(nodes.Count + 2);
        poly.Add(start);
        poly.AddRange(nodes);
        poly.Add(goal);

        // Общая длина
        float total = 0f;
        for (int i = 1; i < poly.Count; i++)
            total += Vector2.Distance(poly[i - 1], poly[i]);

        if (total < 1e-4f)
        {
            outPoints.Add(goal);
            return;
        }

        // Ставим точки на долях 1/targetCount, 2/targetCount, ..., 1
        float step = total / targetCount;
        float nextAt = step; // первая контрольная «доля» пути

        int seg = 1;
        float accumulated = 0f;

        for (int cp = 0; cp < targetCount; cp++)
        {
            float want = step * (cp + 1); // не включаем точку в стартовой позиции

            // Двигаемся по сегментам, пока не достигнем нужной длины
            while (seg < poly.Count && accumulated + Vector2.Distance(poly[seg - 1], poly[seg]) < want)
            {
                accumulated += Vector2.Distance(poly[seg - 1], poly[seg]);
                seg++;
            }
            if (seg >= poly.Count)
            {
                // подстраховка — добавим goal
                TryAddPoint(goal, minSpacing, outPoints);
                continue;
            }

            float segLen = Vector2.Distance(poly[seg - 1], poly[seg]);
            float t = Mathf.InverseLerp(accumulated, accumulated + segLen, want);
            Vector2 p = Vector2.Lerp(poly[seg - 1], poly[seg], t);

            TryAddPoint(p, minSpacing, outPoints);
        }

        // гарантируем goal последним
        if (outPoints.Count == 0 || Vector2.Distance(outPoints[^1], goal) > 0.001f)
            TryAddPoint(goal, minSpacing, outPoints);
    }

    static void TryAddPoint(Vector2 p, float minSpacing, List<Vector2> list)
    {
        if (list.Count == 0 || Vector2.Distance(list[^1], p) >= minSpacing)
            list.Add(p);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (checkpoints == null || checkpoints.Count == 0) return;
        Gizmos.color = Color.green;
        Vector3 prev = transform.position;
        for (int i = 0; i < checkpoints.Count; i++)
        {
            Vector3 p3 = new Vector3(checkpoints[i].x, checkpoints[i].y, prev.z);
            Gizmos.DrawLine(prev, p3);
            Gizmos.DrawSphere(p3, 0.06f);
            prev = p3;
        }
    }
#endif
}
