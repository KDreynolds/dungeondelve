using System.Collections.Generic;

namespace HexCrawlGame;

public static class ContentDefaults
{
    public static ContentCatalog CreateCatalog()
    {
        var catalog = new ContentCatalog();

        catalog.Party.AddRange(new[]
        {
            new PartyTemplate
            {
                Name = "Fighter",
                BaseMaxHp = 12,
                Attack = 5,
                Defense = 3,
                Range = 1,
                MoveRange = 3,
                HasDiagonalMove = false,
                Ability = AbilityType.Cleave,
                AbilityRange = 1,
                AbilityPower = 4,
                AbilityCooldown = 2,
                InitiativeBonus = 2
            },
            new PartyTemplate
            {
                Name = "Rogue",
                BaseMaxHp = 9,
                Attack = 4,
                Defense = 2,
                Range = 1,
                MoveRange = 4,
                HasDiagonalMove = true,
                Ability = AbilityType.ThrowingKnife,
                AbilityRange = 3,
                AbilityPower = 3,
                AbilityCooldown = 2,
                InitiativeBonus = 4
            },
            new PartyTemplate
            {
                Name = "Mage",
                BaseMaxHp = 7,
                Attack = 3,
                Defense = 1,
                Range = 2,
                MoveRange = 3,
                HasDiagonalMove = false,
                Ability = AbilityType.ArcBolt,
                AbilityRange = 4,
                AbilityPower = 4,
                AbilityCooldown = 2,
                InitiativeBonus = 1
            },
            new PartyTemplate
            {
                Name = "Cleric",
                BaseMaxHp = 10,
                Attack = 3,
                Defense = 2,
                Range = 1,
                MoveRange = 3,
                HasDiagonalMove = false,
                Ability = AbilityType.Heal,
                AbilityRange = 3,
                AbilityPower = 4,
                AbilityCooldown = 2,
                InitiativeBonus = 1
            }
        });

        catalog.EnemiesByBiome[BiomeType.Plains] = new List<EnemyTemplate>
        {
            new EnemyTemplate { Name = "Goblin", MaxHp = 6, Attack = 3, Defense = 1, Range = 1, MoveRange = 3, InitiativeBonus = 2 },
            new EnemyTemplate { Name = "Raider", MaxHp = 8, Attack = 4, Defense = 2, Range = 1, MoveRange = 3, InitiativeBonus = 1 }
        };
        catalog.EnemiesByBiome[BiomeType.Forest] = new List<EnemyTemplate>
        {
            new EnemyTemplate { Name = "Wolf", MaxHp = 7, Attack = 4, Defense = 1, Range = 1, MoveRange = 4, InitiativeBonus = 3 },
            new EnemyTemplate { Name = "Goblin", MaxHp = 6, Attack = 3, Defense = 1, Range = 1, MoveRange = 3, InitiativeBonus = 2 }
        };
        catalog.EnemiesByBiome[BiomeType.Hills] = new List<EnemyTemplate>
        {
            new EnemyTemplate { Name = "Raider", MaxHp = 8, Attack = 4, Defense = 2, Range = 1, MoveRange = 3, InitiativeBonus = 1 },
            new EnemyTemplate { Name = "Wolf", MaxHp = 7, Attack = 4, Defense = 1, Range = 1, MoveRange = 4, InitiativeBonus = 3 }
        };

        catalog.CommonEvents.Add(new EventDefinition(
            "Strange Shrine",
            "A weathered shrine sits beside the trail, its stones cold to the touch.",
            BiomeType.Plains,
            new[]
            {
                new EventChoice("Pray for guidance", new EventEffect(1, 0, 0, false, "A calm settles over the party (+1 HP each).")),
                new EventChoice("Leave it be", new EventEffect(0, 0, 5, false, "You move on with fresh resolve (+5 XP)."))
            }));

        catalog.EventsByBiome[BiomeType.Forest] = new List<EventDefinition>
        {
            new EventDefinition(
                "Berry Patch",
                "You find a patch of ripe berries tucked under the trees.",
                BiomeType.Forest,
                new[]
                {
                    new EventChoice("Eat the berries", new EventEffect(2, 0, 0, false, "The party feels refreshed (+2 HP each).")),
                    new EventChoice("Gather and move on", new EventEffect(0, 0, 10, false, "You mark the find in your notes (+10 XP)."))
                }),
            new EventDefinition(
                "Wolf Howls",
                "Howls echo through the forest. Shadows pace you from the treeline.",
                BiomeType.Forest,
                new[]
                {
                    new EventChoice("Stand your ground", new EventEffect(0, 0, 0, true, "A pack attacks!")),
                    new EventChoice("Hide and wait", new EventEffect(0, 0, 5, false, "You avoid danger (+5 XP)."))
                })
        };

        catalog.EventsByBiome[BiomeType.Hills] = new List<EventDefinition>
        {
            new EventDefinition(
                "Rockslide",
                "Loose stones tumble down the slope as you climb.",
                BiomeType.Hills,
                new[]
                {
                    new EventChoice("Dash through", new EventEffect(-2, 0, 0, false, "You are battered by debris (-2 HP each).")),
                    new EventChoice("Find a safer path", new EventEffect(0, 0, 5, false, "You learn the terrain (+5 XP)."))
                }),
            new EventDefinition(
                "Old Mine",
                "A collapsed mine entrance hints at forgotten riches.",
                BiomeType.Hills,
                new[]
                {
                    new EventChoice("Search the shaft", new EventEffect(0, 0, 15, false, "You salvage ore and tools (+15 XP).")),
                    new EventChoice("Keep moving", new EventEffect(0, 0, 5, false, "You stay alert (+5 XP)."))
                })
        };

        catalog.EventsByBiome[BiomeType.Plains] = new List<EventDefinition>
        {
            new EventDefinition(
                "Wayfarer",
                "A lone traveler offers stories of the road ahead.",
                BiomeType.Plains,
                new[]
                {
                    new EventChoice("Trade stories", new EventEffect(0, 0, 10, false, "You learn the region (+10 XP).")),
                    new EventChoice("Decline politely", new EventEffect(0, 0, 5, false, "You keep your distance (+5 XP)."))
                }),
            new EventDefinition(
                "Dust Storm",
                "A wall of dust rolls across the plains.",
                BiomeType.Plains,
                new[]
                {
                    new EventChoice("Push through", new EventEffect(-1, 0, 0, false, "Grit wears you down (-1 HP each).")),
                    new EventChoice("Take shelter", new EventEffect(0, 0, 5, false, "You recover your bearings (+5 XP)."))
                })
        };

        return catalog;
    }
}
