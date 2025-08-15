using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 12f;
    public float lifeTime = 3f;
    public float radius = 0.18f;          // ������� �����

    [Header("Damage")]
    public int damage = 8;
    public float knockback = 5f;

    [Header("Layers")]
    public LayerMask playerMask;          // ���� 0 � ��������� �� Tag "Player"
    public LayerMask obstacleMask;        // �����

    Vector2 _dir;
    bool _armed;

    public void Initialize(Vector2 dir, LayerMask playerLayer, LayerMask obstacleLayer)
    {
        _dir = (dir.sqrMagnitude < 1e-6f) ? Vector2.right : dir.normalized;
        playerMask = playerLayer;
        obstacleMask = obstacleLayer;
        _armed = true;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!_armed) return;

        Vector2 pos = transform.position;
        Vector2 step = _dir * (speed * Time.deltaTime);
        float dist = step.magnitude;

        // ���� �� �����
        if (obstacleMask.value != 0 && Physics2D.CircleCast(pos, radius, _dir, dist, obstacleMask))
        {
            Destroy(gameObject);
            return;
        }

        // ��������� � ������ (����)
        int mask = (playerMask.value != 0) ? playerMask.value : Physics2D.DefaultRaycastLayers;
        var hits = Physics2D.CircleCastAll(pos, radius, _dir, dist, mask);

        foreach (var h in hits)
        {
            var col = h.collider;
            if (playerMask.value == 0 && !col.CompareTag("Player")) continue;

            var health = col.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log(11);
                Destroy(gameObject);
                health.TakeDamage(damage);
            }
            else col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            var prb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody2D>();
            if (prb) prb.AddForce(_dir * knockback, ForceMode2D.Impulse);

            Destroy(gameObject);
            return;
        }

        transform.position = pos + step;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
