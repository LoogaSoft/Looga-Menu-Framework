# Looga Menu Framework

Looga Menu Framework is an asset-driven UGUI menu framework built around reusable panels, composed screens, typed state readers, and rule assets.

The package is intentionally project-agnostic. Games provide their own state reader interfaces and rule assets, then use `LoogaMenuScreenDefinition` assets to compose panels from an additive UI scene.

## Core Concepts

- `LoogaMenuPanelDefinition` identifies a reusable panel in the UI scene.
- `LoogaMenuPanel` lives on the matching panel object and registers itself with the active `LoogaMenuRoot`.
- `LoogaMenuScreenDefinition` describes which panels open together as a screen.
- Content entries are optional panels or screens that can open from an already active screen.
- `LoogaMenuRuleSet` evaluates blackboard-backed conditions before a screen or content entry opens.
- `LoogaMenuInputPolicy` describes which gameplay inputs should be blocked while a screen is active.

## Content Entries

Content entries use hidden stable IDs stored inside their owning screen definition. Designers select entries by display name, while runtime code opens them by stable ID. This avoids fragile list-index references without creating one-off ID assets.

Use a custom display name only when the target panel or screen name is not clear enough. Otherwise, the entry name is generated from the assigned target.

## Typical Setup

1. Add `LoogaMenuRoot` to the UI canvas root.
2. Add `LoogaMenuPanel` to each menu panel object and assign its panel definition.
3. Create screen definitions that list their default panels.
4. Add content entries for panels/screens opened from inside that screen.
5. Add rules and input policies as needed.
6. Use `LoogaMenuOpenButton` or project code to open screens or content entries.
