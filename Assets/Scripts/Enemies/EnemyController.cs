using UnityEngine;
using System.Collections;

/// Поведение: бродит → флангует к слоту → заход → атака (Classic/Dash).
/// Для Classic теперь подходим БЛИЖЕ (персональные дистанции), в конце игнорим слот и берём прямую.
[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ===== Perception & movement =====
    [Header("Perception")]
    public float viewRadius = 12f;                // видит игрока по радиусу (сквозь стены)

    [Header("Movement")]
    public float moveSpeed = 3.2f;                // прокидываем в EnemyPathMover.maxSpeed

    [Header("Orbit → Approach")]
    public float approachEnterSlotDistance = 0.35f;
    [Range(0f, 60f)] public float approachAlignAngleDeg = 30f;
    public bool requireLOSForApproach = false;
    public LayerMask losObstacleMask;             // слой стен только для условий захода
    public float approachCommitTime = 1.0f;

    [Header("Wander")]
    public float wanderRadius = 2.0f;
    public Vector2 wanderPauseRange = new Vector2(0.4f, 1.0f);
    public float wanderPointTolerance = 0.2f;

    // ===== Attacks =====
    public enum AttackKind { Classic, Dash }

    [Header("General Attack")]
    [Tooltip("Выбирается один раз при спавне.")]
    public AttackKind attackKindOnSpawn = AttackKind.Classic;
    public Vector2 chooseRandomKindWeights = new Vector2(0.7f, 0.3f); // Classic=0.7, Dash=0.3
    public float attackCooldown = 1.0f;            // после Execute
    public LayerMask playerMask;                    // слой игрока (или оставь 0 и проверим по тегу)

    [Header("Classic (melee)")]
    public float classicWindup = 1.2f;             // 1–1.5 c телеграф
    public float classicHitRadius = 1.1f;
    public float classicHitOffset = 0.85f;
    public int classicDamage = 10;
    public float classicKnockback = 6f;
    [Tooltip("С какой дистанции начинаем телеграф (ближняя).")]
    public float classicAttackTriggerDistance = 1.25f;
    [Tooltip("На какой дистанции считаем 'вошли в атаку' (останавливаемся).")]
    public float classicStopDistance = 1.0f;
    [Tooltip("При ближнем бою, если ближе этого радиуса — игнорим слот и целимся прямо в игрока.")]
    public float classicSlotIgnoreDistance = 1.6f;

    [Header("Dash (рыбок)")]
    public float dashWindup = 1.0f;
    public float dashDistance = 4.0f;
    public float dashDuration = 0.22f;
    public float dashHitRadius = 0.60f;
    public int dashDamage = 12;
    public float dashKnockback = 8f;
    public bool dashStopOnWall = true;
    [Tooltip("С какой дистанции начинаем телеграф (подальше).")]
    public float dashAttackTriggerDistance = 3.0f;
    [Tooltip("Для рывка можно оставаться подальше.")]
    public float dashStopDistance = 2.4f;
    [Tooltip("Для рывка слоты можно не игнорить так рано.")]
    public float dashSlotIgnoreDistance = 2.6f;

    [Header("VFX (опционально)")]
    public GameObject vfxWindup;                   // частицы на телеграф
    public GameObject vfxHit;                      // вспышка в момент удара/попадания
    public GameObject vfxDashTrail;                // след во время рывка

    [Header("Sprites")]
    public EnemySpriteAnimator anim;

    [Header("Debug")]
    public bool drawGizmos = true;

    // ===== Runtime =====
    Transform player;
    EnemyPathMover mover;
    Rigidbody2D rb;
    Transform orbitTarget; int mySlotIndex = -1; int myId;

    // Wander
    Vector2 spawnOrigin, wanderPoint; float nextWanderPickAt;

    enum State { Wander, Flank, Approach, Windup, Execute, Recover }
    State state = State.Wander;
    float approachUntil;

    // Attack runtime
    Vector2 cachedDashDir;
    bool attackCooling;

    // Текущие (зависят от типа атаки)
    float curAttackTriggerDistance;
    float curStopDistance;
    float curSlotIgnoreDistance;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<EnemyPathMover>();
        mover.maxSpeed = moveSpeed;

        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogError("Player not found (tag 'Player')."); enabled = false; return; }
        player = p.transform;

        if (PlayerOrbitTargets.Instance != null)
        {
            myId = gameObject.GetInstanceID();
            mySlotIndex = PlayerOrbitTargets.Instance.ClaimSlot(myId, true);
            orbitTarget = PlayerOrbitTargets.Instance.GetSlotTransform(mySlotIndex);
        }

        spawnOrigin = transform.position;
        PickNewWanderPoint(true);

        // Выбор типа атаки при появлении
        float sum = Mathf.Max(0.0001f, chooseRandomKindWeights.x + chooseRandomKindWeights.y);
        float r = Random.value * sum;
        attackKindOnSpawn = (r < chooseRandomKindWeights.x) ? AttackKind.Classic : AttackKind.Dash;

        ApplyAttackTuning();

        if (!anim) anim = GetComponentInChildren<EnemySpriteAnimator>();
        if (anim) anim.PlayWalkLoop();
    }

    void ApplyAttackTuning()
    {
        if (attackKindOnSpawn == AttackKind.Classic)
        {
            curAttackTriggerDistance = classicAttackTriggerDistance;
            curStopDistance = classicStopDistance;
            curSlotIgnoreDistance = classicSlotIgnoreDistance;
        }
        else
        {
            curAttackTriggerDistance = dashAttackTriggerDistance;
            curStopDistance = dashStopDistance;
            curSlotIgnoreDistance = dashSlotIgnoreDistance;
        }
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

        FaceByPlayerX();

        switch (state)
        {
            case State.Wander:
                DoWander();
                if (distToPlayer <= viewRadius && !attackCooling) state = State.Flank;
                break;

            case State.Flank:
                {
                    bool slotValid = IsSlotValid();

                    // Куда бежим: слот или прямо в игрока?
                    Vector3 dst;
                    if (attackKindOnSpawn == AttackKind.Classic && distToPlayer <= curSlotIgnoreDistance)
                    {
                        // Ближний бой — финальный участок идём прямо к игроку
                        dst = player.position;
                    }
                    else
                    {
                        dst = (slotValid && orbitTarget != null) ? orbitTarget.position : player.position;
                    }

                    mover.enabled = true;
                    mover.SetDestination(dst);

                    if (!attackCooling && distToPlayer <= curAttackTriggerDistance)
                    {
                        EnterWindup();
                    }
                    else if (CanStartApproach(hasLOSPlayer, slotValid))
                    {
                        state = State.Approach; approachUntil = Time.time + approachCommitTime;
                    }
                    break;
                }

            case State.Approach:
                mover.enabled = true;
                mover.SetDestination(player.position);
                if (!attackCooling && distToPlayer <= curAttackTriggerDistance) { EnterWindup(); }
                else if (Time.time >= approachUntil) { state = State.Flank; }
                break;

            case State.Windup:
            case State.Execute:
            case State.Recover:
                // управление движением идёт через корутины атаки — здесь ничего
                break;
        }
    }

    // ===== WANDER =====
    void DoWander()
    {
        if (Time.time >= nextWanderPickAt || Vector2.Distance(transform.position, wanderPoint) <= wanderPointTolerance)
            PickNewWanderPoint();
        mover.enabled = true;
        mover.SetDestination(wanderPoint);
        if (anim) anim.PlayWalkLoop();
    }

    void PickNewWanderPoint(bool immediate = false)
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        wanderPoint = spawnOrigin + rnd;
        float pause = Random.Range(wanderPauseRange.x, wanderPauseRange.y);
        nextWanderPickAt = Time.time + (immediate ? 0f : pause);
    }

    // ===== ATTACK FLOW =====
    void EnterWindup()
    {
        state = State.Windup;
        mover.enabled = false; // стопим навигацию
        if (anim) anim.ShowWindup(attackKindOnSpawn == AttackKind.Dash);
        if (vfxWindup) Instantiate(vfxWindup, transform.position, Quaternion.identity);

        if (attackKindOnSpawn == AttackKind.Classic)
            StartCoroutine(ClassicRoutine());
        else
            StartCoroutine(DashRoutine());
    }

    IEnumerator ClassicRoutine()
    {
        // Телеграф
        yield return new WaitForSeconds(classicWindup);

        // Execute
        state = State.Execute;
        if (anim) anim.PlayAttackOnce();

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position);
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        Vector2 hitPos = (Vector2)transform.position + dir * classicHitOffset;

        DoDamageCircle(hitPos, classicHitRadius, classicDamage, classicKnockback, dir);

        if (vfxHit) Instantiate(vfxHit, hitPos, Quaternion.identity);

        // Recover
        state = State.Recover;
        yield return new WaitForSeconds(attackCooldown);

        attackCooling = false;
        state = State.Flank;
        if (anim) anim.PlayWalkLoop();
        mover.enabled = true;
    }

    IEnumerator DashRoutine()
    {
        // Телеграф + запоминаем направление
        cachedDashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        yield return new WaitForSeconds(dashWindup);

        // Execute (рывок)
        state = State.Execute;
        if (anim) anim.PlayAttackOnce();

        float t = 0f;
        Vector2 start = rb.position;
        Vector2 target = start + cachedDashDir * dashDistance;

        GameObject trail = null;
        if (vfxDashTrail) trail = Instantiate(vfxDashTrail, transform.position, Quaternion.identity);

        while (t < dashDuration)
        {
            t += Time.fixedDeltaTime;
            float alpha = Mathf.Clamp01(t / dashDuration);
            Vector2 next = Vector2.Lerp(start, target, alpha);

            // столкновение со стеной — остановим
            if (dashStopOnWall && Physics2D.Linecast(rb.position, next, mover.obstacleMask))
                break;

            rb.MovePosition(next);

            // активный хитбокс
            DoDamageCircle(next, dashHitRadius, dashDamage, dashKnockback, cachedDashDir);

            yield return new WaitForFixedUpdate();
        }

        if (trail) Destroy(trail, 0.5f);

        // Recover
        state = State.Recover;
        yield return new WaitForSeconds(attackCooldown);

        attackCooling = false;
        state = State.Flank;
        if (anim) anim.PlayWalkLoop();
        mover.enabled = true;
    }

    // ===== DAMAGE =====
    void DoDamageCircle(Vector2 center, float radius, int dmg, float knockback, Vector2 fromDir)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var h in hits)
        {
            bool isPlayer = (playerMask.value != 0) ? ((playerMask.value & (1 << h.gameObject.layer)) != 0)
                                                    : h.CompareTag("Player");
            if (!isPlayer) continue;

            h.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);

            var prb = h.attachedRigidbody;
            if (prb) prb.AddForce(fromDir.normalized * knockback, ForceMode2D.Impulse);
        }

        attackCooling = true;
    }

    // ===== Helpers =====
    bool CanStartApproach(bool hasLOSToPlayer, bool slotValid)
    {
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

    bool IsSlotValid()
    {
        if (orbitTarget == null) return false;
        if (losObstacleMask.value == 0) return true;
        return !Physics2D.Linecast(player.position, orbitTarget.position, losObstacleMask);
    }

    void FaceByPlayerX()
    {
        if (!anim) return;
        float sign = (player.position.x >= transform.position.x) ? 1f : -1f;
        anim.FaceDir(sign);
    }

    static float AngleFrom(Vector2 center, Vector2 point)
        => Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Текущие дистанции
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, curAttackTriggerDistance);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, curStopDistance);

        // Classic удар (примерная позиция)
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 pos = (Vector2)transform.position + dir * classicHitOffset;
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(pos, classicHitRadius);
        }
    }
#endif
}
