using System;
using UnityEngine;


public class PlayerData: MonoBehaviour
{
    public PlayerHealth playerHealth;
    
    public Action OnDie;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerHealth.OnHpIsGone += Die;
    }

    private void OnDisable()
    {
        playerHealth.OnHpIsGone -= Die;
    }

    private void Die()
    {
        GlobalGameController.Instance.CutsceneFreeze();
        GlobalGameController.Instance.sceneProgress = 1;
        
        StartCoroutine(GlobalGameController.Instance.LoadScene("Base1"));
    }
}