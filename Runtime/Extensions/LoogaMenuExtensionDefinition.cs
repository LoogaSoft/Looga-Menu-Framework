using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Adds optional, screen-scoped behavior without coupling it to the menu manager.
    /// </summary>
    public abstract class LoogaMenuExtensionDefinition : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Disabled extensions override a matching inherited extension without creating runtime behavior.")]
        private bool _enabled = true;

        public bool Enabled => _enabled;

        /// <summary>
        /// Identifies the extension slot. A screen extension replaces a root default with the same ID.
        /// </summary>
        public virtual string ExtensionId => GetType().FullName;

        public abstract ILoogaMenuExtensionRuntime CreateRuntime();
    }

    /// <summary>
    /// Runtime behavior created for one open screen from an extension definition.
    /// </summary>
    public interface ILoogaMenuExtensionRuntime
    {
        void Attach(LoogaMenuExtensionContext context);
        void Show();
        void CollectPanels(List<LoogaMenuPanel> panels);
        bool UsesPanel(LoogaMenuPanelDefinition panel);
        bool UsesParameter(LoogaBlackboardKey key);
        void ReapplyParameters();
        void Release();
    }

    /// <summary>
    /// Restricted operations available to optional extension runtimes.
    /// </summary>
    public sealed class LoogaMenuExtensionContext
    {
        private readonly LoogaMenuManager _manager;

        internal LoogaMenuExtensionContext(LoogaMenuManager manager, LoogaMenuScreenDefinition screen)
        {
            _manager = manager;
            Screen = screen;
        }

        public LoogaMenuScreenDefinition Screen { get; }
        public ILoogaBlackboardReader BlackboardReader => _manager.BlackboardReader;
        public ILoogaBlackboardWriter BlackboardWriter => _manager.BlackboardWriter;

        public LoogaMenuPanel ShowPanel(LoogaMenuPanelDefinition panel)
        {
            return _manager.ShowExtensionPanel(panel, Screen);
        }

        public void HidePanelWhenUnused(LoogaMenuPanelDefinition panel)
        {
            _manager.HideExtensionPanelWhenUnused(panel);
        }

        public void AddPanel(LoogaMenuPanelDefinition panel, List<LoogaMenuPanel> panels)
        {
            _manager.AddExtensionPanel(panel, panels);
        }

        public void ApplyParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            _manager.ApplyExtensionParameters(parameters);
        }

        public void RemoveParameters(IEnumerable<LoogaMenuBlackboardParameter> parameters)
        {
            _manager.RemoveExtensionParameters(parameters);
        }

        public void Refresh()
        {
            _manager.RefreshAfterExtensionChange();
        }
    }
}
