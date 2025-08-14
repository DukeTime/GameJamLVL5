using System;
using UnityEngine;


public class PlayerData: MonoBehaviour
{
    public PlayerHealth playerHealth;
    public float speed;
    public float attackSpeed;
    public float maxHealth;
    public float dmg;
    
    public Action OnDie;
    public Action OnUpdateStats;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerHealth.OnHpIsGone += Die;
        
        UpdateStats();
    }

    private void OnDisable()
    {
        playerHealth.OnHpIsGone -= Die;
    }

    public void UpdateStats()
    {
        dmg = PlayerStats.Damage;
        maxHealth = PlayerStats.Health;
        speed = PlayerStats.Speed;
        attackSpeed = PlayerStats.AttackSpeed;
        
        
        OnUpdateStats?.Invoke();
    }

    private void Die()
    {
        GlobalGameController.Instance.CutsceneFreeze();
        GlobalGameController.Instance.sceneProgress = 0;
        
        StartCoroutine(GlobalGameController.Instance.LoadScene(0));
    }
}