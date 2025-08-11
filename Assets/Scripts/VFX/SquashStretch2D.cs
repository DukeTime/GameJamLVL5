using System.Collections;
using UnityEngine;

public class SquashStretch2D : MonoBehaviour
{
    public float amount = 0.2f;      // 0.2 = 20% сплющивания
    public float duration = 0.12f;   // время на фазу (туда/обратно)

    Vector3 baseScale;
    void Awake() { baseScale = transform.localScale; }

    public void Pulse(Vector2 dir)
    {
        StopAllCoroutines();
        StartCoroutine(CoPulse(dir));
    }

    IEnumerator CoPulse(Vector2 dir)
    {
        dir = dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;
        Vector3 squash = new(dir.x != 0 ? 1f - amount : 1f + amount,
                             dir.y != 0 ? 1f - amount : 1f + amount,
                             1f);

        float t = 0f;
        while (t < duration) { t += Time.deltaTime; transform.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, squash), t / duration); yield return null; }
        t = 0f;
        while (t < duration) { t += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.Scale(baseScale, squash), baseScale, t / duration); yield return null; }
        transform.localScale = baseScale;
    }
}
