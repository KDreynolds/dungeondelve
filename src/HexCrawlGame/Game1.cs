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
    private readonly Random _rng = new(12345);

    private const int MapWidth = 20;
    private const int MapHeight = 20;
    private const int VisionRadius = 2;
    private const int EncounterChancePercent = 20;
    private const int EventChancePercent = 40;
    private const int LegacyXpPerHex = 5;
    private const int LegacyXpPerVictory = 20;
    private const int CombatGridSize = 8;

    private GameMode _mode = GameMode.Overworld;

    private HexMap _hexMap = null!;
    private HexCoord _partyPos;
    private HexCoord _townPos;
    private List<PartyMember> _party = new();
    private int _runNumber = 1;
    private int _legacyXp = 0;
    private ContentCatalog _content = new();
    private EventDefinition? _currentEvent;
    private BiomeType _currentEventBiome;
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
    private readonly PixelFont _font = new();

    private string _toastMessage = string.Empty;
    private float _toastTimer;

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

        _combatGrid = new CombatGrid(CombatGridSize, CombatGridSize);
        _combatState = new CombatState(_rng);
        LoadContentData();
        StartNewRun();

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
        if (_toastTimer > 0f)
        {
            _toastTimer = MathF.Max(0f, _toastTimer - dt);
        }
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
        else if (_mode == GameMode.Combat)
        {
            UpdateCombat();
        }
        else if (_mode == GameMode.Event)
        {
            UpdateEvent();
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
                MovePartyTo(coord);
            }
        }
    }

    private void UpdateCombat()
    {
        if (!_combatState.IsPlayerTurn)
        {
            _combatState.ExecuteEnemyTurn(_combatGrid);
            HandleCombatOutcome();
            return;
        }

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
            _combatState.EndCurrentTurn(_combatGrid);
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
            _combatState.EndCurrentTurn(_combatGrid);
        }

        HandleCombatOutcome();
    }

    private void UpdateEvent()
    {
        if (_currentEvent == null)
        {
            _mode = GameMode.Overworld;
            return;
        }

        if (_input.IsNewKeyPress(Keys.D1) || _input.IsNewKeyPress(Keys.NumPad1))
        {
            ResolveEventChoice(0);
        }
        else if (_input.IsNewKeyPress(Keys.D2) || _input.IsNewKeyPress(Keys.NumPad2))
        {
            ResolveEventChoice(1);
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
        else if (_mode == GameMode.Combat)
        {
            DrawCombat();
        }
        else if (_mode == GameMode.Event)
        {
            DrawEvent();
        }

        DrawToast();

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

                if (tile.Revealed && tile.Visited)
                {
                    var markerRect = new Rectangle((int)center.X - 2, (int)center.Y - 2, 4, 4);
                    var markerColor = tile.Visible ? new Color(230, 230, 235) : new Color(140, 140, 150);
                    _spriteBatch.Draw(_pixel, markerRect, markerColor);
                }

                if (tile.Revealed && coord.Equals(_townPos))
                {
                    var townRect = new Rectangle((int)center.X - 3, (int)center.Y - 12, 6, 6);
                    _spriteBatch.Draw(_pixel, townRect, new Color(220, 200, 120));
                }
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

        DrawOverworldHud();
    }

    private void DrawEvent()
    {
        if (_currentEvent == null)
        {
            return;
        }

        var dim = new Color(0, 0, 0, 160);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), dim);

        string text = BuildEventText(_currentEvent);
        const int scale = 2;
        const int padding = 12;
        var size = _font.MeasureString(text, scale);
        var pos = new Vector2(
            (_graphics.PreferredBackBufferWidth - size.X) / 2f,
            (_graphics.PreferredBackBufferHeight - size.Y) / 2f);
        DrawHudPanel(pos, text, new Color(230, 230, 235), scale, padding);
    }

    private void DrawOverworldHud()
    {
        const int scale = 2;
        const int padding = 8;
        string statusText = BuildOverworldStatusText();
        DrawHudPanel(new Vector2(16, 16), statusText, new Color(225, 225, 235), scale, padding);
    }

    private string BuildEventText(EventDefinition definition)
    {
        string title = definition.Title.ToUpperInvariant();
        string body = definition.Body;
        string choice1 = definition.Choices.Length > 0 ? $"1) {definition.Choices[0].Text}" : "1) -";
        string choice2 = definition.Choices.Length > 1 ? $"2) {definition.Choices[1].Text}" : "2) -";
        return $"{title}\n\n{body}\n\n{choice1}\n{choice2}";
    }

    private string BuildOverworldStatusText()
    {
        string runLine = $"RUN: {_runNumber}";
        string legacyLine = $"LEGACY XP: {_legacyXp}  LEVEL: {LegacyLevel}";
        string partyLine1 = BuildPartyLine("PARTY", 0, 2);
        string partyLine2 = BuildPartyLine(string.Empty, 2, 2);
        string visitedLine = $"VISITED: {_hexMap.CountVisited()}";

        var lines = new List<string> { runLine, legacyLine };
        if (!string.IsNullOrEmpty(partyLine1))
        {
            lines.Add(partyLine1);
        }
        if (!string.IsNullOrEmpty(partyLine2))
        {
            lines.Add(partyLine2);
        }
        lines.Add(visitedLine);

        return string.Join('\n', lines);
    }

    private string BuildPartyLine(string label, int start, int count)
    {
        var parts = new List<string>();
        int end = Math.Min(start + count, _party.Count);
        for (int i = start; i < end; i++)
        {
            var member = _party[i];
            string prefix = member.Name.Length > 0
                ? member.Name.Substring(0, 1).ToUpperInvariant()
                : "?";
            parts.Add($"{prefix} {member.Hp}/{member.MaxHp}");
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Join("  ", parts);
        }

        return $"{label}: {string.Join("  ", parts)}";
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

        DrawCombatHud();
    }

    private void DrawCombatHud()
    {
        const int scale = 2;
        const int padding = 8;

        string statusText = BuildCombatStatusText();
        DrawHudPanel(new Vector2(16, 16), statusText, new Color(225, 225, 235), scale, padding);

        string controlsText = "CONTROLS:\nM MOVE  A ATTACK\nB ABILITY  G GUARD\nE END TURN  SPACE END\nRIGHT CLICK CANCEL";
        var controlSize = _font.MeasureString(controlsText, scale);
        var controlsPos = new Vector2(
            16,
            _graphics.PreferredBackBufferHeight - 16 - controlSize.Y);
        DrawHudPanel(controlsPos, controlsText, new Color(210, 210, 220), scale, padding);
    }

    private void DrawHudPanel(Vector2 position, string text, Color textColor, int scale, int padding)
    {
        var size = _font.MeasureString(text, scale);
        var rect = new Rectangle(
            (int)position.X - padding,
            (int)position.Y - padding,
            size.X + padding * 2,
            size.Y + padding * 2);
        _spriteBatch.Draw(_pixel, rect, new Color(8, 8, 12, 200));
        _font.DrawString(_spriteBatch, _pixel, text, position, textColor, scale);
    }

    private void DrawToast()
    {
        if (string.IsNullOrWhiteSpace(_toastMessage) || _toastTimer <= 0f)
        {
            return;
        }

        const int scale = 2;
        const int padding = 6;
        var size = _font.MeasureString(_toastMessage, scale);
        var position = new Vector2(
            (_graphics.PreferredBackBufferWidth - size.X) / 2f,
            12f);
        DrawHudPanel(position, _toastMessage, new Color(240, 240, 245), scale, padding);
    }

    private string BuildCombatStatusText()
    {
        var current = _combatState.CurrentUnit;
        string roundLine = $"ROUND: {_combatState.Round}";
        string turnLine = current == null
            ? "TURN: NONE"
            : $"{(current.Team == Team.Hero ? "HERO" : "ENEMY")} TURN: {current.Name.ToUpperInvariant()}";
        string modeLine = $"MODE: {GetModeLabel(_combatState.ActionMode)}";
        string orderLine = BuildTurnOrderLine(5);
        int heroes = _combatState.CountAlive(Team.Hero);
        int enemies = _combatState.CountAlive(Team.Enemy);
        string countLine = $"HEROES: {heroes}  ENEMIES: {enemies}";

        var unit = _combatState.SelectedUnit ?? current;
        if (unit == null)
        {
            return string.Join('\n', new[]
            {
                roundLine,
                turnLine,
                modeLine,
                orderLine,
                countLine,
                "UNIT: NONE"
            });
        }

        string unitLine = $"UNIT: {unit.Name.ToUpperInvariant()}  HP {unit.Hp}/{unit.MaxHp}";
        string initLine = $"INIT: {unit.InitiativeRoll}  BONUS: {unit.InitiativeBonus}";
        string moveLine = $"MOVE: {(unit.HasMoved ? "DONE" : "READY")}  ACT: {(unit.HasActed ? "DONE" : "READY")}";
        string abilityLine = BuildAbilityLine(unit);
        string guardLine = unit.IsGuarding ? "STATUS: GUARDING" : "STATUS: NORMAL";

        return string.Join('\n', new[]
        {
            roundLine,
            turnLine,
            modeLine,
            orderLine,
            countLine,
            unitLine,
            initLine,
            moveLine,
            abilityLine,
            guardLine
        });
    }

    private string BuildTurnOrderLine(int maxCount)
    {
        if (_combatState.TurnOrder.Count == 0 || maxCount <= 0)
        {
            return "ORDER: NONE";
        }

        var names = new List<string>();
        int start = Math.Clamp(_combatState.TurnOrderPosition - 1, 0, _combatState.TurnOrder.Count - 1);
        int index = start;
        int visited = 0;

        while (names.Count < maxCount && visited < _combatState.TurnOrder.Count)
        {
            int unitIndex = _combatState.TurnOrder[index];
            if (unitIndex >= 0 && unitIndex < _combatState.Units.Count)
            {
                var unit = _combatState.Units[unitIndex];
                if (unit.IsAlive)
                {
                    names.Add(unit.Name.ToUpperInvariant());
                }
            }

            index = (index + 1) % _combatState.TurnOrder.Count;
            visited++;
        }

        return names.Count == 0 ? "ORDER: NONE" : $"ORDER: {string.Join(" / ", names)}";
    }

    private string BuildAbilityLine(CombatUnit unit)
    {
        if (unit.Ability == AbilityType.None)
        {
            return "ABILITY: NONE";
        }

        string label = GetAbilityLabel(unit.Ability);
        string status = unit.AbilityCooldownRemaining > 0
            ? $"CD {unit.AbilityCooldownRemaining}"
            : "READY";

        return $"ABILITY: {label} {status}";
    }

    private string GetModeLabel(CombatActionMode mode)
    {
        return mode switch
        {
            CombatActionMode.Move => "MOVE",
            CombatActionMode.Attack => "ATTACK",
            CombatActionMode.Ability => "ABILITY",
            _ => "NONE"
        };
    }

    private string GetAbilityLabel(AbilityType ability)
    {
        return ability switch
        {
            AbilityType.Cleave => "CLEAVE",
            AbilityType.ThrowingKnife => "THROWING KNIFE",
            AbilityType.ArcBolt => "ARC BOLT",
            AbilityType.Heal => "HEAL",
            _ => "NONE"
        };
    }

    private int LegacyLevel => _legacyXp / 100;

    private void LoadContentData()
    {
        _content = ContentLoader.Load(AppContext.BaseDirectory);
    }

    private void StartNewRun()
    {
        _mode = GameMode.Overworld;
        _party = CreatePartyFromContent();
        _currentEvent = null;

        int bonusHp = LegacyLevel;
        foreach (var member in _party)
        {
            member.StartNewRun(bonusHp);
        }

        _hexMap = HexMap.Generate(MapWidth, MapHeight, _rng);
        _townPos = new HexCoord(MapWidth / 2, MapHeight / 2);
        _partyPos = _townPos;
        _hexMap.UpdateVisibility(_partyPos, VisionRadius);
        _hexMap.Visit(_partyPos);

        _combatState = new CombatState(_rng);
        _combatCamera = Vector2.Zero;
        _overworldCamera = Vector2.Zero;
    }

    private List<PartyMember> CreatePartyFromContent()
    {
        var members = new List<PartyMember>();
        foreach (var template in _content.Party)
        {
            members.Add(new PartyMember(
                template.Name,
                baseMaxHp: template.BaseMaxHp,
                attack: template.Attack,
                defense: template.Defense,
                range: template.Range,
                moveRange: template.MoveRange,
                hasDiagonalMove: template.HasDiagonalMove,
                ability: template.Ability,
                abilityRange: template.AbilityRange,
                abilityPower: template.AbilityPower,
                abilityCooldown: template.AbilityCooldown,
                initiativeBonus: template.InitiativeBonus));
        }

        return members;
    }

    private void MovePartyTo(HexCoord coord)
    {
        _partyPos = coord;
        _hexMap.UpdateVisibility(_partyPos, VisionRadius);

        bool firstVisit = _hexMap.Visit(_partyPos);
        if (firstVisit)
        {
            AwardLegacyXp(LegacyXpPerHex);
        }

        if (!coord.Equals(_townPos) && _rng.Next(100) < EncounterChancePercent)
        {
            StartEncounter(_hexMap.GetTile(coord).Biome);
        }
    }

    private void StartEncounter(BiomeType biome)
    {
        var eventPool = _content.GetEvents(biome);
        if (eventPool.Count > 0 && _rng.Next(100) < EventChancePercent)
        {
            StartEvent(biome);
        }
        else
        {
            StartCombat(biome, "ENCOUNTER");
        }
    }

    private void StartCombat(BiomeType biome, string toastMessage)
    {
        _combatState = BuildEncounterState(biome);
        _mode = GameMode.Combat;
        ShowToast(toastMessage);
    }

    private void StartEvent(BiomeType biome)
    {
        _currentEventBiome = biome;
        _currentEvent = RollEvent(biome);
        if (_currentEvent == null)
        {
            StartCombat(biome, "ENCOUNTER");
            return;
        }
        _mode = GameMode.Event;
        ShowToast("EVENT");
    }

    private CombatState BuildEncounterState(BiomeType biome)
    {
        var state = new CombatState(_rng);
        var heroPositions = new[]
        {
            new Point(1, 6),
            new Point(2, 6),
            new Point(3, 6),
            new Point(4, 6)
        };

        for (int i = 0; i < _party.Count && i < heroPositions.Length; i++)
        {
            state.Units.Add(_party[i].CreateCombatUnit(heroPositions[i]));
        }

        var enemyPositions = new[]
        {
            new Point(2, 1),
            new Point(4, 1),
            new Point(3, 2),
            new Point(5, 2)
        };
        foreach (var enemy in BuildEnemyUnits(biome, enemyPositions))
        {
            state.Units.Add(enemy);
        }

        state.StartCombat();
        return state;
    }

    private List<CombatUnit> BuildEnemyUnits(BiomeType biome, Point[] positions)
    {
        var pool = _content.GetEnemies(biome);
        int maxCount = Math.Min(positions.Length, 4);
        int enemyCount = _rng.Next(2, maxCount + 1);
        var enemies = new List<CombatUnit>();

        if (pool.Count == 0)
        {
            return enemies;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            var template = pool[_rng.Next(pool.Count)];
            enemies.Add(new CombatUnit(
                template.Name,
                Team.Enemy,
                positions[i],
                template.MoveRange,
                false,
                maxHp: template.MaxHp,
                attack: template.Attack,
                defense: template.Defense,
                range: template.Range,
                ability: AbilityType.None,
                abilityRange: 0,
                abilityPower: 0,
                abilityCooldown: 0,
                initiativeBonus: template.InitiativeBonus));
        }

        return enemies;
    }

    private EventDefinition? RollEvent(BiomeType biome)
    {
        var pool = _content.GetEvents(biome);
        if (pool.Count == 0)
        {
            return null;
        }

        return pool[_rng.Next(pool.Count)];
    }

    private void ResolveEventChoice(int index)
    {
        if (_currentEvent == null || index < 0 || index >= _currentEvent.Choices.Length)
        {
            return;
        }

        var choice = _currentEvent.Choices[index];
        ApplyEventEffect(choice.Effect);
        _currentEvent = null;

        if (IsPartyDefeated())
        {
            HandleDefeat();
            return;
        }

        if (choice.Effect.ForceCombat)
        {
            StartCombat(_currentEventBiome, choice.Effect.ResultText);
        }
        else
        {
            _mode = GameMode.Overworld;
            ShowToast(choice.Effect.ResultText);
        }
    }

    private void ApplyEventEffect(EventEffect effect)
    {
        if (effect.PartyHpDelta != 0)
        {
            foreach (var member in _party)
            {
                member.AdjustHp(effect.PartyHpDelta);
            }
        }

        if (effect.RandomPartyHpDelta != 0)
        {
            var alive = _party.FindAll(m => m.IsAlive);
            if (alive.Count > 0)
            {
                var target = alive[_rng.Next(alive.Count)];
                target.AdjustHp(effect.RandomPartyHpDelta);
            }
        }

        if (effect.LegacyXpDelta > 0)
        {
            AwardLegacyXp(effect.LegacyXpDelta);
        }
    }

    private bool IsPartyDefeated()
    {
        foreach (var member in _party)
        {
            if (member.IsAlive)
            {
                return false;
            }
        }

        return true;
    }

    private void HandleCombatOutcome()
    {
        if (_combatState.Units.Count == 0)
        {
            return;
        }

        if (_combatState.IsVictory)
        {
            HandleVictory();
        }
        else if (_combatState.IsDefeat)
        {
            HandleDefeat();
        }
    }

    private void HandleVictory()
    {
        SyncPartyFromCombat();
        AwardLegacyXp(LegacyXpPerVictory);
        _mode = GameMode.Overworld;
        _combatState = new CombatState(_rng);
        ShowToast("VICTORY");
    }

    private void HandleDefeat()
    {
        _runNumber++;
        StartNewRun();
        ShowToast("PARTY DEAD");
    }

    private void SyncPartyFromCombat()
    {
        foreach (var member in _party)
        {
            var unit = _combatState.Units.Find(u => u.Team == Team.Hero && u.Name == member.Name);
            if (unit != null)
            {
                member.ApplyCombatResult(unit);
            }
        }
    }

    private void AwardLegacyXp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _legacyXp += amount;
    }

    private void ShowToast(string message, float durationSeconds = 2.5f)
    {
        _toastMessage = message;
        _toastTimer = durationSeconds;
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
