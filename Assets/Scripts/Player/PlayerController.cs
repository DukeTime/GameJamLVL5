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

    [SerializeField] private PlayerUI playerUI;

    private void Awake()
    {
        // Инициализация компонентов
        Input = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Animation = GetComponent<PlayerAnimationController>();
        Attacking = GetComponent<PlayerAttackController>();

        Input.AttackPressed += Attacking.Attack;
        Input.InteractNpc += 
        Input.NpcZoneEntered += playerUI.ShowInteractBtn;
        Input.NpcZoneExit += playerUI.HideInteractBtn;
        
        // Инициализация state machine
        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine);
        WalkState = new PlayerWalkState(this, StateMachine);
        InteractState = new PlayerInteractState(this, StateMachine);
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentState.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.FixedUpdate();
    }
    
    
}