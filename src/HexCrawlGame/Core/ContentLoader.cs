using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HexCrawlGame;

public static class ContentLoader
{
    public static ContentCatalog Load(string baseDirectory)
    {
        var defaults = ContentDefaults.CreateCatalog();
        var catalog = new ContentCatalog();

        string dataDir = Path.Combine(baseDirectory, "Data");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var partyFile = LoadJson<PartyFile>(Path.Combine(dataDir, "party.json"), options);
        if (partyFile?.Party != null && partyFile.Party.Count > 0)
        {
            catalog.Party.AddRange(partyFile.Party);
        }
        else
        {
            catalog.Party.AddRange(defaults.Party);
        }

        var enemiesFile = LoadJson<EnemiesFile>(Path.Combine(dataDir, "enemies.json"), options);
        var enemyMap = ConvertEnemyBiomes(enemiesFile?.Biomes);
        EnsureEnemies(enemyMap, defaults.EnemiesByBiome);
        foreach (var entry in enemyMap)
        {
            catalog.EnemiesByBiome[entry.Key] = entry.Value;
        }

        var eventsFile = LoadJson<EventsFile>(Path.Combine(dataDir, "events.json"), options);
        if (eventsFile?.Common != null && eventsFile.Common.Count > 0)
        {
            catalog.CommonEvents.AddRange(eventsFile.Common);
        }
        else
        {
            catalog.CommonEvents.AddRange(defaults.CommonEvents);
        }

        var eventMap = ConvertEventBiomes(eventsFile?.Biomes);
        EnsureEvents(eventMap, defaults.EventsByBiome);
        foreach (var entry in eventMap)
        {
            catalog.EventsByBiome[entry.Key] = entry.Value;
        }

        return catalog;
    }

    private static T? LoadJson<T>(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            return default;
        }
    }

    private static Dictionary<BiomeType, List<EnemyTemplate>> ConvertEnemyBiomes(Dictionary<string, List<EnemyTemplate>>? raw)
    {
        var result = new Dictionary<BiomeType, List<EnemyTemplate>>();
        if (raw == null)
        {
            return result;
        }

        foreach (var entry in raw)
        {
            if (TryParseBiome(entry.Key, out var biome) && entry.Value != null)
            {
                result[biome] = entry.Value;
            }
        }

        return result;
    }

    private static Dictionary<BiomeType, List<EventDefinition>> ConvertEventBiomes(Dictionary<string, List<EventDefinition>>? raw)
    {
        var result = new Dictionary<BiomeType, List<EventDefinition>>();
        if (raw == null)
        {
            return result;
        }

        foreach (var entry in raw)
        {
            if (TryParseBiome(entry.Key, out var biome) && entry.Value != null)
            {
                result[biome] = entry.Value;
            }
        }

        return result;
    }

    private static void EnsureEnemies(
        Dictionary<BiomeType, List<EnemyTemplate>> map,
        Dictionary<BiomeType, List<EnemyTemplate>> defaults)
    {
        foreach (BiomeType biome in Enum.GetValues<BiomeType>())
        {
            if (!map.TryGetValue(biome, out var list) || list.Count == 0)
            {
                map[biome] = defaults[biome];
            }
        }
    }

    private static void EnsureEvents(
        Dictionary<BiomeType, List<EventDefinition>> map,
        Dictionary<BiomeType, List<EventDefinition>> defaults)
    {
        foreach (BiomeType biome in Enum.GetValues<BiomeType>())
        {
            if (!map.TryGetValue(biome, out var list) || list.Count == 0)
            {
                map[biome] = defaults[biome];
            }
        }
    }

    private static bool TryParseBiome(string value, out BiomeType biome)
    {
        return Enum.TryParse(value, ignoreCase: true, out biome);
    }
}
