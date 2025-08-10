using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MovementInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool InteractPressed { get; private set; }

    public Action AttackPressed;
    

    private void Start()
    {
    }

    private void Update()
    {
        MovementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
        
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        if (Input.GetMouseButtonDown(0))
            AttackPressed.Invoke();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        GameObject go = other.gameObject;
        switch (go.tag)
        {
            case "NPC":
                break;
        }
    }
}