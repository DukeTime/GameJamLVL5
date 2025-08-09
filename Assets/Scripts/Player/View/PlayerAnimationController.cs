using UnityEngine;


public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private PlayerInputHandler _input;

    // Хэши для оптимизации
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        //_animator = GetComponent<Animator>();
        _input = GetComponent<PlayerInputHandler>();
    }

    public void UpdateAnimationParameters()
    {
        bool isMoving = _input.MovementInput.magnitude > 0.01f;
        
        _animator.SetBool(IsMoving, isMoving);
        _animator.SetBool(IsRunning, _input.IsRunning);
        
        if (isMoving)
        {
            _animator.SetFloat(MoveX, _input.MovementInput.x);
            _animator.SetFloat(MoveY, _input.MovementInput.y);
        }
    }
}