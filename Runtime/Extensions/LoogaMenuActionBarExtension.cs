using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Runtime action collection consumed by an action-bar presenter.
    /// </summary>
    public interface ILoogaMenuActionBarExtension
    {
        IReadOnlyList<LoogaMenuActionDescriptor> Actions { get; }
        event Action ActionsChanged;
        void RefreshActions();
    }

    [CreateAssetMenu(
        fileName = "New Action Bar Extension",
        menuName = "LoogaSoft/Menu Framework/Extensions/Action Bar")]
    public sealed class LoogaMenuActionBarExtension : LoogaMenuExtensionDefinition
    {
        public const string Id = "LoogaSoft.Menu.ActionBar";

        [Header("Panel")]
        [SerializeField]
        [Tooltip("Panel presented as the action bar while the owning screen is open.")]
        private LoogaMenuPanelDefinition _panel;

        [Header("Back Action")]
        [SerializeField] private bool _showBackAction = true;
        [SerializeField] private string _backLabel = "Back";
        [SerializeField] private InputActionReference _backInputAction;
        [SerializeField] private string _backBindingFallback = "Esc";
        [SerializeField] private int _backSortOrder = -1000;

        [Header("Context")]
        [SerializeField]
        [Tooltip("Include actions contributed by panels covered by an overlay.")]
        private bool _includeCoveredPanels = true;

        public override string ExtensionId => Id;
        public LoogaMenuPanelDefinition Panel => _panel;

        public override ILoogaMenuExtensionRuntime CreateRuntime()
        {
            return new LoogaMenuActionBarExtensionRuntime(
                _panel,
                _showBackAction,
                _backLabel,
                _backInputAction,
                _backBindingFallback,
                _backSortOrder,
                _includeCoveredPanels);
        }
    }

    internal sealed class LoogaMenuActionBarExtensionRuntime :
        ILoogaMenuExtensionRuntime,
        ILoogaMenuActionBarExtension
    {
        private readonly LoogaMenuPanelDefinition _panel;
        private readonly bool _showBackAction;
        private readonly string _backLabel;
        private readonly InputActionReference _backInputAction;
        private readonly string _backBindingFallback;
        private readonly int _backSortOrder;
        private readonly bool _includeCoveredPanels;
        private readonly List<LoogaMenuActionDescriptor> _actions = new();
        private readonly List<LoogaMenuPanel> _sourcePanels = new();
        private LoogaMenuExtensionContext _context;
        private Action _backAction;

        public LoogaMenuActionBarExtensionRuntime(LoogaMenuPanelDefinition panel)
            : this(panel, true, "Back", null, "Esc", -1000, true)
        {
        }

        public LoogaMenuActionBarExtensionRuntime(
            LoogaMenuPanelDefinition panel,
            bool showBackAction,
            string backLabel,
            InputActionReference backInputAction,
            string backBindingFallback,
            int backSortOrder,
            bool includeCoveredPanels)
        {
            _panel = panel;
            _showBackAction = showBackAction;
            _backLabel = backLabel;
            _backInputAction = backInputAction;
            _backBindingFallback = backBindingFallback;
            _backSortOrder = backSortOrder;
            _includeCoveredPanels = includeCoveredPanels;
        }

        public IReadOnlyList<LoogaMenuActionDescriptor> Actions => _actions;

        public event Action ActionsChanged;

        public void Attach(LoogaMenuExtensionContext context)
        {
            _context = context;
            _backAction = Back;
        }

        public void Show()
        {
            if (_panel != null)
            {
                _context.ShowPanel(_panel);
            }

            RefreshActions();
        }

        public void RefreshActions()
        {
            if (_context == null)
                return;

            UnsubscribePanels();
            _actions.Clear();

            if (_showBackAction)
            {
                _actions.Add(new LoogaMenuActionDescriptor(
                    "menu.back",
                    _backLabel,
                    _backInputAction,
                    _backBindingFallback,
                    _backAction,
                    true,
                    _backSortOrder));
            }

            _context.CollectVisiblePanels(_sourcePanels, _includeCoveredPanels);
            foreach (LoogaMenuPanel panel in _sourcePanels)
            {
                if (panel == null || panel.Panel == _panel)
                    continue;

                panel.ActionsChanged += OnPanelActionsChanged;
                panel.CollectMenuActions(_actions);
            }

            _actions.Sort(CompareActions);
            RemoveDuplicateActions();
            ActionsChanged?.Invoke();
        }

        public void CollectPanels(List<LoogaMenuPanel> panels)
        {
            _context.AddPanel(_panel, panels);
        }

        public bool UsesPanel(LoogaMenuPanelDefinition panel)
        {
            return panel != null && panel == _panel;
        }

        public bool UsesParameter(LoogaBlackboardKey key)
        {
            return false;
        }

        public void ReapplyParameters()
        {
        }

        public void Release()
        {
            UnsubscribePanels();
            _actions.Clear();
            _context = null;
            _backAction = null;
            ActionsChanged?.Invoke();
        }

        private void Back()
        {
            _context?.Back();
        }

        private void OnPanelActionsChanged()
        {
            RefreshActions();
        }

        private void UnsubscribePanels()
        {
            foreach (LoogaMenuPanel panel in _sourcePanels)
            {
                if (panel != null)
                {
                    panel.ActionsChanged -= OnPanelActionsChanged;
                }
            }

            _sourcePanels.Clear();
        }

        private static int CompareActions(LoogaMenuActionDescriptor a, LoogaMenuActionDescriptor b)
        {
            int order = a.SortOrder.CompareTo(b.SortOrder);
            return order != 0 ? order : string.CompareOrdinal(a.Label, b.Label);
        }

        private void RemoveDuplicateActions()
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                string id = _actions[i].Id;
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                for (int j = 0; j < i; j++)
                {
                    if (_actions[j].Id != id)
                        continue;

                    _actions.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
