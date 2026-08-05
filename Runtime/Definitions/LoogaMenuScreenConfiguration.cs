using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Defines one named composition of a menu screen. Configurations are sub-assets of their owning screen.
    /// </summary>
    public sealed class LoogaMenuScreenConfiguration : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _stableId;
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [SerializeField] private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();
        [Tooltip("Configuration extensions replace matching screen or root extensions by extension ID.")]
        [SerializeField] private LoogaMenuExtensionDefinition[] _extensions = Array.Empty<LoogaMenuExtensionDefinition>();

        [Header("Initial State")]
        [Tooltip("Optional stable ID of the navigation entry selected when this configuration opens.")]
        [SerializeField] private string _initialNavigationEntryId;

        public string StableId => _stableId;
        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuExtensionDefinition[] Extensions => _extensions;
        public string InitialNavigationEntryId => _initialNavigationEntryId;

        internal void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(_stableId))
                _stableId = Guid.NewGuid().ToString("N");
        }

        internal void RefreshDefaultDisplayName()
        {
            if (!_useCustomDisplayName)
                _displayName = name;
        }

        private void OnValidate()
        {
            EnsureStableId();
            RefreshDefaultDisplayName();
            _panels ??= Array.Empty<LoogaMenuScreenPanelEntry>();
            _extensions ??= Array.Empty<LoogaMenuExtensionDefinition>();
        }
    }
}
