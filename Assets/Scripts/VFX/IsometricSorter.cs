using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class IsometricSorter : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Transform _playerTransform;
    
    [SerializeField] private float _sortingOffset = 0f;
    [SerializeField] private bool _isStatic = false;

    private void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _playerTransform = gameObject.GetComponentInParent<Transform>();
        
        UpdateSortingOrder();
    }

    private void Update()
    {
        if (!_isStatic)
            UpdateSortingOrder();
    }

    private void UpdateSortingOrder()
    {
        // Основная формула для изометрии (Y-координата влияет на порядок)
        _renderer.sortingOrder = Mathf.RoundToInt(
            _sortingOffset - _playerTransform.position.y * 100
        );
    }
}