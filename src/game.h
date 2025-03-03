#ifndef GAME_H
#define GAME_H

#include "raylib.h"
#include <stdbool.h>

// Constants
#define TILE_SIZE       32      // Size of each tile in pixels
#define MAP_WIDTH       80      // Width of the map in tiles
#define MAP_HEIGHT      45      // Height of the map in tiles
#define SCREEN_WIDTH    (MAP_WIDTH * TILE_SIZE)
#define SCREEN_HEIGHT   (MAP_HEIGHT * TILE_SIZE)
#define MAX_ROOMS       15      // Maximum number of rooms
#define MIN_ROOM_SIZE   5       // Minimum room size
#define MAX_ROOM_SIZE   10      // Maximum room size
#define MAX_ENEMIES     20
#define MAX_ITEMS       30
#define MAX_MESSAGE_LEN 100

// Utility macros
#define MIN(a, b) ((a) < (b) ? (a) : (b))
#define MAX(a, b) ((a) > (b) ? (a) : (b))

// Custom colors
#define SILVER  CLITERAL(Color){ 192, 192, 192, 255 }

// Tile types
typedef enum {
    TILE_FLOOR,
    TILE_WALL,
    TILE_DOOR,
    TILE_EMPTY
} TileType;

// Room structure
typedef struct {
    int x;
    int y;
    int width;
    int height;
} Room;

// Player structure
typedef struct {
    int x;
    int y;
    int health;
    int maxHealth;
    int attack;
    int defense;
} Player;

// Entity types
typedef enum {
    ENTITY_PLAYER,
    ENTITY_ENEMY,
    ENTITY_ITEM
} EntityType;

// Item types
typedef enum {
    ITEM_HEALTH_POTION,
    ITEM_WEAPON,
    ITEM_ARMOR,
    ITEM_GOLD,
    ITEM_COUNT
} ItemType;

// Enemy types
typedef enum {
    ENEMY_RAT,
    ENEMY_GOBLIN,
    ENEMY_ORC,
    ENEMY_TROLL,
    ENEMY_COUNT
} EnemyType;

// Enemy structure
typedef struct {
    int x;
    int y;
    char name[20];
    int health;
    int maxHealth;
    int attack;
    int defense;
    int spriteIndex;
    int sightRange;
    EnemyType type;
    bool active;
    bool aggro;
} Enemy;

// Item structure
typedef struct {
    int x;
    int y;
    char name[20];
    int value;
    int spriteIndex;
    ItemType type;
    bool active;
} Item;

// Game map
typedef struct {
    TileType tiles[MAP_WIDTH][MAP_HEIGHT];
    Room rooms[MAX_ROOMS];
    int roomCount;
} GameMap;

// Game state
typedef struct {
    Player player;
    Enemy enemies[MAX_ENEMIES];
    Item items[MAX_ITEMS];
    int enemyCount;
    int itemCount;
    int turn;
    bool playerTurn;
    int level;
    bool gameOver;
    char message[MAX_MESSAGE_LEN];
} GameState;

// Game textures
typedef struct {
    Texture2D tileset;
    Rectangle sources[16];  // Source rectangles for different tiles
} GameTextures;

#endif // GAME_H