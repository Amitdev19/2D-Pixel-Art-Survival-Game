using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MicroRoguelike;

/// <summary>
/// Static game manager class handling core game state, logic, and rendering.
/// Manages the map, entities, player progression, field of view, and UI.
/// </summary>
public static class Game
{
    /// <summary>
    /// Size of each tile in pixels for rendering.
    /// </summary>
    public const int TileSize = 12;
    /// <summary>
    /// Width of the game map in tiles.
    /// </summary>
    public const int MapWidth = 80;
    /// <summary>
    /// Height of the game map in tiles.
    /// </summary>
    public const int MapHeight = 45;
    /// <summary>
    /// Width of the UI panel on the right side in pixels.
    /// </summary>
    public const int UIPanelWidth = 200;
    /// <summary>
    /// Maximum number of enemies that can be spawned.
    /// </summary>
    public const int MaxEnemies = 20;
    /// <summary>
    /// Maximum number of items that can be spawned.
    /// </summary>
    public const int MaxItems = 15;
    /// <summary>
    /// Maximum number of rooms per dungeon level.
    /// </summary>
    public const int MaxRooms = 15;
    /// <summary>
    /// Minimum room dimension in tiles.
    /// </summary>
    public const int MinRoomSize = 5;
    /// <summary>
    /// Maximum room dimension in tiles.
    /// </summary>
    public const int MaxRoomSize = 12;
    /// <summary>
    /// Maximum radius for field of view calculation.
    /// </summary>
    public const int MaxFovRadius = 8;

    /// <summary>
    /// The game map storing tile data.
    /// </summary>
    public static Tile[] Map = new Tile[MapWidth * MapHeight];
    /// <summary>
    /// The player character.
    /// </summary>
    public static Player Player = new();
    /// <summary>
    /// Companion NPC following the player.
    /// </summary>
    public static Buddy Buddy = new();
    /// <summary>
    /// Array of enemy entities.
    /// </summary>
    public static Enemy[] Enemies = new Enemy[MaxEnemies];
    /// <summary>
    /// Array of items on the map.
    /// </summary>
    public static Item[] Items = new Item[MaxItems];
    /// <summary>
    /// Current number of active enemies.
    /// </summary>
    public static int EnemyCount;
    /// <summary>
    /// Current number of active items.
    /// </summary>
    public static int ItemCount;
    /// <summary>
    /// Current dungeon level (starts at 1, increases when descending stairs).
    /// </summary>
    public static int DungeonLevel = 1;
    /// <summary>
    /// Whether the game is still running.
    /// </summary>
    public static bool Running = true;
    /// <summary>
    /// Whether the buddy companion is alive.
    /// </summary>
    public static bool BuddyAlive = false;
    /// <summary>
    /// Status message displayed to the player.
    /// </summary>
    public static string StatusMessage = "Welcome, adventurer! WASD to move, stairs to descend.";
    /// <summary>
    /// Random number generator for game events.
    /// </summary>
    public static Random Rng = new();
    /// <summary>
    /// Visibility map tracking which tiles are in player's FOV.
    /// </summary>
    public static bool[,] Visible = new bool[MapWidth, MapHeight];
    /// <summary>
    /// Explored map tracking which tiles have been seen.
    /// </summary>
    public static bool[,] Explored = new bool[MapWidth, MapHeight];

    /// <summary>
    /// Initializes the game state: clears maps, creates entities, generates dungeon.
    /// </summary>
    public static void Init()
    {
        for (int i = 0; i < MapWidth; i++)
            for (int j = 0; j < MapHeight; j++)
            {
                Visible[i, j] = false;
                Explored[i, j] = false;
            }

        for (int i = 0; i < MaxEnemies; i++) Enemies[i] = new();
        for (int i = 0; i < MaxItems; i++) Items[i] = new();

        DungeonGenerator.GenerateDungeon();
        DungeonGenerator.PlacePlayer();
        DungeonGenerator.PlaceEnemies();
        Game.PlaceBuddy();
        DungeonGenerator.PlaceItems();
        PlaceStairs();
        ComputeFOV();
    }

