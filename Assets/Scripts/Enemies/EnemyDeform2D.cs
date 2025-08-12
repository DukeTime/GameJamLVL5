using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDeform2D : MonoBehaviour
{
    [Header("Breathing (idle)")]
    public bool breathing = true;
    [Range(0f, 0.5f)] public float breathAmount = 0.06f;
    [Range(0.05f, 3f)] public float breathFrequency = 1.1f;
    public bool breathPreserveVolume = true;

    [Header("Pulse (squash & stretch)")]
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0f, 0.5f)] public float defaultPulseAmount = 0.22f;
    [Range(0.05f, 0.6f)] public float defaultPulseDuration = 0.18f;
    [Range(0f, 0.2f)] public float apexHold = 0.02f;

    Vector3 _baseScale;
    Vector3 _pulseScale = Vector3.one;
    float _phase;
    Coroutine _pulseRoutine;

    void Awake()
    {
        _baseScale = transform.localScale;
        _phase = Random.value * Mathf.PI * 2f;
        if (pulseCurve == null || pulseCurve.length < 2)
            pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    void OnEnable() { _pulseScale = Vector3.one; }

    void Update()
    {
        // дыхание
        Vector3 breathScale = Vector3.one;
        if (breathing && breathAmount > 0f && breathFrequency > 0f)
        {
            float s = Mathf.Sin((Time.time + _phase) * Mathf.PI * 2f * breathFrequency) * breathAmount;
            float sx = breathPreserveVolume ? (1f - s) : (1f + s * 0.5f);
            float sy = breathPreserveVolume ? (1f + s) : (1f + s * 0.5f);
            breathScale = new Vector3(sx, sy, 1f);
        }

        // итоговый scale вокруг ног (pivot = GraphicsPivot)
        Vector3 final = Vector3.Scale(_baseScale, Vector3.Scale(breathScale, _pulseScale));
        final.x = Mathf.Max(0.0001f, final.x);
        final.y = Mathf.Max(0.0001f, final.y);
        transform.localScale = final;
    }

    /// Сжатие/растяжение вдоль направления удара/движения
    public void PulseAlong(Vector2 dir, float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f) return;
        dir = dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;
        bool horizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
        float ax = horizontal ? (1f - amount) : (1f + amount);
        float ay = horizontal ? (1f + amount) : (1f - amount);
        Vector3 target = new Vector3(ax, ay, 1f);

        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(CoPulse(target, duration));
    }

    public void PulseDefault(Vector2 dir) => PulseAlong(dir, defaultPulseAmount, defaultPulseDuration);

    IEnumerator CoPulse(Vector3 target, float duration)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = pulseCurve.Evaluate(Mathf.Clamp01(t / half));
            _pulseScale = Vector3.Lerp(Vector3.one, target, k);
            yield return null;
        }

        if (apexHold > 0f) { _pulseScale = target; yield return new WaitForSeconds(apexHold); }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = pulseCurve.Evaluate(Mathf.Clamp01(t / half));
            _pulseScale = Vector3.Lerp(target, Vector3.one, k);
            yield return null;
        }

        _pulseScale = Vector3.one;
        _pulseRoutine = null;
    }
}
