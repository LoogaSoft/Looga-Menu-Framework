using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public sealed class LoogaMenuManager
    {
        private readonly Dictionary<LoogaMenuPanelDefinition, LoogaMenuView> _views = new();
        private readonly List<LoogaMenuScreenDefinition> _openScreens = new();
        private readonly List<LoogaMenuView> _visibleViews = new();
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

        public void RegisterView(LoogaMenuView view)
        {
            if (view == null || view.Panel == null)
                return;

            _views[view.Panel] = view;
            view.Hide();
        }

        public void UnregisterView(LoogaMenuView view)
        {
            if (view == null || view.Panel == null)
                return;

            if (_views.TryGetValue(view.Panel, out LoogaMenuView current) && current == view)
            {
                _views.Remove(view.Panel);
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
            _audioHandler?.PlayOpen(screen);
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
            RefreshVisibleViews();
            RefreshCoveredViews();
            _audioHandler?.PlayClose(screen);
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
                _audioHandler?.PlayClose(screen);
            }

            _openScreens.Clear();
            RefreshVisibleViews();

            if (notify)
            {
                StateChanged?.Invoke(CreateState());
            }
        }

        private void ShowScreen(LoogaMenuScreenDefinition screen)
        {
            _visibleViews.Clear();

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
            _transitionHandler?.PlayOpen(screen, _visibleViews.ToArray());
        }

        private void HideScreen(LoogaMenuScreenDefinition screen)
        {
            LoogaMenuView[] screenViews = ResolveScreenViews(screen);
            _transitionHandler?.PlayClose(screen, screenViews);

            foreach (LoogaMenuView view in screenViews)
            {
                if (!IsPanelUsedByOpenScreen(view.Panel))
                {
                    view.Hide();
                }
            }
        }

        private bool TryShowPanel(LoogaMenuPanelDefinition panel, LoogaMenuPanelMode panelMode, bool required)
        {
            if (panel == null)
                return !required;

            if (!_views.TryGetValue(panel, out LoogaMenuView view) || view == null)
                return false;

            view.Show(panelMode);
            _visibleViews.Add(view);
            return true;
        }

        private void RefreshVisibleViews()
        {
            foreach (LoogaMenuView view in _views.Values)
            {
                if (view != null && !IsPanelUsedByOpenScreen(view.Panel))
                {
                    view.Hide();
                }
            }
        }

        private void RefreshCoveredViews()
        {
            HashSet<LoogaMenuView> topViews = new(ResolveScreenViews(_openScreens.Count > 0 ? _openScreens[^1] : null));

            foreach (LoogaMenuView view in _views.Values)
            {
                if (view == null)
                    continue;

                view.SetCovered(_openScreens.Count > 0 && !topViews.Contains(view));
            }
        }

        private LoogaMenuView[] ResolveScreenViews(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuView>();

            List<LoogaMenuView> views = new();
            AddPanelView(screen.BackgroundPanel, views);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry != null)
                {
                    AddPanelView(entry.Panel, views);
                }
            }

            AddPanelView(screen.ActionBarPanel, views);
            return views.ToArray();
        }

        private void AddPanelView(LoogaMenuPanelDefinition panel, List<LoogaMenuView> views)
        {
            if (panel != null && _views.TryGetValue(panel, out LoogaMenuView view) && view != null && !views.Contains(view))
            {
                views.Add(view);
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
