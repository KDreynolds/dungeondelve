using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexCrawlGame;

public enum GameMode
{
    Overworld,
    Combat
}

public enum BiomeType
{
    Plains,
    Forest,
    Hills
}

public enum Team
{
    Hero,
    Enemy
}

public enum CombatActionMode
{
    None,
    Move,
    Attack,
    Ability
}

public enum AbilityType
{
    None,
    Cleave,
    ThrowingKnife,
    ArcBolt,
    Heal
}

public readonly struct HexCoord : IEquatable<HexCoord>
{
    public int Q { get; }
    public int R { get; }

    public HexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public IEnumerable<HexCoord> Neighbors()
    {
        foreach (var dir in HexMath.Directions)
        {
            yield return new HexCoord(Q + dir.Q, R + dir.R);
        }
    }

    public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

    public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Q, R);

    public static int Distance(HexCoord a, HexCoord b)
    {
        int ax = a.Q;
        int az = a.R;
        int ay = -ax - az;

        int bx = b.Q;
        int bz = b.R;
        int by = -bx - bz;

        return (Math.Abs(ax - bx) + Math.Abs(ay - by) + Math.Abs(az - bz)) / 2;
    }

    public static IEnumerable<HexCoord> Range(HexCoord center, int radius)
    {
        for (int dq = -radius; dq <= radius; dq++)
        {
            int r1 = Math.Max(-radius, -dq - radius);
            int r2 = Math.Min(radius, -dq + radius);
            for (int dr = r1; dr <= r2; dr++)
            {
                yield return new HexCoord(center.Q + dq, center.R + dr);
            }
        }
    }
}

public static class HexMath
{
    public static readonly HexCoord[] Directions =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    };

    public static Vector2 AxialToPixel(HexCoord coord, float size)
    {
        float x = size * MathF.Sqrt(3f) * (coord.Q + coord.R / 2f);
        float y = size * 1.5f * coord.R;
        return new Vector2(x, y);
    }

    public static HexCoord PixelToAxial(Vector2 position, float size)
    {
        float q = (MathF.Sqrt(3f) / 3f * position.X - 1f / 3f * position.Y) / size;
        float r = (2f / 3f * position.Y) / size;
        return RoundAxial(q, r);
    }

    public static HexCoord RoundAxial(float q, float r)
    {
        float x = q;
        float z = r;
        float y = -x - z;

        int rx = (int)MathF.Round(x);
        int ry = (int)MathF.Round(y);
        int rz = (int)MathF.Round(z);

        float xDiff = MathF.Abs(rx - x);
        float yDiff = MathF.Abs(ry - y);
        float zDiff = MathF.Abs(rz - z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new HexCoord(rx, rz);
    }

    public static Vector2[] GetHexCorners(Vector2 center, float size)
    {
        var corners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = MathF.PI / 180f * (60f * i - 30f);
            corners[i] = new Vector2(
                center.X + size * MathF.Cos(angle),
                center.Y + size * MathF.Sin(angle));
        }
        return corners;
    }
}

public sealed class HexTile
{
    public BiomeType Biome { get; set; }
    public bool Revealed { get; set; }
    public bool Visible { get; set; }
}

public sealed class HexMap
{
    private readonly HexTile[] _tiles;

    public int Width { get; }
    public int Height { get; }

