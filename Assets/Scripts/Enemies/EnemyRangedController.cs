using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 14f;                  // радиус обнаружени€ игрока
    public LayerMask losObstacleMask;               // слой стен дл€ LOS (видимости)

    [Header("Movement")]
    public float moveSpeed = 3.0f;                  // прокинем в mover.maxSpeed
    public float repathInterval = 0.2f;             // как часто обновл€ть цель пути

    [Header("Kiting (держим дистанцию)")]
    public float preferredRange = 6.0f;             // Ђкомфортна€ї дистанци€
    public float minRange = 4.5f;                   // если ближе Ч отходим
    public float maxRange = 8.0f;                   // если дальше Ч подходим

    [Header("Orbit logic (как у мили)")]
    public bool useOrbitSlots = true;               // занимать слот у PlayerOrbitTargets
    public float slotReclaimInterval = 0.8f;

    [Header("Shooting")]
    public GameObject projectilePrefab;             // префаб с Projectile.cs
    public Transform muzzle;                        // точка вылета
    public float shootCooldown = 1.2f;              //  ƒ между выстрелами
    public float shootWindup = 0.25f;               // телеграф перед выстрелом (ShowWindup)
    public float maxShootRange = 10f;               // дальше Ч не стрел€ем
    public bool stopToShoot = true;                 // останавливаемс€ на врем€ выстрела

    [Header("Damage filter")]
    public LayerMask playerMask;                    // слой игрока (или оставь 0 и используем Tag "Player")

    [Header("Sprites / Visual")]
    public EnemySpriteAnimator anim;                // тот же аниматор, что у мили
    [SerializeField] private Transform visualRoot;  // общий контейнер (спрайт+тень) Ч вращаем на 0/180 по Y

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
        FaceByPlayerX();

        // движение (кайт/подход/обход)
        if (Time.time >= nextRepathAt && (!onCooldown || !stopToShoot))
        {
            nextRepathAt = Time.time + repathInterval;
            Vector3 destination = ComputeDestination(dist);
            mover.enabled = true;
            mover.SetDestination(destination);

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

        // атака Ч LOS + дистанци€ +  ƒ
        if (!onCooldown && dist <= maxShootRange && HasLOS())
            StartCoroutine(ShootRoutine());
    }

    Vector3 ComputeDestination(float distToPlayer)
    {
        if (distToPlayer < minRange)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)player.position).normalized;
            return (Vector2)player.position + away * preferredRange;
        }

        if (distToPlayer > maxRange)
        {
            if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
                return orbitTarget.position;
            return player.position;
        }

        if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
            return orbitTarget.position;

        Vector2 right = Vector2.Perpendicular(((Vector2)player.position - (Vector2)transform.position).normalized);
        return (Vector2)player.position + right * preferredRange;
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
        return !Physics2D.Linecast(player.position, orbitTarget.position, losObstacleMask);
    }

    IEnumerator ShootRoutine()
    {
        onCooldown = true;

        // стопим навигацию на врем€ телеграфа/шота Ч как у милишника
        bool prevMover = mover.enabled;
        if (stopToShoot) mover.enabled = false;

        // телеграф
        if (anim) anim.ShowWindup(false);
        if (shootWindup > 0f) yield return new WaitForSeconds(shootWindup);

        // выстрел + анимаци€ атаки
        if (anim) anim.PlayAttackOnce();

        if (projectilePrefab)
        {
            Vector3 origin = muzzle ? muzzle.position : transform.position;
            Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var pr = go.GetComponent<Projectile>();
            if (pr) pr.Initialize(dir, playerMask, losObstacleMask);
        }

        // вернуть ходьбу
        if (stopToShoot) mover.enabled = prevMover;
        if (anim) anim.PlayWalkLoop();

        //  ƒ
        yield return new WaitForSeconds(shootCooldown);
        onCooldown = false;
    }

    void FaceByPlayerX()
    {
        if (!visualRoot || !player) return;
        float sign = (player.position.x >= transform.position.x) ? 1f : -1f;
        visualRoot.localRotation = Quaternion.Euler(0f, (sign < 0f ? 180f : 0f), 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f); Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.2f); Gizmos.DrawWireSphere(transform.position, preferredRange);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.2f); Gizmos.DrawWireSphere(transform.position, maxShootRange);
    }
}
