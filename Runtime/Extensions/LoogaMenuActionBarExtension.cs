using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(
        fileName = "New Action Bar Extension",
        menuName = "LoogaSoft/Menu Framework/Extensions/Action Bar")]
    public sealed class LoogaMenuActionBarExtension : LoogaMenuExtensionDefinition
    {
        public const string Id = "LoogaSoft.Menu.ActionBar";

        [SerializeField]
        [Tooltip("Panel presented as the action bar while the owning screen is open.")]
        private LoogaMenuPanelDefinition _panel;

        public override string ExtensionId => Id;
        public LoogaMenuPanelDefinition Panel => _panel;

        public override ILoogaMenuExtensionRuntime CreateRuntime()
        {
            return new LoogaMenuActionBarExtensionRuntime(_panel);
        }
    }

    internal sealed class LoogaMenuActionBarExtensionRuntime : ILoogaMenuExtensionRuntime
    {
        private readonly LoogaMenuPanelDefinition _panel;
        private LoogaMenuExtensionContext _context;

        public LoogaMenuActionBarExtensionRuntime(LoogaMenuPanelDefinition panel)
        {
            _panel = panel;
        }

        public void Attach(LoogaMenuExtensionContext context)
        {
            _context = context;
        }

        public void Show()
        {
            if (_panel != null)
            {
                _context.ShowPanel(_panel);
            }
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

        public void ReapplyParameters() { }

        public void Release()
        {
            _context = null;
        }
    }
}