    /// <summary>
    /// Places a staircase on a random floor tile.
    /// </summary>
    public static void PlaceStairs()
    {
        var floors = new List<int>();
        for (int i = 0; i < MapWidth * MapHeight; i++)
        {
            if (Map[i].Type == TileType.Floor) floors.Add(i);
        }
        if (floors.Count == 0) return;
        int idx = Rng.Next(floors.Count);
        Map[floors[idx]].ContainsStairs = true;
    }

    /// <summary>
    /// Places the buddy companion near the player's starting position.
    /// Only places if buddy is not already alive.
    /// </summary>
    public static void PlaceBuddy()
    {
        if (!BuddyAlive)
        {
            var floors = new List<(int x, int y)>();
            for (int i = 0; i < MapWidth * MapHeight; i++)
            {
                int x = i % MapWidth, y = i / MapWidth;
                if (Map[i].Type == TileType.Floor)
                {
                    int dist = Math.Abs(x - Player.X) + Math.Abs(y - Player.Y);
                    if (dist >= 2 && dist <= 4) floors.Add((x, y));
                }
            }
            if (floors.Count > 0)
            {
                var pos = floors[Rng.Next(floors.Count)];
                Buddy.X = pos.x;
                Buddy.Y = pos.y;
                Buddy.HP = Buddy.MaxHP;
                BuddyAlive = true;
                StatusMessage = "Buddy joined your quest!";
            }
        }
    }

    /// <summary>
    /// Checks if coordinates are within map bounds.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>True if in bounds, false otherwise.</returns>
    public static bool IsInBounds(int x, int y) => x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;

    /// <summary>
    /// Converts 2D coordinates to 1D array index.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>Array index.</returns>
    public static int Index(int x, int y) => y * MapWidth + x;

    /// <summary>
    /// Gets a tile at the specified coordinates.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>Tile at position, or wall tile if out of bounds.</returns>
    public static Tile GetTile(int x, int y) => IsInBounds(x, y) ? Map[Index(x, y)] : new Tile(TileType.Wall);

    /// <summary>
    /// Gets a reference to the tile at the specified coordinates.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>Reference to the tile.</returns>
    public static ref Tile TileRef(int x, int y) => ref Map[Index(x, y)];

    /// <summary>
    /// Ends the player's turn: updates FOV, runs buddy AI, then enemy turn.
    /// </summary>
    public static void PlayerTurnEnd()
    {
        ComputeFOV();
        Buddy.AI();
        EnemyTurn();
    }

