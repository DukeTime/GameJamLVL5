using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Detection & Movement")]
    public float viewRadius = 10f;
    public float stopDistance = 2.5f;      // дистанция для атаки (используем вместе с LOS)
    public float moveSpeed = 3f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public int damage = 10;
    public GameObject attackZonePrefab;
    public float attackZoneOffset = 1.25f;

    [Header("Navigation")]
    public NavGrid2D nav;                  // можно оставить пустым — возьмём через FindObjectOfType
    public LayerMask obstacleMaskFallback; // если nav нет, используем это

    [Header("Rules")]
    public bool requireLineOfSightForAttack = true;

    [Header("DEBUG")]
    public bool drawGizmos = true;

    private Transform player;
    private Transform orbitTarget;
    private EnemyPathMover pathMover;
    private GameObject currentAttackZone;

    private bool canAttack = true;
    private int myId;
    private bool hasAttackSlot = false;

    private enum State { Idle, Chase, Attack }
    private State state = State.Idle;

    void Start()
    {
        myId = gameObject.GetInstanceID();

        // Игрок
        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogError("Player not found! Tag 'Player' required."); return; }
        player = p.transform;

        // Орбитальная точка вокруг игрока (как в твоей системе)
        if (PlayerOrbitTargets.Instance != null)
            orbitTarget = PlayerOrbitTargets.Instance.ClaimBestPoint(true);

        // Навигатор: САМ планирует в Update и двигается в FixedUpdate — ничего вызывать не нужно
        pathMover = GetComponent<EnemyPathMover>();
        if (pathMover == null) pathMover = gameObject.AddComponent<EnemyPathMover>();
        pathMover.speed = moveSpeed;

        if (nav == null) nav = FindObjectOfType<NavGrid2D>();
        pathMover.nav = nav;
        pathMover.obstacleMask = (nav != null) ? nav.obstacleMask : obstacleMaskFallback;

        // Цель навигатора — орбитальная точка, если есть; иначе сам игрок
        pathMover.SetTarget(orbitTarget != null ? orbitTarget : player);
    }

    void OnDisable()
    {
        // на всякий — освобождаем слот, если уходим из сцены/деспавнимся
        if (hasAttackSlot) { AttackCoordinator2D.Instance?.Release(myId); hasAttackSlot = false; }
    }

    void OnDestroy()
    {
        if (PlayerOrbitTargets.Instance != null && orbitTarget != null)
            PlayerOrbitTargets.Instance.ReleasePoint(orbitTarget);
        if (hasAttackSlot) { AttackCoordinator2D.Instance?.Release(myId); hasAttackSlot = false; }
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool hasLOS = HasLOS();

        FacePlayer2D();

        switch (state)
        {
            case State.Idle:
                if (dist <= viewRadius) state = State.Chase;
                break;

            case State.Chase:
                if (dist > viewRadius)
                {
                    state = State.Idle;
                    RemoveAttackZone();
                    ReleaseAttackSlotIfAny();
                }
                else if (dist <= stopDistance && (!requireLineOfSightForAttack || hasLOS))
                {
                    // В атаку только если есть слот (лимитируем количество атакующих)
                    if (AttackCoordinator2D.Instance == null || AttackCoordinator2D.Instance.TryAcquire(myId))
                    {
                        hasAttackSlot = true;
                        state = State.Attack;
                    }
                    // иначе остаёмся в Chase — PathMover держит дистанцию у орбиты, + будет разъезжание
                }
                else
                {
                    RemoveAttackZone(); // двигаемся — навигатор делает это сам
                    ReleaseAttackSlotIfAny();
                }
                break;

            case State.Attack:
                // потеряли дистанцию/LOS — уходим с атаки и освобождаем слот
                if (dist > stopDistance || (requireLineOfSightForAttack && !hasLOS))
                {
                    state = State.Chase;
                    RemoveAttackZone();
                    ReleaseAttackSlotIfAny();
                }
                else
                {
                    CreateAttackZone();
                    if (canAttack) StartCoroutine(Attack());
                }
                break;
        }

        UpdateAttackZonePosition();
    }

    void ReleaseAttackSlotIfAny()
    {
        if (!hasAttackSlot) return;
        AttackCoordinator2D.Instance?.Release(myId);
        hasAttackSlot = false;
    }

    bool HasLOS()
    {
        LayerMask mask = (pathMover != null && pathMover.obstacleMask.value != 0)
            ? pathMover.obstacleMask
            : (nav != null ? nav.obstacleMask : obstacleMaskFallback);

        if (mask.value == 0) return false;
        return !Physics2D.Linecast(transform.position, player.position, mask);
    }

    void FacePlayer2D()
    {
        Vector3 s = transform.localScale;
        s.x = (player.position.x >= transform.position.x) ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    void CreateAttackZone()
    {
        if (currentAttackZone != null || attackZonePrefab == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 spawnPos = (Vector2)transform.position + dir * attackZoneOffset;

        currentAttackZone = Instantiate(
            attackZonePrefab,
            new Vector3(spawnPos.x, spawnPos.y, transform.position.z),
            Quaternion.identity
        );

        var az = currentAttackZone.GetComponent<EnemyAttackZone>();
        if (az != null)
        {
            az.damage = damage;
            az.SetEnemyReference(this);
            az.SetTickInterval(attackCooldown);
        }
    }

    void RemoveAttackZone()
    {
        if (currentAttackZone == null) return;
        Destroy(currentAttackZone);
        currentAttackZone = null;
    }

    void UpdateAttackZonePosition()
    {
        if (currentAttackZone == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 pos = (Vector2)transform.position + dir * attackZoneOffset;

        currentAttackZone.transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        currentAttackZone.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    IEnumerator Attack()
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
    }
}
