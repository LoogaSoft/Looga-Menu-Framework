# Looga Menu Framework

Looga Menu Framework is a designer-first UGUI framework. Its normal workflow uses four concepts:

- A **screen** is a destination that Back can return from.
- A **layout** is one arrangement of the same screen. Switching layouts does not change menu history.
- A **panel** is one reusable UI section.
- A **shared slot** is authored UI reused across screens, such as a header, navigation bar, action bar, or background.

Start from **LoogaSoft > Menu Framework > Menu Project**. This window creates and finds the assets used by the project.

## Choosing The Right Concept

Create a new screen when Back must return to the previous menu. Create a layout when the destination stays the same but its visible panels change. Create a panel when a UI section needs independent visibility, reuse, or transitions.

Examples:

- Pause and Settings are separate screens.
- Shop Buy, Shop Sell, Missions, and Jobs can be layouts of one Faction Services screen.
- Loadout and Backpack can remain one panel when they always appear together.
- A confirmation dialog is usually an overlay screen.

## Basic Setup

1. Open **LoogaSoft > Menu Framework > Menu Project**.
2. Create one Menu Project asset.
3. Add shared slots for the header, navigation, actions, and background used by the project.
4. Add `LoogaMenuRoot` to the main menu canvas and assign the Menu Project.
5. Add one `LoogaMenuRegionHost` to each shared presenter and assign its Shared Slot.
6. Create panel definitions and add `LoogaMenuPanel` to their scene objects.
7. Create a screen. The Menu Project window creates its default layout automatically.
8. Add the panels shown by each layout.
9. Use `LoogaMenuButton` for Open Screen, Switch Layout, Back, and Close All operations.

Panel objects can start disabled. The menu root registers and controls them.

## Navigation

A screen can generate navigation without separate content assets:

1. Assign its Navigation Slot.
2. Enable **Include Layouts In Navigation**.
3. Disable **Include In Navigation** on any layout that should not appear.
4. Add optional links when navigation must open another screen.

Generated layout links use layout asset names. Selecting one switches the active layout without adding a menu-history entry.

## Shared UI

Shared slots keep persistent presenters separate from screen panels. A screen or layout normally inherits each slot. It can also add content, replace content, or hide the slot.

The runtime resolves shared UI in this order:

1. Menu Project default.
2. Active context.
3. Open screen.
4. Active layout.

Contexts are optional advanced assets for persistent application states such as Main Menu, Station, or Raid. They can change shared UI without opening a screen.

## Buttons And Input

Use `LoogaMenuButton` for scene buttons. It supports:

- Open Screen.
- Switch Layout.
- Back.
- Close All.

`LoogaMenuInputRouter` provides the same screen-opening and closing behavior for Input System actions.

## Advanced Features

The framework keeps typed rules, input policies, contexts, and custom shared-slot content available for larger projects. These features are optional. A basic menu does not need them.

Visible panels can implement `ILoogaMenuActionProvider` to contribute contextual actions. Project-specific presenters can consume custom shared-slot content without changing screen or layout definitions.

## Open Modes

- `Replace` closes the current destination before opening the new one.
- `AddAlongside` keeps current content visible.
- `Overlay` opens above the current screen and becomes the next Back target.

## Previewing

Open **LoogaSoft > Menu Framework > Menu Preview**. Expand a screen to preview its layouts. Right-click a row to select its definition. Middle-click a row to select matching scene objects.
