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

        [Header("Regions")]
        [SerializeField] private LoogaMenuRegionOverride[] _regionOverrides = Array.Empty<LoogaMenuRegionOverride>();

        public string DisplayName => name;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuRegionOverride[] RegionOverrides => _regionOverrides;

        private void OnValidate()
        {
            _panels ??= Array.Empty<LoogaMenuScreenPanelEntry>();
            _regionOverrides ??= Array.Empty<LoogaMenuRegionOverride>();
        }
    }
}
