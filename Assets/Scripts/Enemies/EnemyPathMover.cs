using System.Collections.Generic;
using UnityEngine;

/// Ƹ����� ���������� �� �������� ����� ������������� ���������.
/// - A*: ������ ������� � �������������� ��� � N �����
/// - ��������� � checkpoints[0]; ��� ������ ������ � RemoveAt(0), ��� ����������
/// - ������������� ���� ������ ����� ������ ����� ����������
/// - ���� �������� ������ LOS �� ���� � ������� ����� � ��� �����
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathMover : MonoBehaviour
{
    [Header("Navigation")]
    public NavGrid2D nav;                  // ���� �� ������ � ����� � Awake
    public LayerMask obstacleMask;         // ������ �� nav, ���� ������

    [Header("Movement")]
    public float maxSpeed = 3.2f;
    public float acceleration = 25f;       // ����������� (�/�^2)
    public float waypointTolerance = 0.12f;

    [Header("Checkpoints")]
    [Min(2)] public int checkpointCount = 10;      // ������� ����� �� ���� ����
    public float minCheckpointSpacing = 0.15f;     // ����������� ��� ����� �������

    [Header("Direct mode")]
    [Tooltip("���� �� ���� ��������� ������ ��������� � ��� ��������, ��������� �����.")]
    public bool enableDirectMode = true;

    // --- runtime ---
    Rigidbody2D rb;
    Vector2 velocity;

    // ������� ���� �� �����������
    Vector2 desiredTarget;      // ����� ����� ������ (����/�����)
    bool hasTarget = false;

    // ������������� ����� �������� ��������
    readonly List<Vector2> checkpoints = new();

    // API: ���������� �������� ������� ���� (����� ����� ���� ������ ����)
    public void SetDestination(Vector3 worldPos)
    {
        desiredTarget = (Vector2)worldPos;
        hasTarget = true;
        // �����: �� ������������� ���� �����! ������������� ������ ����� points �����������
        // (��� ��� ������ LOS � ����� ������ ��� �����)
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

        // 1) ������ ����� � ���� ���� ��������, ��� �����, ������� �����
        bool hasLOS = (obstacleMask.value == 0)
            ? true
            : !Physics2D.Linecast(rb.position, desiredTarget, obstacleMask);

        if (enableDirectMode && hasLOS)
        {
            checkpoints.Clear();
            return; // �������� � FixedUpdate ����� �������� � desiredTarget
        }

        // 2) ���� ������ ����� ��� � ������� ��� ���� ���
        if (checkpoints.Count == 0)
        {
            BuildCheckpoints(rb.position, desiredTarget);
            return;
        }

        // 3) ���� ������� � �������� ��������� � ������� ��� � ��� � ����������
        if (Vector2.Distance(rb.position, checkpoints[0]) <= waypointTolerance)
        {
            checkpoints.RemoveAt(0);
            // ������� ����������! ������ ����� ������ ���� � ����� BuildCheckpoints
        }
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        Vector2 targetPoint;

        // ������ �����: ��� ���������� � LOS ������
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
                // ���� ����������� (��� �� ���������� ��� ����������) � ��������� ���������
                BuildCheckpoints(rb.position, desiredTarget);
                if (checkpoints.Count == 0) { SlowDown(); return; }
            }
            targetPoint = checkpoints[0];
        }

        // ������� �������� � ������� �����
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

    // --- ���������� ƨ������ ������ ���������� ---
    void BuildCheckpoints(Vector2 start, Vector2 goal)
    {
        checkpoints.Clear();

        if (nav == null)
        {
            // ��� �������� � fallback: ���� �� �������� �����
            checkpoints.Add(goal);
            return;
        }

        var nodes = new List<Vector2>();
        if (!PathFinder2D.FindPath(start, goal, nav, nodes))
        {
            // ���� ��� � fallback: ��������
            checkpoints.Add(goal);
            return;
        }

        GenerateEvenCheckpoints(start, nodes, goal, checkpointCount, minCheckpointSpacing, checkpoints);
    }

    // ����������� ������������� �������� � N ����� (������� goal)
    static void GenerateEvenCheckpoints(
        Vector2 start, List<Vector2> nodes, Vector2 goal,
        int targetCount, float minSpacing, List<Vector2> outPoints)
    {
        outPoints.Clear();
        if (targetCount < 2) targetCount = 2;

        // ���������: start -> nodes -> goal
        var poly = new List<Vector2>(nodes.Count + 2);
        poly.Add(start);
        poly.AddRange(nodes);
        poly.Add(goal);

        // ����� �����
        float total = 0f;
        for (int i = 1; i < poly.Count; i++)
            total += Vector2.Distance(poly[i - 1], poly[i]);

        if (total < 1e-4f)
        {
            outPoints.Add(goal);
            return;
        }

        // ������ ����� �� ����� 1/targetCount, 2/targetCount, ..., 1
        float step = total / targetCount;
        float nextAt = step; // ������ ����������� ������ ����

        int seg = 1;
        float accumulated = 0f;

        for (int cp = 0; cp < targetCount; cp++)
        {
            float want = step * (cp + 1); // �� �������� ����� � ��������� �������

            // ��������� �� ���������, ���� �� ��������� ������ �����
            while (seg < poly.Count && accumulated + Vector2.Distance(poly[seg - 1], poly[seg]) < want)
            {
                accumulated += Vector2.Distance(poly[seg - 1], poly[seg]);
                seg++;
            }
            if (seg >= poly.Count)
            {
                // ������������ � ������� goal
                TryAddPoint(goal, minSpacing, outPoints);
                continue;
            }

            float segLen = Vector2.Distance(poly[seg - 1], poly[seg]);
            float t = Mathf.InverseLerp(accumulated, accumulated + segLen, want);
            Vector2 p = Vector2.Lerp(poly[seg - 1], poly[seg], t);

            TryAddPoint(p, minSpacing, outPoints);
        }

        // ����������� goal ���������
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
