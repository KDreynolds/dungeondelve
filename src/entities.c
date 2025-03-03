#include "entities.h"
#include <stdlib.h>
#include <stdio.h>
#include <math.h>

// Initialize game state
void InitGameState(GameState *state) {
    // Initialize player
    state->player.health = 100;
    state->player.maxHealth = 100;
    state->player.attack = 10;
    state->player.defense = 5;
    
    // Initialize counts
    state->enemyCount = 0;
    state->itemCount = 0;
    state->turn = 0;
    state->playerTurn = true;
    state->level = 1;
    state->gameOver = false;
    
    // Clear message
    state->message[0] = '\0';
}

// Create a new enemy
void CreateEnemy(GameState *state, int x, int y, EnemyType type) {
    if (state->enemyCount >= MAX_ENEMIES) return;
    
    Enemy *enemy = &state->enemies[state->enemyCount];
    enemy->x = x;
    enemy->y = y;
    enemy->type = type;
    enemy->active = true;
    enemy->aggro = false;
    
    // Set enemy properties based on type
    switch (type) {
        case ENEMY_RAT:
            strcpy(enemy->name, "Rat");
            enemy->health = 10;
            enemy->maxHealth = 10;
            enemy->attack = 3;
            enemy->defense = 1;
            enemy->spriteIndex = 16; // Example sprite index
            enemy->sightRange = 4;
            break;
            
        case ENEMY_GOBLIN:
            strcpy(enemy->name, "Goblin");
            enemy->health = 15;
            enemy->maxHealth = 15;
            enemy->attack = 5;
            enemy->defense = 2;
            enemy->spriteIndex = 17; // Example sprite index
            enemy->sightRange = 5;
            break;
            
        case ENEMY_ORC:
            strcpy(enemy->name, "Orc");
            enemy->health = 25;
            enemy->maxHealth = 25;
            enemy->attack = 8;
            enemy->defense = 3;
            enemy->spriteIndex = 18; // Example sprite index
            enemy->sightRange = 6;
            break;
            
        case ENEMY_TROLL:
            strcpy(enemy->name, "Troll");
            enemy->health = 40;
            enemy->maxHealth = 40;
            enemy->attack = 12;
            enemy->defense = 5;
            enemy->spriteIndex = 19; // Example sprite index
            enemy->sightRange = 4;
            break;
            
        default:
            break;
    }
    
    state->enemyCount++;
}

// Create a new item
void CreateItem(GameState *state, int x, int y, ItemType type) {
    if (state->itemCount >= MAX_ITEMS) return;
    
    Item *item = &state->items[state->itemCount];
    item->x = x;
    item->y = y;
    item->type = type;
    item->active = true;
    
    // Set item properties based on type
    switch (type) {
        case ITEM_HEALTH_POTION:
            strcpy(item->name, "Health Potion");
            item->value = 20; // Heals 20 HP
            item->spriteIndex = 32; // Example sprite index
            break;
            
        case ITEM_WEAPON:
            strcpy(item->name, "Sword");
            item->value = 5; // +5 attack
            item->spriteIndex = 33; // Example sprite index
            break;
            
        case ITEM_ARMOR:
            strcpy(item->name, "Armor");
            item->value = 3; // +3 defense
            item->spriteIndex = 34; // Example sprite index
            break;
            
        case ITEM_GOLD:
            strcpy(item->name, "Gold");
            item->value = 10 + rand() % 20; // 10-29 gold
            item->spriteIndex = 35; // Example sprite index
            break;
            
        default:
            break;
    }
    
    state->itemCount++;
}

// Place entities in the dungeon
void PlaceEntities(GameState *state, GameMap *map) {
    // Reset counts
    state->enemyCount = 0;
    state->itemCount = 0;
    
    // Skip the first room (player's starting room)
    for (int i = 1; i < map->roomCount; i++) {
        Room room = map->rooms[i];
        
        // Chance to add enemy to room
        if (rand() % 100 < 60) { // 60% chance for enemy
            int enemyX = room.x + 1 + rand() % (room.width - 2);
            int enemyY = room.y + 1 + rand() % (room.height - 2);
            
            // Choose random enemy type with weighted probability
            int r = rand() % 100;
            EnemyType type;
            
            if (r < 40) type = ENEMY_RAT;
            else if (r < 70) type = ENEMY_GOBLIN;
            else if (r < 90) type = ENEMY_ORC;
            else type = ENEMY_TROLL;
            
            CreateEnemy(state, enemyX, enemyY, type);
        }
        
        // Chance to add item to room
        if (rand() % 100 < 40) { // 40% chance for item
            int itemX = room.x + 1 + rand() % (room.width - 2);
            int itemY = room.y + 1 + rand() % (room.height - 2);
            
            // Choose random item type with weighted probability
            int r = rand() % 100;
            ItemType type;
            
            if (r < 30) type = ITEM_HEALTH_POTION;
            else if (r < 50) type = ITEM_WEAPON;
            else if (r < 70) type = ITEM_ARMOR;
            else type = ITEM_GOLD;
            
            CreateItem(state, itemX, itemY, type);
        }
    }
}

// Move entity if valid move
bool MoveEntity(int *x, int *y, int dx, int dy, GameMap *map) {
    int newX = *x + dx;
    int newY = *y + dy;
    
    // Check bounds
    if (newX < 0 || newX >= MAP_WIDTH || newY < 0 || newY >= MAP_HEIGHT) {
        return false;
    }
    
    // Check if position is walkable
    TileType tile = map->tiles[newX][newY];
    if (tile == TILE_FLOOR || tile == TILE_DOOR) {
        *x = newX;
        *y = newY;
        return true;
    }
    
    return false;
}

