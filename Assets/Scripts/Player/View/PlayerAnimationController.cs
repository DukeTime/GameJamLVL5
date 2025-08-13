using UnityEngine;


public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private MouseAimController mouseAimController;
    [SerializeField] private PlayerAttackController playerAttackController;
    private PlayerInputHandler _input;

    // Хэши для оптимизации
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    public Transform playerTransform;
        
    // Расстояние для определения "значительного" смещения
    public float significantDistance = 1f;

    // Переменная для хранения результата сравнения
    [SerializeField] private float comparisonResult;
    
    private void Awake()
    {
        //_animator = GetComponent<Animator>();
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Start()
    {
        playerAttackController.OnAttack += (() => StartCoroutine(mouseAimController.AttackAnim()));
    }

    public void UpdateAnimationParameters()
    {
        bool isMoving = _input.MovementInput.magnitude > 0.01f;
        
        _animator.SetBool(IsMoving, isMoving);
        _animator.SetBool(IsRunning, _input.IsRunning);
        
        //if (isMoving)
        //{
        _animator.SetFloat(MoveX, comparisonResult);//_input.MovementInput.x);
        _animator.SetFloat(MoveY, _input.MovementInput.y);
        //}
    }
    

    void Update()
    {
        UpdateAnimationParameters();
        // Получаем позицию мыши в мировых координатах
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; // Обнуляем Z, так как мы работаем в 2D

        // Вычисляем разницу по X между мышью и игроком
        float xDifference = mousePosition.x - playerTransform.position.x;

        // Определяем значение comparisonResult в соответствии с условиями
        if (xDifference >= significantDistance)
        {
            comparisonResult = 1f;
        }
        else if (xDifference < -significantDistance)
        {
            comparisonResult = -1f;
        }
        else
        {
            // Нормализуем разницу в диапазоне [-1, 1] для незначительных смещений
            comparisonResult = Mathf.Clamp(xDifference / significantDistance, -1f, 1f);
        }
    }
}