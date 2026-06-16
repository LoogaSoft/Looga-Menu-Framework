using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Screen", menuName = "LoogaSoft/Menu/Screen Definition")]
    public sealed class LoogaMenuScreenDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Composition")]
        [SerializeField] private LoogaMenuScreenPanelEntry[] _panels = Array.Empty<LoogaMenuScreenPanelEntry>();
        [SerializeField] private LoogaMenuPanelDefinition _backgroundPanel;
        [SerializeField] private LoogaMenuPanelDefinition _actionBarPanel;

        [Header("Rules")]
        [SerializeField] private LoogaMenuRuleSet _rules;

        [Header("Behavior")]
        [SerializeField] private LoogaMenuInputPolicy _inputPolicy;
        [SerializeField] private bool _closeAsGroupOnBack = true;
        [SerializeField] private bool _closeExistingScreens = true;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public LoogaMenuScreenPanelEntry[] Panels => _panels;
        public LoogaMenuPanelDefinition BackgroundPanel => _backgroundPanel;
        public LoogaMenuPanelDefinition ActionBarPanel => _actionBarPanel;
        public LoogaMenuRuleSet Rules => _rules;
        public LoogaMenuInputPolicy InputPolicy => _inputPolicy;
        public bool CloseAsGroupOnBack => _closeAsGroupOnBack;
        public bool CloseExistingScreens => _closeExistingScreens;
    }
}
