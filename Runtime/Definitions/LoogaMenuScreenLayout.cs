using System;
using LoogaSoft.Inspector.Runtime;
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
        [Header("Identity")]
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [SerializeField, LoogaList]
        private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();

        [Header("Navigation")]
        [Tooltip("Layers with the same placement replace the screen layer while this layout is active.")]
        [InspectorName("Layer Overrides")]
        [SerializeField] private LoogaMenuNavigationLayer[] _navigationOverrides = Array.Empty<LoogaMenuNavigationLayer>();

        [Header("Action Bar")]
        [InspectorName("Override")]
        [SerializeField] private LoogaMenuActionBarOverride _actionBar;

        public string DisplayName => name;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuNavigationLayer[] NavigationOverrides => _navigationOverrides;
        public LoogaMenuActionBarOverride ActionBar => _actionBar;

        private void OnValidate()
        {
            _panels ??= Array.Empty<LoogaMenuScreenPanelEntry>();
            _navigationOverrides ??= Array.Empty<LoogaMenuNavigationLayer>();
        }
    }
}
