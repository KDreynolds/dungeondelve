#include <stdlib.h>
#include <string.h>
#include <math.h>
#include "raylib.h"
#include "dungeon.h"

// Initialize map with empty tiles
void InitializeMap(GameMap *map) {
    for (int x = 0; x < MAP_WIDTH; x++) {
        for (int y = 0; y < MAP_HEIGHT; y++) {
            map->tiles[x][y] = TILE_EMPTY;
        }
    }
    map->roomCount = 0;
}

// Create a new BSP node
BSPNode* CreateNode(int x, int y, int width, int height) {
    BSPNode *node = malloc(sizeof(BSPNode));
    node->x = x;
    node->y = y;
    node->width = width;
    node->height = height;
    node->left = NULL;
    node->right = NULL;
    node->room = NULL;
    return node;
}

// Split a node either horizontally or vertically
bool SplitNode(BSPNode *node, int minSize) {
    // Don't split if already too small
    if (node->width <= minSize * 2 || node->height <= minSize * 2)
        return false;
        
    // Decide on vertical or horizontal split
    bool horizontalSplit;
    
    // If one dimension is more than 25% larger than the other, split along that dimension
    if (node->width > node->height && (float)node->width / node->height >= 1.25)
        horizontalSplit = false;
    else if (node->height > node->width && (float)node->height / node->width >= 1.25)
        horizontalSplit = true;
    else
        horizontalSplit = (rand() % 2 == 0);
    
    // Calculate split position (with some randomness)
    int splitPosition;
    
    if (horizontalSplit) {
        // Split horizontally (create top and bottom nodes)
        splitPosition = node->y + minSize + rand() % (node->height - minSize * 2);
        node->left = CreateNode(node->x, node->y, node->width, splitPosition - node->y);
        node->right = CreateNode(node->x, splitPosition, node->width, node->height - (splitPosition - node->y));
    } else {
        // Split vertically (create left and right nodes)
        splitPosition = node->x + minSize + rand() % (node->width - minSize * 2);
        node->left = CreateNode(node->x, node->y, splitPosition - node->x, node->height);
        node->right = CreateNode(splitPosition, node->y, node->width - (splitPosition - node->x), node->height);
    }
    
    return true;
}

// Generate BSP tree recursively
void GenerateBSPTree(BSPNode *node, int minSize, int maxDepth) {
    if (maxDepth <= 0) return;
    
    if (SplitNode(node, minSize)) {
        GenerateBSPTree(node->left, minSize, maxDepth - 1);
        GenerateBSPTree(node->right, minSize, maxDepth - 1);
    }
}

// Create a room by setting floor tiles
void CreateRoom(GameMap *map, int x, int y, int width, int height) {
    // Set floor tiles for the room interior
    for (int i = x; i < x + width; i++) {
        for (int j = y; j < y + height; j++) {
            map->tiles[i][j] = TILE_FLOOR;
        }
    }
    
    // Set wall tiles around the room
    for (int i = x - 1; i <= x + width; i++) {
        if (i >= 0 && i < MAP_WIDTH) {
            if (y - 1 >= 0) map->tiles[i][y - 1] = TILE_WALL;
            if (y + height < MAP_HEIGHT) map->tiles[i][y + height] = TILE_WALL;
        }
    }
    
    for (int j = y - 1; j <= y + height; j++) {
        if (j >= 0 && j < MAP_HEIGHT) {
            if (x - 1 >= 0) map->tiles[x - 1][j] = TILE_WALL;
            if (x + width < MAP_WIDTH) map->tiles[x + width][j] = TILE_WALL;
        }
    }
}

