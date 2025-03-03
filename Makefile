# Compiler settings
CC = gcc
CFLAGS = -Wall -std=c99 -D_DEFAULT_SOURCE -Wno-missing-braces
LDFLAGS = -lraylib -lGL -lm -lpthread -ldl -lrt -lX11

# Directories
SRC_DIR = src
BUILD_DIR = build
INCLUDE_DIR = src /usr/local/include

# Source files
SRC_FILES = $(SRC_DIR)/main.c $(SRC_DIR)/dungeon.c $(SRC_DIR)/entities.c $(SRC_DIR)/rendering.c
OBJ_FILES = $(patsubst $(SRC_DIR)/%.c, $(BUILD_DIR)/%.o, $(SRC_FILES))

# Target executable
TARGET = $(BUILD_DIR)/roguelike

# Default target
all: $(BUILD_DIR) $(TARGET)

# Create build directory
$(BUILD_DIR):
	mkdir -p $(BUILD_DIR)

# Compile object files
$(BUILD_DIR)/%.o: $(SRC_DIR)/%.c
	$(CC) $(CFLAGS) -I$(SRC_DIR) -I/usr/local/include -c $< -o $@

# Link executable
$(TARGET): $(OBJ_FILES)
	$(CC) $^ -o $@ $(LDFLAGS)

# Clean build files
clean:
	rm -rf $(BUILD_DIR)

# Run the game
run: all
	$(TARGET)

.PHONY: all clean run