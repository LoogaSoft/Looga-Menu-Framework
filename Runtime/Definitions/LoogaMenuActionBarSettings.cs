using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoogaSoft.Menu
{
    /// <summary>Provides the actions shown by the shared menu action-bar view.</summary>
    public interface ILoogaMenuActionBar
    {
        IReadOnlyList<LoogaMenuActionDescriptor> Actions { get; }
        event Action ActionsChanged;
        void RefreshActions();
    }

    public enum LoogaMenuActionBarMode
    {
        Inherit = 0,
        Override = 1,
        Hide = 2
    }

    [Serializable]
    public sealed class LoogaMenuActionBarSettings
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
    }

    [Serializable]
    public sealed class LoogaMenuActionBarOverride
    {
        [SerializeField] private LoogaMenuActionBarMode _mode = LoogaMenuActionBarMode.Inherit;
        [SerializeField] private LoogaMenuActionBarSettings _settings = new();

        public LoogaMenuActionBarMode Mode => _mode;
        public LoogaMenuActionBarSettings Settings => _settings;
    }
}