    /// <summary>
    /// Executes AI for all visible enemies.
    /// AI type 0: pursue player directly.
    /// AI type 1: random movement with occasional pursuit.
    /// </summary>
    public static void EnemyTurn()
    {
        for (int i = 0; i < EnemyCount; i++)
        {
            ref var e = ref Enemies[i];
            if (e.HP <= 0) continue;
            if (!Visible[e.X, e.Y]) continue;

            int dx = Math.Sign(Player.X - e.X);
            int dy = Math.Sign(Player.Y - e.Y);

            if (e.AIType == 0)
            {
                int ex = e.X + dx, ey = e.Y;
                if (IsInBounds(ex, ey) && Map[Index(ex, ey)].Type == TileType.Floor && !(ex == Player.X && ey == Player.Y))
                {
                    bool blocked = false;
                    for (int j = 0; j < EnemyCount; j++)
                        if (j != i && Enemies[j].HP > 0 && Enemies[j].X == ex && Enemies[j].Y == ey) { blocked = true; break; }
                    if (!blocked) { e.X = ex; e.Y = ey; continue; }
                }
                ex = e.X; ey = e.Y + dy;
                if (IsInBounds(ex, ey) && Map[Index(ex, ey)].Type == TileType.Floor && !(ex == Player.X && ey == Player.Y))
                {
                    bool blocked = false;
                    for (int j = 0; j < EnemyCount; j++)
                        if (j != i && Enemies[j].HP > 0 && Enemies[j].X == ex && Enemies[j].Y == ey) { blocked = true; break; }
                    if (!blocked) { e.X = ex; e.Y = ey; }
                }
            }
            else
            {
                if (Rng.Next(3) == 0)
                {
                    int[] dirs = { -1, 0, 1 };
                    int rdx = dirs[Rng.Next(3)], rdy = dirs[Rng.Next(3)];
                    int ex2 = e.X + rdx, ey2 = e.Y + rdy;
                    if (IsInBounds(ex2, ey2) && Map[Index(ex2, ey2)].Type == TileType.Floor && !(ex2 == Player.X && ey2 == Player.Y))
                    {
                        bool blocked = false;
                        for (int j = 0; j < EnemyCount; j++)
                            if (j != i && Enemies[j].HP > 0 && Enemies[j].X == ex2 && Enemies[j].Y == ey2) { blocked = true; break; }
                        if (!blocked) { e.X = ex2; e.Y = ey2; }
                    }
                }
            }

            if (Math.Abs(e.X - Player.X) <= 1 && Math.Abs(e.Y - Player.Y) <= 1 && !(e.X == Player.X && e.Y == Player.Y))
            {
                Attack(e, Player);
                if (Player.HP <= 0)
                {
                    StatusMessage = "You have been slain!";
                    Running = false;
                }
            }
        }
    }

    /// <summary>
    /// Calculates and applies damage from attacker to defender.
    /// Damage = attacker.Attack - defender.Defense + random(-1 to 1).
    /// </summary>
    /// <param name="attacker">The attacking entity.</param>
    /// <param name="defender">The defending entity.</param>
    public static void Attack(Entity attacker, Entity defender)
    {
        int damage = Math.Max(1, attacker.AttackPower - defender.Defense + Rng.Next(3) - 1);
        defender.HP -= damage;
        StatusMessage = $"{attacker.Name} hits {defender.Name} for {damage} damage!";
    }

    /// <summary>
    /// Levels up the player: increases stats and reduces XP.
    /// </summary>
    public static void LevelUp()
    {
        Player.Level++;
        Player.XP -= Player.XPToLevel;
        Player.XPToLevel = Player.Level * 15;
        Player.MaxHP += 5;
        Player.HP = Player.MaxHP;
        Player.AttackPower += 1;
        StatusMessage = $"Level up! Now level {Player.Level}!";
    }

    /// <summary>
    /// Handles item pickup with appropriate effects based on item type.
    /// </summary>
    /// <param name="item">The item to pick up.</param>
    public static void PickupItem(Item item)
    {
        switch (item.Type)
        {
            case ItemType.HealthPotion:
                int heal = Rng.Next(5, 15) + DungeonLevel;
                Player.HP = Math.Min(Player.MaxHP, Player.HP + heal);
                StatusMessage = $"Potion heals {heal} HP!";
                break;
            case ItemType.Scroll:
                Player.AttackPower += 1;
                StatusMessage = "Scroll of Power! Attack +1!";
                break;
            case ItemType.Treasure:
                int gold = Rng.Next(5, 20) * DungeonLevel;
                Player.Gold += gold;
                StatusMessage = $"Found {gold} gold!";
                break;
            case ItemType.Armor:
                Player.Defense += 1;
                StatusMessage = "Armor found! Defense +1!";
                break;
        }
        item.Active = false;
    }

