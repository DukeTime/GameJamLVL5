using System.Collections;
using UnityEngine;

public class MouseAimController : MonoBehaviour
{
    [Header("Анимация атаки")]
    public bool FreezeRot = false;

    public float attackAnimDelta = 45;
    public float attackAnimSpeed = 45;
    
    
    [Header("Основные настройки")]
    public Transform playerTransform;
    public Transform swordTransform;
    public float significantDistance = 1f;
    public bool smoothAiming = true;
    [Range(0.1f, 1f)] public float smoothSpeed = 0.5f;
    [Range(-180, 180)] public float angleOffset = 90f;
    [SerializeField] private bool flipSpriteBasedOnDirection = true; // Новая опция
    [SerializeField] private bool flipByX = true;
    [SerializeField] private bool swordSwap = false;

    [Header("Ограничения поворота")]
    public bool useRotationLimits = true;
    public float minLeftAngle = 60f;
    public float maxLeftAngle = 120f;
    public float minRightAngle = 240f;
    public float maxRightAngle = 300f;

    [Header("Отладочная информация")]
    [SerializeField] private float comparisonResult;
    [SerializeField] private float currentAngle;
    [SerializeField] private float targetAngle;
    [SerializeField] private bool facingRight = true; // Текущее направление
    [SerializeField] private SpriteRenderer spriteRenderer; // Для отражения спрайта

    private void Awake()
    {
        // if (flipSpriteBasedOnDirection)
        // {
        //     spriteRenderer = GetComponent<SpriteRenderer>();
        //     if (spriteRenderer == null)
        //     {
        //         Debug.LogWarning("SpriteRenderer не найден! Отключение flipSpriteBasedOnDirection");
        //         flipSpriteBasedOnDirection = false;
        //     }
        // }
    }

    public IEnumerator AttackAnim()
    {
        if (!FreezeRot)
        {
            FreezeRot = true;
            yield return StartCoroutine(AttackAnimation(true, 0.5f));
            yield return StartCoroutine(AttackAnimation());
            FreezeRot = false;
        }
    }

