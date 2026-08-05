using System;
using LoogaSoft.Blackboard;
using UnityEngine;
using UnityEngine.Serialization;

namespace LoogaSoft.Menu
{
    /// <summary>Owns menu state, panel registration, extensions, and cursor policy.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Root")]
    public sealed class LoogaMenuRoot : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool _registerChildrenOnAwake = true;
        [SerializeField] private LoogaMenuPanel[] _scenePanels = System.Array.Empty<LoogaMenuPanel>();

        [Header("Default Panels")]
        [SerializeField] private LoogaMenuPanelDefinition _defaultBackgroundPanel;

        [Header("Default Extensions")]
        [SerializeField]
        [FormerlySerializedAs("_defaultFeatures")]
        [Tooltip("Optional behaviors inherited by screens unless a screen supplies an extension with the same ID.")]
        private LoogaMenuExtensionDefinition[] _defaultExtensions = Array.Empty<LoogaMenuExtensionDefinition>();

        [Header("Cursor")]
        [SerializeField] private bool _controlCursor = true;
        [SerializeField] private CursorLockMode _closedLockMode = CursorLockMode.Locked;
        [SerializeField] private bool _closedCursorVisible;

        private LoogaBlackboard _ownedBlackboard;
        private ILoogaBlackboardReader _blackboardReader;
        private ILoogaBlackboardWriter _blackboardWriter;
        private LoogaMenuManager _menuManager;

        /// <summary>Gets the active menu root.</summary>
        public static LoogaMenuRoot Active { get; private set; }

        /// <summary>Gets the menu manager owned by this root.</summary>
        public LoogaMenuManager MenuManager => _menuManager;

        /// <summary>Gets read access to the active menu blackboard.</summary>
        public ILoogaBlackboardReader BlackboardReader => _blackboardReader;

        /// <summary>Gets write access to the active menu blackboard.</summary>
        public ILoogaBlackboardWriter BlackboardWriter => _blackboardWriter;

        /// <summary>Gets the default background panel.</summary>
        public LoogaMenuPanelDefinition DefaultBackgroundPanel => _defaultBackgroundPanel;

        /// <summary>Gets the extensions inherited by screens without matching overrides.</summary>
        public LoogaMenuExtensionDefinition[] DefaultExtensions => _defaultExtensions;

        /// <summary>Applies project-level menu behavior at runtime.</summary>
        public void ApplyRuntimeDefaults(bool registerChildrenOnAwake,
            LoogaMenuPanelDefinition defaultBackgroundPanel,
            bool controlCursor,
            CursorLockMode closedLockMode,
            bool closedCursorVisible)
        {
            _registerChildrenOnAwake = registerChildrenOnAwake;
            _defaultBackgroundPanel = defaultBackgroundPanel;
            _controlCursor = controlCursor;
            _closedLockMode = closedLockMode;
            _closedCursorVisible = closedCursorVisible;
        }

        /// <summary>Applies project-level menu extensions at runtime.</summary>
        public void ApplyRuntimeExtensions(LoogaMenuExtensionDefinition[] defaultExtensions)
        {
            _defaultExtensions = defaultExtensions ?? Array.Empty<LoogaMenuExtensionDefinition>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
        }

        private void Awake()
        {
            Active = this;
            ResolveBlackboard();
            _menuManager = new LoogaMenuManager(
                _blackboardReader,
                _blackboardWriter,
                _defaultBackgroundPanel,
                _defaultExtensions);
            _menuManager.StateChanged += OnMenuStateChanged;

            RegisterStateProviders();
            ResolveHandlers();
            RegisterPanels();
        }

        private void OnDestroy()
        {
            if (_menuManager != null)
            {
                _menuManager.StateChanged -= OnMenuStateChanged;
            }

            UnregisterStateProviders();
            ReleaseOwnedBlackboard();

            if (Active == this)
            {
                Active = null;
            }
        }

        /// <summary>Opens a screen for the specified requester and payload.</summary>
        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, requester, payload);
        }

        /// <summary>Opens a specific configuration of a screen.</summary>
        public bool Open(LoogaMenuScreenDefinition screen, LoogaMenuScreenConfiguration configuration,
            UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, configuration, requester, payload);
        }

        /// <summary>Changes an open screen configuration without adding a back-stack entry.</summary>
        public bool SetConfiguration(LoogaMenuScreenDefinition screen, LoogaMenuScreenConfiguration configuration,
            UnityEngine.Object requester = null)
        {
            return _menuManager != null && _menuManager.SetConfiguration(screen, configuration, requester);
        }

        /// <summary>Opens one content entry on its owning screen.</summary>
        public bool OpenContent(LoogaMenuScreenContentEntry entry, UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.OpenContent(entry, requester, payload);
        }

        /// <summary>
        /// Opens a content entry by its stable ID, opening the owning screen first if needed.
        /// </summary>
        public bool OpenContent(LoogaMenuScreenDefinition screen, string contentEntryId,
            UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.OpenContent(screen, contentEntryId, requester, payload);
        }

        /// <summary>Returns to the previous menu state.</summary>
        public bool Back()
        {
            return _menuManager != null && _menuManager.Back();
        }

        /// <summary>Closes all open screens and extension panels.</summary>
        public void CloseAll()
        {
            _menuManager?.CloseAll();
        }

        /// <summary>Registers a scene panel with this root.</summary>
        public void RegisterPanel(LoogaMenuPanel panel)
        {
            _menuManager?.RegisterPanel(panel);
        }

        private void RegisterPanels()
        {
            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                RegisterPanel(panel);
            }

            if (!_registerChildrenOnAwake)
            {
                return;
            }

            foreach (LoogaMenuPanel panel in GetComponentsInChildren<LoogaMenuPanel>(true))
            {
                RegisterPanel(panel);
            }
        }

        private void RegisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                {
                    provider.RegisterStates(_blackboardWriter);
                }
            }
        }

        private void UnregisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                {
                    provider.UnregisterStates(_blackboardWriter);
                }
            }
        }

        private void ResolveBlackboard()
        {
            LoogaBlackboard blackboard = LoogaBlackboardRegistry.Active;
            if (blackboard == null)
            {
                _ownedBlackboard = new LoogaBlackboard();
                LoogaBlackboardRegistry.SetActive(_ownedBlackboard);
                blackboard = _ownedBlackboard;
            }

            _blackboardReader = blackboard;
            _blackboardWriter = blackboard;
        }

        private void ReleaseOwnedBlackboard()
        {
            if (_ownedBlackboard == null)
            {
                return;
            }

            LoogaBlackboardRegistry.ClearActive(_ownedBlackboard);
            _ownedBlackboard = null;
        }

        private void ResolveHandlers()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is ILoogaMenuTransitionHandler transitionHandler)
                {
                    _menuManager.SetTransitionHandler(transitionHandler);
                }

                if (component is ILoogaMenuAudioHandler audioHandler)
                {
                    _menuManager.SetAudioHandler(audioHandler);
                }
            }
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            if (!_controlCursor)
            {
                return;
            }

            if (state.HasOpenScreens && state.ShowsCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (!state.HasOpenScreens)
            {
                ApplyClosedCursorState();
            }
        }

        private void ApplyClosedCursorState()
        {
            Cursor.visible = _closedCursorVisible;
            Cursor.lockState = _closedLockMode;
        }
    }
}
