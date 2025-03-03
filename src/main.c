#include "game.h"
#include "dungeon.h"
#include "entities.h"
#include "rendering.h"
#include <stdlib.h>
#include <time.h>

int main(void) {
    // Initialize random seed
    srand(time(NULL));
    
    // Initialize raylib
    InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Dungeon Delve");
    SetTargetFPS(60);
    
    // Initialize game state
    GameState state;
    InitGameState(&state);
    
    // Initialize game map
    GameMap map;
    InitializeMap(&map);
    
    // Load textures
    GameTextures textures;
    LoadGameTextures(&textures);
    
    // Generate dungeon
    GenerateBSPDungeon(&map);
    
    // Place player
    PlacePlayer(&map, &state.player);
    
    // Place entities
    PlaceEntities(&state, &map);
    
    // Main game loop
    while (!WindowShouldClose() && !state.gameOver) {
        // Update game state
        if (state.playerTurn) {
            // Handle player input
            if (IsKeyPressed(KEY_UP)) {
                if (MoveEntity(&state.player.x, &state.player.y, 0, -1, &map)) {
                    state.playerTurn = false;
                }
            }
            else if (IsKeyPressed(KEY_DOWN)) {
                if (MoveEntity(&state.player.x, &state.player.y, 0, 1, &map)) {
                    state.playerTurn = false;
                }
            }
            else if (IsKeyPressed(KEY_LEFT)) {
                if (MoveEntity(&state.player.x, &state.player.y, -1, 0, &map)) {
                    state.playerTurn = false;
                }
            }
            else if (IsKeyPressed(KEY_RIGHT)) {
                if (MoveEntity(&state.player.x, &state.player.y, 1, 0, &map)) {
                    state.playerTurn = false;
                }
            }
            else if (IsKeyPressed(KEY_SPACE)) {
                PickupItem(&state);
                state.playerTurn = false;
            }
        }
        else {
            // Enemy turn
            UpdateEnemies(&state, &map);
            state.playerTurn = true;
            state.turn++;
        }
        
        // Check game over condition
        if (CheckGameOver(&state)) {
            state.gameOver = true;
        }
        
        // Render game
        BeginDrawing();
        ClearBackground(BLACK);
        
        // Render map, entities, UI, etc.
        RenderMap(&map, &textures);
        RenderEntities(&state, &textures);
        RenderUI(&state);
        
        // Render game over message if needed
        if (state.gameOver) {
            DrawText("Game Over!", SCREEN_WIDTH/2 - 100, SCREEN_HEIGHT/2, 40, RED);
        }
        
        EndDrawing();
    }
    
    // Clean up
    UnloadGameTextures(&textures);
    CloseWindow();
    
    return 0;
}
