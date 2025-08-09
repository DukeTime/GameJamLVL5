using UnityEngine;


[CreateAssetMenu(menuName = "CONFIG/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("Перемещение")]
    public float walkSpeed = 5f;
    
    [Header("Камера")]
    public float followSpeed = 2f;
    [field: SerializeField, Range(0, 3)] public float deadZoneRadius = 1.5f;
    public bool useLerp = true;
}