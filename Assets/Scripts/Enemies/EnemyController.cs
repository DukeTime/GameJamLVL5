using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 12f;

    [Header("Movement")]
    public float moveSpeed = 3.2f;          // прокинем в EnemyPathMover.maxSpeed

    [Header("Attack")]
    public float stopDistance = 2.4f;
    public float attackCooldown = 1.2f;
    public int damage = 10;
    public GameObject attackZonePrefab;
    public float attackZoneOffset = 1.1f;

    [Header("Orbit → Approach")]
    public float approachEnterSlotDistance = 0.35f;
    [Range(0f, 60f)] public float approachAlignAngleDeg = 30f;
    public bool requireLOSForApproach = false;
    [Tooltip("Слой стен для LOS/валидности слота. Если 0 — LOS не проверяем.")]
    public LayerMask losObstacleMask;
    public float approachCommitTime = 1.3f;

    [Header("Debug")]
    public bool drawGizmos = true;

    Transform player;
    Transform orbitTarget; int mySlotIndex = -1; int myId;

    GameObject currentAttackZone; bool canAttack = true;

    enum State { Idle, Flank, Approach, Attack }
    State state = State.Idle;
    float approachUntil = 0f;

    // Path mover
    EnemyPathMover mover;

    void Start()
    {
        myId = gameObject.GetInstanceID();

        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogError("Player not found (tag 'Player')."); enabled = false; return; }
        player = p.transform;

        if (PlayerOrbitTargets.Instance != null)
        {
            mySlotIndex = PlayerOrbitTargets.Instance.ClaimSlot(myId, preferBehind: true);
            orbitTarget = PlayerOrbitTargets.Instance.GetSlotTransform(mySlotIndex);
        }

        mover = GetComponent<EnemyPathMover>();
        if (mover == null) mover = gameObject.AddComponent<EnemyPathMover>();
        mover.maxSpeed = moveSpeed;

        if (mover.nav == null) mover.nav = FindObjectOfType<NavGrid2D>();
        if (mover.nav != null && mover.obstacleMask.value == 0) mover.obstacleMask = mover.nav.obstacleMask;
    }

    void OnDestroy()
    {
        if (PlayerOrbitTargets.Instance != null && mySlotIndex >= 0)
            PlayerOrbitTargets.Instance.ReleaseSlot(mySlotIndex, myId);
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        bool hasLOSPlayer = (losObstacleMask.value == 0) ? true : !Physics2D.Linecast(transform.position, player.position, losObstacleMask);

        FacePlayer2D();

        switch (state)
        {
            case State.Idle:
                if (distToPlayer <= viewRadius) state = State.Flank;
                break;

            case State.Flank:
                {
                    // Если слот «по другую сторону стены» от игрока — игнорируем слот и идём к игроку
                    bool slotValid = IsSlotValid();
                    Vector3 dst = slotValid && orbitTarget != null ? orbitTarget.position : player.position;

                    mover.SetDestination(dst);

                    if (CanStartApproach(hasLOSPlayer, slotValid))
                    {
                        state = State.Approach; approachUntil = Time.time + approachCommitTime;
                    }
                    else if (distToPlayer <= stopDistance)
                    {
                        state = State.Attack;
                    }
                    break;
                }

            case State.Approach:
                mover.SetDestination(player.position);  // фиксированный заход
                if (distToPlayer <= stopDistance) { state = State.Attack; }
                else if (Time.time >= approachUntil) { state = State.Flank; }
                break;

            case State.Attack:
                if (distToPlayer > stopDistance * 1.1f) { RemoveAttackZone(); state = State.Flank; }
                else { CreateAttackZone(); if (canAttack) StartCoroutine(AttackRoutine()); }
                break;
        }

        UpdateAttackZoneTransform();
    }

    bool CanStartApproach(bool hasLOSToPlayer, bool slotValid)
    {
        // если слота нет или он невалиден (за стеной от игрока) — решение только по LOS/дистанции
        if (orbitTarget == null || !slotValid)
            return !requireLOSForApproach || hasLOSToPlayer;

        float dSlot = Vector2.Distance(transform.position, orbitTarget.position);
        if (dSlot > approachEnterSlotDistance) return false;

        float aEnemy = AngleFrom(player.position, transform.position);
        float aSlot = AngleFrom(player.position, orbitTarget.position);
        float delta = Mathf.Abs(Mathf.DeltaAngle(aEnemy, aSlot));
        if (delta > approachAlignAngleDeg) return false;

        if (requireLOSForApproach && !hasLOSToPlayer) return false;
        return true;
    }

    // слот валиден, если линия Игрок→Слот НЕ пересекает стены
    bool IsSlotValid()
    {
        if (orbitTarget == null) return false;
        if (losObstacleMask.value == 0) return true;
        return !Physics2D.Linecast(player.position, orbitTarget.position, losObstacleMask);
    }

    static float AngleFrom(Vector2 center, Vector2 point)
        => Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;

    void FacePlayer2D()
    {
        var s = transform.localScale;
        s.x = (player.position.x >= transform.position.x) ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // --- атака как прежде ---
    void CreateAttackZone()
    {
        if (currentAttackZone != null || attackZonePrefab == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 pos = (Vector2)transform.position + dir * attackZoneOffset;

        currentAttackZone = Instantiate(
            attackZonePrefab,
            new Vector3(pos.x, pos.y, transform.position.z),
            Quaternion.identity
        );

        var az = currentAttackZone.GetComponent<EnemyAttackZone>();
        if (az != null)
        {
            az.damage = damage;
            az.SetEnemyReference(this);
        }
    }

    void RemoveAttackZone()
    {
        if (currentAttackZone == null) return;
        Destroy(currentAttackZone);
        currentAttackZone = null;
    }

    void UpdateAttackZoneTransform()
    {
        if (currentAttackZone == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 pos = (Vector2)transform.position + dir * attackZoneOffset;

        currentAttackZone.transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        currentAttackZone.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }


    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, stopDistance);
        if (orbitTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, orbitTarget.position);
            Gizmos.DrawSphere(orbitTarget.position, 0.08f);
        }
    }
}
