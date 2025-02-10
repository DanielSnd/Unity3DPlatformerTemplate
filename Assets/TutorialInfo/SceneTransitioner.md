# Scene Transition System

The **SceneTransitioner** provides smooth scene transitions with fade effects and handles scene loading coordination with the checkpoint system.

## Features

- **Smooth Fade Transitions**: Fade in/out between scenes
- **Checkpoint Integration**: Coordinates with CheckpointManager for spawn positions
- **Camera Management**: Handles camera transitions between scenes
- **Reset Scene Support**: Special handling for scene resets

## Usage

```
csharp
// Load a new scene with fade transition
SceneTransitioner.LoadScene("YourSceneName");
```

## Scene Loading Process

1. Fade out current scene
2. Load new scene
3. Position players at checkpoints (if any)
4. Fade in new scene

## Camera Handling

The system automatically:
- Disables unnecessary scene cameras
- Removes duplicate AudioListeners

## Events

Subscribe to scene load events:

### UnityEvent<string> NewSceneLoaded       

This event is triggered when a new scene is loaded.

```
void Awake() {
        SceneTransitioner.NewSceneLoaded.AddListener(OnNewSceneLoaded);
}

void OnNewSceneLoaded(string sceneName) {
        Debug.Log($"New scene loaded: {sceneName}");
}
```


## Best Practices

- Always check `SceneTransitioner.Transitioning` before initiating gameplay-critical actions
- Use `SceneTransitioner.LoadScene()` instead of Unity's `SceneManager.LoadScene()`
- Let SceneTransitioner handle camera setup in new scenes