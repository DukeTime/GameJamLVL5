using System.Collections.Generic;
using UnityEngine;

public static class RuneManager
{
    // Все возможные руны
    private static List<Rune> allRunes = new List<Rune>
    {
        // Простые бонусы
        new Rune("Руна силы", "+20% урона", RuneType.StatBonus, new Dictionary<StatType, float>{{ StatType.Damage, 0.2f }}),
        new Rune("Руна скорости", "+15% скорости", RuneType.StatBonus, new Dictionary<StatType, float>{{ StatType.Speed, 0.15f }}),
        new Rune("Руна здоровья", "+25% здоровья", RuneType.StatBonus, new Dictionary<StatType, float>{{ StatType.Health, 0.25f }}),
        new Rune("Руна атаки", "+10% скорости атаки", RuneType.StatBonus, new Dictionary<StatType, float>{{ StatType.AttackSpeed, 0.1f }}),
        
        // Смешанные руны (бонус + штраф)
        new Rune("Руна ярости", "+30% урона, -15% здоровья", RuneType.Mixed, 
            new Dictionary<StatType, float>{{ StatType.Damage, 0.3f }, { StatType.Health, -0.15f }}),
        new Rune("Руна стремительности", "+25% скорости, -10% урона", RuneType.Mixed,
            new Dictionary<StatType, float>{{ StatType.Speed, 0.25f }, { StatType.Damage, -0.1f }}),
            
        // Уникальные способности
        new Rune("Руна комбо", "Комбо атака: 3 быстрых удара", RuneType.UniqueAbility, 
            null, "Prefabs/Abilities/ComboAttack"),
        new Rune("Руна отражения", "Ударная волна при получении урона", RuneType.UniqueAbility,
            null, "Prefabs/Abilities/ShockwaveOnHit"),
        new Rune("Руна вампиризма", "5% украденного здоровья с каждого удара", RuneType.UniqueAbility,
            null, "Prefabs/Abilities/LifeSteal")
    };

    // Руны, исключенные из текущего пула
    private static List<Rune> excludedRunes = new List<Rune>();

    // Получить список доступных рун
    public static List<Rune> GetAvailableRunes(int count)
    {
        List<Rune> available = new List<Rune>();
        
        // Если запрашиваем больше, чем есть доступных - возвращаем все
        if (count >= allRunes.Count - excludedRunes.Count)
        {
            available.AddRange(allRunes);
            available.RemoveAll(r => excludedRunes.Contains(r));
            return available;
        }

        // Выбираем случайные руны из доступных
        List<Rune> pool = new List<Rune>(allRunes);
        pool.RemoveAll(r => excludedRunes.Contains(r));

        while (available.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            available.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return available;
    }

    // Исключить руну из пула
    public static void ExcludeRune(Rune rune)
    {
        if (!excludedRunes.Contains(rune))
        {
            excludedRunes.Add(rune);
        }
    }

    // Сбросить все исключенные руны (при новой игре)
    public static void ResetExcludedRunes()
    {
        excludedRunes.Clear();
    }
}

public enum RuneType { StatBonus, Mixed, UniqueAbility }
public enum StatType { Damage, Speed, Health, AttackSpeed }

public class Rune
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RuneType Type { get; private set; }
    public Dictionary<StatType, float> StatModifiers { get; private set; }
    public string AbilityPrefabPath { get; private set; }

    public Rune(string name, string description, RuneType type, 
               Dictionary<StatType, float> statModifiers = null, 
               string abilityPrefabPath = "")
    {
        Name = name;
        Description = description;
        Type = type;
        StatModifiers = statModifiers ?? new Dictionary<StatType, float>();
        AbilityPrefabPath = abilityPrefabPath;
    }
}