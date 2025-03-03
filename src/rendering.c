#include "rendering.h"

// Load game textures
void LoadGameTextures(GameTextures *textures) {
    textures->tileset = LoadTexture("resources/32rogues_tiles.png");
    
    // Set up source rectangles for different tiles
    // These coordinates will need to be adjusted based on the actual tileset layout
    
    // Calculate the size of each tile in the tileset (assuming it's a grid)
    int tileWidth = textures->tileset.width / 16;  // Assuming 16 tiles per row
    int tileHeight = textures->tileset.height / 16; // Assuming 16 tiles per column
    
    // Floor tile (example position in tileset)
    textures->sources[TILE_FLOOR] = (Rectangle){ 0 * tileWidth, 0 * tileHeight, tileWidth, tileHeight };
    
    // Wall tile (example position in tileset)
    textures->sources[TILE_WALL] = (Rectangle){ 1 * tileWidth, 0 * tileHeight, tileWidth, tileHeight };
    
    // Door tile (example position in tileset)
    textures->sources[TILE_DOOR] = (Rectangle){ 2 * tileWidth, 0 * tileHeight, tileWidth, tileHeight };
}

// Unload game textures
void UnloadGameTextures(GameTextures *textures) {
    UnloadTexture(textures->tileset);
}

// Render the map
void RenderMap(GameMap *map, GameTextures *textures) {
    for (int x = 0; x < MAP_WIDTH; x++) {
        for (int y = 0; y < MAP_HEIGHT; y++) {
            TileType tile = map->tiles[x][y];
            
            if (tile != TILE_EMPTY) {
                // Draw tile based on type
                Rectangle source = textures->sources[tile];
                Rectangle dest = { x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE };
                
                DrawTexturePro(textures->tileset, source, dest, (Vector2){0, 0}, 0, WHITE);
                
                // Fallback simple rendering if texture issues
                if (textures->tileset.id == 0) {
                    Color tileColor = BLACK;
                    switch (tile) {
                        case TILE_FLOOR: tileColor = DARKGRAY; break;
                        case TILE_WALL: tileColor = GRAY; break;
                        case TILE_DOOR: tileColor = BROWN; break;
                        default: break;
                    }
                    
                    DrawRectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE, tileColor);
                }
            }
        }
    }
}

// Render entities (player, enemies, items)
void RenderEntities(GameState *state, GameTextures *textures) {
    // Render items
    for (int i = 0; i < state->itemCount; i++) {
        Item item = state->items[i];
        
        if (item.active) {
            // Actual sprite rendering would use the texture atlas
            // For now, use colored rectangles for different item types
            Color itemColor;
            
            switch (item.type) {
                case ITEM_HEALTH_POTION: itemColor = RED; break;
                case ITEM_WEAPON: itemColor = SILVER; break;
                case ITEM_ARMOR: itemColor = BLUE; break;
                case ITEM_GOLD: itemColor = GOLD; break;
                default: itemColor = WHITE; break;
            }
            
            DrawRectangle(item.x * TILE_SIZE, item.y * TILE_SIZE, TILE_SIZE, TILE_SIZE, itemColor);
        }
    }
    
    // Render enemies
    for (int i = 0; i < state->enemyCount; i++) {
        Enemy enemy = state->enemies[i];
        
        if (enemy.active) {
            // Actual sprite rendering would use the texture atlas
            // For now, use colored rectangles for different enemy types
            Color enemyColor;
            
            switch (enemy.type) {
                case ENEMY_RAT: enemyColor = BROWN; break;
                case ENEMY_GOBLIN: enemyColor = GREEN; break;
                case ENEMY_ORC: enemyColor = DARKGREEN; break;
                case ENEMY_TROLL: enemyColor = PURPLE; break;
                default: enemyColor = WHITE; break;
            }
            
            DrawRectangle(enemy.x * TILE_SIZE, enemy.y * TILE_SIZE, TILE_SIZE, TILE_SIZE, enemyColor);
            
            // Draw health bar for enemies
            int healthBarWidth = TILE_SIZE;
            int healthBarHeight = 4;
            int currentHealth = (healthBarWidth * enemy.health) / enemy.maxHealth;
            
            DrawRectangle(
                enemy.x * TILE_SIZE,
                enemy.y * TILE_SIZE - healthBarHeight - 2,
                healthBarWidth,
                healthBarHeight,
                BLACK
            );
            
            DrawRectangle(
                enemy.x * TILE_SIZE,
                enemy.y * TILE_SIZE - healthBarHeight - 2,
                currentHealth,
                healthBarHeight,
                RED
            );
        }
    }
    
    // Render player
    DrawRectangle(
        state->player.x * TILE_SIZE,
        state->player.y * TILE_SIZE,
        TILE_SIZE,
        TILE_SIZE,
        BLUE
    );
}

// Render UI
void RenderUI(GameState *state) {
    // Draw health bar
    int healthBarWidth = 200;
    int healthBarHeight = 20;
    int currentHealth = (healthBarWidth * state->player.health) / state->player.maxHealth;
    
    DrawRectangle(20, 20, healthBarWidth, healthBarHeight, BLACK);
    DrawRectangle(20, 20, currentHealth, healthBarHeight, RED);
    
    // Draw health text
    DrawText(
        TextFormat("HP: %d/%d", state->player.health, state->player.maxHealth),
        30,
        22,
        16,
        WHITE
    );
    
    // Draw stats
    DrawText(
        TextFormat("ATK: %d  DEF: %d  LEVEL: %d",
                  state->player.attack,
                  state->player.defense,
                  state->level),
        20,
        50,
        16,
        WHITE
    );
    
    // Draw message log
    DrawRectangle(20, SCREEN_HEIGHT - 40, SCREEN_WIDTH - 40, 30, BLACK);
    DrawText(state->message, 25, SCREEN_HEIGHT - 35, 16, WHITE);
    
    // Draw game over message if applicable
    if (state->gameOver) {
        DrawRectangle(
            SCREEN_WIDTH / 2 - 150,
            SCREEN_HEIGHT / 2 - 50,
            300,
            100,
            BLACK
        );
        
        DrawText(
            "GAME OVER",
            SCREEN_WIDTH / 2 - 100,
            SCREEN_HEIGHT / 2 - 30,
            30,
            RED
        );
        
        DrawText(
            "Press R to restart",
            SCREEN_WIDTH / 2 - 100,
            SCREEN_HEIGHT / 2 + 10,
            20,
            WHITE
        );
    }
}