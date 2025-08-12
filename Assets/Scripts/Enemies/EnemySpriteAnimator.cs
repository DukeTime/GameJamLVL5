using UnityEngine;

/// ���������� �������� �������� (��� Animator): ������ �����, ������� ���������, ������������ �����.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [Header("Renderer")]
    public SpriteRenderer sr;

    [Header("Frames")]
    public Sprite[] walk;        // 4 �����
    public Sprite[] attack;      // 4 �����: [0] ���������� ��� ���� ������ (��������)

    [Header("Timing")]
    public float walkFps = 6f;
    public float attackFps = 10f;

    [Header("Tint")]
    public Color dashWindupTint = new Color(1f, 0.3f, 0.3f, 1f);

    Sprite _idleBackup;          // ������ ���� walk ��� idle
    Color _baseColor;
    float _t;
    int _i;
    bool _playingWalk;
    bool _playingAttackOnce;

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        _baseColor = sr.color;
    }

    void Update()
    {
        if (_playingWalk) TickWalk();
        else if (_playingAttackOnce) TickAttackOnce();
    }

    // === API ===
    public void PlayWalkLoop()
    {
        if (walk != null && walk.Length > 0)
        {
            if (_idleBackup == null) _idleBackup = walk[0];
            _playingWalk = true; _playingAttackOnce = false; _t = 0f; _i = 0;
            sr.sprite = walk[0];
        }
    }

    public void StopWalkShowIdle()
    {
        _playingWalk = false;
        if (_idleBackup != null) sr.sprite = _idleBackup;
    }

    /// �������� ���� ������ (��������). ��� ����� ����� ����������.
    public void ShowWindup(bool tintRed)
    {
        _playingWalk = _playingAttackOnce = false;
        if (attack != null && attack.Length > 0)
            sr.sprite = attack[0];
        if (tintRed) sr.color = dashWindupTint; else sr.color = _baseColor;
    }

    /// ��������� ����� (����� attack) ���� ���.
    public void PlayAttackOnce()
    {
        sr.color = _baseColor;
        if (attack == null || attack.Length == 0) return;
        _playingAttackOnce = true; _playingWalk = false; _t = 0f; _i = 0;
        sr.sprite = attack[0];
    }

    public void FaceDir(float xSign)
    {
        if (!sr) return;
        var s = sr.transform.localScale;
        s.x = Mathf.Abs(s.x) * (xSign >= 0 ? 1 : -1);
        sr.transform.localScale = s;
    }

    // === Tickers ===
    void TickWalk()
    {
        if (walk == null || walk.Length == 0) return;
        _t += Time.deltaTime * Mathf.Max(0.01f, walkFps);
        int idx = Mathf.FloorToInt(_t) % walk.Length;
        if (idx != _i) { _i = idx; sr.sprite = walk[_i]; }
    }

    void TickAttackOnce()
    {
        if (attack == null || attack.Length == 0) { _playingAttackOnce = false; return; }
        _t += Time.deltaTime * Mathf.Max(0.01f, attackFps);
        int idx = Mathf.Clamp(Mathf.FloorToInt(_t), 0, attack.Length - 1);
        if (idx != _i) { _i = idx; sr.sprite = attack[_i]; }
        if (_t >= attack.Length) _playingAttackOnce = false;
    }
}
