namespace MicroRoguelike;

/// <summary>
/// Types of tiles that can exist in the game world.
/// </summary>
public enum TileType { Wall, Floor }

/// <summary>
/// Types of entities that can be spawned in the dungeon.
/// </summary>
public enum EntityType { None, Player, Enemy, Item, Wall, Floor }

/// <summary>
/// Represents a single map tile with its type and optional features.
/// </summary>
public struct Tile
{
    /// <summary>
    /// The visual and collision type of this tile.
    /// </summary>
    public TileType Type;
    /// <summary>
    /// Whether this tile contains a staircase to another level.
    /// </summary>
    public bool ContainsStairs;

    /// <summary>
    /// Initializes a new tile with the specified type.
    /// </summary>
    /// <param name="type">The tile type to set.</param>
    public Tile(TileType type) { Type = type; ContainsStairs = false; }
}

/// <summary>
/// Base class for all entities in the game, including player, enemies, and buddies.
/// Provides position, health stats, combat capabilities, and visual representation.
/// </summary>
public class Entity
{
    /// <summary>
    /// X coordinate on the map grid.
    /// </summary>
    public int X, Y;
    /// <summary>
    /// Current hit points.
    /// </summary>
    public int HP, MaxHP;
    /// <summary>
    /// Attack power used in combat calculations.
    /// </summary>
    public int AttackPower;
    /// <summary>
    /// Defense value that reduces incoming damage.
    /// </summary>
    public int Defense;
    /// <summary>
    /// Display name of the entity.
    /// </summary>
    public string Name;
    /// <summary>
    /// Character glyph used for visual representation.
    /// </summary>
    public char Glyph;

    /// <summary>
    /// Initializes a new entity with full properties.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="hp">Hit points.</param>
    /// <param name="atk">Attack power.</param>
    /// <param name="def">Defense value.</param>
    /// <param name="glyph">Visual character.</param>
    public Entity(string name, int hp, int atk, int def, char glyph)
    {
        Name = name; HP = MaxHP = hp; AttackPower = atk; Defense = def; Glyph = glyph;
    }

    /// <summary>
    /// Initializes a default entity with placeholder values.
    /// </summary>
    public Entity() { Name = ""; HP = 1; MaxHP = 1; AttackPower = 0; Defense = 0; Glyph = '?'; }
}

/// <summary>
/// Player character with progression systems including leveling, XP, and gold.
/// Extends Entity with player-specific stats and abilities.
/// </summary>
public class Player : Entity
{
    /// <summary>
    /// Current character level (starts at 1).
    /// </summary>
    public int Level = 1;
    /// <summary>
    /// Current experience points.
    /// </summary>
    public int XP = 0;
    /// <summary>
    /// XP required to reach the next level.
    /// </summary>
    public int XPToLevel = 10;
    /// <summary>
    /// Accumulated gold for purchasing items.
    /// </summary>
    public int Gold = 0;

    /// <summary>
    /// Creates a player with default stats: 30 HP, 5 attack, 2 defense.
    /// </summary>
    public Player() : base("Hero", 30, 5, 2, '@') { }
}

/// <summary>
/// All possible enemy types with varying difficulty and characteristics.
/// Each type has different stats, abilities, and XP rewards.
/// </summary>
public enum EnemyType { Rat, Goblin, Orc, Skeleton, Troll, Wraith, Demon, Dragon, Spider, Ooze, Mage, Warrior, Boss }

/// <summary>
/// Enemy entity with AI behavior, XP rewards, and special abilities.
/// Extends Entity with enemy-specific properties like AI type and combat variants.
/// </summary>
public class Enemy : Entity
{
    /// <summary>
    /// AI behavior type: 0 = pursue player, 1 = random movement.
    /// </summary>
    public int AIType;
    /// <summary>
    /// Experience points gained when killing this enemy.
    /// </summary>
    public int XPGain;
    /// <summary>
    /// Specific enemy subtype affecting appearance and behavior.
    /// </summary>
    public EnemyType SubType;
    /// <summary>
    /// Chance (0-100) for poison attack to trigger.
    /// </summary>
    public int PoisonChance;
    /// <summary>
    /// Number of entities to split into when killed (for Ooze).
    /// </summary>
    public int SplitCount;
    /// <summary>
    /// Ranged attack power (0 if melee only).
    /// </summary>
    public int RangedAttack;

    /// <summary>
    /// Creates a default enemy with minimal stats.
    /// </summary>
    public Enemy() : base() { }

