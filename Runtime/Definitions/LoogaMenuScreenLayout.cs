using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Defines one named panel composition within a screen. Changing a layout does not add a history entry.
    /// </summary>
    [MovedFrom(true, "LoogaSoft.Menu", "LoogaSoft.Menu.Runtime", "LoogaMenuScreenConfiguration")]
    public sealed class LoogaMenuScreenLayout : ScriptableObject
    {
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [SerializeField] private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();

        [Header("Navigation")]
        [Tooltip("Layers with the same placement replace the screen layer while this layout is active.")]
        [SerializeField] private LoogaMenuNavigationLayer[] _navigationOverrides = Array.Empty<LoogaMenuNavigationLayer>();

        [Header("Action Bar")]
        [SerializeField] private LoogaMenuActionBarOverride _actionBar;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuNavigationLayer[] NavigationOverrides => _navigationOverrides;
        public LoogaMenuActionBarOverride ActionBar => _actionBar;

        private void OnValidate()
        {
            if (!_useCustomDisplayName)
                _displayName = name;

            _panels ??= Array.Empty<LoogaMenuScreenPanelEntry>();
            _navigationOverrides ??= Array.Empty<LoogaMenuNavigationLayer>();
        }
    }
}
