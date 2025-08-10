using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _fadeSpeed = 5f;
    
    private float _duration;
    private float _arc;
    private float _distance;
    private float _timer;
    
    public void Initialize(float duration, float arc, float distance)
    {
        _duration = duration;
        _arc = arc;
        _distance = distance;
        _timer = 0f;
        
        DrawSlashArc();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        
        // Плавное затухание
        if (_lineRenderer != null)
        {
            Gradient gradient = _lineRenderer.colorGradient;
            float alpha = Mathf.Lerp(1f, 0f, _timer / _duration);
            
            // Обновляем градиент (можно оптимизировать)
            gradient.alphaKeys = new GradientAlphaKey[] { new(alpha, 0f), new(alpha, 1f) };
            _lineRenderer.colorGradient = gradient;
        }
        
        if (_timer >= _duration)
        {
            Destroy(gameObject);
        }
    }

    private void DrawSlashArc()
    {
        if (_lineRenderer == null) return;
        
        int segments = 10;
        _lineRenderer.positionCount = segments + 1;
        
        float startAngle = -_arc * 0.5f;
        float angleStep = _arc / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * transform.right;
            Vector2 pos = (Vector2)transform.position + dir * _distance;
            _lineRenderer.SetPosition(i, pos);
        }
    }
}