    private HexMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new HexTile[width * height];
        for (int i = 0; i < _tiles.Length; i++)
        {
            _tiles[i] = new HexTile();
        }
    }

    public static HexMap Generate(int width, int height, Random rng)
    {
        var map = new HexMap(width, height);
        for (int r = 0; r < height; r++)
        {
            for (int q = 0; q < width; q++)
            {
                map.GetTile(new HexCoord(q, r)).Biome = PickBiome(rng);
            }
        }
        return map;
    }

    public bool InBounds(HexCoord coord)
        => coord.Q >= 0 && coord.Q < Width && coord.R >= 0 && coord.R < Height;

    public HexTile GetTile(HexCoord coord)
    {
        return _tiles[coord.R * Width + coord.Q];
    }

    public void UpdateVisibility(HexCoord center, int radius)
    {
        foreach (var tile in _tiles)
        {
            tile.Visible = false;
        }

        foreach (var coord in HexCoord.Range(center, radius))
        {
            if (!InBounds(coord))
            {
                continue;
            }

            var tile = GetTile(coord);
            tile.Visible = true;
            tile.Revealed = true;
        }
    }

    private static BiomeType PickBiome(Random rng)
    {
        int roll = rng.Next(100);
        if (roll < 60)
        {
            return BiomeType.Plains;
        }
        if (roll < 85)
        {
            return BiomeType.Forest;
        }
        return BiomeType.Hills;
    }
}

public sealed class CombatGrid
{
    public int Width { get; }
    public int Height { get; }
    private readonly bool[,] _blocked;

    public CombatGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _blocked = new bool[width, height];
    }

    public bool InBounds(Point point)
        => point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;

    public bool IsWalkable(Point point)
        => InBounds(point) && !_blocked[point.X, point.Y];

    public IEnumerable<Point> GetNeighbors(Point point, bool allowDiagonal)
    {
        Point[] deltas = allowDiagonal
            ? new[]
            {
                new Point(1, 0),
                new Point(-1, 0),
                new Point(0, 1),
                new Point(0, -1),
                new Point(1, 1),
                new Point(1, -1),
                new Point(-1, 1),
                new Point(-1, -1)
            }
            : new[]
            {
                new Point(1, 0),
                new Point(-1, 0),
                new Point(0, 1),
                new Point(0, -1)
            };

        foreach (var delta in deltas)
        {
            var candidate = new Point(point.X + delta.X, point.Y + delta.Y);
            if (InBounds(candidate))
            {
                yield return candidate;
            }
        }
    }
}

public sealed class CombatUnit
{
    public string Name { get; }
    public Team Team { get; }
    public Point Position { get; set; }
    public int MoveRange { get; }
    public bool HasDiagonalMove { get; }
    public int MaxHp { get; }
    public int Hp { get; set; }
    public int Attack { get; }
    public int Defense { get; }
    public int Range { get; }
    public AbilityType Ability { get; }
    public int AbilityRange { get; }
    public int AbilityPower { get; }
    public int AbilityCooldown { get; }
    public int AbilityCooldownRemaining { get; set; }
    public int InitiativeBonus { get; }
    public int InitiativeRoll { get; set; }
    public int InitiativeTieBreaker { get; set; }
    public bool HasMoved { get; set; }
    public bool HasActed { get; set; }
    public bool TurnEnded { get; set; }
    public bool IsGuarding { get; set; }
    public bool IsAlive => Hp > 0;

    public CombatUnit(
        string name,
        Team team,
        Point position,
        int moveRange,
        bool hasDiagonalMove,
        int maxHp,
        int attack,
        int defense,
        int range,
        AbilityType ability,
        int abilityRange,
        int abilityPower,
        int abilityCooldown,
        int initiativeBonus)
    {
        Name = name;
        Team = team;
        Position = position;
        MoveRange = moveRange;
        HasDiagonalMove = hasDiagonalMove;
        MaxHp = maxHp;
        Hp = maxHp;
        Attack = attack;
        Defense = defense;
        Range = range;
        Ability = ability;
        AbilityRange = abilityRange;
        AbilityPower = abilityPower;
        AbilityCooldown = abilityCooldown;
        AbilityCooldownRemaining = 0;
        InitiativeBonus = initiativeBonus;
        InitiativeRoll = 0;
        InitiativeTieBreaker = 0;
    }
}

public sealed class CombatState
{
    private readonly Random _rng;
    private readonly List<int> _turnOrder = new();
    private int _turnOrderIndex;

