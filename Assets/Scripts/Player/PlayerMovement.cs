using Map.Player;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    [SerializeField] private PlayerConfig config;
    
    private float WalkSpeed => config.walkSpeed;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    public void Move(bool isRunning)
    {
        float speed = isRunning ? WalkSpeed * 2 : WalkSpeed;
        
        _rb.linearVelocity = _input.MovementInput * speed;
    }
    
    public void Push(Vector2 direction, float power = 10f)
    {
        _rb.AddForce(direction * power, ForceMode2D.Impulse);
    }

    public void Stop() => _rb.linearVelocity = Vector2.zero;
}