    /// <summary>
    /// Creates an enemy with basic properties including AI and XP.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="hp">Hit points.</param>
    /// <param name="atk">Attack power.</param>
    /// <param name="def">Defense.</param>
    /// <param name="glyph">Visual character.</param>
    /// <param name="ai">AI type (0=pursue, 1=random).</param>
    /// <param name="xp">XP reward.</param>
    public Enemy(string name, int hp, int atk, int def, char glyph, int ai, int xp) : base(name, hp, atk, def, glyph)
    {
        AIType = ai; XPGain = xp; SubType = EnemyType.Rat;
    }

    /// <summary>
    /// Creates a fully-configured enemy with all properties.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="hp">Hit points.</param>
    /// <param name="atk">Attack power.</param>
    /// <param name="def">Defense.</param>
    /// <param name="glyph">Visual character.</param>
    /// <param name="ai">AI type.</param>
    /// <param name="xp">XP reward.</param>
    /// <param name="type">Enemy subtype.</param>
    /// <param name="poisonChance">Poison chance percentage.</param>
    /// <param name="splitCount">Split count for Ooze.</param>
    /// <param name="rangedAtk">Ranged attack power.</param>
    public Enemy(string name, int hp, int atk, int def, char glyph, int ai, int xp, EnemyType type, int poisonChance = 0, int splitCount = 0, int rangedAtk = 0) : base(name, hp, atk, def, glyph)
    {
        AIType = ai; XPGain = xp; SubType = type; PoisonChance = poisonChance; SplitCount = splitCount; RangedAttack = rangedAtk;
    }
}

/// <summary>
/// Companion NPC that follows the player and assists in combat.
/// Buddies have lower stats than player but can attack nearby enemies.
/// </summary>
public class Buddy : Entity
{
    /// <summary>
    /// Experience points this buddy contributes when following.
    /// </summary>
    public int XPGain;

    /// <summary>
    /// Creates a default buddy with 15 HP, 4 attack, 1 defense.
    /// </summary>
    public Buddy() : base("Buddy", 15, 4, 1, 'b') { }

    /// <summary>
    /// Creates a buddy with custom properties.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="hp">Hit points.</param>
    /// <param name="atk">Attack power.</param>
    /// <param name="def">Defense.</param>
    /// <param name="glyph">Visual character.</param>
    /// <param name="xp">XP gain value.</param>
    public Buddy(string name, int hp, int atk, int def, char glyph, int xp) : base(name, hp, atk, def, glyph)
    {
        XPGain = xp;
    }

    /// <summary>
    /// Moves the buddy one step closer to the player using Manhattan distance.
    /// Tries horizontal movement first, then vertical.
    /// </summary>
    public void FollowPlayer()
    {
        int dx = Math.Sign(Game.Player.X - X);
        int dy = Math.Sign(Game.Player.Y - Y);

        int tx = X + dx, ty = Y;
        if (Game.IsInBounds(tx, ty) && Game.GetTile(tx, ty).Type == TileType.Floor && !(tx == Game.Player.X && ty == Game.Player.Y))
        {
            X = tx; Y = ty;
            return;
        }
        tx = X; ty = Y + dy;
        if (Game.IsInBounds(tx, ty) && Game.GetTile(tx, ty).Type == TileType.Floor && !(tx == Game.Player.X && ty == Game.Player.Y))
        {
            X = tx; Y = ty;
        }
    }

    /// <summary>
    /// Executes AI behavior: follow player and attack nearby enemies.
    /// </summary>
    public void AI()
    {
        if (!Game.BuddyAlive || HP <= 0) return;

        FollowPlayer();

        for (int i = 0; i < Game.EnemyCount; i++)
        {
            ref var e = ref Game.Enemies[i];
            if (e.HP > 0 && Math.Abs(e.X - X) <= 1 && Math.Abs(e.Y - Y) <= 1 && !(e.X == X && e.Y == Y))
            {
                Game.Attack(this, e);
            }
        }
    }
}

/// <summary>
/// Types of items that can be found in the dungeon.
/// </summary>
public enum ItemType { HealthPotion, Scroll, Treasure, Armor }

/// <summary>
/// Item that can be found and picked up by the player.
/// Contains position, type, and active state.
/// </summary>
public struct Item
{
    /// <summary>
    /// X coordinate on the map grid.
    /// </summary>
    public int X, Y;
    /// <summary>
    /// Type of item (determines effect when picked up).
    /// </summary>
    public ItemType Type;
    /// <summary>
    /// Whether this item is still on the map and available.
    /// </summary>
    public bool Active;

    /// <summary>
    /// Creates an inactive item placeholder.
    /// </summary>
    public Item() { Active = false; }

    /// <summary>
    /// Creates an active item at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="type">Item type.</param>
    public Item(int x, int y, ItemType type) { X = x; Y = y; Type = type; Active = true; }
}