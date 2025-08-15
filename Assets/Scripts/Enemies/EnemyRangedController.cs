using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 14f;                  // ������ ����������� ������
    public LayerMask losObstacleMask;               // ���� ���� ��� LOS (���������)

    [Header("Movement")]
    public float moveSpeed = 3.0f;                  // �������� � mover.maxSpeed
    public float repathInterval = 0.2f;             // ��� ����� ��������� ���� ����

    [Header("Kiting (������ ���������)")]
    public float preferredRange = 6.0f;             // ������������ ���������
    public float minRange = 4.5f;                   // ���� ����� � �������
    public float maxRange = 8.0f;                   // ���� ������ � ��������

    [Header("Orbit logic (��� � ����)")]
    public bool useOrbitSlots = true;               // �������� ���� � PlayerOrbitTargets
    public float slotReclaimInterval = 0.8f;

    [Header("Shooting")]
    public GameObject projectilePrefab;             // ������ � Projectile.cs
    public Transform muzzle;                        // ����� ������
    public float shootCooldown = 1.2f;              // �� ����� ����������
    public float shootWindup = 0.25f;               // �������� ����� ��������� (ShowWindup)
    public float maxShootRange = 10f;               // ������ � �� ��������
    public bool stopToShoot = true;                 // ��������������� �� ����� ��������

    [Header("Damage filter")]
    public LayerMask playerMask;                    // ���� ������ (��� ������ 0 � ���������� Tag "Player")

    [Header("Sprites / Visual")]
    public EnemySpriteAnimator anim;                // ��� �� ��������, ��� � ����
    [SerializeField] private Transform visualRoot;  // ����� ��������� (������+����) � ������� �� 0/180 �� Y

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
    
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyView view;
    [SerializeField] private bool spawning = true;

    void Start()
    {
        StartCoroutine(Timer(1f));
        mover = GetComponent<EnemyPathMover>();
        rb = GetComponent<Rigidbody2D>();
        mover.maxSpeed = moveSpeed;
        
        data.OnDie += () => StartCoroutine(view.Death());

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
    private IEnumerator Timer(float amount)
    {
        yield return new WaitForSeconds(amount);
        spawning = false;
    }

    void OnDestroy()
    {
        if (useOrbitSlots && PlayerOrbitTargets.Instance != null && mySlotIndex >= 0)
            PlayerOrbitTargets.Instance.ReleaseSlot(mySlotIndex, myId);
    }

    void Update()
    {
        if (spawning) return;
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.position);
        FaceByPlayerX();

        // �������� (����/������/�����)
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

        // ����� � LOS + ��������� + ��
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

        // ������ ��������� �� ����� ���������/���� � ��� � ���������
        bool prevMover = mover.enabled;
        if (stopToShoot) mover.enabled = false;

        // ��������
        if (anim) anim.ShowWindup(false);
        if (shootWindup > 0f) yield return new WaitForSeconds(shootWindup);

        // ������� + �������� �����
        if (anim) anim.PlayAttackOnce();

        if (projectilePrefab)
        {
            Vector3 origin = muzzle ? muzzle.position : transform.position;
            Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var pr = go.GetComponent<Projectile>();
            if (pr) pr.Initialize(dir, playerMask, losObstacleMask);
        }

        // ������� ������
        if (stopToShoot) mover.enabled = prevMover;
        if (anim) anim.PlayWalkLoop();

        // ��
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
