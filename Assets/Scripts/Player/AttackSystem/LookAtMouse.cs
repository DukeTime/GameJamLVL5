using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    [SerializeField] private bool flipSpriteBasedOnDirection = true; // Опция для отражения спрайта
    [SerializeField] private int offset = 0;
    
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // Получаем SpriteRenderer, если нужно отражать спрайт
        if (flipSpriteBasedOnDirection)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        // Получаем позицию курсора в мировых координатах
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; // Обнуляем Z-координату для 2D
        
        // Направление от объекта к курсору
        Vector3 direction = mousePosition - transform.position;
        
        // Поворачиваем объект в направлении курсора
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + offset;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Если нужно, отражаем спрайт по горизонтали при повороте влево
        if (flipSpriteBasedOnDirection && spriteRenderer != null)
        {
            spriteRenderer.flipY = direction.x < 0;
        }
    }
}