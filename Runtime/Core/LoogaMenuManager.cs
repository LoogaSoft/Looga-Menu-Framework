using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Owns screen history, layout state, panel visibility, navigation, and action collection.</summary>
    public sealed class LoogaMenuManager
    {
        private readonly Dictionary<LoogaMenuPanelDefinition, LoogaMenuPanel> _panels = new();
        private readonly List<LoogaMenuScreenDefinition> _openScreens = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, LoogaMenuScreenLayout> _activeLayouts = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, LoogaMenuOpenMode> _openModes = new();
        private readonly List<LoogaMenuPanel> _visiblePanels = new();
        private readonly HashSet<LoogaBlackboardKey> _ownedParameterKeys = new();
        private readonly ILoogaBlackboardReader _blackboardReader;
        private readonly ILoogaBlackboardWriter _blackboardWriter;
        private readonly LoogaMenuPanelDefinition _defaultBackgroundPanel;
        private readonly LoogaMenuActionBarSettings _defaultActionBar;
        private readonly NavigationRuntime _primaryNavigation;
        private readonly NavigationRuntime _secondaryNavigation;
        private readonly ActionBarRuntime _actionBar;

        private ILoogaMenuTransitionHandler _transitionHandler;
        private ILoogaMenuAudioHandler _audioHandler;

        public LoogaMenuManager(
            ILoogaBlackboardReader blackboardReader,
            ILoogaBlackboardWriter blackboardWriter,
            LoogaMenuPanelDefinition defaultBackgroundPanel = null,
            LoogaMenuActionBarSettings defaultActionBar = null)
        {
            _blackboardReader = blackboardReader;
            _blackboardWriter = blackboardWriter;
            _defaultBackgroundPanel = defaultBackgroundPanel;
            _defaultActionBar = defaultActionBar;
            _primaryNavigation = new NavigationRuntime(this, LoogaMenuNavigationPlacement.Primary);
            _secondaryNavigation = new NavigationRuntime(this, LoogaMenuNavigationPlacement.Secondary);
            _actionBar = new ActionBarRuntime(this);
        }

        public event Action<LoogaMenuState> StateChanged;

        public IReadOnlyList<LoogaMenuScreenDefinition> OpenScreens => _openScreens;
        public IReadOnlyList<LoogaMenuPanel> VisiblePanels => _visiblePanels;
        public LoogaMenuInputPolicy ActiveInputPolicy => TopScreen != null ? TopScreen.InputPolicy : null;

        internal ILoogaBlackboardReader BlackboardReader => _blackboardReader;
        internal LoogaMenuScreenDefinition TopScreen => _openScreens.Count > 0 ? _openScreens[^1] : null;

        public LoogaMenuScreenLayout GetActiveLayout(LoogaMenuScreenDefinition screen)
        {
            return screen != null && _activeLayouts.TryGetValue(screen, out LoogaMenuScreenLayout layout)
                ? layout
                : screen?.ResolveLayout(null);
        }

        public bool TryGetNavigation(LoogaMenuNavigationPlacement placement, out ILoogaMenuNavigation navigation)
        {
            NavigationRuntime runtime = placement == LoogaMenuNavigationPlacement.Secondary
                ? _secondaryNavigation
                : _primaryNavigation;

            if (runtime.HasEntries)
            {
                navigation = runtime;
                return true;
            }

            navigation = null;
            return false;
        }

        public bool TryGetActionBar(out ILoogaMenuActionBar actionBar)
        {
            if (_actionBar.IsVisible)
            {
                actionBar = _actionBar;
                return true;
            }

            actionBar = null;
            return false;
        }

        public void SetTransitionHandler(ILoogaMenuTransitionHandler transitionHandler)
        {
            _transitionHandler = transitionHandler;
        }

        public void SetAudioHandler(ILoogaMenuAudioHandler audioHandler)
        {
            _audioHandler = audioHandler;
        }

        public void RegisterPanel(LoogaMenuPanel panel)
        {
            if (panel == null || panel.Panel == null)
                return;

            _panels[panel.Panel] = panel;
            panel.Hide();
        }

        public void UnregisterPanel(LoogaMenuPanel panel)
        {
            if (panel == null || panel.Panel == null)
                return;

            if (_panels.TryGetValue(panel.Panel, out LoogaMenuPanel current) && current == panel)
                _panels.Remove(panel.Panel);
        }

        public bool Open(LoogaMenuDestination destination, UnityEngine.Object requester = null, object payload = null)
        {
            return destination != null
                && Open(destination.Screen, destination.Layout, destination.OpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            return screen != null && Open(screen, null, screen.DefaultOpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester = null, object payload = null)
        {
            return screen != null && Open(screen, layout, screen.DefaultOpenMode, requester, payload);
        }

        public bool Open(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout, LoogaMenuOpenMode mode,
            UnityEngine.Object requester = null, object payload = null)
        {
            if (screen == null || (layout != null && !screen.ContainsLayout(layout)))
                return false;

            layout = screen.ResolveLayout(layout);
            if (_openScreens.Contains(screen))
                return SetLayout(screen, layout, requester);

            if (screen.Rules != null && !screen.Rules.CanOpen(_blackboardReader, out string failedReason))
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}'. {failedReason}", requester);
                return false;
            }

            if (!CanOpen(screen, layout, requester))
                return false;

            if (mode == LoogaMenuOpenMode.Replace)
                CloseAll(false);

            _openScreens.Add(screen);
            _activeLayouts[screen] = layout;
            _openModes[screen] = mode;
            RebuildPresentation();

            LoogaMenuPanel[] openedPanels = ResolvePanels(screen, layout, includeActionBar: true);
            _transitionHandler?.PlayOpen(screen, openedPanels);
            _audioHandler?.PlayOpen(screen, openedPanels);
            NotifyStateChanged();
            return true;
        }

        /// <summary>Changes a screen composition without adding a history entry.</summary>
        public bool SetLayout(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester = null)
        {
            if (screen == null || !_openScreens.Contains(screen) || !screen.ContainsLayout(layout))
                return false;

            if (GetActiveLayout(screen) == layout)
                return true;

            if (!CanOpen(screen, layout, requester))
                return false;

            _activeLayouts[screen] = layout;
            RebuildPresentation();
            NotifyStateChanged();
            return true;
        }

        public bool Back()
        {
            if (_openScreens.Count == 0)
                return false;

            LoogaMenuScreenDefinition screen = _openScreens[^1];
            LoogaMenuScreenLayout layout = GetActiveLayout(screen);
            LoogaMenuPanel[] closingPanels = ResolvePanels(screen, layout, includeActionBar: true);
            _openScreens.RemoveAt(_openScreens.Count - 1);
            _activeLayouts.Remove(screen);
            _openModes.Remove(screen);
            _audioHandler?.PlayClose(screen, closingPanels);

            void CompleteClose()
            {
                RebuildPresentation();
                NotifyStateChanged();
            }

            if (_transitionHandler != null)
                _transitionHandler.PlayClose(screen, closingPanels, CompleteClose);
            else
                CompleteClose();

            return true;
        }

        public void CloseAll()
        {
            CloseAll(true);
        }

        internal void CollectVisiblePanels(List<LoogaMenuPanel> panels, bool includeCovered)
        {
            if (panels == null)
                return;

            panels.Clear();
            foreach (LoogaMenuPanel panel in _visiblePanels)
            {
                if (panel != null && (includeCovered || !panel.IsCovered))
                    panels.Add(panel);
            }
        }

        internal LoogaMenuNavigationLayer ResolveNavigationLayer(LoogaMenuNavigationPlacement placement)
        {
            LoogaMenuScreenDefinition screen = TopScreen;
            return screen != null ? screen.ResolveNavigation(GetActiveLayout(screen), placement) : null;
        }

        internal LoogaMenuActionBarSettings ResolveActionBarSettings()
        {
            LoogaMenuScreenDefinition screen = TopScreen;
            if (screen == null)
                return null;

            LoogaMenuActionBarSettings settings = _defaultActionBar;
            if (!ApplyActionBarOverride(screen.ActionBar, ref settings))
                return null;

            LoogaMenuScreenLayout layout = GetActiveLayout(screen);
            return ApplyActionBarOverride(layout?.ActionBar, ref settings) ? settings : null;
        }

        private static bool ApplyActionBarOverride(LoogaMenuActionBarOverride actionBar,
            ref LoogaMenuActionBarSettings settings)
        {
            if (actionBar == null || actionBar.Mode == LoogaMenuActionBarMode.Inherit)
                return true;

            if (actionBar.Mode == LoogaMenuActionBarMode.Hide)
                return false;

            settings = actionBar.Settings;
            return true;
        }

        private void CloseAll(bool notify)
        {
            _openScreens.Clear();
            _activeLayouts.Clear();
            _openModes.Clear();
            RebuildPresentation();
            if (notify)
                NotifyStateChanged();
        }

        private bool CanOpen(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            UnityEngine.Object requester)
        {
            if (layout == null)
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}' because it has no layout.", requester);
                return false;
            }

            if (screen.MissingPanelBehavior != LoogaMenuMissingPanelBehavior.BlockOpen)
                return true;

            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
            {
                if (entry?.Panel != null && _panels.TryGetValue(entry.Panel, out LoogaMenuPanel panel) && panel != null)
                    continue;

                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}' because a required panel is missing.",
                    requester);
                return false;
            }

            return true;
        }

        private void RebuildPresentation()
        {
            _actionBar.ReleasePanelSubscriptions();
            ClearOwnedParameters();
            _visiblePanels.Clear();

            HashSet<LoogaMenuPanel> desired = new();
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                LoogaMenuScreenLayout layout = GetActiveLayout(screen);
                AddPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), screen, desired);
                foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
                {
                    if (entry == null)
                        continue;

                    ApplyParameters(entry.Parameters);
                    AddPanel(entry.Panel, screen, desired);
                }
            }

            LoogaMenuActionBarSettings actionBar = ResolveActionBarSettings();
            AddPanel(actionBar?.Panel, TopScreen, desired);

            foreach (LoogaMenuPanel panel in _panels.Values)
            {
                if (panel == null)
                    continue;

                if (desired.Contains(panel))
                {
                    panel.Show();
                    _visiblePanels.Add(panel);
                }
                else
                {
                    panel.Hide();
                }
            }

            ApplyOverlayCoverage();
            _actionBar.RefreshActions();
        }

        private void AddPanel(LoogaMenuPanelDefinition definition, LoogaMenuScreenDefinition owner,
            HashSet<LoogaMenuPanel> desired)
        {
            if (definition == null)
                return;

            if (_panels.TryGetValue(definition, out LoogaMenuPanel panel) && panel != null)
            {
                desired.Add(panel);
                return;
            }

            if (owner != null && owner.MissingPanelBehavior == LoogaMenuMissingPanelBehavior.Warn)
                Debug.LogWarning($"Menu screen '{owner.DisplayName}' could not find panel '{definition.name}'.");
        }

        private void ApplyOverlayCoverage()
        {
            LoogaMenuScreenDefinition top = TopScreen;
            bool hasOverlay = top != null
                && _openScreens.Count > 1
                && _openModes.TryGetValue(top, out LoogaMenuOpenMode mode)
                && mode == LoogaMenuOpenMode.Overlay;

            HashSet<LoogaMenuPanel> topPanels = hasOverlay
                ? new HashSet<LoogaMenuPanel>(ResolvePanels(top, GetActiveLayout(top), includeActionBar: true))
                : null;

            foreach (LoogaMenuPanel panel in _visiblePanels)
                panel.SetCovered(hasOverlay && !topPanels.Contains(panel));
        }

        private LoogaMenuPanel[] ResolvePanels(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout,
            bool includeActionBar)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> result = new();
            AddResolvedPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), result);
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
                AddResolvedPanel(entry?.Panel, result);

            if (includeActionBar && screen == TopScreen)
                AddResolvedPanel(ResolveActionBarSettings()?.Panel, result);

            return result.ToArray();
        }

        private void AddResolvedPanel(LoogaMenuPanelDefinition definition, List<LoogaMenuPanel> panels)
        {
            if (definition != null
                && _panels.TryGetValue(definition, out LoogaMenuPanel panel)
                && panel != null
                && !panels.Contains(panel))
            {
                panels.Add(panel);
            }
        }

        private void ApplyParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            if (parameters == null)
                return;

            foreach (LoogaMenuBlackboardParameter parameter in parameters)
            {
                if (parameter == null || !parameter.TryGetValue(out LoogaBlackboardValue value))
                    continue;

                _blackboardWriter.SetValue(parameter.Key, value);
                _ownedParameterKeys.Add(parameter.Key);
            }
        }

        private void ClearOwnedParameters()
        {
            foreach (LoogaBlackboardKey key in _ownedParameterKeys)
            {
                if (key != null)
                    _blackboardWriter.RemoveValue(key);
            }

            _ownedParameterKeys.Clear();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(new LoogaMenuState(_openScreens.ToArray(), ActiveInputPolicy));
        }

        private sealed class NavigationRuntime : ILoogaMenuNavigation
        {
            private readonly LoogaMenuManager _manager;

            public NavigationRuntime(LoogaMenuManager manager, LoogaMenuNavigationPlacement placement)
            {
                _manager = manager;
                Placement = placement;
            }

            public LoogaMenuNavigationPlacement Placement { get; }
            public IReadOnlyList<LoogaMenuNavigationEntry> Entries => Layer?.Entries ?? Array.Empty<LoogaMenuNavigationEntry>();
            public bool HasEntries => Layer is { Visible: true } && Entries.Count > 0;
            public int SelectedIndex
            {
                get
                {
                    LoogaMenuNavigationLayer layer = Layer;
                    if (layer == null || !layer.Visible)
                        return -1;

                    for (int i = 0; i < layer.Entries.Length; i++)
                    {
                        LoogaMenuDestination destination = layer.Entries[i]?.Destination;
                        if (destination != null
                            && destination.Matches(_manager.TopScreen,
                                _manager.GetActiveLayout(_manager.TopScreen)))
                        {
                            return i;
                        }
                    }

                    return layer.DefaultEntryIndex;
                }
            }

            private LoogaMenuNavigationLayer Layer => _manager.ResolveNavigationLayer(Placement);

            public bool Select(int index)
            {
                LoogaMenuNavigationLayer layer = Layer;
                if (layer == null || !layer.Visible || index < 0 || index >= layer.Entries.Length)
                    return false;

                LoogaMenuNavigationEntry entry = layer.Entries[index];
                if (entry?.Destination == null || !entry.Destination.IsAssigned)
                    return false;

                if (entry.Requirements != null
                    && !entry.Requirements.CanOpen(_manager._blackboardReader, out string failedReason))
                {
                    Debug.LogWarning($"Cannot open '{entry.DisplayName}'. {failedReason}");
                    return false;
                }

                LoogaMenuDestination destination = entry.Destination;
                if (destination.Screen == _manager.TopScreen)
                    return _manager.SetLayout(destination.Screen,
                        destination.Screen.ResolveLayout(destination.Layout));

                return _manager.Open(destination);
            }

            public bool SelectRelative(int direction)
            {
                if (!HasEntries || direction == 0)
                    return false;

                int current = SelectedIndex;
                int next = (Mathf.Max(0, current) + Math.Sign(direction) + Entries.Count) % Entries.Count;
                return Select(next);
            }

        }

        private sealed class ActionBarRuntime : ILoogaMenuActionBar
        {
            private readonly LoogaMenuManager _manager;
            private readonly List<LoogaMenuActionDescriptor> _actions = new();
            private readonly List<LoogaMenuPanel> _sourcePanels = new();

            public ActionBarRuntime(LoogaMenuManager manager)
            {
                _manager = manager;
            }

            public IReadOnlyList<LoogaMenuActionDescriptor> Actions => _actions;
            public bool IsVisible => _manager.ResolveActionBarSettings()?.Panel != null;
            public event Action ActionsChanged;

            public void RefreshActions()
            {
                ReleasePanelSubscriptions();
                _actions.Clear();
                LoogaMenuActionBarSettings settings = _manager.ResolveActionBarSettings();
                if (settings == null)
                {
                    ActionsChanged?.Invoke();
                    return;
                }

                if (settings.ShowBackAction)
                {
                    _actions.Add(new LoogaMenuActionDescriptor(
                        "menu.back",
                        settings.BackLabel,
                        settings.BackInputAction,
                        settings.BackBindingFallback,
                        () => _manager.Back(),
                        true,
                        settings.BackSortOrder));
                }

                _manager.CollectVisiblePanels(_sourcePanels, settings.IncludeCoveredPanels);
                foreach (LoogaMenuPanel panel in _sourcePanels)
                {
                    if (panel == null || panel.Panel == settings.Panel)
                        continue;

                    panel.ActionsChanged += RefreshActions;
                    panel.CollectMenuActions(_actions);
                }

                _actions.Sort(CompareActions);
                RemoveDuplicateActions();
                ActionsChanged?.Invoke();
            }

            public void ReleasePanelSubscriptions()
            {
                foreach (LoogaMenuPanel panel in _sourcePanels)
                {
                    if (panel != null)
                        panel.ActionsChanged -= RefreshActions;
                }

                _sourcePanels.Clear();
            }

            private static int CompareActions(LoogaMenuActionDescriptor left, LoogaMenuActionDescriptor right)
            {
                int order = left.SortOrder.CompareTo(right.SortOrder);
                return order != 0 ? order : string.CompareOrdinal(left.Label, right.Label);
            }

            private void RemoveDuplicateActions()
            {
                HashSet<string> ids = new(StringComparer.Ordinal);
                for (int i = _actions.Count - 1; i >= 0; i--)
                {
                    string id = _actions[i].Id;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                        _actions.RemoveAt(i);
                }
            }
        }
    }
}
