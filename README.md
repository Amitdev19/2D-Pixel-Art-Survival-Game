# MicroRoguelike

A lightweight roguelike dungeon crawler built with C# and SkiaSharp.

## Overview

MicroRoguelike is a console-style dungeon exploration game where you navigate procedurally generated dungeons, fight enemies, collect items, and descend to deeper levels. The game features:

- Procedurally generated dungeon layouts using a room-and-corridor algorithm
- Turn-based combat with multiple enemy types
- Companion NPC (buddy) system
- Level progression with XP and stat increases
- Field of view with fog of war exploration

## Build Instructions

### Prerequisites
- .NET 8.0 SDK or later
- Windows (for Windows Forms)

### Building
```bash
dotnet build
```

### Running
```bash
dotnet run
```

Or open the project in Visual Studio and run with debugging.

## How to Play

- **Movement**: WASD or Arrow Keys
- **Descend**: Move onto the brown staircase tile to go to the next dungeon level
- **Combat**: Move into enemies to attack them
- **Items**: Move onto items to pick them up
- **Exit**: Escape key to quit

### Game Elements

| Glyph | Entity | Description |
|-------|--------|-------------|
| @ | Player | Your hero character |
| b | Buddy | Companion NPC that follows and assists |
| r/g/o/k/T/W/D/&/s/O/M/w/B | Enemies | Various enemies with different stats |
| Health Potion (red) | Item | Restores HP |
| Scroll (yellow) | Item | Increases attack power |
| Treasure (gold) | Item | Adds gold |
| Armor (cyan) | Item | Increases defense |

### Stats

- **HP**: Current and maximum hit points
- **Lvl**: Character level
- **XP**: Experience progress toward next level
- **Gold**: Currency collected

## Code Structure

```
MicroRoguelike/
├── Program.cs          # Entry point, launches GameForm
├── Game.cs             # Core game logic, state, and rendering
├── Entities.cs         # Entity classes and data structures
├── DungeonGenerator.cs # Procedural dungeon generation
```

### Key Classes

- **Game**: Static manager for game state, combat, FOV, and main game loop
- **GameForm**: Windows Forms window with SkiaSharp rendering
- **Entity**: Base class for player, enemies, and buddies
- **Player**: Player character with level, XP, and gold
- **Enemy**: Combat entities with AI behavior types
- **Buddy**: Companion NPC with follow and assist AI
- **Tile**: Map tile with type and stairs flag
- **Item**: Collectible items with various effects
- **DungeonGenerator**: Procedural dungeon creation

## Future Development

Potential improvements for future versions:

1. **Enhanced Combat**: Add spell system, special abilities, equipment/items
2. **AI Improvements**: More sophisticated enemy behaviors and pathfinding
3. **Visual Polish**: Better tile graphics, animations, and UI enhancements
4. **Sound**: Add sound effects and background music
5. **Save System**: Persistent character progression across sessions
6. **Additional Content**: New enemy types, items, and dungeon features
7. **Difficulty Scaling**: Adjustable difficulty settings
8. **Multiplayer**: Potential co-op mode