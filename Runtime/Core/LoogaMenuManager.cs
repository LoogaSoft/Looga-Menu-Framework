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

        private ILoogaMenuTransitionHandler _transitionHandler;
        private ILoogaMenuAudioHandler _audioHandler;

        public LoogaMenuManager(LoogaStateRegistry stateRegistry)
        {
            _stateRegistry = stateRegistry;
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
            for (int i = _openScreens.Count - 1; i >= 0; i--)
            {
                LoogaMenuScreenDefinition screen = _openScreens[i];
                HideScreen(screen);
            }

            _openScreens.Clear();

            if (notify)
            {
                StateChanged?.Invoke(CreateState());
            }
        }

        private void ShowScreen(LoogaMenuScreenDefinition screen)
        {
            _visiblePanels.Clear();

            TryShowPanel(screen.BackgroundPanel, null, false);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null)
                    continue;

                bool shown = TryShowPanel(entry.Panel, entry.PanelMode, entry.Required);
                if (!shown && entry.Required)
                {
                    Debug.LogWarning($"Menu screen '{screen.DisplayName}' could not find required panel '{entry.Panel}'.");
                }
            }

            TryShowPanel(screen.ActionBarPanel, null, false);
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
                _transitionHandler.PlayClose(screen, screenPanels, HideUnusedPanels);
                return;
            }

            HideUnusedPanels();
        }

        private bool TryShowPanel(LoogaMenuPanelDefinition definition, LoogaMenuPanelMode panelMode, bool required)
        {
            if (definition == null)
                return !required;

            if (!_panels.TryGetValue(definition, out LoogaMenuPanel panelComponent) || panelComponent == null)
                return false;

            panelComponent.Show(panelMode);
            _visiblePanels.Add(panelComponent);
            return true;
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
                new(ResolveScreenPanels(_openScreens.Count > 0 ? _openScreens[^1] : null));

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
            AddPanel(screen.BackgroundPanel, panels);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry != null)
                {
                    AddPanel(entry.Panel, panels);
                }
            }

            AddPanel(screen.ActionBarPanel, panels);
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
                if (screen.BackgroundPanel == panel || screen.ActionBarPanel == panel)
                    return true;

                foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
                {
                    if (entry != null && entry.Panel == panel)
                        return true;
                }
            }

            return false;
        }

        private LoogaMenuState CreateState()
        {
            return new LoogaMenuState(_openScreens.ToArray(), ActiveInputPolicy);
        }
    }
}

