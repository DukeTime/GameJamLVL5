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
        Debug.Log(GlobalGameController.CutsceneFreezed);
        if (!GlobalGameController.CutsceneFreezed)
        {
            Debug.Log(1);
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
                Debug.Log(2);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log(3);
                    InteractNpc?.Invoke();
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
                _interactEnabled = true;
                _selectedNpc = go.GetComponent<NpcController>();
                NpcZoneEntered?.Invoke();
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