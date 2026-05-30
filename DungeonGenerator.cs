namespace MicroRoguelike;

/// <summary>
/// Static dungeon generation class using room-and-corridor algorithm.
/// Creates procedurally generated dungeons with varying enemy and item placement.
/// </summary>
public static class DungeonGenerator
{
    /// <summary>
    /// Represents a rectangular room or area with boundaries and center point.
    /// </summary>
    private struct Rect
    {
        /// <summary>
        /// Left boundary X coordinate.
        /// </summary>
        public int X1, Y1, X2, Y2;
        /// <summary>
        /// X coordinate of the room center.
        /// </summary>
        public int CenterX => (X1 + X2) / 2;
        /// <summary>
        /// Y coordinate of the room center.
        /// </summary>
        public int CenterY => (Y1 + Y2) / 2;
    }

    /// <summary>
    /// Generates a complete dungeon: clears the map, creates rooms, and connects them with corridors.
    /// Uses a room-and-corridor algorithm with optional loop creation for variety.
    /// </summary>
    public static void GenerateDungeon()
    {
        for (int i = 0; i < Game.MapWidth * Game.MapHeight; i++)
            Game.Map[i] = new Tile(TileType.Wall);

        var rooms = new Rect[Game.MaxRooms];
        int roomCount = 0;
        Game.EnemyCount = 0;
        Game.ItemCount = 0;

        for (int attempt = 0; attempt < 100 && roomCount < Game.MaxRooms; attempt++)
        {
            int w = Game.Rng.Next(Game.MinRoomSize, Game.MaxRoomSize + 1);
            int h = Game.Rng.Next(Game.MinRoomSize, Game.MaxRoomSize + 1);
            int x = Game.Rng.Next(1, Game.MapWidth - w - 1);
            int y = Game.Rng.Next(1, Game.MapHeight - h - 1);

            var newRoom = new Rect { X1 = x, Y1 = y, X2 = x + w - 1, Y2 = y + h - 1 };

            bool overlap = false;
            for (int i = 0; i < roomCount; i++)
            {
                if (RectsOverlap(newRoom, rooms[i])) { overlap = true; break; }
            }
            if (overlap) continue;

            CarveRoom(newRoom);
            if (roomCount > 0)
            {
                CarveCorridor(
                    rooms[roomCount - 1].CenterX, rooms[roomCount - 1].CenterY,
                    newRoom.CenterX, newRoom.CenterY);
            }
            rooms[roomCount++] = newRoom;
        }

        for (int i = 0; i < roomCount / 2; i++)
        {
            int a = Game.Rng.Next(roomCount);
            int b = Game.Rng.Next(roomCount);
            if (a != b)
            {
                CarveCorridor(
                    rooms[a].CenterX, rooms[a].CenterY,
                    rooms[b].CenterX, rooms[b].CenterY);
            }
        }
    }

    /// <summary>
    /// Checks if two rectangles overlap (with optional 1-tile buffer).
    /// </summary>
    /// <param name="a">First rectangle.</param>
    /// <param name="b">Second rectangle.</param>
    /// <returns>True if rectangles overlap or are adjacent.</returns>
    private static bool RectsOverlap(Rect a, Rect b)
    {
        return a.X1 <= b.X2 + 1 && a.X2 >= b.X1 - 1 && a.Y1 <= b.Y2 + 1 && a.Y2 >= b.Y1 - 1;
    }

    /// <summary>
    /// Carves a room by setting all tiles within boundaries to floor.
    /// </summary>
    /// <param name="r">Room rectangle defining the area to carve.</param>
    private static void CarveRoom(Rect r)
    {
        for (int y = r.Y1; y <= r.Y2; y++)
            for (int x = r.X1; x <= r.X2; x++)
                Game.Map[Game.Index(x, y)] = new Tile(TileType.Floor);
    }

    /// <summary>
    /// Carves a corridor between two points using L-shaped path.
    /// </summary>
    /// <param name="x1">Start X coordinate.</param>
    /// <param name="y1">Start Y coordinate.</param>
    /// <param name="x2">End X coordinate.</param>
    /// <param name="y2">End Y coordinate.</param>
    private static void CarveCorridor(int x1, int y1, int x2, int y2)
    {
        if (Game.Rng.Next(2) == 0)
        {
            CarveHorizontal(x1, x2, y1);
            CarveVertical(y1, y2, x2);
        }
        else
        {
            CarveVertical(y1, y2, x1);
            CarveHorizontal(x1, x2, y2);
        }
    }

    /// <summary>
    /// Carves a horizontal corridor at the specified Y coordinate.
    /// </summary>
    /// <param name="x1">Start X coordinate.</param>
    /// <param name="x2">End X coordinate.</param>
    /// <param name="y">Y coordinate (row).</param>
    private static void CarveHorizontal(int x1, int x2, int y)
    {
        int min = Math.Min(x1, x2), max = Math.Max(x1, x2);
        for (int x = min; x <= max; x++)
            if (Game.IsInBounds(x, y))
                Game.Map[Game.Index(x, y)] = new Tile(TileType.Floor);
    }

