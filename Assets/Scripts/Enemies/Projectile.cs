using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 12f;
    public float lifeTime = 3f;
    public float radius = 0.18f;           // Ђтолщинаї снар€да дл€ свипа

    [Header("Damage")]
    public int damage = 8;
    public float knockback = 5f;

    [Header("Layers")]
    public LayerMask playerMask;           // если 0 Ч используем Tag "Player"
    public LayerMask obstacleMask;         // стены/преп€тстви€

    Vector2 _dir;
    Vector2 _prev;
    bool _initialized;

    public void Initialize(Vector2 dir, LayerMask playerLayer, LayerMask obstacleLayer)
    {
        _dir = (dir.sqrMagnitude < 1e-6f) ? Vector2.right : dir.normalized;
        _prev = transform.position;
        playerMask = playerLayer;
        obstacleMask = obstacleLayer;
        _initialized = true;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!_initialized) return;

        Vector2 pos = transform.position;
        Vector2 next = pos + _dir * (speed * Time.deltaTime);

        // 1) проверка попадани€ в стену по пути
        if (obstacleMask.value != 0)
        {
            if (Physics2D.CircleCast(pos, radius, _dir, (next - pos).magnitude, obstacleMask))
            {
                Destroy(gameObject);
                return;
            }
        }

        // 2) свип попадани€ по игроку
        int mask = (playerMask.value != 0) ? playerMask.value : Physics2D.DefaultRaycastLayers;
        var hits = Physics2D.CircleCastAll(pos, radius, _dir, (next - pos).magnitude, mask);

        foreach (var hit in hits)
        {
            var col = hit.collider;

            // если нет €вной маски Ч фильтруем по тегу
            if (playerMask.value == 0 && !col.CompareTag("Player")) continue;

            // урон
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);
            else col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // нокбэк
            var prb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody2D>();
            if (prb) prb.AddForce(_dir * knockback, ForceMode2D.Impulse);

            Destroy(gameObject);
            return;
        }

        // 3) движение
        transform.position = next;
        _prev = next;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
