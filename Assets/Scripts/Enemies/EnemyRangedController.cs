using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyPathMover))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedController : MonoBehaviour
{
    [Header("Perception")]
    public float viewRadius = 14f;                         // ����� ������
    public LayerMask losObstacleMask;                      // ���� ���� ��� LOS

    [Header("Movement")]
    public float moveSpeed = 3.0f;                         // ����������� � mover.maxSpeed
    public float repathInterval = 0.2f;                    // ��� ����� ��������� ����

    [Header("Kiting (������ ���������)")]
    public float preferredRange = 6.0f;                    // ������� ������
    public float minRange = 4.5f;                          // ���� ����� � �������
    public float maxRange = 8.0f;                          // ���� ������ � ��������

    [Header("Orbit logic")]
    public bool useOrbitSlots = true;                      // �������� �������� ���� ������ ������
    public float slotReclaimInterval = 0.8f;

    [Header("Shooting")]
    public GameObject projectilePrefab;                    // ������ � ����������� Projectile
    public float shootCooldown = 1.2f;                     // �������� ����� ����������
    public float shootWindup = 0.25f;                      // �������� ����� ��������� (����� 0)
    public float maxShootRange = 10f;                      // ������ � �� ��������
    public Transform muzzle;                               // ����� ������ (���� null � ���� transform)

    [Header("Damage filter")]
    public LayerMask playerMask;                           // ���� ������ (��� �������� 0 � ������������ Tag)

    [Header("Visual (rotate this)")]
    [SerializeField] private Transform visualRoot;         // ���������: ������+����
    public EnemySpriteAnimator anim;                       // ����������� (���� ����)

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
            // ��������� ����� ��������� ������ �������
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
            // ����� �������� �������/�������, �� ���� �����
            mover.enabled = false;
            return;
        }

        // --- �������� (����/������/�����) ---
        if (Time.time >= nextRepathAt)
        {
            nextRepathAt = Time.time + repathInterval;

            Vector3 destination = ComputeDestination(dist);
            mover.enabled = true;
            mover.SetDestination(destination);

            // ������������ ��������������� ���� (�� ������ ������� ������)
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

        // --- ����� (LOS + ��������� + ��) ---
        if (!onCooldown && dist <= maxShootRange && HasLOS())
        {
            StartCoroutine(ShootRoutine());
        }
    }

    Vector3 ComputeDestination(float distToPlayer)
    {
        // 1) ���� ������� ������ � ������� �� ���� (�� ������)
        if (distToPlayer < minRange)
        {
            Vector2 dirAway = ((Vector2)transform.position - (Vector2)player.position).normalized;
            Vector2 tgt = (Vector2)player.position + dirAway * preferredRange;
            return tgt;
        }

        // 2) ���� ������� ������ � ��������: ����, ���� �������, ����� ����� � ������
        if (distToPlayer > maxRange)
        {
            if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
                return orbitTarget.position;
            return player.position;
        }

        // 3) ���� � ���������� �������� � ������ �������� ���� / ����� ��������
        if (useOrbitSlots && orbitTarget != null && IsSlotVisibleFromPlayer())
            return orbitTarget.position;

        // fallback � ����� ������ �������� �������
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
        // ��������� ����� ������ �� ������ (���� ���� �� ��� �� ������ �� ������)
        return !Physics2D.Linecast(player.position, orbitTarget.position, losObstacleMask);
    }

    IEnumerator ShootRoutine()
    {
        onCooldown = true;

        // ����� ��������/�������� ����� ���������
        if (shootWindup > 0f)
            yield return new WaitForSeconds(shootWindup);

        // �������
        if (projectilePrefab)
        {
            Vector3 origin = muzzle ? muzzle.position : transform.position;
            Vector2 dir = ((Vector2)player.position - (Vector2)origin).normalized;

            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var pr = go.GetComponent<Projectile>();
            if (pr)
            {
                // �������� �������/��������� �������
                pr.Initialize(dir, playerMask, losObstacleMask);
            }
        }

        // ��
        yield return new WaitForSeconds(shootCooldown);
        onCooldown = false;
    }

    void FaceByPlayerX()
    {
        float sign = (player.position.x >= transform.position.x) ? 1f : -1f;
        if (visualRoot)
            visualRoot.localRotation = Quaternion.Euler(0f, (sign < 0f ? 180f : 0f), 0f);
        // ���� EnemySpriteAnimator.FaceDir(sign) � ���� ���� ������� ������ � �� ������� ��� ������������
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
