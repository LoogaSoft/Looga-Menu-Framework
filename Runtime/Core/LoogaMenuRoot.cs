using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Owns menu state, panel registration, shared presentation, and cursor policy.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Root")]
    public sealed class LoogaMenuRoot : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool _registerChildrenOnAwake = true;
        [SerializeField] private LoogaMenuPanel[] _scenePanels = System.Array.Empty<LoogaMenuPanel>();

        [Header("Structure")]
        [SerializeField] private LoogaMenuStructureProfile _structure;

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

        /// <summary>Gets the project-authored menu region structure.</summary>
        public LoogaMenuStructureProfile Structure => _structure;

        /// <summary>Applies project-level menu behavior at runtime.</summary>
        public void ApplyRuntimeDefaults(
            bool registerChildrenOnAwake,
            LoogaMenuStructureProfile structure,
            bool controlCursor,
            CursorLockMode closedLockMode,
            bool closedCursorVisible)
        {
            _registerChildrenOnAwake = registerChildrenOnAwake;
            _structure = structure;
            _controlCursor = controlCursor;
            _closedLockMode = closedLockMode;
            _closedCursorVisible = closedCursorVisible;
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
                _structure);
            _menuManager.StateChanged += OnMenuStateChanged;

            RegisterStateProviders();
            ResolveHandlers();
            RegisterPanels();
        }

        private void OnDestroy()
        {
            if (_menuManager != null)
                _menuManager.StateChanged -= OnMenuStateChanged;

            UnregisterStateProviders();
            ReleaseOwnedBlackboard();

            if (Active == this)
                Active = null;
        }

        /// <summary>Opens a typed menu destination.</summary>
        public bool Open(LoogaMenuDestination destination, Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(destination, requester, payload);
        }

        /// <summary>Opens the default layout of a screen.</summary>
        public bool Open(LoogaMenuScreenDefinition screen, Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, requester, payload);
        }

        /// <summary>Opens a specific layout of a screen.</summary>
        public bool Open(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            Object requester = null,
            object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, layout, requester, payload);
        }

        /// <summary>Changes an open screen layout without adding a history entry.</summary>
        public bool SetLayout(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            Object requester = null)
        {
            return _menuManager != null && _menuManager.SetLayout(screen, layout, requester);
        }

        /// <summary>Returns to the previous menu state.</summary>
        public bool Back()
        {
            return _menuManager != null && _menuManager.Back();
        }

        /// <summary>Closes all open screens.</summary>
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
                RegisterPanel(panel);

            if (!_registerChildrenOnAwake)
                return;

            foreach (LoogaMenuPanel panel in GetComponentsInChildren<LoogaMenuPanel>(true))
                RegisterPanel(panel);
        }

        private void RegisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                    provider.RegisterStates(_blackboardWriter);
            }
        }

        private void UnregisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                    provider.UnregisterStates(_blackboardWriter);
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
                return;

            LoogaBlackboardRegistry.ClearActive(_ownedBlackboard);
            _ownedBlackboard = null;
        }

        private void ResolveHandlers()
        {
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is ILoogaMenuTransitionHandler transitionHandler)
                    _menuManager.SetTransitionHandler(transitionHandler);

                if (component is ILoogaMenuAudioHandler audioHandler)
                    _menuManager.SetAudioHandler(audioHandler);
            }
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            if (!_controlCursor)
                return;

            if (state.HasOpenScreens && state.ShowsCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (!state.HasOpenScreens)
                ApplyClosedCursorState();
        }

        private void ApplyClosedCursorState()
        {
            Cursor.visible = _closedCursorVisible;
            Cursor.lockState = _closedLockMode;
        }
    }
}
