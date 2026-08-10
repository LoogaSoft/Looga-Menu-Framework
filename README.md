# Looga Menu Framework

Looga Menu Framework is an asset-driven UGUI framework for game menus. It uses a small set of explicit concepts so designers can build simple and complex menu flows in the same way.

The package is project-agnostic. A game supplies its UI scene, panel objects, input bridge, blackboard state, and optional presentation handlers.

## Authoring Model

Use these rules when deciding what to create:

- A **screen** is a destination in menu history. Opening another screen adds or replaces a history entry.
- A **layout** is one composition of the same screen. Changing a layout does not add a history entry.
- A **panel** is one reusable UI region that a layout shows.
- A **region** is one shared presentation area, such as navigation, actions, a header, or a background.
- A **destination** selects a screen, an optional layout, and an open mode.

Examples:

- Pause and Settings are separate screens because Back must return from Settings to Pause.
- Faction Selection, Shop Buy, Shop Sell, Missions, and Jobs are layouts of one Faction Services screen.
- Loadout and Backpack can remain one panel when they always appear and close together.
- A confirmation dialog is normally an overlay screen because it has its own focus and Back behavior.

Do not split a stable UI region into several panels only because it contains several child objects. Make a separate panel when the region needs independent reuse, visibility, transition, or contextual actions.

## Basic Setup

1. Create one `LoogaMenuStructureProfile` for the project.
2. Add typed region definitions and default content to the structure profile.
3. Add `LoogaMenuRoot` to the main menu canvas and assign the structure profile.
4. Add `LoogaMenuRegionHost` to each shared presenter and select its region.
5. Create one `LoogaMenuPanelDefinition` for each reusable panel.
6. Add `LoogaMenuPanel` to each authored panel object and assign its definition.
7. Create one `LoogaMenuScreenDefinition` for each menu-history destination.
8. Create owned `LoogaMenuScreenLayout` sub-assets for the screen's supported compositions.
9. Add the required panel definitions to each layout.
10. Override or hide configured regions only where the screen or layout differs from the project default.
11. Open the screen through `LoogaMenuDestination`, `LoogaMenuOpenButton`, input routing, or code.

Panel objects may start disabled. The menu root registers them and controls their active state.

## Screens

`LoogaMenuScreenDefinition` owns the behavior shared by one menu destination:

- Layouts and the default layout.
- Region overrides.
- Open requirements.
- Input policy.
- Default open mode.
- Missing-panel behavior.

Create a new screen when the menu needs an independent history entry. Back returns to the previous screen.

## Layouts

`LoogaMenuScreenLayout` is an owned sub-asset of a screen. It defines:

- The panels shown together.
- Optional region overrides.

Use layouts for states of the same destination. For example, a Faction Services screen can own Faction Selection, Shop Buy, Shop Sell, Missions, and Jobs layouts. Changing among these layouts keeps one screen-history entry.

Every screen has one default layout. A destination that does not select a layout uses that default.

## Panels

A panel is one reusable UGUI region, such as:

- Stockpile.
- Loadout and Backpack.
- Faction selection.
- Shop buy list.
- Settings.
- Shared background.
- Compact profile and currency header regions.
- Shared action bar.

Each authored panel object needs a `LoogaMenuPanel` and a matching `LoogaMenuPanelDefinition`.

Panels can implement `ILoogaMenuActionProvider` to contribute contextual action-bar commands. Call `LoogaMenuPanel.NotifyActionsChanged()` when those commands change.

## Typed Destinations

`LoogaMenuDestination` replaces string IDs and indirect content-entry lists. It contains:

- A required screen.
- An optional layout owned by that screen.
- An open mode.

Use the same destination type in buttons, input bindings, and navigation entries. The inspector limits layout selection to layouts owned by the selected screen.

## Shared Regions

`LoogaMenuStructureProfile` defines the shared presentation areas available to a project. Each `LoogaMenuRegionDefinition` has a typed `LoogaMenuRegionContent` default. Screens and layouts can inherit, override, or hide that content.

The framework includes content types for navigation entries, contextual actions, and shared panels. Projects can add more content and presenter types without changing screen or layout definitions.

Add `LoogaMenuRegionHost` to each authored presenter and assign the matching region. For example, one project can define Primary Navigation, Secondary Navigation, Action Bar, Header, and Background regions. Another project can use a different set without changing the framework.

Visible panels can implement `ILoogaMenuActionProvider` to contribute contextual commands to an action region. Call `LoogaMenuPanel.NotifyActionsChanged()` when those commands change.

## Open Modes

- `Replace` closes the current destination and opens the new destination.
- `AddAlongside` keeps current content visible and adds the destination.
- `Overlay` opens the destination above the current focus and Back target.

Use `Overlay` for dialogs and temporary submenus that must close before the parent screen.

## Requirements And Input

`LoogaMenuRuleSet` checks typed blackboard conditions before a destination opens. `LoogaMenuInputPolicy` controls cursor behavior and blocked gameplay input categories while the screen is active.

Keep game-specific rules and input bridges outside the package. The package exposes typed assets and runtime contracts for those integrations.

## Previewing

Open the Menu Preview window from the LoogaSoft menu. Each screen appears once. Screens with several layouts can expand to preview each layout. Right-click a preview button to select and ping its screen definition.

## Design Guidance

- Prefer one obvious authoring path.
- Use screens for history, layouts for composition, and panels for reusable regions.
- Use typed destinations instead of string identifiers.
- Keep navigation, menu headers, and action bars shared; screens contribute data instead of duplicating their UI.
- Keep layout changes out of the Back stack.
- Keep game-specific view logic in the game project.