    public List<CombatUnit> Units { get; } = new();
    public int? SelectedUnitIndex { get; private set; }
    public CombatUnit? SelectedUnit
        => SelectedUnitIndex.HasValue ? Units[SelectedUnitIndex.Value] : null;
    public int CurrentUnitIndex { get; private set; } = -1;
    public CombatUnit? CurrentUnit
        => CurrentUnitIndex >= 0 && CurrentUnitIndex < Units.Count ? Units[CurrentUnitIndex] : null;
    public IReadOnlyList<int> TurnOrder => _turnOrder;
    public int TurnOrderPosition => _turnOrder.Count == 0 ? 0 : _turnOrderIndex + 1;
    public int Round { get; private set; } = 1;
    public int TurnId { get; private set; } = 0;
    public CombatActionMode ActionMode { get; private set; } = CombatActionMode.None;
    public HashSet<Point> MoveRange { get; } = new();
    public HashSet<Point> AttackRange { get; } = new();
    public HashSet<Point> AbilityRange { get; } = new();
    public bool IsPlayerTurn => CurrentUnit?.Team == Team.Hero;

    public CombatState(Random rng)
    {
        _rng = rng;
    }

    public static CombatState CreateDefault(Random rng)
    {
        var state = new CombatState(rng);
        state.Units.Add(new CombatUnit(
            "Fighter", Team.Hero, new Point(1, 5), 3, false,
            maxHp: 12, attack: 5, defense: 3, range: 1,
            ability: AbilityType.Cleave, abilityRange: 1, abilityPower: 4, abilityCooldown: 2,
            initiativeBonus: 2));
        state.Units.Add(new CombatUnit(
            "Rogue", Team.Hero, new Point(2, 5), 4, true,
            maxHp: 9, attack: 4, defense: 2, range: 1,
            ability: AbilityType.ThrowingKnife, abilityRange: 3, abilityPower: 3, abilityCooldown: 2,
            initiativeBonus: 4));
        state.Units.Add(new CombatUnit(
            "Mage", Team.Hero, new Point(3, 5), 3, false,
            maxHp: 7, attack: 3, defense: 1, range: 2,
            ability: AbilityType.ArcBolt, abilityRange: 4, abilityPower: 4, abilityCooldown: 2,
            initiativeBonus: 1));
        state.Units.Add(new CombatUnit(
            "Cleric", Team.Hero, new Point(4, 5), 3, false,
            maxHp: 10, attack: 3, defense: 2, range: 1,
            ability: AbilityType.Heal, abilityRange: 3, abilityPower: 4, abilityCooldown: 2,
            initiativeBonus: 1));

        state.Units.Add(new CombatUnit(
            "Goblin", Team.Enemy, new Point(2, 1), 3, false,
            maxHp: 6, attack: 3, defense: 1, range: 1,
            ability: AbilityType.None, abilityRange: 0, abilityPower: 0, abilityCooldown: 0,
            initiativeBonus: 2));
        state.Units.Add(new CombatUnit(
            "Raider", Team.Enemy, new Point(5, 2), 3, false,
            maxHp: 8, attack: 4, defense: 2, range: 1,
            ability: AbilityType.None, abilityRange: 0, abilityPower: 0, abilityCooldown: 0,
            initiativeBonus: 1));
        state.Units.Add(new CombatUnit(
            "Wolf", Team.Enemy, new Point(4, 1), 4, false,
            maxHp: 7, attack: 4, defense: 1, range: 1,
            ability: AbilityType.None, abilityRange: 0, abilityPower: 0, abilityCooldown: 0,
            initiativeBonus: 3));
        state.StartCombat();
        return state;
    }

    public void StartCombat()
    {
        Round = 1;
        RollInitiative();
        StartTurnAtPosition(0);
    }

