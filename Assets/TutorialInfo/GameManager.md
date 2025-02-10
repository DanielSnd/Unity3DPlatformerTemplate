# GameManager Documentation

The GameManager is the core manager for this Game Template, handling essential game functionality through the [Singleton pattern](Singleton.md). It's designed to be self-instantiating, meaning you can create new scenes without manually adding it.

## Key Features

- **Automatic Initialization**: Self-instantiates through the Singleton pattern
- **Split-screen Multiplayer**: Manages player joining/leaving
- **Basic UI Management**: Handles coin counter and "Press to Join" prompts
- **System Integration**: Coordinates with other major systems:
  - [Scene Transitions](SceneTransitioner.md)
  - [Sound Management](SoundManager.md)
  - [Checkpoint System](CheckpointSystem.md)
  - [Menu Helper](MenuHelper.md)

## Setup Instructions

1. **New Scene Setup**
   - Create a new scene
   - The GameManager will automatically instantiate itself
   - No manual setup required!

2. **Layer Setup**
   Configure these LayerMasks in Unity's Inspector:
   - `groundMask`: Layers that players can walk on and collide with
   - `enemyMask`: Layers containing enemies
   - `playerMask`: Layers containing players

## Player Management

- Players can join with keyboard (Space) or gamepad (A button) using Unity's own PlayerManager script.
- Supports multiple players in split-screen.
- Automatically manages camera setup for single/multiplayer.