// Create rooms within leaf nodes
Room* CreateRoomInNode(BSPNode *node, int minRoomSize) {
    // Only create rooms at leaf nodes
    if (node->left != NULL || node->right != NULL) 
        return NULL;
    
    // Room dimensions (leave some space for walls)
    int roomWidth = minRoomSize + rand() % (node->width - minRoomSize - 2);
    int roomHeight = minRoomSize + rand() % (node->height - minRoomSize - 2);
    
    // Room position within node (centered with some randomness)
    int roomX = node->x + (node->width - roomWidth) / 2;
    int roomY = node->y + (node->height - roomHeight) / 2;
    
    // Add some randomness to position
    roomX += (rand() % 3) - 1;
    roomY += (rand() % 3) - 1;
    
    // Ensure room is within node boundaries
    if (roomX < node->x) roomX = node->x;
    if (roomY < node->y) roomY = node->y;
    if (roomX + roomWidth > node->x + node->width) roomX = node->x + node->width - roomWidth;
    if (roomY + roomHeight > node->y + node->height) roomY = node->y + node->height - roomHeight;
    
    // Allocate and initialize room
    Room *room = malloc(sizeof(Room));
    room->x = roomX;
    room->y = roomY;
    room->width = roomWidth;
    room->height = roomHeight;
    
    // Store room in node
    node->room = room;
    
    return room;
}

// Create rooms in all leaf nodes
void CreateRoomsInBSP(BSPNode *node, GameMap *map, int minRoomSize) {
    if (node == NULL) return;
    
    // If leaf node, create a room
    if (node->left == NULL && node->right == NULL) {
        Room *room = CreateRoomInNode(node, minRoomSize);
        if (room != NULL) {
            // Add room to our map
            if (map->roomCount < MAX_ROOMS) {
                map->rooms[map->roomCount] = *room;
                map->roomCount++;
                
                // Create the room in our tile map
                CreateRoom(map, room->x, room->y, room->width, room->height);
                
                // We can free the allocated room since we copied it
                free(room);
            }
        }
    } else {
        // Recursively create rooms in child nodes
        CreateRoomsInBSP(node->left, map, minRoomSize);
        CreateRoomsInBSP(node->right, map, minRoomSize);
    }
}

// Connect two rooms with a corridor
void ConnectRooms(GameMap *map, Room room1, Room room2) {
    // Find center points of rooms
    int x1 = room1.x + room1.width / 2;
    int y1 = room1.y + room1.height / 2;
    int x2 = room2.x + room2.width / 2;
    int y2 = room2.y + room2.height / 2;
    
    // Create L-shaped corridor
    int cornerX, cornerY;
    
    // Choose randomly which corner to use
    if (rand() % 2 == 0) {
        cornerX = x2;
        cornerY = y1;
    } else {
        cornerX = x1;
        cornerY = y2;
    }
    
    // Create horizontal portion of corridor
    for (int x = MIN(x1, cornerX); x <= MAX(x1, cornerX); x++) {
        // Carve floor
        map->tiles[x][y1] = TILE_FLOOR;
        
        // Add walls
        if (y1 - 1 >= 0 && map->tiles[x][y1 - 1] == TILE_EMPTY)
            map->tiles[x][y1 - 1] = TILE_WALL;
        if (y1 + 1 < MAP_HEIGHT && map->tiles[x][y1 + 1] == TILE_EMPTY)
            map->tiles[x][y1 + 1] = TILE_WALL;
    }
    
    // Create vertical portion of corridor
    for (int y = MIN(y1, cornerY); y <= MAX(y1, cornerY); y++) {
        // Carve floor
        map->tiles[cornerX][y] = TILE_FLOOR;
        
        // Add walls
        if (cornerX - 1 >= 0 && map->tiles[cornerX - 1][y] == TILE_EMPTY)
            map->tiles[cornerX - 1][y] = TILE_WALL;
        if (cornerX + 1 < MAP_WIDTH && map->tiles[cornerX + 1][y] == TILE_EMPTY)
            map->tiles[cornerX + 1][y] = TILE_WALL;
    }
    
    // Complete the corridor to room2
    for (int x = MIN(cornerX, x2); x <= MAX(cornerX, x2); x++) {
        // Carve floor
        map->tiles[x][cornerY] = TILE_FLOOR;
        
        // Add walls
        if (cornerY - 1 >= 0 && map->tiles[x][cornerY - 1] == TILE_EMPTY)
            map->tiles[x][cornerY - 1] = TILE_WALL;
        if (cornerY + 1 < MAP_HEIGHT && map->tiles[x][cornerY + 1] == TILE_EMPTY)
            map->tiles[x][cornerY + 1] = TILE_WALL;
    }
    
    // Possibly place doors at corridor entrances to rooms
    if (rand() % 100 < 30) { // 30% chance of door
        map->tiles[x1][y1] = TILE_DOOR;
    }
    if (rand() % 100 < 30) { // 30% chance of door
        map->tiles[x2][y2] = TILE_DOOR;
    }
}

