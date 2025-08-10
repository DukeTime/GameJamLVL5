using System;
using UnityEngine;

public class PlayerAttackVisualizer : MonoBehaviour
{
    public static PlayerAttackVisualizer Instance { get; private set; }
    
    // [SerializeField] private GameObject _slashEffectPrefab;
    [SerializeField] private GameObject[] _slashEffectPrefabs;
    [SerializeField] private float pushPower = 10;
    private PlayerMovement _movement;

    private int _slashObjNum = 0;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    public void PlaySlashEffect(Vector2 position, Vector2 direction)//, float duration, float arc, float distance)
    {
        CameraShake cameraShake = Camera.main.GetComponent<CameraShake>();
        cameraShake.Shake();
        
        GameObject slashObj = _slashEffectPrefabs[_slashObjNum];
        if (_slashObjNum < _slashEffectPrefabs.Length - 1)
            _slashObjNum++;
        else
            _slashObjNum = 0;
        
        
        GameObject slash = Instantiate(slashObj, position, Quaternion.identity);
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180;
        slash.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        _movement.Push(Quaternion.AngleAxis(angle, Vector3.forward).ToEulerAngles(), pushPower);
        
        Destroy(slash, 0.3f);
        
        
        // if (_slashEffectPrefab == null) return;
        //
        // // Создаём эффект
        // GameObject slash = Instantiate(_slashEffectPrefab, position, Quaternion.identity);
        //
        // // Поворачиваем в направлении атаки
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // slash.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        //
        // // Настраиваем параметры (если нужно, например, для анимации)
        // if (slash.TryGetComponent<SlashEffect>(out var effect))
        // {
        //     effect.Initialize(duration, arc, distance);
        // }
        // else
        // {
        //     Destroy(slash, duration);
        // }
    }
}