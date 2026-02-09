using System;
using System.Collections.Generic;

namespace HexCrawlGame;

public sealed class PartyTemplate
{
    public string Name { get; set; } = string.Empty;
    public int BaseMaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Range { get; set; }
    public int MoveRange { get; set; }
    public bool HasDiagonalMove { get; set; }
    public AbilityType Ability { get; set; }
    public int AbilityRange { get; set; }
    public int AbilityPower { get; set; }
    public int AbilityCooldown { get; set; }
    public int InitiativeBonus { get; set; }
}

public sealed class EnemyTemplate
{
    public string Name { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Range { get; set; }
    public int MoveRange { get; set; }
    public int InitiativeBonus { get; set; }
}

public sealed class PartyFile
{
    public List<PartyTemplate> Party { get; set; } = new();
}

public sealed class EnemiesFile
{
    public Dictionary<string, List<EnemyTemplate>> Biomes { get; set; } = new();
}

public sealed class EventsFile
{
    public List<EventDefinition> Common { get; set; } = new();
    public Dictionary<string, List<EventDefinition>> Biomes { get; set; } = new();
}

public sealed class ContentCatalog
{
    public List<PartyTemplate> Party { get; } = new();
    public Dictionary<BiomeType, List<EnemyTemplate>> EnemiesByBiome { get; } = new();
    public List<EventDefinition> CommonEvents { get; } = new();
    public Dictionary<BiomeType, List<EventDefinition>> EventsByBiome { get; } = new();

    public IReadOnlyList<EnemyTemplate> GetEnemies(BiomeType biome)
    {
        if (EnemiesByBiome.TryGetValue(biome, out var list))
        {
            return list;
        }

        return Array.Empty<EnemyTemplate>();
    }

    public List<EventDefinition> GetEvents(BiomeType biome)
    {
        var results = new List<EventDefinition>();
        results.AddRange(CommonEvents);

        if (EventsByBiome.TryGetValue(biome, out var list))
        {
            results.AddRange(list);
        }

        return results;
    }
}
