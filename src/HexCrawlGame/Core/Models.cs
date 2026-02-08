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

    public CombatUnit(string name, Team team, Point position, int moveRange, bool hasDiagonalMove)
    {
        Name = name;
        Team = team;
        Position = position;
        MoveRange = moveRange;
        HasDiagonalMove = hasDiagonalMove;
    }
}

public sealed class CombatState
{
    public List<CombatUnit> Units { get; } = new();
    public int? SelectedUnitIndex { get; private set; }
    public HashSet<Point> Reachable { get; } = new();

    public static CombatState CreateDefault()
    {
        var state = new CombatState();
        state.Units.Add(new CombatUnit("Fighter", Team.Hero, new Point(1, 5), 3, false));
        state.Units.Add(new CombatUnit("Rogue", Team.Hero, new Point(2, 5), 4, true));
        state.Units.Add(new CombatUnit("Mage", Team.Hero, new Point(3, 5), 3, false));
        state.Units.Add(new CombatUnit("Cleric", Team.Hero, new Point(4, 5), 3, false));

        state.Units.Add(new CombatUnit("Goblin", Team.Enemy, new Point(2, 1), 3, false));
        state.Units.Add(new CombatUnit("Raider", Team.Enemy, new Point(5, 2), 3, false));
        state.Units.Add(new CombatUnit("Wolf", Team.Enemy, new Point(4, 1), 4, false));
        return state;
    }

    public CombatUnit? GetUnitAt(Point point)
    {
        foreach (var unit in Units)
        {
            if (unit.Position == point)
            {
                return unit;
            }
        }
        return null;
    }

    public bool SelectOrMove(Point gridPos, CombatGrid grid)
    {
        var unitAt = GetUnitAt(gridPos);
        if (unitAt != null && unitAt.Team == Team.Hero)
        {
            SelectedUnitIndex = Units.IndexOf(unitAt);
            RecalculateReachable(grid);
            return true;
        }

        if (SelectedUnitIndex.HasValue && Reachable.Contains(gridPos) && !IsOccupied(gridPos))
        {
            Units[SelectedUnitIndex.Value].Position = gridPos;
            RecalculateReachable(grid);
            return true;
        }

        return false;
    }

    public void RunEnemyTurn(CombatGrid grid)
    {
        foreach (var unit in Units)
        {
            if (unit.Team != Team.Enemy)
            {
                continue;
            }

            var target = FindNearestHero(unit.Position);
            if (target == null)
            {
                continue;
            }

            var step = StepToward(unit.Position, target.Position, grid);
            if (step.HasValue)
            {
                unit.Position = step.Value;
            }
        }

        if (SelectedUnitIndex.HasValue)
        {
            RecalculateReachable(grid);
        }
    }

    private CombatUnit? FindNearestHero(Point from)
    {
        CombatUnit? best = null;
        int bestDistance = int.MaxValue;

        foreach (var unit in Units)
        {
            if (unit.Team != Team.Hero)
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
            if (unit.Position == point)
            {
                return true;
            }
        }
        return false;
    }

    private void RecalculateReachable(CombatGrid grid)
    {
        Reachable.Clear();
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
                Reachable.Add(neighbor);
                frontier.Enqueue((neighbor, current.Distance + 1));
            }
        }
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