// Connect all adjacent rooms using BSP tree structure
void ConnectRoomsInBSP(BSPNode *node, GameMap *map) {
    if (node == NULL) return;
    
    // Only work with internal nodes (nodes with children)
    if (node->left != NULL && node->right != NULL) {
        // Find a room in each child
        Room *leftRoom = NULL;
        Room *rightRoom = NULL;
        
        // Find a room in left subtree (could be in a leaf or further down)
        BSPNode *leftLeaf = node->left;
        while (leftLeaf->left != NULL || leftLeaf->right != NULL) {
            if (leftLeaf->left != NULL && (rand() % 2 == 0 || leftLeaf->right == NULL))
                leftLeaf = leftLeaf->left;
            else
                leftLeaf = leftLeaf->right;
        }
        
        if (leftLeaf->room != NULL) {
            leftRoom = leftLeaf->room;
        }
        
        // Find a room in right subtree
        BSPNode *rightLeaf = node->right;
        while (rightLeaf->left != NULL || rightLeaf->right != NULL) {
            if (rightLeaf->left != NULL && (rand() % 2 == 0 || rightLeaf->right == NULL))
                rightLeaf = rightLeaf->left;
            else
                rightLeaf = rightLeaf->right;
        }
        
        if (rightLeaf->room != NULL) {
            rightRoom = rightLeaf->room;
        }
        
        // Connect rooms if we found them
        if (leftRoom != NULL && rightRoom != NULL) {
            ConnectRooms(map, *leftRoom, *rightRoom);
        }
        
        // Recursively connect rooms in both subtrees
        ConnectRoomsInBSP(node->left, map);
        ConnectRoomsInBSP(node->right, map);
    }
}

// Free BSP tree memory
void FreeBSPTree(BSPNode *node) {
    if (node == NULL) return;
    
    FreeBSPTree(node->left);
    FreeBSPTree(node->right);
    
    if (node->room != NULL) {
        free(node->room);
    }
    
    free(node);
}

// Generate dungeon using BSP
void GenerateBSPDungeon(GameMap *map) {
    // Initialize map with empty tiles
    InitializeMap(map);
    
    // Create root node covering the entire map
    BSPNode *rootNode = CreateNode(0, 0, MAP_WIDTH, MAP_HEIGHT);
    
    // Generate BSP tree
    int maxDepth = 5;  // Adjust for more or fewer splits
    GenerateBSPTree(rootNode, MIN_ROOM_SIZE, maxDepth);
    
    // Create rooms in leaf nodes
    CreateRoomsInBSP(rootNode, map, MIN_ROOM_SIZE);
    
    // Connect rooms
    ConnectRoomsInBSP(rootNode, map);
    
    // Clean up memory
    FreeBSPTree(rootNode);
}

// Place player in the first room
void PlacePlayer(GameMap *map, Player *player) {
    if (map->roomCount > 0) {
        Room firstRoom = map->rooms[0];
        player->x = firstRoom.x + firstRoom.width / 2;
        player->y = firstRoom.y + firstRoom.height / 2;
    }
}

// Check if a room overlaps with existing rooms
bool CheckRoomOverlap(GameMap *map, Room newRoom) {
    for (int i = 0; i < map->roomCount; i++) {
        Room existingRoom = map->rooms[i];
        
        // Check for overlap with a little padding
        if (newRoom.x - 2 <= existingRoom.x + existingRoom.width + 2 &&
            newRoom.x + newRoom.width + 2 >= existingRoom.x - 2 &&
            newRoom.y - 2 <= existingRoom.y + existingRoom.height + 2 &&
            newRoom.y + newRoom.height + 2 >= existingRoom.y - 2) {
            return true;
        }
    }
    
    return false;
}