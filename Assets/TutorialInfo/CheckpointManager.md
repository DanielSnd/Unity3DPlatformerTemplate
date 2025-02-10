# Checkpoint System

The checkpoint system manages player progress and spawn points across scenes through CheckpointManager and Checkpoint components.

## Components

### Checkpoint Component
- Visual feedback for activation
- Particle effects and sound
- Initial checkpoint designation
- Automatic registration with CheckpointManager

### CheckpointManager
- Manages checkpoint state across scenes
- Handles player respawning
- Coordinates scene transitions
- Maintains checkpoint persistence

## Usage

### Setting Up Checkpoints

1. Add the Checkpoint component to an object.
2. Add a collider to the checkpoint object and set it to trigger.
3. Optionally if this is the initial checkpoint for a level toggle the checkbox "IsInitialCheckpoint".
4. Optionally set different materials for active/inactive states, as well as particle effects and sounds.

### Checkpoint Management

```
// Get current checkpoint (The w in this Vector4 is used to store a hashed version of the current's scene name).
Vector4 spawnPoint = CheckpointManager.CurrentCheckpoint;
// Teleport player to checkpoint
CheckpointManager.TeleportPlayerToCheckpoint();
// Set new checkpoint from code
CheckpointManager.SetCheckpoint(position, sceneName);
```

## Features

- **Visual Feedback**: Materials change on activation
- **Audio & Effects**: Customizable activation feedback
- **Multi-Player Support**: Handles multiple player spawns
- **Scene Persistence**: Maintains checkpoint state across scenes
- **Initial Spawn Points**: Designate starting checkpoints per scene

## Best Practices

1. Use initial checkpoints for level start points
2. Consider multiple player spacing in checkpoint placement. When multiple players spawn together they spawn with some spacing between them.