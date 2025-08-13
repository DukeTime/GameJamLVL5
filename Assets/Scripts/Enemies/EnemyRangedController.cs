using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 14f;                         // видим игрока
    public LayerMask losObstacleMask;                      // слой стен дл€ LOS

    [Header("Movement")]
    public float moveSpeed = 3.0f;                         // прокидываем в mover.maxSpeed
    public float repathInterval = 0.2f;                    // как часто обновл€ть путь

    [Header("Kiting (держим дистанцию)")]
    public float preferredRange = 6.0f;                    // целевой радиус
    public float minRange = 4.5f;                          // если ближе Ч отходим
    public float maxRange = 8.0f;                          // если дальше Ч подходим

    [Header("Orbit logic")]
    public bool useOrbitSlots = true;                      // пытатьс€ занимать слот вокруг игрока
    public float slotReclaimInterval = 0.8f;

    [Header("Shooting")]
    public GameObject projectilePrefab;                    // префаб с компонентом Projectile
    public float shootCooldown = 1.2f;                     // задержка между выстрелами
    public float shootWindup = 0.25f;                      // телеграф перед выстрелом (можно 0)
    public float maxShootRange = 10f;                      // дальше Ч не стрел€ем
    public Transform muzzle;                               // точка вылета (если null Ч берЄм transform)

    [Header("Damage filter")]
    public LayerMask playerMask;                           // слой игрока (или оставить 0 и использовать Tag)

    [Header("Visual (rotate this)")]
    [SerializeField] private Transform visualRoot;         // контейнер: спрайт+тень
    public EnemySpriteAnimator anim;                       // опционально (если есть)

    [Header("Debug")]
    public bool drawGizmos = true;

    // runtime
    Transform player;
    EnemyPathMover mover;
    Rigidbody2D rb;

    int mySlotIndex = -1;
    int myId;
    Transform orbitTarget;
    float nextRepathAt;
    float nextSlotReclaimAt;
    bool hasTarget;
    bool onCooldown;

    void Start()
    {
        mover = GetComponent<EnemyPathMover>();
        rb = GetComponent<Rigidbody2D>();
        mover.maxSpeed = moveSpeed;

        var p = GameObject.FindWithTag("Player");
        if (!p) { Debug.LogError("Player not found (tag 'Player')."); enabled = false; return; }
        player = p.transform;

        if (useOrbitSlots && PlayerOrbitTargets.Instance != null)
        {
            myId = GetInstanceID();
            mySlotIndex = PlayerOrbitTargets.Instance.ClaimSlot(myId, true);
            orbitTarget = PlayerOrbitTargets.Instance.GetSlotTransform(mySlotIndex);
        }

        if (!anim) anim = GetComponentInChildren<EnemySpriteAnimator>();
        if (anim) anim.PlayWalkLoop();

        if (!visualRoot)
        {
            // ѕопробуем найти контейнер вокруг спрайта
            if (anim) visualRoot = anim.transform.parent ? anim.transform.parent : anim.transform;
            else
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr) visualRoot = sr.transform.parent ? sr.transform.parent : sr.transform;
            }
        }

        if (!muzzle) muzzle = transform;
    }

    void OnDestroy()
    {
        if (useOrbitSlots && PlayerOrbitTargets.Instance != null && mySlotIndex >= 0)
            PlayerOrbitTargets.Instance.ReleaseSlot(mySlotIndex, myId);
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.position);
        hasTarget = (dist <= viewRadius);

        FaceByPlayerX();

        if (!hasTarget)
        {
            // ћожно добавить шатание/патруль, но пока стоим
            mover.enabled = false;
            return;
        }

        // --- ƒ¬»∆≈Ќ»≈ (кайт/подход/обход) ---
        if (Time.time >= nextRepathAt)
        {
            nextRepathAt = Time.time + repathInterval;

            Vector3 destination = ComputeDestination(dist);
            mover.enabled = true;
            mover.SetDestination(destination);

            // периодически переприв€зываем слот (на случай ротации слотов)
            if (useOrbitSlots && Time.time >= nextSlotReclaimAt && PlayerOrbitTargets.Instance != null)
            {
                nextSlotReclaimAt = Time.time + slotReclaimInterval;
                if (mySlotIndex < 0)
                {
                    mySlotIndex = PlayerOrbitTargets.Instance.ClaimSlot(myId, true);
                    orbitTarget = PlayerOrbitTargets.Instance.GetSlotTransform(mySlotIndex);
                }
            }
        }

        // --- ј“ј ј (LOS + дистанци€ +  ƒ) ---
        if (!onCooldown && dist <= maxShootRange && HasLOS())
        {
            StartCoroutine(ShootRoutine());
        }
    }

    Vector3 ComputeDestination(float distToPlayer)
    {
        // 1) если слишком близко Ч отходим на дуге (от игрока)
        if (distToPlayer < minRange)
        {
            Vector2 dirAway = ((Vector2)transform.position - (Vector2)player.position).normalized;
            Vector2 tgt = (Vector2)player.position + dirAway * preferredRange;
            return tgt;
        }

        // 2) если слишком далеко Ч подходим: слот, если валиден, иначе пр€мо к игроку
        if (distToPlayer > maxRange)
        {
            if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
                return orbitTarget.position;
            return player.position;
        }

        // 3) если в допустимом коридоре Ч держим нынешний слот / лЄгкий орбитинг
        if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
            return orbitTarget.position;

        // fallback Ч точка вокруг целевого радиуса
        Vector2 right = Vector2.Perpendicular(((Vector2)player.position - (Vector2)transform.position).normalized);
        Vector2 circlePoint = (Vector2)player.position + right * preferredRange;
        return circlePoint;
    }

    bool HasLOS()
    {
        if (losObstacleMask.value == 0) return true;
        return !Physics2D.Linecast(transform.position, player.position, losObstacleMask);
    }

    bool IsSlotVisibleFromPlayer()
    {
        if (orbitTarget == null) return false;
        if (losObstacleMask.value == 0) return true;
        // видимость слота именно от игрока (чтоб слот не был за стеной от игрока)
        return !Physics2D.Linecast(player.position, orbitTarget.position, losObstacleMask);
    }

    IEnumerator ShootRoutine()
    {
        onCooldown = true;

        // лЄгкий телеграф/задержка перед выстрелом
        if (shootWindup > 0f)
            yield return new WaitForSeconds(shootWindup);

        // выстрел
        if (projectilePrefab)
        {
            Vector3 origin = muzzle ? muzzle.position : transform.position;
            Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var pr = go.GetComponent<Projectile>();
            if (pr)
            {
                // настроим фильтры/параметры снаружи
                pr.Initialize(dir, playerMask, losObstacleMask);
            }
        }

        //  ƒ
        yield return new WaitForSeconds(shootCooldown);
        onCooldown = false;
    }

    void FaceByPlayerX()
    {
        float sign = (player.position.x >= transform.position.x) ? 1f : -1f;
        if (visualRoot)
            visualRoot.localRotation = Quaternion.Euler(0f, (sign < 0f ? 180f : 0f), 0f);
        // если EnemySpriteAnimator.FaceDir(sign) у теб€ тоже флипает спрайт Ч не вызывай его одновременно
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, preferredRange);

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxShootRange);
    }
}
