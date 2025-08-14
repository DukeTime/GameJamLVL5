using System.Collections.Generic;
using UnityEngine;

public static class PlayerStats
{
    // Базовые характеристики
    private static float baseDamage = 10f;
    private static float baseSpeed = 5f;
    private static float baseHealth = 100;
    private static float baseAttackSpeed = 1f;

    // Текущие модификаторы
    private static Dictionary<StatType, float> statModifiers = new Dictionary<StatType, float>();
    
    // Список активных способностей
    private static List<string> abilityPrefabPaths = new List<string>();

    // Текущие характеристики с учетом модификаторов
    public static float Damage { get { return baseDamage * (1 + GetModifier(StatType.Damage)); } }
    public static float Speed { get { return baseSpeed * (1 + GetModifier(StatType.Speed)); } }
    public static float Health { get { return baseHealth * (1 + GetModifier(StatType.Health)); } }
    public static float AttackSpeed { get { return baseAttackSpeed * (1 + GetModifier(StatType.AttackSpeed)); } }
    
    public static List<string> ActiveAbilities { get { return new List<string>(abilityPrefabPaths); } }

    // Применить руну
    public static void ApplyRune(Rune rune)
    {
        if (rune.Type == RuneType.UniqueAbility && !string.IsNullOrEmpty(rune.AbilityPrefabPath))
        {
            if (!abilityPrefabPaths.Contains(rune.AbilityPrefabPath))
            {
                abilityPrefabPaths.Add(rune.AbilityPrefabPath);
            }
        }
        else
        {
            foreach (var modifier in rune.StatModifiers)
            {
                AddModifier(modifier.Key, modifier.Value);
            }
        }
    }

    // Сбросить все характеристики (при смерти/новой игре)
    public static void ResetStats()
    {
        statModifiers.Clear();
        abilityPrefabPaths.Clear();
    }

    // Добавить модификатор характеристики
    private static void AddModifier(StatType type, float value)
    {
        if (statModifiers.ContainsKey(type))
        {
            statModifiers[type] += value;
        }
        else
        {
            statModifiers.Add(type, value);
        }
    }

    // Получить модификатор характеристики
    private static float GetModifier(StatType type)
    {
        return statModifiers.ContainsKey(type) ? statModifiers[type] : 0f;
    }
}