using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Настройки тряски
    [SerializeField] private float shakePower = 0.1f; // Сила тряски
    [SerializeField] private float shakeDuration = 0.5f; // Длительность тряски
    [SerializeField] private float dampingSpeed = 1.0f; // Скорость затухания

    private Vector3 initialPosition; // Начальная позиция камеры
    private float currentShakeDuration; // Текущая длительность тряски
    private bool isShaking = false; // Флаг тряски

    void Awake()
    {
        // Запоминаем начальную позицию камеры
        if (Camera.main != null)
        {
            initialPosition = Camera.main.transform.localPosition;
        }
        else
        {
            initialPosition = transform.localPosition;
        }
    }

    void Update()
    {
        if (isShaking)
        {
            if (currentShakeDuration > 0)
            {
                // Случайное смещение камеры
                transform.localPosition = initialPosition + Random.insideUnitSphere * shakePower;
                
                // Уменьшаем длительность тряски
                currentShakeDuration -= Time.deltaTime * dampingSpeed;
            }
            else
            {
                // Завершаем тряску и возвращаем камеру на место
                isShaking = false;
                currentShakeDuration = 0f;
                transform.localPosition = initialPosition;
            }
        }
    }

    // Метод для запуска тряски
    public void Shake()
    {
        // Используем настройки по умолчанию
        initialPosition = transform.position;
        Shake(shakePower, shakeDuration);
    }

    // Метод для запуска тряски с кастомными параметрами
    public void Shake(float power, float duration)
    {
        initialPosition = transform.position;
        isShaking = true;
        currentShakeDuration = duration;
        shakePower = power;
    }

    // Метод для остановки тряски
    public void StopShake()
    {
        isShaking = false;
        currentShakeDuration = 0f;
        transform.localPosition = initialPosition;
    }
}