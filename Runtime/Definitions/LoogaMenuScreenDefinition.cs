using System;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuPanelReferenceMode
    {
        UseRootDefault = 0,
        Override = 1,
        None = 2
    }

    [CreateAssetMenu(fileName = "New Menu Screen", menuName = "LoogaSoft/Menu Framework/Screen Definition")]
    public sealed class LoogaMenuScreenDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private bool _useCustomDisplayName;
        [ShowIf(nameof(_useCustomDisplayName))]
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [HideInInspector]
        [SerializeField] private LoogaMenuScreenConfiguration[] _configurations = Array.Empty<LoogaMenuScreenConfiguration>();
        [HideInInspector]
        [SerializeField] private LoogaMenuScreenConfiguration _defaultConfiguration;
        [Tooltip("Used only by screens that have not been migrated to configurations.")]
        [InspectorName("Default Panels")]
        [HideInInspector]
        [SerializeField] private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();
        [SerializeField] private LoogaMenuScreenContentEntry[] _contentEntries = Array.Empty<LoogaMenuScreenContentEntry>();
        [SerializeField]
        [FormerlySerializedAs("_features")]
        [Tooltip("Optional behaviors composed into this screen. A screen extension replaces a root default with the same extension ID.")]
        private LoogaMenuExtensionDefinition[] _extensions = Array.Empty<LoogaMenuExtensionDefinition>();

        [Header("Background")]
        [InspectorName("Background Panel Source")]
        [SerializeField] private LoogaMenuPanelReferenceMode _backgroundPanelMode = LoogaMenuPanelReferenceMode.UseRootDefault;
        [ShowIf(nameof(_backgroundPanelMode), (int)LoogaMenuPanelReferenceMode.Override)]
        [SerializeField] private LoogaMenuPanelDefinition _backgroundPanel;

        [Header("Behavior")]
        [InspectorName("Open Requirements")]
        [SerializeField] private LoogaMenuRuleSet _rules;
        [SerializeField] private LoogaMenuInputPolicy _inputPolicy;
        [SerializeField] private LoogaMenuMissingPanelBehavior _missingPanelBehavior = LoogaMenuMissingPanelBehavior.Warn;
        [SerializeField] private bool _closeAsGroupOnBack = true;
        [SerializeField] private bool _closeExistingScreens = true;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public string Description => _description;
        public LoogaMenuScreenConfiguration[] Configurations => _configurations;
        public LoogaMenuScreenConfiguration DefaultConfiguration => ResolveConfiguration(null);
        public LoogaMenuScreenPanelEntry[] DefaultPanels => _panels;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuScreenContentEntry[] ContentEntries => _contentEntries;
        public LoogaMenuExtensionDefinition[] Extensions => _extensions;
        public LoogaMenuPanelReferenceMode BackgroundPanelMode => _backgroundPanelMode;
        public LoogaMenuPanelDefinition BackgroundPanelOverride => _backgroundPanel;
        public LoogaMenuRuleSet Rules => _rules;
        public LoogaMenuMissingPanelBehavior MissingPanelBehavior => _missingPanelBehavior;
        public LoogaMenuInputPolicy InputPolicy => _inputPolicy;
        public bool CloseAsGroupOnBack => _closeAsGroupOnBack;
        public bool CloseExistingScreens => _closeExistingScreens;

        /// <summary>
        /// Resolves a requested configuration. Existing screens without configurations use their legacy composition.
        /// </summary>
        public LoogaMenuScreenConfiguration ResolveConfiguration(LoogaMenuScreenConfiguration requested)
        {
            if (requested != null && ContainsConfiguration(requested))
                return requested;

            if (_defaultConfiguration != null && ContainsConfiguration(_defaultConfiguration))
                return _defaultConfiguration;

            foreach (LoogaMenuScreenConfiguration configuration in _configurations ?? Array.Empty<LoogaMenuScreenConfiguration>())
            {
                if (configuration != null)
                    return configuration;
            }

            return null;
        }

        /// <summary>Gets the panel composition for one configuration.</summary>
        public LoogaMenuScreenPanelEntry[] GetPanels(LoogaMenuScreenConfiguration configuration)
        {
            LoogaMenuScreenConfiguration resolved = ResolveConfiguration(configuration);
            return resolved != null ? resolved.Panels : _panels;
        }

        /// <summary>Returns whether this screen owns the configuration.</summary>
        public bool ContainsConfiguration(LoogaMenuScreenConfiguration configuration)
        {
            if (configuration == null)
                return false;

            foreach (LoogaMenuScreenConfiguration candidate in _configurations ?? Array.Empty<LoogaMenuScreenConfiguration>())
            {
                if (candidate == configuration)
                    return true;
            }

            return false;
        }

        /// <summary>Finds an owned configuration by stable ID.</summary>
        public bool TryGetConfiguration(string stableId, out LoogaMenuScreenConfiguration configuration)
        {
            configuration = null;
            if (string.IsNullOrWhiteSpace(stableId))
                return false;

            foreach (LoogaMenuScreenConfiguration candidate in _configurations ?? Array.Empty<LoogaMenuScreenConfiguration>())
            {
                if (candidate == null || candidate.StableId != stableId)
                    continue;

                configuration = candidate;
                return true;
            }

            return false;
        }

        public LoogaMenuPanelDefinition GetBackgroundPanel(LoogaMenuPanelDefinition rootDefault)
        {
            return ResolveOptionalPanel(_backgroundPanelMode, _backgroundPanel, rootDefault);
        }

        /// <summary>
        /// Finds a content entry by its serialized stable ID.
        /// </summary>
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
                entry?.RefreshDefaultDisplayName();
            }

            _configurations ??= Array.Empty<LoogaMenuScreenConfiguration>();
            foreach (LoogaMenuScreenConfiguration configuration in _configurations)
            {
                configuration?.EnsureStableId();
                configuration?.RefreshDefaultDisplayName();
            }

            if (_defaultConfiguration != null && !ContainsConfiguration(_defaultConfiguration))
                _defaultConfiguration = null;

            if (_defaultConfiguration == null)
                _defaultConfiguration = ResolveConfiguration(null);

            _extensions ??= Array.Empty<LoogaMenuExtensionDefinition>();
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

