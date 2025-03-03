#ifndef RENDERING_H
#define RENDERING_H

#include "game.h"
#include "dungeon.h"
#include "entities.h"

// Function declarations
void LoadGameTextures(GameTextures *textures);
void UnloadGameTextures(GameTextures *textures);
void RenderMap(GameMap *map, GameTextures *textures);
void RenderEntities(GameState *state, GameTextures *textures);
void RenderUI(GameState *state);

#endif // RENDERING_H