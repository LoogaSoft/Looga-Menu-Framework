using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public sealed class LoogaMenuManager
    {
        private readonly Dictionary<LoogaMenuPanelDefinition, LoogaMenuPanel> _panels = new();
        private readonly List<LoogaMenuScreenDefinition> _openScreens = new();
        private readonly List<LoogaMenuPanel> _visiblePanels = new();
        private readonly LoogaStateRegistry _stateRegistry;
        private readonly LoogaMenuPanelDefinition _defaultBackgroundPanel;
        private readonly LoogaMenuPanelDefinition _defaultActionBarPanel;

        private ILoogaMenuTransitionHandler _transitionHandler;
        private ILoogaMenuAudioHandler _audioHandler;

        public LoogaMenuManager(LoogaStateRegistry stateRegistry, LoogaMenuPanelDefinition defaultBackgroundPanel = null,
            LoogaMenuPanelDefinition defaultActionBarPanel = null)
        {
            _stateRegistry = stateRegistry;
            _defaultBackgroundPanel = defaultBackgroundPanel;
            _defaultActionBarPanel = defaultActionBarPanel;
        }

        public event Action<LoogaMenuState> StateChanged;

        public IReadOnlyList<LoogaMenuScreenDefinition> OpenScreens => _openScreens;
        public LoogaMenuInputPolicy ActiveInputPolicy => _openScreens.Count > 0
            ? _openScreens[^1].InputPolicy
            : null;

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
            {
                _panels.Remove(panel.Panel);
            }
        }

        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            if (screen == null)
                return false;

            LoogaMenuOpenContext context = new(screen, requester, payload);
            if (screen.Rules != null && !screen.Rules.CanOpen(context, _stateRegistry, out LoogaMenuRule failedRule))
            {
                string reason = failedRule != null ? failedRule.FailureReason : "Unknown rule failed.";
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}'. {reason}", requester);
                return false;
            }

            if (!CanOpenScreen(screen, requester))
                return false;

            if (screen.CloseExistingScreens)
            {
                CloseAll(false);
            }

            _openScreens.Add(screen);
            ShowScreen(screen);
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
            return true;
        }

        public bool Back()
        {
            if (_openScreens.Count == 0)
                return false;

            LoogaMenuScreenDefinition screen = _openScreens[^1];
            _openScreens.RemoveAt(_openScreens.Count - 1);
            HideScreen(screen);
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
            return true;
        }

        public void CloseAll()
        {
            CloseAll(true);
        }

        private void CloseAll(bool notify)
        {
            LoogaMenuScreenDefinition[] screens = _openScreens.ToArray();
            _openScreens.Clear();

            for (int i = screens.Length - 1; i >= 0; i--)
            {
                HideScreen(screens[i]);
            }

            if (notify)
            {
                StateChanged?.Invoke(CreateState());
            }
        }

        private void ShowScreen(LoogaMenuScreenDefinition screen)
        {
            _visiblePanels.Clear();
            List<LoogaMenuPanel> replaceablePanels = new();

            TryShowPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), LoogaMenuMissingPanelBehavior.Ignore, screen);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null)
                    continue;

                if (entry.OpenMode == LoogaMenuOpenMode.Replace)
                {
                    HidePanels(replaceablePanels);
                    replaceablePanels.Clear();
                }

                ApplyPanelParameters(entry);

                LoogaMenuPanel panel = TryShowPanel(entry.Panel, entry.MissingPanelBehavior, screen);
                if (panel != null)
                {
                    replaceablePanels.Add(panel);
                }
            }

            TryShowPanel(screen.GetActionBarPanel(_defaultActionBarPanel), LoogaMenuMissingPanelBehavior.Ignore, screen);
            LoogaMenuPanel[] panels = _visiblePanels.ToArray();
            _transitionHandler?.PlayOpen(screen, panels);
            _audioHandler?.PlayOpen(screen, panels);
        }

        private void HideScreen(LoogaMenuScreenDefinition screen)
        {
            LoogaMenuPanel[] screenPanels = ResolveScreenPanels(screen);
            _audioHandler?.PlayClose(screen, screenPanels);

            void HideUnusedPanels()
            {
                foreach (LoogaMenuPanel panel in screenPanels)
                {
                    if (!IsPanelUsedByOpenScreen(panel.Panel))
                    {
                        panel.Hide();
                    }
                }
            }

            if (_transitionHandler != null)
            {
                _transitionHandler.PlayClose(screen, screenPanels, () =>
                {
                    HideUnusedPanels();
                    RemovePanelParameters(screen);
                    ReapplyOpenScreenParameters();
                });
                return;
            }

            HideUnusedPanels();
            RemovePanelParameters(screen);
            ReapplyOpenScreenParameters();
        }

        private LoogaMenuPanel TryShowPanel(LoogaMenuPanelDefinition definition,
            LoogaMenuMissingPanelBehavior missingPanelBehavior, LoogaMenuScreenDefinition screen)
        {
            if (definition == null)
            {
                ReportMissingPanel(screen, definition, missingPanelBehavior);
                return null;
            }

            if (!_panels.TryGetValue(definition, out LoogaMenuPanel panelComponent) || panelComponent == null)
            {
                ReportMissingPanel(screen, definition, missingPanelBehavior);
                return null;
            }

            panelComponent.Show();
            _visiblePanels.Add(panelComponent);
            return panelComponent;
        }

        private void RefreshVisiblePanels()
        {
            foreach (LoogaMenuPanel panel in _panels.Values)
            {
                if (panel != null && !IsPanelUsedByOpenScreen(panel.Panel))
                {
                    panel.Hide();
                }
            }
        }

        private void RefreshCoveredViews()
        {
            HashSet<LoogaMenuPanel> topPanels =
                new(ResolveTopScreenPanels(_openScreens.Count > 0 ? _openScreens[^1] : null));

            foreach (LoogaMenuPanel panel in _panels.Values)
            {
                if (panel == null)
                    continue;

                panel.SetCovered(_openScreens.Count > 0 && !topPanels.Contains(panel));
            }
        }

        private LoogaMenuPanel[] ResolveScreenPanels(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> panels = new();
            AddPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), panels);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry != null)
                {
                    AddPanel(entry.Panel, panels);
                }
            }

            AddPanel(screen.GetActionBarPanel(_defaultActionBarPanel), panels);
            return panels.ToArray();
        }

        private LoogaMenuPanel[] ResolveTopScreenPanels(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> panels = new();
            LoogaMenuPanelDefinition backgroundPanel = screen.GetBackgroundPanel(_defaultBackgroundPanel);
            LoogaMenuPanelDefinition actionBarPanel = screen.GetActionBarPanel(_defaultActionBarPanel);

            AddPanel(backgroundPanel, panels);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null)
                    continue;

                if (entry.OpenMode == LoogaMenuOpenMode.Replace || entry.OpenMode == LoogaMenuOpenMode.AddOverlay)
                {
                    panels.RemoveAll(panel => panel != null
                        && panel.Panel != backgroundPanel
                        && panel.Panel != actionBarPanel);
                }

                AddPanel(entry.Panel, panels);
            }

            AddPanel(actionBarPanel, panels);
            return panels.ToArray();
        }

        private void AddPanel(LoogaMenuPanelDefinition definition, List<LoogaMenuPanel> panels)
        {
            if (definition != null
                && _panels.TryGetValue(definition, out LoogaMenuPanel panelComponent)
                && panelComponent != null
                && !panels.Contains(panelComponent))
            {
                panels.Add(panelComponent);
            }
        }

        private bool IsPanelUsedByOpenScreen(LoogaMenuPanelDefinition panel)
        {
            if (panel == null)
                return false;

            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                if (screen.GetBackgroundPanel(_defaultBackgroundPanel) == panel
                    || screen.GetActionBarPanel(_defaultActionBarPanel) == panel)
                    return true;

                foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
                {
                    if (entry != null && entry.Panel == panel)
                        return true;
                }
            }

            return false;
        }

        private bool CanOpenScreen(LoogaMenuScreenDefinition screen, UnityEngine.Object requester)
        {
            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null || entry.MissingPanelBehavior != LoogaMenuMissingPanelBehavior.BlockOpen)
                    continue;

                if (entry.Panel == null || !_panels.TryGetValue(entry.Panel, out LoogaMenuPanel panel) || panel == null)
                {
                    Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}' because required panel '{entry.Panel}' is missing.",
                        requester);
                    return false;
                }
            }

            return true;
        }

        private void ApplyPanelParameters(LoogaMenuScreenPanelEntry entry)
        {
            foreach (LoogaMenuBlackboardParameter parameter in entry.Parameters)
            {
                if (parameter != null && parameter.TryGetValue(out LoogaSoft.Blackboard.LoogaBlackboardValue value))
                {
                    _stateRegistry.SetValue(parameter.Key, value);
                }
            }
        }

        private void RemovePanelParameters(LoogaMenuScreenDefinition closedScreen)
        {
            foreach (LoogaMenuScreenPanelEntry entry in closedScreen.Panels)
            {
                if (entry == null)
                    continue;

                foreach (LoogaMenuBlackboardParameter parameter in entry.Parameters)
                {
                    if (parameter == null || parameter.Key == null || IsParameterUsedByOpenScreen(parameter.Key))
                        continue;

                    _stateRegistry.RemoveValue(parameter.Key);
                }
            }
        }

        private bool IsParameterUsedByOpenScreen(LoogaSoft.Blackboard.LoogaBlackboardKey key)
        {
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
                {
                    if (entry == null)
                        continue;

                    foreach (LoogaMenuBlackboardParameter parameter in entry.Parameters)
                    {
                        if (parameter != null && parameter.Key == key)
                            return true;
                    }
                }
            }

            return false;
        }

        private void ReapplyOpenScreenParameters()
        {
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
                {
                    if (entry != null)
                    {
                        ApplyPanelParameters(entry);
                    }
                }
            }
        }

        private void HidePanels(List<LoogaMenuPanel> panels)
        {
            foreach (LoogaMenuPanel panel in panels)
            {
                if (panel == null)
                    continue;

                panel.Hide();
                _visiblePanels.Remove(panel);
            }
        }

        private static void ReportMissingPanel(LoogaMenuScreenDefinition screen, LoogaMenuPanelDefinition panel,
            LoogaMenuMissingPanelBehavior missingPanelBehavior)
        {
            if (missingPanelBehavior != LoogaMenuMissingPanelBehavior.Warn)
                return;

            string panelName = panel != null ? panel.name : "Unassigned";
            Debug.LogWarning($"Menu screen '{screen.DisplayName}' could not find panel '{panelName}'.");
        }

        private LoogaMenuState CreateState()
        {
            return new LoogaMenuState(_openScreens.ToArray(), ActiveInputPolicy);
        }
    }
}