    /// <summary>
    /// Computes field of view using raycasting from player position.
    /// Marks tiles as visible if they're within radius and not behind walls.
    /// </summary>
    public static void ComputeFOV()
    {
        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                Visible[x, y] = false;

        int radius = MaxFovRadius;
        for (int angle = 0; angle < 360; angle += 1)
        {
            double rad = angle * Math.PI / 180.0;
            double dx = Math.Cos(rad);
            double dy = Math.Sin(rad);
            double cx = Player.X + 0.5;
            double cy = Player.Y + 0.5;
            for (int step = 0; step < radius; step++)
            {
                int tx = (int)cx;
                int ty = (int)cy;
                if (!IsInBounds(tx, ty)) break;
                Visible[tx, ty] = true;
                Explored[tx, ty] = true;
                if (Map[Index(tx, ty)].Type == TileType.Wall) break;
                cx += dx;
                cy += dy;
            }
        }
    }
}

/// <summary>
/// Windows Forms window containing the game canvas and UI panel.
/// Handles input, rendering, and game loop via timer.
/// </summary>
public class GameForm : Form
{
    private readonly SKControl _canvasView;
    private readonly System.Windows.Forms.Timer _updateTimer;

    /// <summary>
    /// Initializes the game form with window properties and controls.
    /// </summary>
    public GameForm()
    {
        Text = "MicroRoguelike";
        ClientSize = new System.Drawing.Size(Game.MapWidth * Game.TileSize + Game.UIPanelWidth, Game.MapHeight * Game.TileSize + 32);
        DoubleBuffered = true;
        KeyPreview = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        _canvasView = new SKControl { Dock = DockStyle.Fill };
        Controls.Add(_canvasView);

        _canvasView.PaintSurface += OnPaintSurface;
        KeyDown += OnKeyDown;

        _updateTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _updateTimer.Tick += (s, ev) =>
        {
            _canvasView.Invalidate();
            Application.DoEvents();
        };
    }

