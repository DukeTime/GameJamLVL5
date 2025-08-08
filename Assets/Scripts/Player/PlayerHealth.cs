using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Игрок получил {damage} урона! Осталось здоровья: {health}");
        // Тут логика смерти или эффектов
    }
}
