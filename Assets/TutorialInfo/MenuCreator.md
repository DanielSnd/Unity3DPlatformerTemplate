# Menu Helper Documentation

The MenuCreator (MenuHelper) provides a simple UI system for creating in-game menus using Unity's UI Toolkit. It handles menu creation, transitions, and basic UI elements.

## Key Features

- **Dynamic Menu Creation**: Create menus at runtime
- **Fade Transitions**: Smooth transitions between menus
- **Multiple Button Types**: Various button styles and layouts
- **Loading Screen Support**: Built-in loading screen functionality
- **Persistent UI Elements**: Manages bottom-left and middle-screen labels

## Basic Menu Creation

Here's how to create a basic pause menu like what we have in [GameManager](GameManager.md).

```
// Start a new menu with title
GameManager.Instance.menuHelper.StartBasicMenu("Pause Menu");
// Add buttons with callbacks
GameManager.Instance.menuHelper.AddButton(new MenuButton("Reset", ResetLevel));
GameManager.Instance.menuHelper.AddButton(new MenuButton("Unpause", TogglePauseMenu));
```

## Button Types

Buttons can be created with different colors:

```
// Standard blue button
new MenuButton("Play", StartGame)
// Red button
new MenuButton("Delete", DeleteSave, ButtonColors.ButtonRed)
// Green button
new MenuButton("Accept", AcceptInvite, ButtonColors.ButtonGreen)
```

## Two-Button Layout

Create side-by-side buttons:
```
GameManager.Instance.menuHelper.AddTwoButton(
        new MenuButton("Yes", OnYes),
        new MenuButton("No", OnNo)
);
```

## Persistent Labels

Access built-in labels:

```
// Bottom left text (e.g., for score)
GameManager.Instance.menuHelper.m_BottomLeftLabel.text = "Score: 100";
// Middle screen text (e.g., for prompts)
GameManager.Instance.menuHelper.m_MiddleScreenLabel.text = "Press A to Join!";
```

## Loading Screens

Show loading screens menus with progress:

```
// Simple loading screen
GameManager.Instance.menuHelper.ShowLoading("Loading Level...",
        () => loadingComplete, // condition
        OnLoadingFinished // callback
);
// With progress bar
GameManager.Instance.menuHelper.ChangeLoadingProgress(50); // 0-100
GameManager.Instance.menuHelper.ChangeLoadingText("Loading Assets...");
```

## Menu Transitions

Control menu visibility:

```
// Basic hide with default duration (0.2s)
GameManager.Instance.menuHelper.HideMenu();
// Hide with callback
GameManager.Instance.menuHelper.HideMenu(() => {
// Code to run after menu is hidden
LoadNextLevel();
});
// Hide with custom duration and callback

GameManager.Instance.menuHelper.HideMenu(
WhenFinished: () => OnMenuClosed(),
duration: 0.5f // Longer fade duration
);
// Show menu with fade
GameManager.Instance.menuHelper.StartBasicMenu("Menu Title");
// Abort an in-progress hide transition

GameManager.Instance.menuHelper.AbortHideMenu();
// Hide menu instantly (no fade)
GameManager.Instance.menuHelper.InstantHideMenu();
```


### Transition Timing

- Default fade duration is 0.2 seconds
- Can specify custom duration for slower/faster transitions
- Callbacks execute after fade completes
- Transitions use DOTween for smooth animations

### Chaining Menus

You can transition between menus smoothly:

```
// Hide current menu and show new one
GameManager.Instance.menuHelper.HideMenu(() => {
GameManager.Instance.menuHelper.StartBasicMenu("New Menu");
GameManager.Instance.menuHelper.AddButton(new MenuButton("Option", OnOption));
});
// Change menu with different transition speed
GameManager.Instance.menuHelper.HideMenu(
() => ShowOptionsMenu(),
duration: 0.1f // Quick transition
);
```

### Loading Screen Transitions

Loading screens have their own transition handling:

```
// Show loading with success callback
GameManager.Instance.menuHelper.ShowLoading("Loading...",
WhileFalse: () => isLoaded, // Condition to check
Afterwards: () => OnLoadComplete() // Runs after loading
);

// Loading with possible failure
GameManager.Instance.menuHelper.ShowLoadingWithFailPossibility(
"Connecting...",
WhileFalse: () => isConnected,
Afterwards: () => OnSuccess(),
CheckFailed: () => hasError,
Failed: () => OnFailure()
);

```