// Check if there's line of sight between two points
bool IsVisible(int x1, int y1, int x2, int y2, GameMap *map) {
    int dx = abs(x2 - x1);
    int dy = -abs(y2 - y1);
    int sx = x1 < x2 ? 1 : -1;
    int sy = y1 < y2 ? 1 : -1;
    int err = dx + dy;
    int e2;
    
    while (1) {
        if (x1 == x2 && y1 == y2) break;
        
        e2 = 2 * err;
        if (e2 >= dy) {
            if (x1 == x2) break;
            err += dy;
            x1 += sx;
        }
        if (e2 <= dx) {
            if (y1 == y2) break;
            err += dx;
            y1 += sy;
        }
        
        // Check if current tile blocks line of sight
        if (map->tiles[x1][y1] == TILE_WALL) {
            return false;
        }
    }
    
    return true;
}

// Update enemies
void UpdateEnemies(GameState *state, GameMap *map) {
    // Process each enemy
    for (int i = 0; i < state->enemyCount; i++) {
        Enemy *enemy = &state->enemies[i];
        
        if (enemy->active) {
            // Check if player is in sight range
            int dx = abs(state->player.x - enemy->x);
            int dy = abs(state->player.y - enemy->y);
            int distance = dx + dy;
            
            // If player is in sight range and visible
            if (distance <= enemy->sightRange && 
                IsVisible(enemy->x, enemy->y, state->player.x, state->player.y, map)) {
                
                enemy->aggro = true;
                
                // Move towards player if not adjacent
                if (distance > 1) {
                    int moveX = 0;
                    int moveY = 0;
                    
                    // Simple pathfinding - move in direction of player
                    if (enemy->x < state->player.x) moveX = 1;
                    else if (enemy->x > state->player.x) moveX = -1;
                    
                    if (enemy->y < state->player.y) moveY = 1;
                    else if (enemy->y > state->player.y) moveY = -1;
                    
                    // Try to move horizontally or vertically first (randomly)
                    if (rand() % 2 == 0) {
                        if (!MoveEntity(&enemy->x, &enemy->y, moveX, 0, map)) {
                            MoveEntity(&enemy->x, &enemy->y, 0, moveY, map);
                        }
                    } else {
                        if (!MoveEntity(&enemy->x, &enemy->y, 0, moveY, map)) {
                            MoveEntity(&enemy->x, &enemy->y, moveX, 0, map);
                        }
                    }
                } else if (distance == 1) {
                    // Attack player if adjacent
                    int damage = enemy->attack - state->player.defense;
                    if (damage < 1) damage = 1;
                    
                    state->player.health -= damage;
                    
                    // Update message
                    sprintf(state->message, "%s attacks you for %d damage!", enemy->name, damage);
                }
            }
            // Random movement for non-aggro enemies (20% chance)
            else if (rand() % 100 < 20) {
                int dirs[4][2] = {{0, -1}, {1, 0}, {0, 1}, {-1, 0}}; // Up, Right, Down, Left
                int r = rand() % 4;
                MoveEntity(&enemy->x, &enemy->y, dirs[r][0], dirs[r][1], map);
            }
        }
    }
}

// Handle combat when player attacks enemy
void HandleCombat(GameState *state, int targetX, int targetY) {
    // Find enemy at target position
    for (int i = 0; i < state->enemyCount; i++) {
        Enemy *enemy = &state->enemies[i];
        
        if (enemy->active && enemy->x == targetX && enemy->y == targetY) {
            // Calculate damage
            int damage = state->player.attack - enemy->defense;
            if (damage < 1) damage = 1;
            
            enemy->health -= damage;
            
            // Update message
            sprintf(state->message, "You hit %s for %d damage!", enemy->name, damage);
            
            // Check if enemy is defeated
            if (enemy->health <= 0) {
                sprintf(state->message, "You defeated the %s!", enemy->name);
                enemy->active = false;
            }
            
            return;
        }
    }
}

// Pickup item at player's position
void PickupItem(GameState *state) {
    for (int i = 0; i < state->itemCount; i++) {
        Item *item = &state->items[i];
        
        if (item->active && item->x == state->player.x && item->y == state->player.y) {
            // Handle different item types
            switch (item->type) {
                case ITEM_HEALTH_POTION:
                    state->player.health += item->value;
                    if (state->player.health > state->player.maxHealth) {
                        state->player.health = state->player.maxHealth;
                    }
                    sprintf(state->message, "You drink a health potion and recover %d HP!", item->value);
                    break;
                    
                case ITEM_WEAPON:
                    state->player.attack += item->value;
                    sprintf(state->message, "You equip a better weapon! Attack +%d", item->value);
                    break;
                    
                case ITEM_ARMOR:
                    state->player.defense += item->value;
                    sprintf(state->message, "You equip better armor! Defense +%d", item->value);
                    break;
                    
                case ITEM_GOLD:
                    sprintf(state->message, "You found %d gold pieces!", item->value);
                    break;
                    
                default:
                    break;
            }
            
            item->active = false;
            return;
        }
    }
    
    strcpy(state->message, "There's nothing here to pick up.");
}

// Check if the game is over
bool CheckGameOver(GameState *state) {
    if (state->player.health <= 0) {
        strcpy(state->message, "Game Over! You were defeated!");
        state->gameOver = true;
        return true;
    }
    
    return false;
}