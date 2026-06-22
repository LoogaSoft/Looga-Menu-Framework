# Looga Menu Framework

Looga Menu Framework is an asset-driven UGUI menu framework for reusable panels, composed screens, content entries, blackboard-backed open requirements, input policies, and optional menu audio/transition handlers.

The package is intentionally project-agnostic. A game provides its own UI scene, panel objects, input bridge, blackboard state providers, and optional visual/audio handlers.

## Core Concepts

- `LoogaMenuRoot` is the runtime entry point. Put one on the root object of the menu canvas.
- `LoogaMenuPanelDefinition` is an asset identity for one reusable panel.
- `LoogaMenuPanel` lives on the actual UGUI panel object and registers that panel with the active root.
- `LoogaMenuScreenDefinition` describes a screen: default panels, optional content entries, requirements, background/action bar behavior, and input policy.
- `LoogaMenuScreenContentEntry` describes content that can open from a screen, such as a submenu panel or child screen.
- `LoogaMenuRuleSet` gates opening through blackboard conditions.
- `LoogaMenuInputPolicy` describes cursor behavior and which gameplay input categories should be blocked.

## Basic Setup

1. Add `LoogaMenuRoot` to the main UI canvas/root object.
2. Assign optional default background and action bar panel definitions on the root.
3. Create one `LoogaMenuPanelDefinition` asset for each reusable panel.
4. Add `LoogaMenuPanel` to each panel object in the UI scene and assign its definition.
5. Create `LoogaMenuScreenDefinition` assets for major menu flows.
6. Add default panels to each screen definition.
7. Add content entries for submenus or optional panels opened from that screen.
8. Assign an input policy and open requirements as needed.
9. Open screens with `LoogaMenuOpenButton`, `LoogaMenuScreenContentReference`, or code.

Panel objects may start disabled in the UI scene. The menu root/manager will show and hide registered panels at runtime.

## Panels

A panel is one reusable UGUI object, such as:

- Pause Menu
- Loadout
- Stockpile
- Raid Container
- Social Menu
- Settings
- Background
- Action Bar

Each panel object needs:

- A `LoogaMenuPanel` component.
- A matching `LoogaMenuPanelDefinition` asset.
- Any project-specific UI scripts required by that panel.

Panel definitions contain display metadata and per-panel toggles such as skipping transitions or open/close sounds.

## Screens

A screen is a composed menu state. It can activate several default panels at once.

Examples:

- Pause screen: background, pause panel, action bar.
- Station inventory screen: background, stockpile panel, loadout panel, action bar.
- Raid inventory screen: background, loadout panel, action bar.

Screen definition fields:

- `Default Panels`: panels that open immediately with the screen.
- `Content Entries`: panels or screens that can be opened from this screen.
- `Background Panel Mode`: use root default, override, or none.
- `Action Bar Panel Mode`: use root default, override, or none.
- `Open Requirements`: optional rule set that must pass before opening.
- `Input Policy`: cursor and gameplay input blocking behavior.
- `Missing Panel Behavior`: what to do if a referenced panel is not registered.
- `Close As Group On Back`: closes the whole screen group when backing out.
- `Close Existing Screens`: closes already-open screens before this screen opens.

## Content Entries

Content entries are used when a screen can open extra content from within itself.

Example: a pause screen can include content entries for:

- Social Menu panel.
- Settings screen.
- Confirm Quit panel.

Each content entry has:

- Target type: `Panel` or `Screen`.
- Target asset: the panel or screen to open.
- Open mode.
- Back behavior.
- Optional open requirements.
- Optional blackboard parameters.

Content entries use hidden stable IDs. Designers see display names, while code stores stable IDs so references do not break if list order changes.

Use `LoogaMenuScreenContentReference` for inspector-friendly references to a content entry.

## Open Modes

`Replace`
: Close current content in the flow and show the new target.

`AddAlongside`
: Add the new target while keeping existing visible content active.

`Overlay`
: Show the new target as the top focus/back target. Use this for modals, popups, and submenus that should receive back input first.