    /// <summary>
    /// Carves a vertical corridor at the specified X coordinate.
    /// </summary>
    /// <param name="y1">Start Y coordinate.</param>
    /// <param name="y2">End Y coordinate.</param>
    /// <param name="x">X coordinate (column).</param>
    private static void CarveVertical(int y1, int y2, int x)
    {
        int min = Math.Min(y1, y2), max = Math.Max(y1, y2);
        for (int y = min; y <= max; y++)
            if (Game.IsInBounds(x, y))
                Game.Map[Game.Index(x, y)] = new Tile(TileType.Floor);
    }

    /// <summary>
    /// Places the player on the first available floor tile.
    /// </summary>
    public static void PlacePlayer()
    {
        for (int i = 0; i < Game.MapWidth * Game.MapHeight; i++)
        {
            if (Game.Map[i].Type == TileType.Floor)
            {
                Game.Player.X = i % Game.MapWidth;
                Game.Player.Y = i / Game.MapWidth;
                return;
            }
        }
    }

    /// <summary>
    /// Spawns enemies based on dungeon level using predefined templates.
    /// Enemy stats scale with dungeon level.
    /// </summary>
    public static void PlaceEnemies()
    {
        int lvl = Game.DungeonLevel;
        var templates = new (string name, int hp, int atk, int def, char glyph, int ai, int lvlReq, int xp, EnemyType type, int poisonChance, int splitCount, int rangedAtk)[]
        {
            ("Rat", 4, 1, 0, 'r', 1, 1, 3, EnemyType.Rat, 0, 0, 0),
            ("Goblin", 8, 3, 1, 'g', 0, 1, 5, EnemyType.Goblin, 0, 0, 0),
            ("Orc", 15, 5, 2, 'o', 0, 2, 8, EnemyType.Orc, 0, 0, 0),
            ("Skeleton", 12, 7, 3, 'k', 0, 3, 12, EnemyType.Skeleton, 0, 0, 0),
            ("Troll", 25, 8, 4, 'T', 0, 4, 20, EnemyType.Troll, 0, 0, 0),
            ("Wraith", 18, 10, 3, 'W', 0, 5, 30, EnemyType.Wraith, 0, 0, 0),
            ("Demon", 35, 12, 5, 'D', 0, 6, 50, EnemyType.Demon, 0, 0, 0),
            ("Dragon", 50, 15, 7, '&', 0, 8, 100, EnemyType.Dragon, 0, 0, 0),
            ("Spider", 6, 2, 1, 's', 0, 1, 4, EnemyType.Spider, 20, 0, 0),
            ("Ooze", 10, 3, 0, 'O', 1, 2, 6, EnemyType.Ooze, 0, 1, 0),
            ("Mage", 12, 4, 1, 'M', 0, 4, 15, EnemyType.Mage, 0, 0, 6),
            ("Warrior", 30, 6, 5, 'w', 0, 5, 25, EnemyType.Warrior, 0, 0, 0),
            ("Boss", 60, 10, 6, 'B', 0, 8, 100, EnemyType.Boss, 0, 0, 8),
        };

        Game.EnemyCount = Math.Min(Game.MaxEnemies, 4 + lvl * 2);

        for (int i = 0; i < Game.EnemyCount; i++)
        {
            var pos = GetRandomFloorPos();
            if (pos == (-1, -1)) continue;

            int count = 0;
            for (int t = 0; t < templates.Length; t++)
                if (templates[t].lvlReq <= lvl + 1) count++;
            if (count == 0) count = 1;

            int idx = Game.Rng.Next(count);
            int picked = 0;
            var pick = templates[0];
            for (int t = 0; t < templates.Length; t++)
            {
                if (templates[t].lvlReq <= lvl + 1)
                {
                    if (picked == idx) { pick = templates[t]; break; }
                    picked++;
                }
            }
            int bonus = lvl - 1;

            Game.Enemies[i] = new Enemy(
                pick.name,
                pick.hp + bonus * 2,
                pick.atk + bonus,
                pick.def + bonus / 2,
                pick.glyph,
                pick.ai,
                pick.xp + bonus,
                pick.type,
                pick.poisonChance,
                pick.splitCount,
                pick.rangedAtk);
        }
    }

    /// <summary>
    /// Places items on random floor tiles.
    /// Number of items scales with dungeon level.
    /// </summary>
    public static void PlaceItems()
    {
        Game.ItemCount = Game.Rng.Next(Game.MaxItems / 2, Game.MaxItems);

        for (int i = 0; i < Game.ItemCount; i++)
        {
            var pos = GetRandomFloorPos();
            if (pos == (-1, -1)) continue;

            var type = (ItemType)Game.Rng.Next(4);
            Game.Items[i] = new Item(pos.Item1, pos.Item2, type);
        }
    }

    /// <summary>
    /// Finds a random floor position that is not occupied by the player.
    /// </summary>
    /// <returns>Tuple of (x, y) coordinates, or (-1, -1) if no valid position found.</returns>
    private static (int, int) GetRandomFloorPos()
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int x = Game.Rng.Next(1, Game.MapWidth - 1);
            int y = Game.Rng.Next(1, Game.MapHeight - 1);
            if (Game.Map[Game.Index(x, y)].Type == TileType.Floor)
            {
                if (x == Game.Player.X && y == Game.Player.Y) continue;
                return (x, y);
            }
        }
        return (-1, -1);
    }
}