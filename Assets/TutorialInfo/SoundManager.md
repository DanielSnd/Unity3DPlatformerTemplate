# Sound Management System

The SoundManager provides a robust audio system with pooling, spatial audio, and volume control.

## Features

- **Sound Pooling**: Efficient reuse of audio sources
- **Group Management**: Organize sounds into groups
- **Volume Control**: Separate Music/SFX volume control
- **Spatial Audio**: Automatic positioning between players
- **Scene-Based Music**: Automatic music changes per scene


## Usage

```
// Play sound by name
SoundManager.Play("SoundName");
// Play sound at position
SoundManager.Play("SoundName", position);
// Play music
SoundManager.PlayMusic("MusicName");
// Extension method usage
"SoundName".PlaySound();
audioClip.PlaySound(position);
```


## Sound Groups

Place sound groups in `Resources/SoundGroups` folder:
- Automatically loaded at startup
- Grouped by functionality
- Configurable properties per group
- Randomize sound playback
- Optional random pitch


## Volume Control

```
// Access volume controls
SoundManager.Instance.SFXVolume = 0.5f;
SoundManager.Instance.MusicVolume = 0.8f;
// Volume saves to PlayerPrefs automatically
```
