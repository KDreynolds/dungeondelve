#ifndef ENTITIES_H
#define ENTITIES_H

#include "game.h"
#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <string.h>

// Function declarations
void InitGameState(GameState *state);
void CreateEnemy(GameState *state, int x, int y, EnemyType type);
void CreateItem(GameState *state, int x, int y, ItemType type);
void PlaceEntities(GameState *state, GameMap *map);
bool MoveEntity(int *x, int *y, int dx, int dy, GameMap *map);
bool IsVisible(int x1, int y1, int x2, int y2, GameMap *map);
void UpdateEnemies(GameState *state, GameMap *map);
void HandleCombat(GameState *state, int targetX, int targetY);
void PickupItem(GameState *state);
bool CheckGameOver(GameState *state);

#endif // ENTITIES_H