using UnityEngine;


[CreateAssetMenu(menuName = "CONFIG/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("Перемещение")]
    public float walkSpeed = 5f;
    
    [Header("Боевая система")]
    public float Damage = 10f;
    public float Cooldown = 0.5f;
    public float SwingDuration = 0.2f; // Длительность анимации маха
    public float SwingArc = 90f; // Угол размаха в градусах
    public float SwingDistance = 1.5f; // Дистанция от игрока до точки удара
    public GameObject SlashEffectPrefab; // Префаб эффекта удара (белый след)
    
    [Header("Камера")]
    public float followSpeed = 2f;
    [field: SerializeField, Range(0, 3)] public float deadZoneRadius = 1.5f;
    public bool useLerp = true;
}