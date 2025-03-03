#ifndef DUNGEON_H
#define DUNGEON_H

#include "game.h"

// BSP Node structure
typedef struct BSPNode {
    int x, y, width, height;
    struct BSPNode *left;
    struct BSPNode *right;
    Room *room;
} BSPNode;

// Function declarations
void InitializeMap(GameMap *map);
void GenerateBSPDungeon(GameMap *map);
void CreateRoom(GameMap *map, int x, int y, int width, int height);
void ConnectRooms(GameMap *map, Room room1, Room room2);
bool CheckRoomOverlap(GameMap *map, Room newRoom);
void PlacePlayer(GameMap *map, Player *player);

#endif // DUNGEON_H