    public int CountAlive(Team team)
    {
        int count = 0;
        foreach (var unit in Units)
        {
            if (unit.Team == team && unit.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    public CombatUnit? GetUnitAt(Point point)
    {
        foreach (var unit in Units)
        {
            if (unit.IsAlive && unit.Position == point)
            {
                return unit;
            }
        }
        return null;
    }

    public bool TrySelectUnit(Point gridPos, CombatGrid grid)
    {
        if (!IsPlayerTurn)
        {
            return false;
        }

        var unitAt = GetUnitAt(gridPos);
        if (unitAt == null || CurrentUnitIndex < 0 || Units[CurrentUnitIndex] != unitAt)
        {
            return false;
        }

        SelectedUnitIndex = CurrentUnitIndex;
        SetActionMode(CombatActionMode.Move, grid);
        return true;
    }

    public void SetActionMode(CombatActionMode mode, CombatGrid grid)
    {
        ActionMode = CombatActionMode.None;
        ClearRanges();

        if (!SelectedUnitIndex.HasValue || !IsPlayerTurn || SelectedUnitIndex.Value != CurrentUnitIndex)
        {
            return;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (!unit.IsAlive || unit.TurnEnded)
        {
            return;
        }

        switch (mode)
        {
            case CombatActionMode.Move:
                if (unit.HasMoved)
                {
                    return;
                }
                ActionMode = mode;
                RecalculateMoveRange(grid);
                break;
            case CombatActionMode.Attack:
                if (unit.HasActed)
                {
                    return;
                }
                ActionMode = mode;
                RecalculateAttackRange(grid);
                break;
            case CombatActionMode.Ability:
                if (unit.HasActed || unit.Ability == AbilityType.None || unit.AbilityCooldownRemaining > 0)
                {
                    return;
                }
                ActionMode = mode;
                RecalculateAbilityRange(grid);
                break;
            default:
                break;
        }
    }

    public void ClearActionMode()
    {
        ActionMode = CombatActionMode.None;
        ClearRanges();
    }

    public bool TryMoveSelected(Point gridPos, CombatGrid grid)
    {
        if (!SelectedUnitIndex.HasValue || !IsPlayerTurn || SelectedUnitIndex.Value != CurrentUnitIndex)
        {
            return false;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (unit.HasMoved || unit.TurnEnded || !unit.IsAlive)
        {
            return false;
        }

        if (MoveRange.Contains(gridPos) && !IsOccupied(gridPos))
        {
            unit.Position = gridPos;
            unit.HasMoved = true;
            ClearActionMode();
            MaybeAutoEndTurn(unit, grid);
            return true;
        }

        return false;
    }

    public bool TryAttackSelected(Point gridPos, CombatGrid grid)
    {
        if (!SelectedUnitIndex.HasValue || !IsPlayerTurn || SelectedUnitIndex.Value != CurrentUnitIndex)
        {
            return false;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (unit.HasActed || unit.TurnEnded || !unit.IsAlive)
        {
            return false;
        }

        if (!AttackRange.Contains(gridPos))
        {
            return false;
        }

        var target = GetUnitAt(gridPos);
        if (target == null || target.Team == Team.Hero)
        {
            return false;
        }

        ResolveAttack(unit, target, unit.Attack);
        unit.HasActed = true;
        ClearActionMode();
        MaybeAutoEndTurn(unit, grid);
        return true;
    }

    public bool TryAbilitySelected(Point gridPos, CombatGrid grid)
    {
        if (!SelectedUnitIndex.HasValue || !IsPlayerTurn || SelectedUnitIndex.Value != CurrentUnitIndex)
        {
            return false;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (unit.HasActed || unit.TurnEnded || !unit.IsAlive)
        {
            return false;
        }

        if (unit.Ability == AbilityType.None || unit.AbilityCooldownRemaining > 0)
        {
            return false;
        }

        if (!AbilityRange.Contains(gridPos))
        {
            return false;
        }

        if (!ResolveAbility(unit, gridPos))
        {
            return false;
        }

        unit.HasActed = true;
        unit.AbilityCooldownRemaining = unit.AbilityCooldown;
        ClearActionMode();
        MaybeAutoEndTurn(unit, grid);
        return true;
    }

    public bool TryGuardSelected(CombatGrid grid)
    {
        if (!SelectedUnitIndex.HasValue || !IsPlayerTurn || SelectedUnitIndex.Value != CurrentUnitIndex)
        {
            return false;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (unit.HasActed || unit.TurnEnded || !unit.IsAlive)
        {
            return false;
        }

        unit.IsGuarding = true;
        unit.HasActed = true;
        ClearActionMode();
        MaybeAutoEndTurn(unit, grid);
        return true;
    }

    public void EndCurrentTurn(CombatGrid grid)
    {
        if (CurrentUnitIndex < 0)
        {
            return;
        }

        var unit = Units[CurrentUnitIndex];
        if (!unit.IsAlive)
        {
            AdvanceTurn(grid);
            return;
        }

        unit.TurnEnded = true;
        SelectedUnitIndex = null;
        ClearActionMode();
        AdvanceTurn(grid);
    }

    public void ExecuteEnemyTurn(CombatGrid grid)
    {
        if (CurrentUnitIndex < 0)
        {
            return;
        }

        var unit = Units[CurrentUnitIndex];
        if (unit.Team != Team.Enemy || !unit.IsAlive)
        {
            AdvanceTurn(grid);
            return;
        }

        var target = FindNearestHero(unit.Position);
        if (target == null)
        {
            AdvanceTurn(grid);
            return;
        }

        for (int step = 0; step < unit.MoveRange; step++)
        {
            if (IsInRange(unit.Position, target.Position, unit.Range))
            {
                break;
            }

            var nextStep = StepToward(unit.Position, target.Position, grid);
            if (!nextStep.HasValue)
            {
                break;
            }

            unit.Position = nextStep.Value;
        }

        var attackTarget = FindHeroInRange(unit.Position, unit.Range);
        if (attackTarget != null)
        {
            ResolveAttack(unit, attackTarget, unit.Attack);
        }

        EndCurrentTurn(grid);
    }

    private CombatUnit? FindNearestHero(Point from)
    {
        CombatUnit? best = null;
        int bestDistance = int.MaxValue;

        foreach (var unit in Units)
        {
            if (unit.Team != Team.Hero || !unit.IsAlive)
            {
                continue;
            }

            int dist = Math.Abs(unit.Position.X - from.X) + Math.Abs(unit.Position.Y - from.Y);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = unit;
            }
        }

        return best;
    }

    private CombatUnit? FindHeroInRange(Point from, int range)
    {
        CombatUnit? best = null;
        int bestDistance = int.MaxValue;

        foreach (var unit in Units)
        {
            if (unit.Team != Team.Hero || !unit.IsAlive)
            {
                continue;
            }

            int dist = Math.Abs(unit.Position.X - from.X) + Math.Abs(unit.Position.Y - from.Y);
            if (dist <= range && dist < bestDistance)
            {
                bestDistance = dist;
                best = unit;
            }
        }

        return best;
    }

    private Point? StepToward(Point from, Point target, CombatGrid grid)
    {
        var candidates = new List<Point>
        {
            new(from.X + 1, from.Y),
            new(from.X - 1, from.Y),
            new(from.X, from.Y + 1),
            new(from.X, from.Y - 1)
        };

        Point? best = null;
        int bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!grid.IsWalkable(candidate) || IsOccupied(candidate))
            {
                continue;
            }

            int dist = Math.Abs(candidate.X - target.X) + Math.Abs(candidate.Y - target.Y);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = candidate;
            }
        }

        return best;
    }

    private bool IsOccupied(Point point)
    {
        foreach (var unit in Units)
        {
            if (unit.IsAlive && unit.Position == point)
            {
                return true;
            }
        }
        return false;
    }

    private void RecalculateMoveRange(CombatGrid grid)
    {
        MoveRange.Clear();
        if (!SelectedUnitIndex.HasValue)
        {
            return;
        }

        var unit = Units[SelectedUnitIndex.Value];
        var frontier = new Queue<(Point Position, int Distance)>();
        var visited = new HashSet<Point>();
        frontier.Enqueue((unit.Position, 0));
        visited.Add(unit.Position);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current.Distance >= unit.MoveRange)
            {
                continue;
            }

            foreach (var neighbor in grid.GetNeighbors(current.Position, unit.HasDiagonalMove))
            {
                if (visited.Contains(neighbor))
                {
                    continue;
                }

                if (!grid.IsWalkable(neighbor) || IsOccupied(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                MoveRange.Add(neighbor);
                frontier.Enqueue((neighbor, current.Distance + 1));
            }
        }
    }

    private void RecalculateAttackRange(CombatGrid grid)
    {
        AttackRange.Clear();
        if (!SelectedUnitIndex.HasValue)
        {
            return;
        }

        var unit = Units[SelectedUnitIndex.Value];
        foreach (var tile in GetRangeTiles(unit.Position, unit.Range, grid))
        {
            AttackRange.Add(tile);
        }
    }

    private void RecalculateAbilityRange(CombatGrid grid)
    {
        AbilityRange.Clear();
        if (!SelectedUnitIndex.HasValue)
        {
            return;
        }

        var unit = Units[SelectedUnitIndex.Value];
        if (unit.Ability == AbilityType.None)
        {
            return;
        }

        foreach (var tile in GetRangeTiles(unit.Position, unit.AbilityRange, grid))
        {
            AbilityRange.Add(tile);
        }
    }

    private IEnumerable<Point> GetRangeTiles(Point origin, int range, CombatGrid grid)
    {
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int dist = Math.Abs(dx) + Math.Abs(dy);
                if (dist == 0 || dist > range)
                {
                    continue;
                }

                var point = new Point(origin.X + dx, origin.Y + dy);
                if (grid.InBounds(point))
                {
                    yield return point;
                }
            }
        }
    }

    private void ClearRanges()
    {
        MoveRange.Clear();
        AttackRange.Clear();
        AbilityRange.Clear();
    }

    private void MaybeAutoEndTurn(CombatUnit unit, CombatGrid grid)
    {
        if (unit.HasMoved && unit.HasActed)
        {
            EndCurrentTurn(grid);
        }
    }

    private void ResolveAttack(CombatUnit attacker, CombatUnit target, int power)
    {
        int damage = Math.Max(1, power - target.Defense);
        if (target.IsGuarding)
        {
            damage = Math.Max(1, (int)MathF.Ceiling(damage * 0.5f));
            target.IsGuarding = false;
        }

        target.Hp = Math.Max(0, target.Hp - damage);
    }

    private bool ResolveAbility(CombatUnit caster, Point targetPoint)
    {
        switch (caster.Ability)
        {
            case AbilityType.Cleave:
            {
                if (!IsAdjacent(caster.Position, targetPoint))
                {
                    return false;
                }

                var target = GetUnitAt(targetPoint);
                if (target == null || target.Team == caster.Team)
                {
                    return false;
                }

                foreach (var enemy in Units)
                {
                    if (enemy.Team != caster.Team && enemy.IsAlive && IsAdjacent(caster.Position, enemy.Position))
                    {
                        ResolveAttack(caster, enemy, caster.AbilityPower);
                    }
                }
                return true;
            }
            case AbilityType.ThrowingKnife:
            case AbilityType.ArcBolt:
            {
                var target = GetUnitAt(targetPoint);
                if (target == null || target.Team == caster.Team)
                {
                    return false;
                }

                ResolveAttack(caster, target, caster.AbilityPower);
                return true;
            }
            case AbilityType.Heal:
            {
                var target = GetUnitAt(targetPoint);
                if (target == null || target.Team != caster.Team)
                {
                    return false;
                }

                target.Hp = Math.Min(target.MaxHp, target.Hp + caster.AbilityPower);
                return true;
            }
            default:
                return false;
        }
    }

    private static bool IsAdjacent(Point a, Point b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    private static bool IsInRange(Point a, Point b, int range)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) <= range;

    private void RollInitiative()
    {
        _turnOrder.Clear();
        var entries = new List<(int Index, int Roll, int InitiativeBonus, int Tie)>();

        for (int i = 0; i < Units.Count; i++)
        {
            var unit = Units[i];
            if (!unit.IsAlive)
            {
                continue;
            }

            unit.InitiativeRoll = _rng.Next(1, 21) + unit.InitiativeBonus;
            unit.InitiativeTieBreaker = _rng.Next();
            entries.Add((i, unit.InitiativeRoll, unit.InitiativeBonus, unit.InitiativeTieBreaker));
        }

        entries.Sort((a, b) =>
        {
            int rollCompare = b.Roll.CompareTo(a.Roll);
            if (rollCompare != 0)
            {
                return rollCompare;
            }

            int initCompare = b.InitiativeBonus.CompareTo(a.InitiativeBonus);
            if (initCompare != 0)
            {
                return initCompare;
            }

            return b.Tie.CompareTo(a.Tie);
        });

        foreach (var entry in entries)
        {
            _turnOrder.Add(entry.Index);
        }

        _turnOrderIndex = 0;
    }

    private void StartTurnAtPosition(int orderPosition)
    {
        if (_turnOrder.Count == 0)
        {
            CurrentUnitIndex = -1;
            SelectedUnitIndex = null;
            ClearActionMode();
            return;
        }

        _turnOrderIndex = Math.Clamp(orderPosition, 0, _turnOrder.Count - 1);
        CurrentUnitIndex = _turnOrder[_turnOrderIndex];
        BeginTurnForCurrentUnit();
    }

    private void BeginTurnForCurrentUnit()
    {
        TurnId++;
        ClearActionMode();

        var unit = CurrentUnit;
        if (unit == null || !unit.IsAlive)
        {
            return;
        }

        unit.HasMoved = false;
        unit.HasActed = false;
        unit.TurnEnded = false;
        unit.IsGuarding = false;

        if (unit.AbilityCooldownRemaining > 0)
        {
            unit.AbilityCooldownRemaining--;
        }

        SelectedUnitIndex = unit.Team == Team.Hero ? CurrentUnitIndex : null;
    }

    private void AdvanceTurn(CombatGrid grid)
    {
        if (_turnOrder.Count == 0)
        {
            return;
        }

        int nextPosition = FindNextAlivePosition(_turnOrderIndex + 1);
        if (nextPosition == -1)
        {
            Round++;
            nextPosition = FindNextAlivePosition(0);
            if (nextPosition == -1)
            {
                CurrentUnitIndex = -1;
                SelectedUnitIndex = null;
                ClearActionMode();
                return;
            }
        }

        StartTurnAtPosition(nextPosition);
    }

    private int FindNextAlivePosition(int start)
    {
        for (int i = start; i < _turnOrder.Count; i++)
        {
            int unitIndex = _turnOrder[i];
            if (unitIndex >= 0 && unitIndex < Units.Count && Units[unitIndex].IsAlive)
            {
                return i;
            }
        }

        return -1;
    }
}

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public Point MousePosition => new(_currentMouse.X, _currentMouse.Y);

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    public bool IsNewKeyPress(Keys key)
        => _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    public bool IsKeyDown(Keys key)
        => _currentKeyboard.IsKeyDown(key);

    public bool IsLeftClick()
        => _currentMouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released;

    public bool IsRightClick()
        => _currentMouse.RightButton == ButtonState.Pressed
            && _previousMouse.RightButton == ButtonState.Released;
}

public static class IsoMath
{
    public static Vector2 GridToScreen(Point grid, int tileWidth, int tileHeight)
    {
        float x = (grid.X - grid.Y) * (tileWidth / 2f);
        float y = (grid.X + grid.Y) * (tileHeight / 2f);
        return new Vector2(x, y);
    }

    public static Point ScreenToGrid(Vector2 screen, int tileWidth, int tileHeight)
    {
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;

        float gx = (screen.X / halfW + screen.Y / halfH) / 2f;
        float gy = (screen.Y / halfH - screen.X / halfW) / 2f;

        int ix = (int)MathF.Round(gx);
        int iy = (int)MathF.Round(gy);
        return new Point(ix, iy);
    }
}
