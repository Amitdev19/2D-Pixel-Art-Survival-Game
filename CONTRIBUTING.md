# Contributing to MicroRoguelike

Thank you for your interest in contributing to MicroRoguelike! This document provides guidelines for development and contribution.

## Development Setup

1. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/MicroRoguelike.git
   cd MicroRoguelike
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build**
   ```bash
   dotnet build
   ```

4. **Run**
   ```bash
   dotnet run
   ```

## Code Style Guidelines

### Naming Conventions
- **Classes**: PascalCase (e.g., `Player`, `GameForm`)
- **Methods**: PascalCase (e.g., `DrawTile`, `PlacePlayer`)
- **Variables**: camelCase (e.g., `playerX`, `enemyCount`)
- **Constants**: PascalCase (e.g., `MapWidth`, `TileSize`)
- **Private fields**: _camelCase with underscore prefix (e.g., `_canvasView`)

### Formatting
- Use 4 spaces for indentation
- Keep lines under 120 characters
- Use meaningful variable names
- Organize usings alphabetically

### Documentation
- Add XML documentation comments (`///`) to all public members
- Document parameters and return values
- Keep comments concise but informative

Example:
```csharp
/// <summary>
/// Places the player on the first available floor tile.
/// </summary>
public static void PlacePlayer()
{
    // Implementation
}
```

## How to Add New Content

### Adding a New Enemy Type

1. Add the enemy type to `EnemyType` enum in `Entities.cs`
2. Add enemy template data in `DungeonGenerator.PlaceEnemies()`
3. Add color/glyph mapping in `GameForm.DrawEnemy()`

### Adding a New Item Type

1. Add to `ItemType` enum in `Entities.cs`
2. Implement effect in `Game.PickupItem()`
3. Add visual representation in `GameForm.DrawItem()`

### Adding a New Tile Type

1. Add to `TileType` enum in `Entities.cs`
2. Update `GameForm.DrawTile()` to handle new appearance
3. Update FOV logic if needed

### Adding New Game Features

1. Define state fields in the `Game` static class if needed
2. Implement core logic in `Game` class methods
3. Add rendering in `GameForm` drawing methods
4. Wire up input handling in `OnKeyDown`

## Pull Request Process

1. Create a feature branch from `main`
2. Make your changes with clear commit messages
3. Add or update tests if applicable
4. Update documentation (README.md, code comments)
5. Submit a pull request with a clear description of changes

## Testing

Run the game and manually test all functionality. For automated tests:
```bash
dotnet test
```

## Questions?

Open an issue for discussion before starting major work.