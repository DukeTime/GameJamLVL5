using UnityEngine.Animations;
using UnityEngine;
using System;


// PlayerStateMachine.cs
public class PlayerStateMachine
{
    public PlayerBaseState CurrentState { get; private set; }

    public void Initialize(PlayerBaseState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(PlayerBaseState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}

// PlayerBaseState.cs
public abstract class PlayerBaseState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;

    protected PlayerBaseState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
}

// PlayerIdleState.cs
public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Movement.Stop();
    }

    public override void Update()
    {
        if (player.Input.MovementInput.magnitude > 0.01f)
        {
            stateMachine.ChangeState(player.WalkState);
        }
        // else if (player.Input.InteractPressed)
        // {
        //     stateMachine.ChangeState(player.InteractState);
        // }
    }
}

// PlayerWalkState.cs
public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Update()
    {
        player.Animation.UpdateAnimationParameters();
        
        if (player.Input.MovementInput.magnitude < 0.01f)
        {
            stateMachine.ChangeState(player.IdleState);
        }
        // else if (player.Input.InteractPressed)
        // {
        //     stateMachine.ChangeState(player.InteractState);
        // }
    }

    public override void FixedUpdate()
    {
        player.Movement.Move(player.Input.IsRunning);
    }
}

// PlayerInteractState.cs
public class PlayerInteractState : PlayerBaseState
{
    public PlayerInteractState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
    }

    public override void Update()
    {
    }
}