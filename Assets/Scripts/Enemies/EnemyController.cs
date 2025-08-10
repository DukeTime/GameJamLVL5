using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Detection & Movement")]
    public float viewRadius = 10f;
    public float stopDistance = 2.5f;
    public float moveSpeed = 3f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public int damage = 10;
    public GameObject attackZonePrefab; // Префаб зоны атаки
    public float attackZoneOffset = 1.25f; // смещение зоны атаки вперёд

    [Header("DEBUG")]
    public bool drawGizmos = true;

    private Transform player;
    private bool canAttack = true;
    private State state = State.Idle;
    private GameObject currentAttackZone; // Ссылка на активную зону

    private enum State { Idle, Chase, Attack }

    void Start()
    {
        var foundPlayer = GameObject.FindWithTag("Player");
        if (foundPlayer != null)
            player = foundPlayer.transform;
        else
            Debug.LogError("Player not found! Ensure your player has tag 'Player'");
    }

    void Update()
    {
        if (player == null) return;
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        FacePlayer2D();

        switch (state)
        {
            case State.Idle:
                if (distToPlayer <= viewRadius)
                    state = State.Chase;
                break;
            case State.Chase:
                if (distToPlayer > viewRadius)
                {
                    state = State.Idle;
                    RemoveAttackZone();
                }
                else if (distToPlayer > stopDistance)
                {
                    MoveTowardsPlayer();
                    RemoveAttackZone();
                }
                else
                {
                    state = State.Attack;
                }
                break;
            case State.Attack:
                if (distToPlayer > stopDistance)
                {
                    state = State.Chase;
                    RemoveAttackZone();
                }
                else
                {
                    CreateAttackZone();
                    if (canAttack)
                        StartCoroutine(Attack());
                }
                break;
        }

        // Если зона атаки есть — она всегда следует за врагом!
        UpdateAttackZonePosition();
    }

    void MoveTowardsPlayer()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)dir * moveSpeed * Time.deltaTime;
    }

    void FacePlayer2D()
    {
        if (player == null) return;
        Vector3 scale = transform.localScale;
        if (player.position.x > transform.position.x)
            scale.x = Mathf.Abs(scale.x); // вправо
        else
            scale.x = -Mathf.Abs(scale.x); // влево
        transform.localScale = scale;
    }

    void CreateAttackZone()
    {
        if (currentAttackZone == null && attackZonePrefab != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 spawnPos = (Vector2)transform.position + direction * attackZoneOffset;

            currentAttackZone = Instantiate(
                attackZonePrefab,
                new Vector3(spawnPos.x, spawnPos.y, transform.position.z),
                Quaternion.identity
            );

            // Настроим параметры зоны:
            var attackZone = currentAttackZone.GetComponent<EnemyAttackZone>();
            attackZone.damage = damage;
            attackZone.enemy = this;
            attackZone.SetEnemyReference(this);
        }
    }

    void RemoveAttackZone()
    {
        if (currentAttackZone != null)
        {
            Destroy(currentAttackZone);
            currentAttackZone = null;
        }
    }

    void UpdateAttackZonePosition()
    {
        if (currentAttackZone != null && player != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 offsetPos = (Vector2)transform.position + direction * attackZoneOffset;
            currentAttackZone.transform.position = new Vector3(offsetPos.x, offsetPos.y, transform.position.z);

            // Можно повернуть визуал зоны, если это нужно:
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            currentAttackZone.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    System.Collections.IEnumerator Attack()
    {
        canAttack = false;
        // (урон игроку — через EnemyAttackZone.cs)
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        if (player != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 spawnPos = (Vector2)transform.position + direction * attackZoneOffset;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(spawnPos.x, spawnPos.y, transform.position.z), 0.15f);
        }
    }
}
