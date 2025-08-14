using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Рендерит 2D-луч из 5 видов секций (Start, MidA, MidB, MidC, End),
/// автоматом тянет от точки A (враг) до точки B (цель),
/// плавно появляется и исчезает.
/// </summary>
public class BeamRenderer2D : MonoBehaviour
{
    [Header("Sprites (assign in Inspector)")]
    public Sprite startSprite;
    public Sprite midASprite;
    public Sprite midBSprite;
    public Sprite midCSprite;
    public Sprite endSprite;

    [Header("References")]
    public Transform muzzle;     // откуда выходит луч (если null, берём transform)
    public Transform segmentsRoot; // куда складывать созданные секции (если null, создадим)

    [Header("Visual")]
    public float pixelsPerUnit = 100f; // только если понадобятся точные размеры
    public float sortingOrder = 20;     // порядок отрисовки
    public string sortingLayer = "Effects";
    public float thickness = 1.0f;     // масштаб по Y для всех секций

    [Header("Timing")]
    public float fadeIn = 0.08f;
    public float stayTime = 0.10f;    // сколько держать луч "на полную"
    public float fadeOut = 0.10f;

    [Header("Debug")]
    public bool drawGizmos = false;

    // Пул спрайтов для повторного использования
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    float currentAlpha = 0f;
    Coroutine playRoutine;

    void Reset()
    {
        sortingLayer = "Effects";
        sortingOrder = 20;
        thickness = 1f;
        fadeIn = 0.08f; stayTime = 0.10f; fadeOut = 0.10f;
    }

    void Awake()
    {
        if (!muzzle) muzzle = transform;
        if (!segmentsRoot)
        {
            var go = new GameObject("Segments");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            segmentsRoot = go.transform;
        }
    }

    /// <summary>
    /// Проиграть луч от from к to, с автоматической сборкой секций и фейдом.
    /// </summary>
    public void Play(Vector3 fromWorld, Vector3 toWorld)
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(Co_Play(fromWorld, toWorld));
    }

    /// <summary>
    /// Удобный вызов: тянуть луч от muzzle к целевому трансформу.
    /// </summary>
    public void PlayTo(Transform target)
    {
        if (!target) return;
        Play(muzzle ? muzzle.position : transform.position, target.position);
    }

    IEnumerator Co_Play(Vector3 from, Vector3 to)
    {
        // 1) Сборка и выкладка секций
        LayoutBeam(from, to);

        // 2) Фейд-ин
        yield return FadeTo(1f, fadeIn);

        // 3) Подержать
        if (stayTime > 0f) yield return new WaitForSeconds(stayTime);

        // 4) Фейд-аут
        yield return FadeTo(0f, fadeOut);

        // 5) Выключить рендереры (не уничтожаем — пул)
        SetRenderersEnabled(false);
        playRoutine = null;
    }

    void LayoutBeam(Vector3 from, Vector3 to)
    {
        // Включаем рендереры (на случай, если были выключены)
        SetRenderersEnabled(true);

        Vector3 dir = (to - from);
        float length = dir.magnitude;
        if (length < 0.0001f) { ClearAll(); return; }
        dir /= length;

        // Поворот корня по направлению
        transform.position = from;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);

        // Местная выкладка по оси +X (мы уже повернули объект)
        float cursorX = 0f;

        // 1) START
        float wStart = GetSpriteWidthUnits(startSprite);
        var srStart = GetSR(0);
        SetupSR(srStart, startSprite, cursorX, wStart);
        cursorX += wStart;

        // 2) MID (повторяем A->B->C… пока не дойдём до места для END)
        float wEnd = GetSpriteWidthUnits(endSprite);
        float remaining = Mathf.Max(0f, length - wStart - wEnd);

        int poolIndex = 1; // 0 уже занят стартом
        int midIndex = 0;  // 0:A, 1:B, 2:C
        Sprite[] mids = new[] { midASprite, midBSprite, midCSprite };
        float[] midW = new float[3] {
            GetSpriteWidthUnits(midASprite),
            GetSpriteWidthUnits(midBSprite),
            GetSpriteWidthUnits(midCSprite)
        };

        while (remaining > 0.0001f)
        {
            Sprite pick = mids[midIndex];
            float w = Mathf.Max(0.0001f, midW[midIndex]);

            // Если следующий целиком не влезает — подрежем масштабом по X
            float slice = Mathf.Min(w, remaining);
            var sr = GetSR(poolIndex++);
            SetupSR(sr, pick, cursorX, w);

            // Подрезка: масштаб по X = (сколько влезло) / (натуральная ширина)
            float scaleX = slice / w;
            sr.transform.localScale = new Vector3(scaleX, thickness, 1f);

            cursorX += slice;
            remaining -= slice;

            // циклим A->B->C
            midIndex = (midIndex + 1) % 3;

            // если совсем мало осталось (меньше 0.5 пикселя) — выходим
            if (remaining <= 0.0001f) break;
        }

        // 3) END
        var srEnd = GetSR(poolIndex++);
        SetupSR(srEnd, endSprite, cursorX, wEnd);

        // Спрятать лишние из пула (если в прошлый раз их было больше)
        for (int i = poolIndex; i < pool.Count; i++)
            pool[i].enabled = false;
    }

    // === helpers ===

    SpriteRenderer GetSR(int index)
    {
        while (pool.Count <= index)
        {
            var go = new GameObject("seg_" + pool.Count);
            go.transform.SetParent(segmentsRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = (int)sortingOrder;
            pool.Add(sr);
        }
        var r = pool[index];
        r.enabled = true;
        return r;
    }

    void SetupSR(SpriteRenderer sr, Sprite sprite, float xStart, float widthUnits)
    {
        sr.sprite = sprite;
        sr.transform.localPosition = new Vector3(xStart + widthUnits * 0.5f, 0f, 0f); // по центру секции
        sr.transform.localRotation = Quaternion.identity;
        sr.transform.localScale = new Vector3(1f, thickness, 1f);
        var c = sr.color; c.a = currentAlpha; sr.color = c;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = (int)sortingOrder;
    }

    float GetSpriteWidthUnits(Sprite s)
    {
        if (!s) return 0.1f;
        return s.bounds.size.x; // ширина в юнитах (учитывает PPU)
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = currentAlpha;
        if (duration <= 0f)
        {
            currentAlpha = target;
            ApplyAlpha();
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            currentAlpha = Mathf.Lerp(start, target, t / duration);
            ApplyAlpha();
            yield return null;
        }
        currentAlpha = target;
        ApplyAlpha();
    }

    void ApplyAlpha()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].enabled) continue;
            var c = pool[i].color;
            c.a = currentAlpha;
            pool[i].color = c;
        }
    }

    void SetRenderersEnabled(bool on)
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].enabled = on;
    }

    void ClearAll()
    {
        for (int i = 0; i < pool.Count; i++) pool[i].enabled = false;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(1, 0.5f, 0f, 0.8f);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 5f);
    }
}
    