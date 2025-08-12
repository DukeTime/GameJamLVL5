using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MovementInput { get; private set; }
    public bool IsRunning { get; private set; }

    
    public Action AttackPressed;
    public Action InteractNpc;
    
    public Action NpcZoneEntered;
    public Action NpcZoneExit;


    private bool _interactEnabled = false;
    private NpcController _selectedNpc;

    private void Update()
    {
        if (!GlobalGameController.Instance.CutsceneFreezed)
        {
            MovementInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            if (Input.GetMouseButtonDown(0))
                AttackPressed.Invoke();

            if (_interactEnabled)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    InteractNpc?.Invoke();
                    NpcZoneExit?.Invoke();
                    
                    _selectedNpc.Interact();
                }
            }
        }
                
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject go = other.gameObject;
        switch (go.tag)
        {
            case "NPC":
                _selectedNpc = go.GetComponent<NpcController>();
                if (!_selectedNpc.interacted)
                {
                    _interactEnabled = true;
                    NpcZoneEntered?.Invoke();
                }
                break;
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject go = other.gameObject;
        switch (go.tag)
        {
            case "NPC":
                _interactEnabled = false;
                _selectedNpc = null;
                NpcZoneExit?.Invoke();
                break;
        }
    }
}