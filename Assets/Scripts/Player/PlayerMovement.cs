using System;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float slowdownFactor = 0.9f;
    [SerializeField] private PlayerConfig config;
    
    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    
    public float WalkSpeed = 5f;
    private bool _isPushed = false;
    private Vector2 _pushVelocity;

    private void Update()
    {
        if (_isPushed)
        {
            _pushVelocity *= slowdownFactor;
            if (_pushVelocity.magnitude < 0.1f)
            {
                _pushVelocity = Vector2.zero;
                _isPushed = false;
            }
        }
        
        _rb.linearVelocity = _pushVelocity;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    public void Move(bool isRunning)
    {
        float speed = isRunning ? WalkSpeed * 2 : WalkSpeed;
        
        _rb.linearVelocity = _input.MovementInput * speed + _pushVelocity;
    }
    
    public void Push(Vector2 direction, float power = 10f)
    {
        _rb.linearVelocity = Vector2.zero;
        _pushVelocity = direction.normalized * power;
        //_rb.AddForce(direction * power, ForceMode2D.Impulse);
        _isPushed = true;
    }

    public void Stop() => _rb.linearVelocity = Vector2.zero;
}