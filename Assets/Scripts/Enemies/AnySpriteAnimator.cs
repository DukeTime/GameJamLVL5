using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AnySpriteAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer sr;

    [Header("Walk/Idle")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float walkFps = 8f;

    [Header("Attack")]
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private float attackFps = 10f;

    [Header("Windup (optional)")]
    [SerializeField] private Sprite windupSprite;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;

    private Sprite[] _currentSet;
    private float _fps;
    private int _frame;
    private float _accum;
    private bool _loop = true;
    private bool _playing = true;
    private Coroutine _attackCo;

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        PlayWalkLoop(); // стартуем походку по умолчанию
    }

    void Update()
    {
        if (!_playing || _currentSet == null || _currentSet.Length == 0 || _fps <= 0f)
            return;

        _accum += Time.deltaTime * _fps;

        // аккуратно продвигаем на нужное число кадров (без пропусков)
        while (_accum >= 1f)
        {
            _accum -= 1f;
            _frame++;

            if (_frame >= _currentSet.Length)
            {
                if (_loop) _frame = 0;
                else { _frame = _currentSet.Length - 1; _playing = false; break; }
            }
        }

        // выставляем спрайт
        if (_frame >= 0 && _frame < _currentSet.Length)
            sr.sprite = _currentSet[_frame];
        else if (fallbackSprite)
            sr.sprite = fallbackSprite;
    }

    // === ПУБЛИЧНЫЕ API (как у твоего EnemySpriteAnimator) ===

    public void PlayWalkLoop()
    {
        StartSet(walkFrames, walkFps, loop: true);
    }

    /// <summary>Показать позу замаха/телеграфа (если нужен другой спрайт — задай windupSprite).</summary>
    public void ShowWindup(bool isDash)
    {
        _playing = false;
        _loop = false;
        _currentSet = null;
        _accum = 0f;
        _frame = 0;

        if (windupSprite) sr.sprite = windupSprite;
        // если нет windupSprite, просто замрём на текущем кадре
    }

    /// <summary>Проиграть атаку один раз и вернуться к ходьбе.</summary>
    public void PlayAttackOnce()
    {
        if (_attackCo != null) StopCoroutine(_attackCo);
        _attackCo = StartCoroutine(CoAttackOnce());
    }

    IEnumerator CoAttackOnce()
    {
        StartSet(attackFrames, attackFps, loop: false);
        // дождёмся конца атаки
        while (_playing) yield return null;
        PlayWalkLoop();
        _attackCo = null;
    }

    // === Вспомогательное ===
    void StartSet(Sprite[] set, float fps, bool loop)
    {
        _currentSet = set;
        _fps = Mathf.Max(0.01f, fps);
        _loop = loop;
        _frame = 0;
        _accum = 0f;
        _playing = true;

        if (_currentSet == null || _currentSet.Length == 0)
        {
            _playing = false;
            if (fallbackSprite) sr.sprite = fallbackSprite;
        }
        else
        {
            sr.sprite = _currentSet[0];
        }
    }

    // Удобные сеттеры из инспектора/кода
    public void SetWalk(Sprite[] frames, float fps)
    {
        walkFrames = frames; walkFps = fps;
        // если сейчас играется walk — перезапустим, чтобы обновить длину
        if (_currentSet == walkFrames) PlayWalkLoop();
    }

    public void SetAttack(Sprite[] frames, float fps)
    {
        attackFrames = frames; attackFps = fps;
    }
}
