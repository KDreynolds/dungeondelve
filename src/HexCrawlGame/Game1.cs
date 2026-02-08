using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexCrawlGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private readonly InputState _input = new();

    private GameMode _mode = GameMode.Overworld;

    private HexMap _hexMap = null!;
    private HexCoord _partyPos;
    private Vector2 _overworldCamera = Vector2.Zero;
    private Vector2 _overworldOrigin;
    private float _hexSize = 28f;
    private Texture2D _hexTexture = null!;

    private CombatGrid _combatGrid = null!;
    private CombatState _combatState = null!;
    private Vector2 _combatCamera = Vector2.Zero;
    private Vector2 _combatOrigin;
    private readonly int _isoTileWidth = 64;
    private readonly int _isoTileHeight = 32;
    private Texture2D _isoTileTexture = null!;

    private Texture2D _pixel = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        var rng = new Random(12345);
        _hexMap = HexMap.Generate(20, 20, rng);
        _partyPos = new HexCoord(10, 10);
        _hexMap.UpdateVisibility(_partyPos, 2);

        _combatGrid = new CombatGrid(8, 8);
        _combatState = CombatState.CreateDefault();

        _overworldOrigin = new Vector2(_graphics.PreferredBackBufferWidth / 2f, 80f);
        _combatOrigin = new Vector2(_graphics.PreferredBackBufferWidth / 2f, 80f);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = PrimitiveTextures.CreatePixel(GraphicsDevice);
        _hexTexture = PrimitiveTextures.CreateHexMask(GraphicsDevice, _hexSize);
        _isoTileTexture = PrimitiveTextures.CreateIsoDiamond(GraphicsDevice, _isoTileWidth, _isoTileHeight);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.IsNewKeyPress(Keys.Escape))
        {
            Exit();
        }

        if (_input.IsNewKeyPress(Keys.Tab))
        {
            _mode = _mode == GameMode.Overworld ? GameMode.Combat : GameMode.Overworld;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 cameraDelta = Vector2.Zero;

        if (_input.IsKeyDown(Keys.W))
        {
            cameraDelta.Y -= 1f;
        }
        if (_input.IsKeyDown(Keys.S))
        {
            cameraDelta.Y += 1f;
        }
        if (_input.IsKeyDown(Keys.A))
        {
            cameraDelta.X -= 1f;
        }
        if (_input.IsKeyDown(Keys.D))
        {
            cameraDelta.X += 1f;
        }

        if (cameraDelta != Vector2.Zero)
        {
            cameraDelta.Normalize();
            float speed = 300f * dt;
            if (_mode == GameMode.Overworld)
            {
                _overworldCamera += cameraDelta * speed;
            }
            else
            {
                _combatCamera += cameraDelta * speed;
            }
        }

        if (_mode == GameMode.Overworld)
        {
            UpdateOverworld();
        }
        else
        {
            UpdateCombat();
        }

        base.Update(gameTime);
    }

    private void UpdateOverworld()
    {
        if (_input.IsLeftClick())
        {
            var screen = new Vector2(_input.MousePosition.X, _input.MousePosition.Y);
            var world = screen - _overworldOrigin - _overworldCamera;
            var coord = HexMath.PixelToAxial(world, _hexSize);

            if (_hexMap.InBounds(coord) && HexCoord.Distance(coord, _partyPos) == 1)
            {
                _partyPos = coord;
                _hexMap.UpdateVisibility(_partyPos, 2);
            }
        }
    }

    private void UpdateCombat()
    {
        if (_input.IsNewKeyPress(Keys.M))
        {
            _combatState.SetActionMode(CombatActionMode.Move, _combatGrid);
        }
        if (_input.IsNewKeyPress(Keys.A))
        {
            _combatState.SetActionMode(CombatActionMode.Attack, _combatGrid);
        }
        if (_input.IsNewKeyPress(Keys.B))
        {
            _combatState.SetActionMode(CombatActionMode.Ability, _combatGrid);
        }
        if (_input.IsNewKeyPress(Keys.G))
        {
            _combatState.TryGuardSelected(_combatGrid);
        }
        if (_input.IsNewKeyPress(Keys.E))
        {
            _combatState.EndSelectedTurn(_combatGrid);
        }
        if (_input.IsLeftClick())
        {
            var screen = new Vector2(_input.MousePosition.X, _input.MousePosition.Y);
            var world = screen - _combatOrigin - _combatCamera;
            var gridPos = IsoMath.ScreenToGrid(world, _isoTileWidth, _isoTileHeight);

            if (_combatGrid.InBounds(gridPos))
            {
                if (!_combatState.TrySelectUnit(gridPos, _combatGrid))
                {
                    switch (_combatState.ActionMode)
                    {
                        case CombatActionMode.Move:
                            _combatState.TryMoveSelected(gridPos, _combatGrid);
                            break;
                        case CombatActionMode.Attack:
                            _combatState.TryAttackSelected(gridPos, _combatGrid);
                            break;
                        case CombatActionMode.Ability:
                            _combatState.TryAbilitySelected(gridPos, _combatGrid);
                            break;
                    }
                }
            }
        }

        if (_input.IsRightClick())
        {
            _combatState.ClearActionMode();
        }

        if (_input.IsNewKeyPress(Keys.Space))
        {
            _combatState.ForceEndPlayerRound(_combatGrid);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 24));

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

        if (_mode == GameMode.Overworld)
        {
            DrawOverworld();
        }
        else
        {
            DrawCombat();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawOverworld()
    {
        for (int r = 0; r < _hexMap.Height; r++)
        {
            for (int q = 0; q < _hexMap.Width; q++)
            {
                var coord = new HexCoord(q, r);
                var tile = _hexMap.GetTile(coord);
                var center = HexMath.AxialToPixel(coord, _hexSize) + _overworldOrigin + _overworldCamera;
                var drawPos = center - new Vector2(_hexTexture.Width / 2f, _hexTexture.Height / 2f);

                Color color = GetBiomeColor(tile.Biome);
                if (!tile.Revealed)
                {
                    color = new Color(10, 10, 14);
                }
                else if (!tile.Visible)
                {
                    color *= 0.4f;
                }

                _spriteBatch.Draw(_hexTexture, drawPos, color);
            }
        }

        var partyCenter = HexMath.AxialToPixel(_partyPos, _hexSize) + _overworldOrigin + _overworldCamera;
        var partyRect = new Rectangle((int)partyCenter.X - 5, (int)partyCenter.Y - 10, 10, 20);
        _spriteBatch.Draw(_pixel, partyRect, new Color(230, 230, 240));

        var hover = GetHoveredHex();
        if (hover.HasValue && _hexMap.InBounds(hover.Value))
        {
            var center = HexMath.AxialToPixel(hover.Value, _hexSize) + _overworldOrigin + _overworldCamera;
            var drawPos = center - new Vector2(_hexTexture.Width / 2f, _hexTexture.Height / 2f);
            _spriteBatch.Draw(_hexTexture, drawPos, new Color(255, 255, 255) * 0.15f);
        }
    }

    private void DrawCombat()
    {
        HashSet<Point>? highlight = null;
        Color highlightColor = new(65, 90, 130);

        switch (_combatState.ActionMode)
        {
            case CombatActionMode.Move:
                highlight = _combatState.MoveRange;
                highlightColor = new Color(65, 90, 130);
                break;
            case CombatActionMode.Attack:
                highlight = _combatState.AttackRange;
                highlightColor = new Color(140, 70, 70);
                break;
            case CombatActionMode.Ability:
                highlight = _combatState.AbilityRange;
                highlightColor = new Color(70, 120, 120);
                break;
        }

        for (int y = 0; y < _combatGrid.Height; y++)
        {
            for (int x = 0; x < _combatGrid.Width; x++)
            {
                var gridPos = new Point(x, y);
                var tilePos = IsoMath.GridToScreen(gridPos, _isoTileWidth, _isoTileHeight)
                    + _combatOrigin + _combatCamera;
                Color color = new Color(40, 40, 50);

                if (highlight != null && highlight.Contains(gridPos))
                {
                    color = highlightColor;
                }

                _spriteBatch.Draw(_isoTileTexture, tilePos, color);

                if (_combatState.SelectedUnit?.Position == gridPos)
                {
                    _spriteBatch.Draw(_isoTileTexture, tilePos, new Color(120, 120, 160) * 0.35f);
                }
            }
        }

        foreach (var unit in _combatState.Units)
        {
            if (!unit.IsAlive)
            {
                continue;
            }

            var tilePos = IsoMath.GridToScreen(unit.Position, _isoTileWidth, _isoTileHeight)
                + _combatOrigin + _combatCamera;
            var center = tilePos + new Vector2(_isoTileWidth / 2f, _isoTileHeight / 2f);

            var rect = new Rectangle((int)center.X - 6, (int)center.Y - 14, 12, 20);
            Color color = unit.Team == Team.Hero ? new Color(90, 200, 120) : new Color(200, 90, 90);
            if (unit.IsGuarding)
            {
                color = new Color(120, 180, 220);
            }
            _spriteBatch.Draw(_pixel, rect, color);

            DrawHealthBar(center, unit);
        }
    }

    private void DrawHealthBar(Vector2 center, CombatUnit unit)
    {
        const int barWidth = 24;
        const int barHeight = 4;
        float ratio = unit.MaxHp > 0 ? unit.Hp / (float)unit.MaxHp : 0f;

        int x = (int)center.X - barWidth / 2;
        int y = (int)center.Y - 24;
        var backRect = new Rectangle(x, y, barWidth, barHeight);
        _spriteBatch.Draw(_pixel, backRect, new Color(15, 15, 18));

        int fillWidth = (int)MathF.Round(barWidth * Math.Clamp(ratio, 0f, 1f));
        if (fillWidth > 0)
        {
            var fillRect = new Rectangle(x, y, fillWidth, barHeight);
            Color fillColor = unit.Team == Team.Hero ? new Color(70, 200, 90) : new Color(200, 80, 80);
            _spriteBatch.Draw(_pixel, fillRect, fillColor);
        }
    }

    private Color GetBiomeColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Plains => new Color(80, 150, 90),
            BiomeType.Forest => new Color(50, 110, 70),
            BiomeType.Hills => new Color(130, 110, 70),
            _ => new Color(90, 90, 90)
        };
    }

    private HexCoord? GetHoveredHex()
    {
        var screen = new Vector2(_input.MousePosition.X, _input.MousePosition.Y);
        var world = screen - _overworldOrigin - _overworldCamera;
        var coord = HexMath.PixelToAxial(world, _hexSize);
        return coord;
    }
}
