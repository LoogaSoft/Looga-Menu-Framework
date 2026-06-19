using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuPanelReferenceMode
    {
        UseRootDefault = 0,
        Override = 1,
        None = 2
    }

    [CreateAssetMenu(fileName = "New Menu Screen", menuName = "LoogaSoft/Menu/Screen Definition")]
    public sealed class LoogaMenuScreenDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [SerializeField] private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();
        [SerializeField] private LoogaMenuScreenContentEntry[] _contentEntries = Array.Empty<LoogaMenuScreenContentEntry>();
        [SerializeField] private LoogaMenuPanelReferenceMode _backgroundPanelMode = LoogaMenuPanelReferenceMode.UseRootDefault;
        [SerializeField] private LoogaMenuPanelDefinition _backgroundPanel;
        [SerializeField] private LoogaMenuPanelReferenceMode _actionBarPanelMode = LoogaMenuPanelReferenceMode.UseRootDefault;
        [SerializeField] private LoogaMenuPanelDefinition _actionBarPanel;

        [Header("Behavior")]
        [SerializeField] private LoogaMenuRuleSet _rules;
        [SerializeField] private LoogaMenuInputPolicy _inputPolicy;
        [SerializeField] private LoogaMenuMissingPanelBehavior _missingPanelBehavior = LoogaMenuMissingPanelBehavior.Warn;
        [SerializeField] private bool _closeAsGroupOnBack = true;
        [SerializeField] private bool _closeExistingScreens = true;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] DefaultPanels => _panels;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuScreenContentEntry[] ContentEntries => _contentEntries;
        public LoogaMenuPanelReferenceMode BackgroundPanelMode => _backgroundPanelMode;
        public LoogaMenuPanelDefinition BackgroundPanelOverride => _backgroundPanel;
        public LoogaMenuPanelReferenceMode ActionBarPanelMode => _actionBarPanelMode;
        public LoogaMenuPanelDefinition ActionBarPanelOverride => _actionBarPanel;
        public LoogaMenuRuleSet Rules => _rules;
        public LoogaMenuMissingPanelBehavior MissingPanelBehavior => _missingPanelBehavior;
        public LoogaMenuInputPolicy InputPolicy => _inputPolicy;
        public bool CloseAsGroupOnBack => _closeAsGroupOnBack;
        public bool CloseExistingScreens => _closeExistingScreens;

        public LoogaMenuPanelDefinition GetBackgroundPanel(LoogaMenuPanelDefinition rootDefault)
        {
            return ResolveOptionalPanel(_backgroundPanelMode, _backgroundPanel, rootDefault);
        }

        public LoogaMenuPanelDefinition GetActionBarPanel(LoogaMenuPanelDefinition rootDefault)
        {
            return ResolveOptionalPanel(_actionBarPanelMode, _actionBarPanel, rootDefault);
        }

        public bool TryGetContentEntry(string stableId, out LoogaMenuScreenContentEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(stableId))
                return false;

            foreach (LoogaMenuScreenContentEntry candidate in _contentEntries)
            {
                if (candidate == null || candidate.StableId != stableId)
                    continue;

                entry = candidate;
                return true;
            }

            return false;
        }

        private void OnValidate()
        {
            if (!_useCustomDisplayName)
            {
                _displayName = name;
            }

            foreach (LoogaMenuScreenContentEntry entry in _contentEntries)
            {
                entry?.EnsureStableId();
            }
        }

        private static LoogaMenuPanelDefinition ResolveOptionalPanel(LoogaMenuPanelReferenceMode mode,
            LoogaMenuPanelDefinition overridePanel, LoogaMenuPanelDefinition rootDefault)
        {
            return mode switch
            {
                LoogaMenuPanelReferenceMode.UseRootDefault => rootDefault != null ? rootDefault : overridePanel,
                LoogaMenuPanelReferenceMode.Override => overridePanel,
                LoogaMenuPanelReferenceMode.None => null,
                _ => null
            };
        }
    }
}

