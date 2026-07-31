using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public sealed class LoogaMenuManager
    {
        private readonly Dictionary<LoogaMenuPanelDefinition, LoogaMenuPanel> _panels = new();
        private readonly List<LoogaMenuScreenDefinition> _openScreens = new();
        private readonly List<LoogaMenuActiveContent> _openContent = new();
        private readonly List<LoogaMenuPanel> _visiblePanels = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, List<ILoogaMenuExtensionRuntime>> _extensions = new();
        private readonly ILoogaBlackboardReader _blackboardReader;
        private readonly ILoogaBlackboardWriter _blackboardWriter;
        private readonly LoogaMenuPanelDefinition _defaultBackgroundPanel;
        private readonly LoogaMenuPanelDefinition _defaultActionBarPanel;
        private readonly IReadOnlyList<LoogaMenuExtensionDefinition> _defaultExtensions;

        private ILoogaMenuTransitionHandler _transitionHandler;
        private ILoogaMenuAudioHandler _audioHandler;

        public LoogaMenuManager(ILoogaBlackboardReader blackboardReader, ILoogaBlackboardWriter blackboardWriter,
            LoogaMenuPanelDefinition defaultBackgroundPanel = null,
            LoogaMenuPanelDefinition defaultActionBarPanel = null,
            IReadOnlyList<LoogaMenuExtensionDefinition> defaultExtensions = null)
        {
            _blackboardReader = blackboardReader;
            _blackboardWriter = blackboardWriter;
            _defaultBackgroundPanel = defaultBackgroundPanel;
            _defaultActionBarPanel = defaultActionBarPanel;
            _defaultExtensions = defaultExtensions ?? Array.Empty<LoogaMenuExtensionDefinition>();
        }

        public event Action<LoogaMenuState> StateChanged;

        public IReadOnlyList<LoogaMenuScreenDefinition> OpenScreens => _openScreens;
        public LoogaMenuInputPolicy ActiveInputPolicy => _openScreens.Count > 0
            ? _openScreens[^1].InputPolicy
            : null;
        internal ILoogaBlackboardReader BlackboardReader => _blackboardReader;
        internal ILoogaBlackboardWriter BlackboardWriter => _blackboardWriter;

        // Compatibility accessors keep existing navigation presenters working while authoring moves to extensions.
        public LoogaMenuScreenDefinition ActiveNavigationScreen =>
            TryGetActiveNavigation(out LoogaMenuScreenDefinition screen, out _) ? screen : null;
        public IReadOnlyList<LoogaMenuNavigationEntry> ActiveNavigationEntries =>
            TryGetActiveNavigation(out _, out ILoogaMenuNavigationExtension extension)
                ? extension.Entries
                : Array.Empty<LoogaMenuNavigationEntry>();
        public int ActiveNavigationIndex =>
            TryGetActiveNavigation(out _, out ILoogaMenuNavigationExtension extension)
                ? extension.SelectedIndex
                : -1;
        public bool IsNavigationActive =>
            TryGetActiveNavigation(out _, out ILoogaMenuNavigationExtension extension) && extension.IsActive;

        public bool SelectNavigation(int index)
        {
            return TryGetActiveNavigation(out _, out ILoogaMenuNavigationExtension extension)
                && extension.Select(index);
        }

        public bool SelectRelativeNavigation(int direction)
        {
            return TryGetActiveNavigation(out _, out ILoogaMenuNavigationExtension extension)
                && extension.SelectRelative(direction);
        }

        public bool SetNavigationActive(bool active)
        {
            return TryGetTopExtension(out ILoogaMenuNavigationExtension extension) && extension.SetActive(active);
        }

        /// <summary>
        /// Finds the highest extension of the requested type in the open screen stack.
        /// </summary>
        public bool TryGetActiveExtension<TExtension>(out TExtension extension)
            where TExtension : class
        {
            return TryGetTopExtension(out extension);
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
            {
                _panels.Remove(panel.Panel);
            }
        }

        public bool OpenContent(LoogaMenuScreenContentEntry entry, UnityEngine.Object requester = null, object payload = null)
        {
            if (entry == null || _openScreens.Count == 0)
                return false;

            LoogaMenuScreenDefinition parentScreen = _openScreens[^1];
            return OpenContent(parentScreen, entry, requester, payload);
        }

        public bool OpenContent(LoogaMenuScreenDefinition screen, string contentEntryId,
            UnityEngine.Object requester = null, object payload = null)
        {
            if (screen == null || string.IsNullOrWhiteSpace(contentEntryId))
                return false;

            if (!_openScreens.Contains(screen) && !Open(screen, requester, payload))
                return false;

            if (!screen.TryGetContentEntry(contentEntryId, out LoogaMenuScreenContentEntry entry))
            {
                Debug.LogWarning(
                    $"Cannot open menu content '{contentEntryId}' because screen '{screen.DisplayName}' does not contain an entry with that ID.",
                    requester);
                return false;
            }

            return OpenContent(screen, entry, requester, payload);
        }

        private bool OpenContent(LoogaMenuScreenDefinition parentScreen, LoogaMenuScreenContentEntry entry,
            UnityEngine.Object requester, object payload)
        {
            if (parentScreen == null || entry == null)
                return false;

            foreach (LoogaMenuActiveContent activeContent in _openContent)
            {
                if (activeContent.ParentScreen == parentScreen && activeContent.Entry == entry)
                    return true;
            }

            if (entry.Rules != null && !entry.Rules.CanOpen(_blackboardReader, out string failedReason))
            {
                Debug.LogWarning($"Cannot open menu content from '{parentScreen.DisplayName}'. {failedReason}", requester);
                return false;
            }

            if (entry.OpenMode == LoogaMenuOpenMode.Replace)
            {
                CloseContentOwnedBy(parentScreen, false);
            }

            ApplyParameters(entry.Parameters);

            if (entry.TargetType == LoogaMenuContentTargetType.Screen)
                return OpenContentScreen(parentScreen, entry, requester, payload);

            LoogaMenuPanel panel = TryShowPanel(entry.Panel, parentScreen.MissingPanelBehavior, parentScreen);
            if (panel == null)
                return false;

            _openContent.Add(new LoogaMenuActiveContent(parentScreen, entry, panel, null));
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
            return true;
        }

        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            if (screen == null)
                return false;

            if (screen.Rules != null && !screen.Rules.CanOpen(_blackboardReader, out string failedReason))
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}'. {failedReason}", requester);
                return false;
            }

            if (!CanOpenScreen(screen, requester))
                return false;

            if (screen.CloseExistingScreens)
            {
                CloseAll(false);
            }

            _openScreens.Add(screen);
            AttachExtensions(screen);
            ShowScreen(screen);
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
            return true;
        }

        public bool Back()
        {
            if (_openScreens.Count == 0)
                return false;

            if (_openContent.Count > 0)
            {
                LoogaMenuActiveContent activeContent = _openContent[^1];
                if (HandleContentBack(activeContent))
                    return true;
            }

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
            LoogaMenuActiveContent[] contentEntries = _openContent.ToArray();
            for (int i = contentEntries.Length - 1; i >= 0; i--)
            {
                CloseActiveContent(contentEntries[i], false);
            }

            LoogaMenuScreenDefinition[] screens = _openScreens.ToArray();
            for (int i = screens.Length - 1; i >= 0; i--)
            {
                CloseScreen(screens[i], false);
            }

            if (notify)
            {
                StateChanged?.Invoke(CreateState());
            }
        }

        private void ShowScreen(LoogaMenuScreenDefinition screen)
        {
            _visiblePanels.Clear();

            TryShowPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), LoogaMenuMissingPanelBehavior.Ignore, screen);

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry == null)
                    continue;

                ApplyParameters(entry.Parameters);

                TryShowPanel(entry.Panel, screen.MissingPanelBehavior, screen);
            }

            ShowExtensions(screen);
            LoogaMenuPanel[] panels = _visiblePanels.ToArray();
            _transitionHandler?.PlayOpen(screen, panels);
            _audioHandler?.PlayOpen(screen, panels);
        }

        private bool OpenContentScreen(LoogaMenuScreenDefinition parentScreen, LoogaMenuScreenContentEntry entry,
            UnityEngine.Object requester, object payload)
        {
            LoogaMenuScreenDefinition screen = entry.Screen;
            if (screen == null)
            {
                ReportMissingPanel(parentScreen, null, parentScreen.MissingPanelBehavior);
                return false;
            }

            if (screen.Rules != null && !screen.Rules.CanOpen(_blackboardReader, out string failedReason))
            {
                Debug.LogWarning($"Cannot open menu screen '{screen.DisplayName}'. {failedReason}", requester);
                return false;
            }

            if (!CanOpenScreen(screen, requester))
                return false;

            _openScreens.Add(screen);
            AttachExtensions(screen);
            _openContent.Add(new LoogaMenuActiveContent(parentScreen, entry, null, screen));
            ShowScreen(screen);
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
            return true;
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
                    ReleaseExtensions(screen);
                    ReapplyOpenScreenParameters();
                });
                return;
            }

            HideUnusedPanels();
            RemovePanelParameters(screen);
            ReleaseExtensions(screen);
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

        private void RefreshCoveredViews()
        {
            HashSet<LoogaMenuPanel> overlayPanels = new(ResolveTopOverlayPanels());
            bool hasOverlay = overlayPanels.Count > 0;

            foreach (LoogaMenuPanel panel in _panels.Values)
            {
                if (panel == null)
                    continue;

                panel.SetCovered(hasOverlay && !overlayPanels.Contains(panel));
            }
        }

        private LoogaMenuPanel[] ResolveTopOverlayPanels()
        {
            for (int i = _openContent.Count - 1; i >= 0; i--)
            {
                LoogaMenuActiveContent activeContent = _openContent[i];
                if (activeContent.Entry.OpenMode != LoogaMenuOpenMode.Overlay)
                    continue;

                List<LoogaMenuPanel> contentPanels = new();
                if (activeContent.Panel != null)
                {
                    contentPanels.Add(activeContent.Panel);
                }
                else if (activeContent.Screen != null)
                {
                    contentPanels.AddRange(ResolveTopScreenPanels(activeContent.Screen));
                }

                return contentPanels.ToArray();
            }

            return Array.Empty<LoogaMenuPanel>();
        }

        private LoogaMenuPanel[] ResolveScreenPanels(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> panels = new();
            AddPanel(screen.GetBackgroundPanel(_defaultBackgroundPanel), panels);

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry != null)
                {
                    AddPanel(entry.Panel, panels);
                }
            }

            CollectExtensionPanels(screen, panels);
            return panels.ToArray();
        }

        private LoogaMenuPanel[] ResolveTopScreenPanels(LoogaMenuScreenDefinition screen)
        {
            if (screen == null)
                return Array.Empty<LoogaMenuPanel>();

            List<LoogaMenuPanel> panels = new();
            LoogaMenuPanelDefinition backgroundPanel = screen.GetBackgroundPanel(_defaultBackgroundPanel);
            AddPanel(backgroundPanel, panels);

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry == null)
                    continue;

                AddPanel(entry.Panel, panels);
            }

            CollectExtensionPanels(screen, panels);
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
                if (screen.GetBackgroundPanel(_defaultBackgroundPanel) == panel)
                    return true;

                foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
                {
                    if (entry != null && entry.Panel == panel)
                        return true;
                }

                if (ExtensionUsesPanel(screen, panel))
                    return true;
            }

            foreach (LoogaMenuActiveContent content in _openContent)
            {
                if (content.Panel != null && content.Panel.Panel == panel)
                    return true;
            }

            return false;
        }

        private bool CanOpenScreen(LoogaMenuScreenDefinition screen, UnityEngine.Object requester)
        {
            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry == null || screen.MissingPanelBehavior != LoogaMenuMissingPanelBehavior.BlockOpen)
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

        private void ApplyParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            foreach (LoogaMenuBlackboardParameter parameter in parameters)
            {
                if (parameter != null && parameter.TryGetValue(out LoogaSoft.Blackboard.LoogaBlackboardValue value))
                {
                    _blackboardWriter.SetValue(parameter.Key, value);
                }
            }
        }

        private void RemovePanelParameters(LoogaMenuScreenDefinition closedScreen)
        {
            foreach (LoogaMenuScreenPanelEntry entry in closedScreen.DefaultPanels)
            {
                if (entry == null)
                    continue;

                foreach (LoogaMenuBlackboardParameter parameter in entry.Parameters)
                {
                    if (parameter == null || parameter.Key == null || IsParameterUsedByOpenScreen(parameter.Key))
                        continue;

                    _blackboardWriter.RemoveValue(parameter.Key);
                }
            }

        }

        private bool IsParameterUsedByOpenScreen(LoogaSoft.Blackboard.LoogaBlackboardKey key)
        {
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
                {
                    if (entry == null)
                        continue;

                    foreach (LoogaMenuBlackboardParameter parameter in entry.Parameters)
                    {
                        if (parameter != null && parameter.Key == key)
                            return true;
                    }
                }

                if (ExtensionUsesParameter(screen, key))
                    return true;
            }

            foreach (LoogaMenuActiveContent content in _openContent)
            {
                foreach (LoogaMenuBlackboardParameter parameter in content.Entry.Parameters)
                {
                    if (parameter != null && parameter.Key == key)
                        return true;
                }
            }

            return false;
        }

        private void ReapplyOpenScreenParameters()
        {
            foreach (LoogaMenuScreenDefinition screen in _openScreens)
            {
                foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
                {
                    if (entry != null)
                    {
                        ApplyParameters(entry.Parameters);
                    }
                }

                ReapplyExtensionParameters(screen);
            }

            foreach (LoogaMenuActiveContent content in _openContent)
            {
                ApplyParameters(content.Entry.Parameters);
            }
        }

        private bool HandleContentBack(LoogaMenuActiveContent activeContent)
        {
            switch (activeContent.Entry.BackBehavior)
            {
                case LoogaMenuContentBackBehavior.CloseWholeFlow:
                    CloseAll();
                    return true;

                case LoogaMenuContentBackBehavior.CloseParent:
                    CloseActiveContent(activeContent, false);
                    CloseScreen(activeContent.ParentScreen, false);
                    RefreshCoveredViews();
                    StateChanged?.Invoke(CreateState());
                    return true;

                case LoogaMenuContentBackBehavior.CloseThisEntry:
                case LoogaMenuContentBackBehavior.ReturnToParent:
                default:
                    CloseActiveContent(activeContent, true);
                    return true;
            }
        }

        private void CloseContentOwnedBy(LoogaMenuScreenDefinition parentScreen, bool notify)
        {
            for (int i = _openContent.Count - 1; i >= 0; i--)
            {
                LoogaMenuActiveContent content = _openContent[i];
                if (content.ParentScreen == parentScreen)
                {
                    CloseActiveContent(content, false);
                }
            }

            if (notify)
            {
                RefreshCoveredViews();
                StateChanged?.Invoke(CreateState());
            }
        }

        private void CloseActiveContent(LoogaMenuActiveContent activeContent, bool notify)
        {
            _openContent.Remove(activeContent);

            if (activeContent.Screen != null)
            {
                CloseScreen(activeContent.Screen, false);
            }

            if (activeContent.Panel != null && !IsPanelUsedByOpenScreen(activeContent.Panel.Panel))
            {
                activeContent.Panel.Hide();
            }

            RemoveParameters(activeContent.Entry.Parameters);
            ReapplyOpenScreenParameters();

            if (notify)
            {
                RefreshCoveredViews();
                StateChanged?.Invoke(CreateState());
            }
        }

        private void CloseScreen(LoogaMenuScreenDefinition screen, bool notify)
        {
            if (screen == null)
                return;

            CloseContentOwnedBy(screen, false);
            _openScreens.Remove(screen);
            HideScreen(screen);

            if (notify)
            {
                RefreshCoveredViews();
                StateChanged?.Invoke(CreateState());
            }
        }

        private void RemoveParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            foreach (LoogaMenuBlackboardParameter parameter in parameters)
            {
                if (parameter == null || parameter.Key == null || IsParameterUsedByOpenScreen(parameter.Key))
                    continue;

                _blackboardWriter.RemoveValue(parameter.Key);
            }
        }

        private void AttachExtensions(LoogaMenuScreenDefinition screen)
        {
            List<LoogaMenuExtensionDefinition> definitions = ResolveExtensionDefinitions(screen);
            List<ILoogaMenuExtensionRuntime> runtimes = new(definitions.Count + 2);

            foreach (LoogaMenuExtensionDefinition definition in definitions)
            {
                if (definition == null || !definition.Enabled)
                    continue;

                ILoogaMenuExtensionRuntime runtime = definition.CreateRuntime();
                if (runtime == null)
                    continue;

                runtime.Attach(new LoogaMenuExtensionContext(this, screen));
                runtimes.Add(runtime);
            }

            AddLegacyExtensionRuntimes(screen, definitions, runtimes);
            _extensions[screen] = runtimes;
        }

        private List<LoogaMenuExtensionDefinition> ResolveExtensionDefinitions(LoogaMenuScreenDefinition screen)
        {
            List<LoogaMenuExtensionDefinition> definitions = new();
            Dictionary<string, int> indicesById = new(StringComparer.Ordinal);

            AddOrReplaceExtensionDefinitions(_defaultExtensions, definitions, indicesById);
            AddOrReplaceExtensionDefinitions(screen.Extensions, definitions, indicesById);
            return definitions;
        }

        private static void AddOrReplaceExtensionDefinitions(
            IReadOnlyList<LoogaMenuExtensionDefinition> source,
            List<LoogaMenuExtensionDefinition> destination,
            Dictionary<string, int> indicesById)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                LoogaMenuExtensionDefinition definition = source[i];
                if (definition == null)
                    continue;

                string extensionId = definition.ExtensionId;
                if (string.IsNullOrWhiteSpace(extensionId))
                    extensionId = definition.GetType().FullName;

                if (indicesById.TryGetValue(extensionId, out int existingIndex))
                {
                    destination[existingIndex] = definition;
                    continue;
                }

                indicesById.Add(extensionId, destination.Count);
                destination.Add(definition);
            }
        }

        private void AddLegacyExtensionRuntimes(
            LoogaMenuScreenDefinition screen,
            IReadOnlyList<LoogaMenuExtensionDefinition> definitions,
            List<ILoogaMenuExtensionRuntime> runtimes)
        {
            bool hasNavigation = ContainsExtension(definitions, LoogaMenuNavigationExtension.Id);
            if (!hasNavigation && screen.NavigationEntries.Length > 0)
            {
                LoogaMenuNavigationExtensionRuntime navigation = new(
                    screen.NavigationEntries,
                    screen.ActivateNavigationOnOpen);
                navigation.Attach(new LoogaMenuExtensionContext(this, screen));
                runtimes.Add(navigation);
            }

            bool hasActionBar = ContainsExtension(definitions, LoogaMenuActionBarExtension.Id);
            if (hasActionBar)
                return;

            LoogaMenuPanelDefinition actionBarPanel = screen.GetActionBarPanel(_defaultActionBarPanel);
            if (actionBarPanel == null)
                return;

            LoogaMenuActionBarExtensionRuntime actionBar = new(actionBarPanel);
            actionBar.Attach(new LoogaMenuExtensionContext(this, screen));
            runtimes.Add(actionBar);
        }

        private static bool ContainsExtension(
            IReadOnlyList<LoogaMenuExtensionDefinition> definitions,
            string extensionId)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                LoogaMenuExtensionDefinition definition = definitions[i];
                if (definition != null && definition.ExtensionId == extensionId)
                    return true;
            }

            return false;
        }

        private void ShowExtensions(LoogaMenuScreenDefinition screen)
        {
            if (!_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                runtime.Show();
            }
        }

        private void ReleaseExtensions(LoogaMenuScreenDefinition screen)
        {
            if (!_extensions.Remove(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                runtime.Release();
            }
        }

        private void CollectExtensionPanels(
            LoogaMenuScreenDefinition screen,
            List<LoogaMenuPanel> panels)
        {
            if (!_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                runtime.CollectPanels(panels);
            }
        }

        private bool ExtensionUsesPanel(
            LoogaMenuScreenDefinition screen,
            LoogaMenuPanelDefinition panel)
        {
            if (!_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return false;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                if (runtime.UsesPanel(panel))
                    return true;
            }

            return false;
        }

        private bool ExtensionUsesParameter(
            LoogaMenuScreenDefinition screen,
            LoogaBlackboardKey key)
        {
            if (!_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return false;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                if (runtime.UsesParameter(key))
                    return true;
            }

            return false;
        }

        private void ReapplyExtensionParameters(LoogaMenuScreenDefinition screen)
        {
            if (!_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
                return;

            foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
            {
                runtime.ReapplyParameters();
            }
        }

        private bool TryGetActiveNavigation(
            out LoogaMenuScreenDefinition screen,
            out ILoogaMenuNavigationExtension extension)
        {
            for (int i = _openScreens.Count - 1; i >= 0; i--)
            {
                screen = _openScreens[i];
                if (!TryGetExtension(screen, out extension) || !extension.IsActive)
                    continue;

                return true;
            }

            screen = null;
            extension = null;
            return false;
        }

        private bool TryGetTopExtension<TExtension>(out TExtension extension)
            where TExtension : class
        {
            for (int i = _openScreens.Count - 1; i >= 0; i--)
            {
                if (TryGetExtension(_openScreens[i], out extension))
                    return true;
            }

            extension = null;
            return false;
        }

        private bool TryGetExtension<TExtension>(
            LoogaMenuScreenDefinition screen,
            out TExtension extension)
            where TExtension : class
        {
            if (_extensions.TryGetValue(screen, out List<ILoogaMenuExtensionRuntime> runtimes))
            {
                foreach (ILoogaMenuExtensionRuntime runtime in runtimes)
                {
                    if (runtime is TExtension typedExtension)
                    {
                        extension = typedExtension;
                        return true;
                    }
                }
            }

            extension = null;
            return false;
        }

        internal LoogaMenuPanel ShowExtensionPanel(
            LoogaMenuPanelDefinition panel,
            LoogaMenuScreenDefinition screen)
        {
            return TryShowPanel(panel, screen.MissingPanelBehavior, screen);
        }

        internal void HideExtensionPanelWhenUnused(LoogaMenuPanelDefinition panel)
        {
            if (panel == null
                || !_panels.TryGetValue(panel, out LoogaMenuPanel panelComponent)
                || panelComponent == null
                || IsPanelUsedByOpenScreen(panel))
                return;

            panelComponent.Hide();
        }

        internal void AddExtensionPanel(
            LoogaMenuPanelDefinition panel,
            List<LoogaMenuPanel> panels)
        {
            AddPanel(panel, panels);
        }

        internal void ApplyExtensionParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            ApplyParameters(parameters);
        }

        internal void RemoveExtensionParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            RemoveParameters(parameters);
        }

        internal void RefreshAfterExtensionChange()
        {
            ReapplyOpenScreenParameters();
            RefreshCoveredViews();
            StateChanged?.Invoke(CreateState());
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

        private sealed class LoogaMenuActiveContent
        {
            public LoogaMenuActiveContent(LoogaMenuScreenDefinition parentScreen, LoogaMenuScreenContentEntry entry,
                LoogaMenuPanel panel, LoogaMenuScreenDefinition screen)
            {
                ParentScreen = parentScreen;
                Entry = entry;
                Panel = panel;
                Screen = screen;
            }

            public LoogaMenuScreenDefinition ParentScreen { get; }
            public LoogaMenuScreenContentEntry Entry { get; }
            public LoogaMenuPanel Panel { get; }
            public LoogaMenuScreenDefinition Screen { get; }
        }
    }
}