## Back Behavior

`CloseThisEntry`
: Close only the content entry.

`ReturnToParent`
: Close the entry and return focus to the parent screen.

`CloseParent`
: Close the entry and its parent screen.

`CloseWholeFlow`
: Close the full menu flow.

Use `CloseWholeFlow` for cases like opening Social from Pause where pressing back should leave both Social and Pause.

## Missing Panel Behavior

`Ignore`
: Open what can be opened and ignore missing panel references.

`Warn`
: Open what can be opened, but log a warning for missing panels.

`BlockOpen`
: Do not open the screen if any required panel is missing.

Use `Warn` while setting up UI scenes and `BlockOpen` for stricter production flows.

## Input Policies

`LoogaMenuInputPolicy` controls cursor behavior and gameplay input blocking.

Presets:

- `None`: do not block gameplay input.
- `BlockAllGameplay`: pause-style behavior.
- `InventoryMovementOnly`: allow limited movement, block camera/combat/equipment.
- `CombatAndEquipment`: block combat/equipment while keeping broader control.
- `Custom`: manually select blocked categories.

The project should read the active policy from menu state and apply it to its own input system.

## Blackboard Rules

Rules use blackboard keys so menu conditions are data-driven and not string-based.

Typical keys:

- `Is Raid Scene`
- `Is Station Scene`
- `Has Stockpile Access`
- `Has Container Access`
- `Is Player Alive`

Runtime systems write values:

```csharp
using LoogaSoft.Blackboard;
using LoogaSoft.Menu;

public sealed class SceneMenuStateWriter : MonoBehaviour
{
    [SerializeField] private LoogaBlackboardKey _isRaidScene;

    private void Start()
    {
        LoogaMenuRoot.Active.BlackboardWriter.SetBool(_isRaidScene, true);
    }
}
```

Screen definitions and content entries can then reference rule sets that evaluate those values.

## Opening Menus From Code

Use `LoogaMenuRoot` as the main API.

```csharp
using LoogaSoft.Menu;
using UnityEngine;

public sealed class PauseMenuOpener : MonoBehaviour
{
    [SerializeField] private LoogaMenuScreenDefinition _pauseScreen;

    public void OpenPause()
    {
        LoogaMenuRoot.Active.Open(_pauseScreen, this);
    }
}
```

Prefer a serialized root reference if the object already has one:

```csharp
[SerializeField] private LoogaMenuRoot _menuRoot;
[SerializeField] private LoogaMenuScreenDefinition _inventoryScreen;

private void OpenInventory()
{
    _menuRoot.Open(_inventoryScreen, this);
}
```

Open a content entry by stable ID:

```csharp
_menuRoot.OpenContent(_raidScreen, "content-entry-stable-id", this, payload);
```

Open a content entry through an inspector reference:

```csharp
[SerializeField] private LoogaMenuScreenContentReference _raidContainerContent;

private void OpenRaidContainer(object containerPayload)
{
    _raidContainerContent.Open(LoogaMenuRoot.Active, this, containerPayload);
}
```

Back and close:

```csharp
LoogaMenuRoot.Active.Back();
LoogaMenuRoot.Active.CloseAll();
```

## Button Setup

Use `LoogaMenuOpenButton` on UGUI buttons.

For a normal screen button:

1. Add `LoogaMenuOpenButton` to the button.
2. Set target to `Screen`.
3. Assign the screen definition.
4. Optionally assign a menu root. If unset, it uses `LoogaMenuRoot.Active`.

For a submenu/content button:

1. Add `LoogaMenuOpenButton` to the button.
2. Set target to `Screen Content Entry`.
3. Assign the owning screen.
4. Select the content entry from the dropdown.

Use `LoogaMenuBackButton` for standard back/cancel buttons. It calls the active root's `Back()`.

## Input Router Setup

Use `LoogaMenuInputRouter` for player/controller input that opens menus directly.

Typical setup:

1. Add `LoogaMenuInputRouter` to the local player prefab or an input-owned object.
2. Leave `Menu Root` empty if the router should use `LoogaMenuRoot.Active`.
3. Leave `Manage Action Enabled State` off when a `PlayerInput` or project input bootstrapper already enables actions.
4. Add one binding per menu input.
5. Assign the `Input Action Reference`.
6. Choose the trigger phase, usually `Started` for keyboard/menu toggles.
7. Choose the target: screen, screen content entry, back, or close all.
8. Choose the open behavior.
9. Optionally assign requirements to gate the binding through blackboard rules.

Example bindings:

- Inventory key in station: target station inventory screen, requirements require station scene.
- Inventory key in raid: target raid inventory screen, requirements require raid scene.
- Social key: target social screen, behavior `Toggle`.
- Escape/back key: target `Back`.

Multiple bindings can use the same input action. They are evaluated in list order, and only the first successful binding is handled for that frame.

## Parameters And Payloads

Use blackboard parameters when a screen/content entry should push simple typed values into state as it opens.

Use the `payload` object parameter when the caller needs to pass a runtime object, such as:

- A loot container instance.
- A selected item.
- A target entity.

Keep payloads project-specific and cast them only inside project-specific panel scripts.

## Custom Panel Reactions

Use content-entry blackboard parameters for simple mode switches, such as opening the same stockpile panel in normal or attachment-selection mode. Panels can read those values from the active blackboard or receive a richer runtime `payload` from the caller when object references are needed.

## Transition And Audio Hooks

Projects can provide transition/audio handlers to the menu manager.

```csharp
public sealed class MyMenuTransitions : MonoBehaviour, ILoogaMenuTransitionHandler
{
    public void PlayOpen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels)
    {
        // Play open animation.
    }

    public void PlayClose(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels, Action onComplete)
    {
        // Play close animation, then call onComplete.
        onComplete?.Invoke();
    }
}
```

```csharp
public sealed class MyMenuAudio : MonoBehaviour, ILoogaMenuAudioHandler
{
    public void PlayOpen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels)
    {
        // Play menu open sound.
    }

    public void PlayClose(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] panels)
    {
        // Play menu close sound.
    }
}
```

The framework owns the menu state. Project handlers own presentation.

## Public Runtime API

Common calls:

```csharp
LoogaMenuRoot.Active.Open(screenDefinition);
LoogaMenuRoot.Active.OpenContent(screenDefinition, contentEntryId);
LoogaMenuRoot.Active.Back();
LoogaMenuRoot.Active.CloseAll();
```

State registry:

```csharp
LoogaMenuRoot.Active.BlackboardWriter.SetBool(key, true);
LoogaMenuRoot.Active.BlackboardWriter.SetInt(key, 2);
LoogaMenuRoot.Active.BlackboardWriter.SetFloat(key, 1.5f);
LoogaMenuRoot.Active.BlackboardWriter.SetString(key, "Station");
LoogaMenuRoot.Active.BlackboardWriter.RemoveValue(key);
```

Usually avoid calling `LoogaMenuPanel.Show()`, `Hide()`, or `SetCovered()` directly. Let `LoogaMenuRoot` and `LoogaMenuManager` control panel visibility.

## Recommended Workflows

For a new menu panel:

1. Build the UGUI panel object in the UI scene.
2. Add `LoogaMenuPanel`.
3. Create and assign a panel definition.
4. Add it to a screen as a default panel or content entry.

For a new screen:

1. Create a screen definition.
2. Add default panels.
3. Choose input policy.
4. Choose background/action bar behavior.
5. Add open requirements if needed.
6. Add content entries for nested panels/screens.

For a context-sensitive menu:

1. Create blackboard keys for the needed state.
2. Write those values from project code.
3. Create a rule set with the required conditions.
4. Assign the rule set to the screen or content entry.

## Notes

- Avoid list-index references for content. Use content entry references or stable IDs.
- Keep panels reusable and project-specific behavior on project scripts.
- Keep screen definitions focused on menu composition, requirements, and input policy.
- Use the blackboard for simple state and requirements. Use payloads for runtime object references.
