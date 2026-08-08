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

    /// <summary>
    /// Defines one menu destination. A screen owns its layouts, navigation entries, and menu behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "New Menu Screen", menuName = "LoogaSoft/Menu Framework/Screen Definition")]
    public sealed class LoogaMenuScreenDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, TextArea] private string _description;

        [Header("Layouts")]
        [FormerlySerializedAs("_configurations")]
        [SerializeField] private LoogaMenuScreenLayout[] _layouts = Array.Empty<LoogaMenuScreenLayout>();
        [FormerlySerializedAs("_defaultConfiguration")]
        [SerializeField] private LoogaMenuScreenLayout _defaultLayout;

        [Header("Navigation")]
        [Tooltip("Entries contributed to the shared navigation presenter while this screen is active.")]
        [InspectorName("Layers")]
        [SerializeField] private LoogaMenuNavigationLayer[] _navigation = Array.Empty<LoogaMenuNavigationLayer>();

        [Header("Action Bar")]
        [InspectorName("Override")]
        [SerializeField] private LoogaMenuActionBarOverride _actionBar;

        [Header("Background")]
        [InspectorName("Background Panel Source")]
        [SerializeField] private LoogaMenuPanelReferenceMode _backgroundPanelMode = LoogaMenuPanelReferenceMode.UseRootDefault;
        [ShowIf(nameof(_backgroundPanelMode), (int)LoogaMenuPanelReferenceMode.Override)]
        [SerializeField] private LoogaMenuPanelDefinition _backgroundPanel;

        [Header("Behavior")]
        [InspectorName("Open Requirements")]
        [SerializeField] private LoogaMenuRuleSet _rules;
        [SerializeField] private LoogaMenuInputPolicy _inputPolicy;
        [SerializeField] private LoogaMenuOpenMode _defaultOpenMode = LoogaMenuOpenMode.Replace;
        [SerializeField] private LoogaMenuMissingPanelBehavior _missingPanelBehavior = LoogaMenuMissingPanelBehavior.Warn;

        public string DisplayName => name;
        public string Description => _description;
        public LoogaMenuScreenLayout[] Layouts => _layouts;
        public LoogaMenuScreenLayout DefaultLayout => ResolveLayout(null);
        public LoogaMenuNavigationLayer[] Navigation => _navigation;
        public LoogaMenuActionBarOverride ActionBar => _actionBar;
        public LoogaMenuPanelReferenceMode BackgroundPanelMode => _backgroundPanelMode;
        public LoogaMenuPanelDefinition BackgroundPanelOverride => _backgroundPanel;
        public LoogaMenuRuleSet Rules => _rules;
        public LoogaMenuInputPolicy InputPolicy => _inputPolicy;
        public LoogaMenuOpenMode DefaultOpenMode => _defaultOpenMode;
        public LoogaMenuMissingPanelBehavior MissingPanelBehavior => _missingPanelBehavior;

        public LoogaMenuScreenLayout ResolveLayout(LoogaMenuScreenLayout requested)
        {
            if (requested != null && ContainsLayout(requested))
                return requested;

            if (_defaultLayout != null && ContainsLayout(_defaultLayout))
                return _defaultLayout;

            foreach (LoogaMenuScreenLayout layout in _layouts ?? Array.Empty<LoogaMenuScreenLayout>())
            {
                if (layout != null)
                    return layout;
            }

            return null;
        }

        public LoogaMenuScreenPanelEntry[] GetPanels(LoogaMenuScreenLayout layout)
        {
            return ResolveLayout(layout)?.Panels ?? Array.Empty<LoogaMenuScreenPanelEntry>();
        }

        public bool ContainsLayout(LoogaMenuScreenLayout layout)
        {
            if (layout == null)
                return false;

            foreach (LoogaMenuScreenLayout candidate in _layouts ?? Array.Empty<LoogaMenuScreenLayout>())
            {
                if (candidate == layout)
                    return true;
            }

            return false;
        }

        public LoogaMenuPanelDefinition GetBackgroundPanel(LoogaMenuPanelDefinition rootDefault)
        {
            return _backgroundPanelMode switch
            {
                LoogaMenuPanelReferenceMode.UseRootDefault => rootDefault != null ? rootDefault : _backgroundPanel,
                LoogaMenuPanelReferenceMode.Override => _backgroundPanel,
                _ => null
            };
        }

        public LoogaMenuNavigationLayer ResolveNavigation(LoogaMenuScreenLayout layout,
            LoogaMenuNavigationPlacement placement)
        {
            LoogaMenuScreenLayout resolvedLayout = ResolveLayout(layout);
            LoogaMenuNavigationLayer layoutLayer = FindNavigation(resolvedLayout?.NavigationOverrides, placement);
            return layoutLayer ?? FindNavigation(_navigation, placement);
        }

        private void OnValidate()
        {
            _layouts ??= Array.Empty<LoogaMenuScreenLayout>();
            _navigation ??= Array.Empty<LoogaMenuNavigationLayer>();
            if (_defaultLayout != null && !ContainsLayout(_defaultLayout))
                _defaultLayout = null;

            _defaultLayout ??= ResolveLayout(null);
        }

        private static LoogaMenuNavigationLayer FindNavigation(
            LoogaMenuNavigationLayer[] layers,
            LoogaMenuNavigationPlacement placement)
        {
            foreach (LoogaMenuNavigationLayer layer in layers ?? Array.Empty<LoogaMenuNavigationLayer>())
            {
                if (layer != null && layer.Placement == placement)
                    return layer;
            }

            return null;
        }
    }
}