    private IEnumerator AttackAnimation(bool reversed = false, float modifaer = 1f)
    {
        float delta = reversed ? -1 * modifaer * attackAnimDelta : 
            modifaer * attackAnimDelta;
        float startAngle = currentAngle;
        float progress = 0f;
        
        if (currentAngle > 0 & currentAngle < 180)
        {
            if (currentAngle > 90)
                targetAngle = currentAngle - delta;
            else
                targetAngle = currentAngle + delta;
        }
        else
        {
            if (currentAngle > 270)
                targetAngle = currentAngle - delta;
            else
                targetAngle = currentAngle + delta;
        }
        
        if (useRotationLimits)
        {
            if (!swordSwap)
            {
                if (targetAngle >= 90 & targetAngle <= 270)
                {
                    //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
                    //targetAngle = 360 - convertedAngle;
                    if (targetAngle > maxLeftAngle)
                        targetAngle = maxLeftAngle;
                    else if (targetAngle < minLeftAngle)
                        targetAngle = minLeftAngle;
                }
                else
                {
                    //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
                    if (targetAngle < 90 & targetAngle > maxRightAngle)
                        targetAngle = maxRightAngle;
                    else if (targetAngle > 90 & targetAngle < minRightAngle)
                        targetAngle = minRightAngle;
                }
            }
            else
            {
                if (targetAngle > 0 & targetAngle < 180)
                {
                    //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
                    //targetAngle = 360 - convertedAngle;
                    if (targetAngle > maxLeftAngle)
                        targetAngle = maxLeftAngle;
                    else if (targetAngle < minLeftAngle)
                        targetAngle = minLeftAngle;
                }
                else
                {
                    //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
                    if (targetAngle < 90 & targetAngle > maxRightAngle)
                        targetAngle = maxRightAngle;
                    else if (targetAngle > 90 & targetAngle < minRightAngle)
                        targetAngle = minRightAngle;
                }
            }
        }
    
        while (progress < 1f)
        {
            progress += attackAnimSpeed * Time.deltaTime;
        
            // Плавное изменение угла
            currentAngle = Mathf.LerpAngle(startAngle, targetAngle, progress);
            playerTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
        
            yield return null;
        }
    
        // Гарантируем точное завершение анимации
        currentAngle = targetAngle;
        playerTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    // private IEnumerator AttackAnimation()
    // {
    //     targetAngle = currentAngle - 45;
    //     
    //     if (swordSwap)
    //     {
    //         // if (currentAngle > 0 & currentAngle < 180)
    //         // {
    //         //     if (currentAngle > 90)
    //         //         targetAngle = currentAngle - attackAnimDelta;
    //         //     else
    //         //         targetAngle = currentAngle + attackAnimDelta;
    //         // }
    //         // else
    //         // {
    //         //     if (currentAngle > 270)
    //         //         targetAngle = currentAngle - attackAnimDelta;
    //         //     else
    //         //         targetAngle = currentAngle + attackAnimDelta;
    //         // }
    //         // if (useRotationLimits)
    //         // {
    //         //     if (!swordSwap)
    //         //     {
    //         //         if (targetAngle >= 90 & targetAngle <= 270)
    //         //         {
    //         //             //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
    //         //             //targetAngle = 360 - convertedAngle;
    //         //             if (targetAngle > maxLeftAngle)
    //         //                 targetAngle = maxLeftAngle;
    //         //             else if (targetAngle < minLeftAngle)
    //         //                 targetAngle = minLeftAngle;
    //         //         }
    //         //         else
    //         //         {
    //         //             //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
    //         //             if (targetAngle < 90 & targetAngle > maxRightAngle)
    //         //                 targetAngle = maxRightAngle;
    //         //             else if (targetAngle > 90 & targetAngle < minRightAngle)
    //         //                 targetAngle = minRightAngle;
    //         //         }
    //         //     }
    //         //     else
    //         //     {
    //         //         if (targetAngle > 0 & targetAngle < 180)
    //         //         {
    //         //             //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
    //         //             //targetAngle = 360 - convertedAngle;
    //         //             if (targetAngle > maxLeftAngle)
    //         //                 targetAngle = maxLeftAngle;
    //         //             else if (targetAngle < minLeftAngle)
    //         //                 targetAngle = minLeftAngle;
    //         //         }
    //         //         else
    //         //         {
    //         //             //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
    //         //             if (targetAngle < 90 & targetAngle > maxRightAngle)
    //         //                 targetAngle = maxRightAngle;
    //         //             else if (targetAngle > 90 & targetAngle < minRightAngle)
    //         //                 targetAngle = minRightAngle;
    //         //         }
    //         //     }
    //         // }
    //         
    //         Debug.Log(targetAngle);
    //         
    //         
    //         while (targetAngle - currentAngle > 5 | targetAngle - currentAngle < -5)
    //         {
    //             yield return null;//new WaitForSeconds(Time.deltaTime);
    //
    //             // Плавное наведение
    //             currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, attackAnimSpeed);
    //             playerTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
    //         }
    //
    //         FreezeRot = false;
    //     }
    // }
    private void Update()
    {
        if (!GlobalGameController.Instance.CutsceneFreezed & !FreezeRot)
        {
            // Получаем позицию мыши в мировых координатах
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;

            // Вычисляем направление к мыши
            Vector3 directionToMouse = Vector3.zero;
            if (swordSwap)
                directionToMouse = mousePosition - swordTransform.position;
            else
                directionToMouse = mousePosition - playerTransform.position;

            if (flipSpriteBasedOnDirection && spriteRenderer != null)
            {
                spriteRenderer.flipY = directionToMouse.x <= 0;
                spriteRenderer.flipX = directionToMouse.x <= 0;
            }

            // Вычисляем угол поворота к мыши (в градусах) с учетом offset
            targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg + angleOffset;
            targetAngle = NormalizeAngle(targetAngle);

            // Применяем ограничения поворота
            if (useRotationLimits)
            {
                if (!swordSwap)
                {
                    if (targetAngle >= 90 & targetAngle <= 270)
                    {
                        //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
                        //targetAngle = 360 - convertedAngle;
                        if (targetAngle > maxLeftAngle)
                            targetAngle = maxLeftAngle;
                        else if (targetAngle < minLeftAngle)
                            targetAngle = minLeftAngle;
                    }
                    else
                    {
                        //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
                        if (targetAngle < 90 & targetAngle > maxRightAngle)
                            targetAngle = maxRightAngle;
                        else if (targetAngle > 90 & targetAngle < minRightAngle)
                            targetAngle = minRightAngle;
                    }
                }
                else
                {
                    if (targetAngle > 0 & targetAngle < 180)
                    {
                        //float convertedAngle = Mathf.Clamp(targetAngle, minLeftAngle, maxLeftAngle);
                        //targetAngle = 360 - convertedAngle;
                        if (targetAngle > maxLeftAngle)
                            targetAngle = maxLeftAngle;
                        else if (targetAngle < minLeftAngle)
                            targetAngle = minLeftAngle;
                    }
                    else
                    {
                        //targetAngle = Mathf.Clamp(targetAngle, minRightAngle, maxRightAngle);
                        if (targetAngle < 90 & targetAngle > maxRightAngle)
                            targetAngle = maxRightAngle;
                        else if (targetAngle > 90 & targetAngle < minRightAngle)
                            targetAngle = minRightAngle;
                    }
                }
            }

            // Плавное наведение
            currentAngle = smoothAiming ? Mathf.LerpAngle(currentAngle, targetAngle, smoothSpeed) : targetAngle;

            // Обработка отражения спрайта
            // if (flipSpriteBasedOnDirection)
            // {
            //     // Определяем направление взгляда
            //     bool shouldFaceRight = (currentAngle > 360 || currentAngle < 0);
            //     
            //     // Если направление изменилось
            //     if (shouldFaceRight != facingRight)
            //     {
            //         facingRight = shouldFaceRight;
            //         if (flipByX)
            //         {
            //             Debug.Log(1);
            //             spriteRenderer.flipX = facingRight;
            //         }
            //         else
            //         {
            //             Debug.Log(2);
            //             spriteRenderer.flipY = facingRight;
            //         }
            //     }
            // }

            // Вычисляем разницу по X между мышью и игроком
            float xDifference = mousePosition.x - playerTransform.position.x;

            // Определяем значение comparisonResult
            comparisonResult = Mathf.Abs(xDifference) > significantDistance
                ? Mathf.Sign(xDifference)
                : Mathf.Clamp(xDifference / significantDistance, -1f, 1f);

            // Применяем поворот
            playerTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    private float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        return angle;
    }

    public float GetComparisonResult() => comparisonResult;
    public float GetCurrentAngle() => currentAngle;

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useRotationLimits || playerTransform == null) return;

        float radius = 2f;
        
        // Рисуем ограничения для левой стороны с учетом offset
        DrawAngleLimitGizmo(360 - maxLeftAngle + angleOffset, 360 - minLeftAngle + angleOffset, radius, Color.red);
        
        // Рисуем ограничения для правой стороны с учетом offset
        DrawAngleLimitGizmo(minRightAngle + angleOffset, maxRightAngle + angleOffset, radius, Color.green);
    }

    private void DrawAngleLimitGizmo(float minAngle, float maxAngle, float radius, Color color)
    {
        minAngle = NormalizeAngle(minAngle);
        maxAngle = NormalizeAngle(maxAngle);

        Gizmos.color = color;
        
        Vector3 minDir = Quaternion.Euler(0, 0, minAngle) * Vector3.right;
        Vector3 maxDir = Quaternion.Euler(0, 0, maxAngle) * Vector3.right;
        
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + minDir * radius);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + maxDir * radius);
        
        // Рисуем дугу между ограничениями
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireArc(playerTransform.position, Vector3.forward, minDir, 
            NormalizeAngle(maxAngle - minAngle), radius);
    }
    #endif
}