    /// <summary>
    /// Loads the game: initializes state and starts the update timer.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Game.Init();
        _updateTimer.Start();
    }

    /// <summary>
    /// Main rendering method: draws map, entities, and UI panel.
    /// </summary>
    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        for (int i = 0; i < Game.MapWidth * Game.MapHeight; i++)
        {
            int tx = i % Game.MapWidth;
            int ty = i / Game.MapWidth;

            if (Game.Visible[tx, ty])
            {
                DrawTile(canvas, tx, ty, Game.Map[i]);
            }
            else if (Game.Explored[tx, ty])
            {
                DrawTileExplored(canvas, tx, ty, Game.Map[i]);
            }
        }

        for (int i = 0; i < Game.ItemCount; i++)
        {
            var item = Game.Items[i];
            if (item.Active && Game.Visible[item.X, item.Y])
            {
                DrawItem(canvas, item);
            }
        }

        for (int i = 0; i < Game.EnemyCount; i++)
        {
            var enemy = Game.Enemies[i];
            if (enemy.HP > 0 && Game.Visible[enemy.X, enemy.Y])
            {
                DrawEnemy(canvas, enemy);
            }
        }

        DrawPlayer(canvas);
        if (Game.BuddyAlive && Game.Buddy.HP > 0)
            DrawBuddy(canvas);
        DrawUIPanel(canvas);

        canvas.Flush();
    }

    /// <summary>
    /// Draws a visible tile with appropriate appearance (wall, floor, or stairs).
    /// </summary>
    private static void DrawTile(SKCanvas canvas, int x, int y, Tile tile)
    {
        int px = x * Game.TileSize;
        int py = y * Game.TileSize;

        if (tile.Type == TileType.Wall)
        {
            var paint = new SKPaint { Color = new SKColor(0xFF555555), IsAntialias = false };
            canvas.DrawRect(new SKRect(px, py, px + Game.TileSize, py + Game.TileSize), paint);
            for (int dy = 4; dy < Game.TileSize; dy += 8)
                canvas.DrawLine(px, py + dy, px + Game.TileSize, py + dy, new SKPaint { Color = new SKColor(0xFF808080), IsAntialias = false });
            for (int dx = 4; dx < Game.TileSize; dx += 8)
                canvas.DrawLine(px + dx, py, px + dx, py + Game.TileSize, new SKPaint { Color = new SKColor(0xFF808080), IsAntialias = false });
        }
        else if (tile.ContainsStairs)
        {
            var paint = new SKPaint { Color = new SKColor(0xFF8B4513), IsAntialias = false };
            canvas.DrawRect(new SKRect(px, py, px + Game.TileSize, py + Game.TileSize), paint);
            for (int stairY = py + 8; stairY < py + Game.TileSize - 4; stairY += 8)
                canvas.DrawLine(px + 8, stairY, px + Game.TileSize - 8, stairY, new SKPaint { Color = new SKColor(0xFF5D2906), IsAntialias = false, StrokeWidth = 1 });
        }
        else
        {
            var paint = new SKPaint { Color = new SKColor(0xFF2A2A2A), IsAntialias = false };
            canvas.DrawRect(new SKRect(px, py, px + Game.TileSize, py + Game.TileSize), paint);
            for (int dy = 0; dy < Game.TileSize; dy += 8)
            {
                for (int dx = 0; dx < Game.TileSize; dx += 8)
                {
                    if ((dx + dy) % 16 < 8)
                        canvas.DrawPoint(px + dx + 4, py + dy + 4, new SKColor(0xFF1A1A1A));
                }
            }
        }
    }

    /// <summary>
    /// Draws an explored but not currently visible tile (dimmed appearance).
    /// </summary>
    private static void DrawTileExplored(SKCanvas canvas, int x, int y, Tile tile)
    {
        int px = x * Game.TileSize;
        int py = y * Game.TileSize;

        if (tile.Type == TileType.Wall)
        {
            for (int dy = 0; dy < Game.TileSize; dy += 8)
            {
                for (int dx = 0; dx < Game.TileSize; dx += 8)
                {
                    if ((dx + dy) % 16 < 8)
                        canvas.DrawPoint(px + dx, py + dy, new SKColor(0xFF404040));
                }
            }
        }
        else
        {
            var paint = new SKPaint { Color = new SKColor(0xFF2A2A2A), IsAntialias = false };
            canvas.DrawRect(new SKRect(px, py, px + Game.TileSize, py + Game.TileSize), paint);
            for (int dy = 0; dy < Game.TileSize; dy += 8)
            {
                for (int dx = 0; dx < Game.TileSize; dx += 8)
                {
                    if ((dx + dy) % 16 < 8)
                        canvas.DrawPoint(px + dx + 4, py + dy + 4, new SKColor(0xFF1A1A1A));
                }
            }
        }
    }

    /// <summary>
    /// Draws the player character as a lime crosshair.
    /// </summary>
    private static void DrawPlayer(SKCanvas canvas)
    {
        int px = Game.Player.X * Game.TileSize;
        int py = Game.Player.Y * Game.TileSize;
        var paint = new SKPaint { Color = SKColors.Lime, IsAntialias = false, Style = SKPaintStyle.Fill };
        
        for (int dy = 0; dy < Game.TileSize; dy++)
        {
            for (int dx = 0; dx < Game.TileSize; dx++)
            {
                if ((dx == 3 || dx == 4) || (dy == 3 || dy == 4) || (dx >= 2 && dx <= 5 && dy >= 2 && dy <= 5))
                    canvas.DrawPoint(px + dx, py + dy, paint);
            }
        }
    }

    /// <summary>
    /// Draws an enemy with color coding based on type.
    /// </summary>
    private static void DrawEnemy(SKCanvas canvas, Enemy e)
    {
        int px = e.X * Game.TileSize;
        int py = e.Y * Game.TileSize;

        SKColor color = e.Glyph switch
        {
            'r' => new SKColor(0xFF8B4513),
            'g' => SKColors.Green,
            'o' => SKColors.Red,
            'O' => new SKColor(0xFF50C850),
            's' => new SKColor(0xFFAA00AA),
            'M' => new SKColor(0xFF8000FF),
            'w' => new SKColor(0xFF4040FF),
            'B' => new SKColor(0xFFFF0000),
            'k' => SKColors.White,
            'T' => new SKColor(0xFF800080),
            'D' => SKColors.Red,
            '&' => new SKColor(0xFFFF8000),
            _ => SKColors.White
        };

        var paint = new SKPaint { Color = color, IsAntialias = false, Style = SKPaintStyle.Fill };

        for (int dy = 2; dy < Game.TileSize - 2; dy++)
        {
            for (int dx = 2; dx < Game.TileSize - 2; dx++)
            {
                if (dx == 3 || dx == 4 || dy == 3 || dy == 4)
                    canvas.DrawPoint(px + dx, py + dy, paint);
                if (dx >= 2 && dx <= 5 && dy >= 2 && dy <= 5)
                    canvas.DrawPoint(px + dx, py + dy, paint);
            }
        }

        canvas.DrawPoint(px + 3, py + 1, paint);
        canvas.DrawPoint(px + 4, py + 1, paint);
        canvas.DrawPoint(px + 2, py + 2, paint);
        canvas.DrawPoint(px + 5, py + 2, paint);
    }

    /// <summary>
    /// Draws an item with color coding based on type.
    /// </summary>
    private static void DrawItem(SKCanvas canvas, Item item)
    {
        int px = item.X * Game.TileSize;
        int py = item.Y * Game.TileSize;

        SKColor color = item.Type switch
        {
            ItemType.HealthPotion => SKColors.Red,
            ItemType.Scroll => SKColors.Yellow,
            ItemType.Treasure => new SKColor(0xFFD4AF37),
            ItemType.Armor => SKColors.Cyan,
            _ => SKColors.White
        };

        var paint = new SKPaint { Color = color, IsAntialias = false, Style = SKPaintStyle.Fill };

        switch (item.Type)
        {
            case ItemType.HealthPotion:
                canvas.DrawPoint(px + 4, py + 2, paint);
                canvas.DrawPoint(px + 3, py + 3, paint);
                canvas.DrawPoint(px + 4, py + 3, paint);
                canvas.DrawPoint(px + 5, py + 3, paint);
                canvas.DrawPoint(px + 2, py + 4, paint);
                canvas.DrawPoint(px + 3, py + 4, paint);
                canvas.DrawPoint(px + 4, py + 4, paint);
                canvas.DrawPoint(px + 5, py + 4, paint);
                canvas.DrawPoint(px + 6, py + 4, paint);
                canvas.DrawPoint(px + 4, py + 5, paint);
                canvas.DrawPoint(px + 4, py + 6, paint);
                break;
            case ItemType.Scroll:
                for (int dx = 2; dx <= 5; dx++)
                    for (int dy = 2; dy <= 5; dy++)
                        canvas.DrawPoint(px + dx, py + dy, paint);
                break;
            case ItemType.Treasure:
                canvas.DrawPoint(px + 2, py + 3, paint);
                canvas.DrawPoint(px + 3, py + 2, paint);
                canvas.DrawPoint(px + 4, py + 2, paint);
                canvas.DrawPoint(px + 5, py + 3, paint);
                canvas.DrawPoint(px + 3, py + 4, paint);
                canvas.DrawPoint(px + 4, py + 4, paint);
                canvas.DrawPoint(px + 2, py + 5, paint);
                canvas.DrawPoint(px + 5, py + 5, paint);
                break;
            case ItemType.Armor:
                canvas.DrawPoint(px + 2, py + 2, paint);
                canvas.DrawPoint(px + 3, py + 2, paint);
                canvas.DrawPoint(px + 4, py + 2, paint);
                canvas.DrawPoint(px + 5, py + 2, paint);
                canvas.DrawPoint(px + 2, py + 3, paint);
                canvas.DrawPoint(px + 5, py + 3, paint);
                canvas.DrawPoint(px + 2, py + 4, paint);
                canvas.DrawPoint(px + 5, py + 4, paint);
                canvas.DrawPoint(px + 3, py + 5, paint);
                canvas.DrawPoint(px + 4, py + 5, paint);
                break;
        }
    }

    /// <summary>
    /// Draws the buddy companion in blue.
    /// </summary>
    private static void DrawBuddy(SKCanvas canvas)
    {
        int px = Game.Buddy.X * Game.TileSize;
        int py = Game.Buddy.Y * Game.TileSize;
        var paint = new SKPaint { Color = SKColors.Blue, IsAntialias = false, Style = SKPaintStyle.Fill };

        for (int dy = 2; dy < Game.TileSize - 2; dy++)
        {
            for (int dx = 2; dx < Game.TileSize - 2; dx++)
            {
                if (dx == 3 || dx == 4 || dy == 3 || dy == 4)
                    canvas.DrawPoint(px + dx, py + dy, paint);
                if (dx >= 2 && dx <= 5 && dy >= 2 && dy <= 5)
                    canvas.DrawPoint(px + dx, py + dy, paint);
            }
        }

        canvas.DrawPoint(px + 3, py + 1, paint);
        canvas.DrawPoint(px + 4, py + 1, paint);
        canvas.DrawPoint(px + 2, py + 2, paint);
        canvas.DrawPoint(px + 5, py + 2, paint);
    }

    /// <summary>
    /// Draws the right-side UI panel with player stats and nearby entity/item lists.
    /// </summary>
    private static void DrawUIPanel(SKCanvas canvas)
    {
        int panelX = Game.MapWidth * Game.TileSize;
        var panelPaint = new SKPaint { Color = new SKColor(0xFF333333), IsAntialias = false, Style = SKPaintStyle.Fill };
        canvas.DrawRect(new SKRect(panelX, 0, panelX + Game.UIPanelWidth, Game.MapHeight * Game.TileSize), panelPaint);

        int yOffset = 5;

        var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, TextSize = 12 };
        var titlePaint = new SKPaint { Color = SKColors.Yellow, IsAntialias = true, TextSize = 14 };

        canvas.DrawText("PLAYER", panelX + 10, yOffset + 12, titlePaint);
        yOffset += 25;

        string stats = $"HP: {Game.Player.HP}/{Game.Player.MaxHP}";
        canvas.DrawText(stats, panelX + 10, yOffset + 12, textPaint);
        yOffset += 18;

        string level = $"Lvl: {Game.Player.Level} XP: {Game.Player.XP}/{Game.Player.XPToLevel}";
        canvas.DrawText(level, panelX + 10, yOffset + 12, textPaint);
        yOffset += 18;

        string gold = $"Gold: {Game.Player.Gold}";
        canvas.DrawText(gold, panelX + 10, yOffset + 12, textPaint);
        yOffset += 25;

        canvas.DrawText("NEARBY ENEMIES", panelX + 10, yOffset + 12, titlePaint);
        yOffset += 25;

        for (int i = 0; i < Game.EnemyCount; i++)
        {
            ref var e = ref Game.Enemies[i];
            if (e.HP > 0 && Game.Visible[e.X, e.Y] && Math.Abs(e.X - Game.Player.X) <= 5 && Math.Abs(e.Y - Game.Player.Y) <= 5)
            {
                canvas.DrawText($"{e.Name} HP: {e.HP}/{e.MaxHP}", panelX + 10, yOffset + 12, textPaint);
                yOffset += 16;

                int barWidth = 150;
                var backPaint = new SKPaint { Color = new SKColor(0xFF111111), IsAntialias = false, Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(panelX + 10, yOffset + 4, panelX + 10 + barWidth, yOffset + 10), backPaint);

                int filled = Math.Max(1, (int)(e.HP * barWidth / e.MaxHP));
                SKColor hpColor = e.HP > e.MaxHP / 2 ? SKColors.Lime : e.HP > e.MaxHP / 4 ? SKColors.Yellow : SKColors.Red;
                var fillPaint = new SKPaint { Color = hpColor, IsAntialias = false, Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(panelX + 10, yOffset + 4, panelX + 10 + filled, yOffset + 10), fillPaint);

                yOffset += 20;
                if (yOffset > Game.MapHeight * Game.TileSize - 120) break;
            }
        }

        yOffset += 10;
        canvas.DrawText("NEARBY ITEMS", panelX + 10, yOffset + 12, titlePaint);
        yOffset += 25;

        for (int i = 0; i < Game.ItemCount; i++)
        {
            var item = Game.Items[i];
            if (item.Active && Game.Visible[item.X, item.Y] && Math.Abs(item.X - Game.Player.X) <= 5 && Math.Abs(item.Y - Game.Player.Y) <= 5)
            {
                string itemName = item.Type switch
                {
                    ItemType.HealthPotion => "Health Potion",
                    ItemType.Scroll => "Scroll",
                    ItemType.Treasure => "Treasure",
                    ItemType.Armor => "Armor",
                    _ => "Item"
                };
                canvas.DrawText(itemName, panelX + 10, yOffset + 12, textPaint);
                yOffset += 18;
                if (yOffset > Game.MapHeight * Game.TileSize - 40) break;
            }
        }

        yOffset = Game.MapHeight * Game.TileSize - 25;
        var smallTextPaint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true, TextSize = 10 };
        canvas.DrawText(Game.StatusMessage, panelX + 10, yOffset, smallTextPaint);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        int nx = Game.Player.X, ny = Game.Player.Y;
        switch (e.KeyCode)
        {
            case Keys.W or Keys.Up: ny--; break;
            case Keys.S or Keys.Down: ny++; break;
            case Keys.A or Keys.Left: nx--; break;
            case Keys.D or Keys.Right: nx++; break;
            case Keys.Escape: Game.Running = false; Close(); return;
            default: return;
        }

        if (!Game.IsInBounds(nx, ny)) return;

        if (Game.Map[Game.Index(nx, ny)].ContainsStairs)
        {
            Game.DungeonLevel++;
            DungeonGenerator.GenerateDungeon();
            DungeonGenerator.PlacePlayer();
            DungeonGenerator.PlaceEnemies();
            Game.PlaceBuddy();
            DungeonGenerator.PlaceItems();
            Game.PlaceStairs();
            Game.ComputeFOV();
            Game.StatusMessage = $"Descended to level {Game.DungeonLevel}.";
            return;
        }

        for (int i = 0; i < Game.EnemyCount; i++)
        {
            if (Game.Enemies[i].HP > 0 && Game.Enemies[i].X == nx && Game.Enemies[i].Y == ny)
            {
                Game.Attack(Game.Player, Game.Enemies[i]);
                if (Game.Enemies[i].HP <= 0)
                {
                    Game.StatusMessage = $"Killed {Game.Enemies[i].Name}! +{Game.Enemies[i].XPGain} XP.";
                    Game.Player.XP += Game.Enemies[i].XPGain;
                    if (Game.Player.XP >= Game.Player.XPToLevel)
                        Game.LevelUp();
                }
                Game.PlayerTurnEnd();
                return;
            }
        }

        if (Game.Map[Game.Index(nx, ny)].Type == TileType.Wall) return;

        for (int i = 0; i < Game.ItemCount; i++)
        {
            if (Game.Items[i].Active && Game.Items[i].X == nx && Game.Items[i].Y == ny)
            {
                Game.PickupItem(Game.Items[i]);
            }
        }

        Game.Player.X = nx;
        Game.Player.Y = ny;
        Game.PlayerTurnEnd();
    }
}