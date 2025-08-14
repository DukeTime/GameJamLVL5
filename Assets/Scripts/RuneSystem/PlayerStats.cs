using System;
using System.Collections.Generic;
using System.Linq;
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

    // 1. Функция преобразования строки в массив
    static string[] ParseStringArray(string input)
    {
        // Удаляем квадратные скобки и пробелы
        string cleaned = input.Trim(new char[] {'[', ']', ' '})
            .Replace(" ", "");
        
        // Разделяем элементы по запятым, удаляя кавычки
        return cleaned.Split(new[] { '\'', ',' }, StringSplitOptions.RemoveEmptyEntries);
    }
    
    // 2. Функция обратного преобразования (массив в строку)
    static string ToArrayString(string[] array)
    {
        return "[" + string.Join(", ", array.Select(x => $"'{x}'")) + "]";
    }
    
    // 3. Функция добавления элемента в массив
    static string[] AddToArray(string[] sourceArray, string newElement)
    {
        string[] newArray = new string[sourceArray.Length + 1];
        Array.Copy(sourceArray, newArray, sourceArray.Length);
        newArray[newArray.Length - 1] = newElement;
        return newArray;
    }
    
    // Применить руну
    public static void ApplyRune(Rune rune)
    {
        string oldRunes = PlayerPrefs.GetString("Runes");
        string[] newRunes = AddToArray(ParseStringArray(oldRunes), rune.Name);
        Debug.Log(rune.Name + ToArrayString(newRunes));
        PlayerPrefs.SetString("Runes", ToArrayString(newRunes));
        
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

    public static bool IsLearned(string runeName)
    {
        string[] newRunes = ParseStringArray(PlayerPrefs.GetString("Runes"));
        Debug.Log(runeName + newRunes.Contains(runeName));
        return newRunes.Contains(runeName);
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