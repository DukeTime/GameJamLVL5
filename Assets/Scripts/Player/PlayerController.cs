using UnityEngine;


[RequireComponent(
    typeof(PlayerInputHandler), 
    typeof(PlayerMovement),
    typeof(PlayerAnimationController)
)]
public class PlayerController : MonoBehaviour, IService
{
    [Header("State System")]
    public PlayerStateMachine StateMachine { get; private set; }
    
    // Состояния
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerInteractState InteractState { get; private set; }

    // Компоненты
    [HideInInspector] public PlayerInputHandler Input { get; private set; }
    [HideInInspector] public PlayerMovement Movement { get; private set; }
    [HideInInspector] public PlayerAnimationController Animation { get; private set; }
    [HideInInspector] public PlayerAttackController Attacking  { get; private set; }

    private void Awake()
    {
        // Инициализация компонентов
        Input = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Animation = GetComponent<PlayerAnimationController>();
        Attacking = GetComponent<PlayerAttackController>();

        Input.AttackPressed += Attacking.Attack;
        
        // Инициализация state machine
        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine);
        WalkState = new PlayerWalkState(this, StateMachine);
        InteractState = new PlayerInteractState(this, StateMachine);
    }



    private void Start()
    {
        // Если машина состояний/стартовый стейт не назначены — не падаем, а логируем.
        if (StateMachine == null || IdleState == null)
        {
            Debug.LogError($"[PlayerController] StateMachine/IdleState not set. " +
                           $"Make sure you create states in Awake() and pass IdleState to Initialize().", this);
            return;
        }
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        // Безопасная обвязка – пока машина/стейт не готовы, выходим
        var st = StateMachine != null ? StateMachine.CurrentState : null;
        if (st == null) return;
        st.Update();
    }

    private void FixedUpdate()
    {
        var st = StateMachine != null ? StateMachine.CurrentState : null;
        if (st == null) return;
        st.FixedUpdate();
    }

}