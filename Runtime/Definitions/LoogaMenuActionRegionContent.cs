using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoogaSoft.Menu
{
    /// <summary>Provides shared and context-sensitive commands for an action presenter.</summary>
    public sealed class LoogaMenuActionRegionContent : LoogaMenuRegionContent
    {
        [SerializeField] private LoogaMenuPanelDefinition _panel;
        [SerializeField] private bool _showBackAction = true;
        [SerializeField] private string _backLabel = "Back";
        [SerializeField] private InputActionReference _backInputAction;
        [SerializeField] private string _backBindingFallback = "Esc";
        [SerializeField] private int _backSortOrder = -1000;
        [SerializeField] private bool _includeCoveredPanels = true;

        public LoogaMenuPanelDefinition Panel => _panel;
        public bool ShowBackAction => _showBackAction;
        public string BackLabel => _backLabel;
        public InputActionReference BackInputAction => _backInputAction;
        public string BackBindingFallback => _backBindingFallback;
        public int BackSortOrder => _backSortOrder;
        public bool IncludeCoveredPanels => _includeCoveredPanels;

        public override void CollectPanels(List<LoogaMenuPanelDefinition> panels)
        {
            if (_panel != null && panels != null && !panels.Contains(_panel))
                panels.Add(_panel);
        }
    }
}
