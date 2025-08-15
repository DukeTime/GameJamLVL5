using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 12f;

    [Header("Movement")]
    public float moveSpeed = 3.2f;

    [Header("Orbit → Approach")]
    public float approachEnterSlotDistance = 0.35f;
    [Range(0f, 60f)] public float approachAlignAngleDeg = 30f;
    public bool requireLOSForApproach = false;
    public LayerMask losObstacleMask;
    public float approachCommitTime = 1.0f;

    [Header("Wander")]
    public float wanderRadius = 2.0f;
    public Vector2 wanderPauseRange = new Vector2(0.4f, 1.0f);
    public float wanderPointTolerance = 0.2f;

    public enum AttackKind { Classic, Dash }

    [Header("General Attack")]
    public AttackKind attackKindOnSpawn = AttackKind.Classic;
    public Vector2 chooseRandomKindWeights = new Vector2(0.7f, 0.3f);
    public float attackCooldown = 1.0f;
    public LayerMask playerMask;

    [Header("Classic (melee)")]
    public float classicWindup = 1.2f;
    public float classicHitRadius = 1.1f;
    public float classicHitOffset = 0.85f;
    public int classicDamage = 10;
    public float classicKnockback = 6f;
    public float classicAttackTriggerDistance = 1.25f;
    public float classicStopDistance = 1.0f;
    public float classicSlotIgnoreDistance = 1.6f;

    [Header("Dash (рыбок)")]
    public float dashWindup = 1.0f;
    public float dashDistance = 4.0f;
    public float dashDuration = 0.22f;
    public float dashHitRadius = 0.60f;
    public int dashDamage = 20; // один урон за скольжение
    public float dashKnockback = 8f;
    public bool dashStopOnWall = true;
    public float dashAttackTriggerDistance = 3.0f;
    public float dashStopDistance = 2.4f;
    public float dashSlotIgnoreDistance = 2.6f;

    [Header("VFX (опционально)")]
    public GameObject vfxWindup;
    public GameObject vfxHit;
    public GameObject vfxDashTrail;

    [Header("Sprites")]
    public EnemySpriteAnimator anim;

    [Header("Visual (rotate this to face)")]
    [SerializeField] private Transform visualRoot; // сюда тень+спрайт

    [Header("Debug")]
    public bool drawGizmos = true;

    Transform player;
    EnemyPathMover mover;
    Rigidbody2D rb;
    Transform orbitTarget; int mySlotIndex = -1; int myId;

    Vector2 spawnOrigin, wanderPoint; float nextWanderPickAt;

    enum State { Wander, Flank, Approach, Windup, Execute, Recover }
    State state = State.Wander;
    float approachUntil;

    Vector2 cachedDashDir;
    bool attackCooling;
    bool dashDidDamage; // флаг для одного удара за рывок

    float curAttackTriggerDistance;
    float curStopDistance;
    float curSlotIgnoreDistance;

    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyView view;
    [SerializeField] private bool spawning = true;
    void Start()
    {
        StartCoroutine(Timer(1f));
        data.OnDie += () => StartCoroutine(view.Death());
        
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<EnemyPathMover>();
        mover.maxSpeed = moveSpeed;

        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogError("Player not found."); enabled = false; return; }
        player = p.transform;

        if (PlayerOrbitTargets.Instance != null)
        {
            myId = gameObject.GetInstanceID();
            mySlotIndex = PlayerOrbitTargets.Instance.ClaimSlot(myId, true);
            orbitTarget = PlayerOrbitTargets.Instance.GetSlotTransform(mySlotIndex);
        }

        spawnOrigin = transform.position;
        PickNewWanderPoint(true);

        float sum = Mathf.Max(0.0001f, chooseRandomKindWeights.x + chooseRandomKindWeights.y);
        float r = Random.value * sum;
        attackKindOnSpawn = (r < chooseRandomKindWeights.x) ? AttackKind.Classic : AttackKind.Dash;

        ApplyAttackTuning();

        if (!anim) anim = GetComponentInChildren<EnemySpriteAnimator>();
        if (anim) anim.PlayWalkLoop();

        if (!visualRoot && anim) visualRoot = anim.transform.parent != null ? anim.transform.parent : anim.transform;
    }

    private IEnumerator Timer(float amount)
    {
        yield return new WaitForSeconds(amount);
        spawning = false;
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
        if (spawning) return;
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
                    Vector3 dst;
                    if (attackKindOnSpawn == AttackKind.Classic && distToPlayer <= curSlotIgnoreDistance)
                        dst = player.position;
                    else
                        dst = (slotValid && orbitTarget != null) ? orbitTarget.position : player.position;

                    mover.enabled = true;
                    mover.SetDestination(dst);

                    if (!attackCooling && distToPlayer <= curAttackTriggerDistance)
                        EnterWindup();
                    else if (CanStartApproach(hasLOSPlayer, slotValid))
                        state = State.Approach;
                    break;
                }

            case State.Approach:
                mover.enabled = true;
                mover.SetDestination(player.position);
                if (!attackCooling && distToPlayer <= curAttackTriggerDistance) EnterWindup();
                break;
        }
    }

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

    void EnterWindup()
    {
        state = State.Windup;
        mover.enabled = false;
        dashDidDamage = false; // сбрасываем перед рывком
        if (anim) anim.ShowWindup(attackKindOnSpawn == AttackKind.Dash);
        if (vfxWindup) Instantiate(vfxWindup, transform.position, Quaternion.identity);

        if (attackKindOnSpawn == AttackKind.Classic)
            StartCoroutine(ClassicRoutine());
        else
            StartCoroutine(DashRoutine());
    }

    IEnumerator ClassicRoutine()
    {
        yield return new WaitForSeconds(classicWindup);
        state = State.Execute;
        if (anim) anim.PlayAttackOnce();

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 hitPos = (Vector2)transform.position + dir * classicHitOffset;

        DoDamageCircle(hitPos, classicHitRadius, classicDamage, classicKnockback, dir);

        if (vfxHit) Instantiate(vfxHit, hitPos, Quaternion.identity);

        yield return new WaitForSeconds(attackCooldown);
        attackCooling = false;
        state = State.Flank;
        if (anim) anim.PlayWalkLoop();
        mover.enabled = true;
    }

    IEnumerator DashRoutine()
    {
        cachedDashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        yield return new WaitForSeconds(dashWindup);

        state = State.Execute;
        if (anim) anim.PlayAttackOnce();

        float t = 0f;
        Vector2 start = rb.position;
        Vector2 target = start + cachedDashDir * dashDistance;

        while (t < dashDuration)
        {
            t += Time.fixedDeltaTime;
            float alpha = Mathf.Clamp01(t / dashDuration);
            Vector2 next = Vector2.Lerp(start, target, alpha);

            if (dashStopOnWall && Physics2D.Linecast(rb.position, next, mover.obstacleMask))
                break;

            rb.MovePosition(next);

            if (!dashDidDamage)
            {
                dashDidDamage = DoDamageCircle(next, dashHitRadius, dashDamage, dashKnockback, cachedDashDir);
            }

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(attackCooldown);
        attackCooling = false;
        state = State.Flank;
        if (anim) anim.PlayWalkLoop();
        mover.enabled = true;
    }

    bool DoDamageCircle(Vector2 center, float radius, int dmg, float knockback, Vector2 fromDir)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius);
        bool didDamage = false;
        foreach (var h in hits)
        {
            bool isPlayer = (playerMask.value != 0) ? ((playerMask.value & (1 << h.gameObject.layer)) != 0)
                                                    : h.CompareTag("Player");
            if (!isPlayer) continue;

            h.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
            var prb = h.attachedRigidbody;
            if (prb) prb.AddForce(fromDir.normalized * knockback, ForceMode2D.Impulse);
            didDamage = true;
        }
        attackCooling = true;
        return didDamage;
    }

    void FaceByPlayerX()
    {
        float sign = (player.position.x >= transform.position.x) ? 1f : -1f;
        if (visualRoot)
            visualRoot.localRotation = Quaternion.Euler(0f, (sign < 0f ? 180f : 0f), 0f);
    }

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

    static float AngleFrom(Vector2 center, Vector2 point)
        => Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;
}
