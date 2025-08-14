using UnityEngine;


public class CameraFollowing : MonoBehaviour
{
    [SerializeField] private PlayerConfig _playerConfig;
    
    private Transform _target;
    private float _followSpeed => _playerConfig.followSpeed;
    private float _deadZoneRadius => _playerConfig.deadZoneRadius;
    private bool _useLerp => _playerConfig.useLerp;
    private Vector3 _offset;
    
    private void Start()
    {
        PlayerController playerController = ServiceLocator.Current.Get<PlayerController>();
        
        _target = playerController.gameObject.transform;
        _offset = new Vector3(0, 0, transform.position.z - _target.position.z);
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = _target.position + _offset;
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        // Если цель вне мертвой зоны
        if (distance > _deadZoneRadius)
        {
            if (_useLerp)
            {
                // Плавное приближение с Lerp
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    _followSpeed * Time.deltaTime
                );
            }
            else
            {
                // Плавное движение с SmoothDamp (более естественное ускорение/торможение)
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    _followSpeed * Time.deltaTime
                );
            }
        }
    }
}