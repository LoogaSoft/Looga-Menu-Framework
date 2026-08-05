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

        /// <summary>Gets whether this definition creates runtime behavior.</summary>
        public bool Enabled => _enabled;

        /// <summary>
        /// Identifies the extension slot. A screen extension replaces a root default with the same ID.
        /// </summary>
        public virtual string ExtensionId => GetType().FullName;

        /// <summary>Creates runtime behavior for one open screen.</summary>
        public abstract ILoogaMenuExtensionRuntime CreateRuntime();
    }

    /// <summary>
    /// Runtime behavior created for one open screen from an extension definition.
    /// </summary>
    public interface ILoogaMenuExtensionRuntime
    {
        /// <summary>Attaches the runtime to its owning screen.</summary>
        void Attach(LoogaMenuExtensionContext context);

        /// <summary>Shows the extension and applies its initial state.</summary>
        void Show();

        /// <summary>Adds panels that currently belong to the extension.</summary>
        void CollectPanels(List<LoogaMenuPanel> panels);

        /// <summary>Returns whether the extension currently uses a panel.</summary>
        bool UsesPanel(LoogaMenuPanelDefinition panel);

        /// <summary>Returns whether the extension currently uses a blackboard key.</summary>
        bool UsesParameter(LoogaBlackboardKey key);

        /// <summary>Applies the current blackboard parameters again.</summary>
        void ReapplyParameters();

        /// <summary>Releases panel, parameter, and event ownership.</summary>
        void Release();
    }

    /// <summary>
    /// Restricted operations available to optional extension runtimes.
    /// </summary>
    public sealed class LoogaMenuExtensionContext
    {
        private readonly LoogaMenuManager _manager;

        internal LoogaMenuExtensionContext(LoogaMenuManager manager, LoogaMenuScreenDefinition screen,
            LoogaMenuScreenConfiguration configuration)
        {
            _manager = manager;
            Screen = screen;
            Configuration = configuration;
        }

        public LoogaMenuScreenDefinition Screen { get; }
        public LoogaMenuScreenConfiguration Configuration { get; }
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

        public bool Back()
        {
            return _manager.Back();
        }

        public void CollectVisiblePanels(List<LoogaMenuPanel> panels, bool includeCovered)
        {
            _manager.CollectVisiblePanels(panels, includeCovered);
        }
    }
}
