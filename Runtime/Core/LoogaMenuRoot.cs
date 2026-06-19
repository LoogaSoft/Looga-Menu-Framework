using UnityEngine;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Root")]
    public sealed class LoogaMenuRoot : MonoBehaviour
    {
        [Header("Registration")]
        [SerializeField] private bool _registerChildrenOnAwake = true;
        [SerializeField] private LoogaMenuPanel[] _scenePanels = System.Array.Empty<LoogaMenuPanel>();

        [Header("Default Panels")]
        [SerializeField] private LoogaMenuPanelDefinition _defaultBackgroundPanel;
        [SerializeField] private LoogaMenuPanelDefinition _defaultActionBarPanel;

        [Header("Cursor")]
        [SerializeField] private bool _controlCursor = true;
        [SerializeField] private CursorLockMode _closedLockMode = CursorLockMode.Locked;
        [SerializeField] private bool _closedCursorVisible;

        private readonly LoogaStateRegistry _stateRegistry = new();
        private LoogaMenuManager _menuManager;

        public static LoogaMenuRoot Active { get; private set; }
        public LoogaMenuManager MenuManager => _menuManager;
        public LoogaStateRegistry StateRegistry => _stateRegistry;
        public LoogaMenuPanelDefinition DefaultBackgroundPanel => _defaultBackgroundPanel;
        public LoogaMenuPanelDefinition DefaultActionBarPanel => _defaultActionBarPanel;

        private void Awake()
        {
            Active = this;
            _menuManager = new LoogaMenuManager(_stateRegistry, _defaultBackgroundPanel, _defaultActionBarPanel);
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

            if (Active == this)
            {
                Active = null;
            }
        }

        public bool Open(LoogaMenuScreenDefinition screen, UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.Open(screen, requester, payload);
        }

        public bool OpenContent(LoogaMenuScreenContentEntry entry, UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.OpenContent(entry, requester, payload);
        }

        public bool OpenContent(LoogaMenuScreenDefinition screen, LoogaMenuContentId contentId,
            UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.OpenContent(screen, contentId, requester, payload);
        }

        public bool Back()
        {
            return _menuManager != null && _menuManager.Back();
        }

        public void CloseAll()
        {
            _menuManager?.CloseAll();
        }

        public void RegisterPanel(LoogaMenuPanel panel)
        {
            _menuManager?.RegisterPanel(panel);
        }

        public void RegisterState<TState>(TState state) where TState : class
        {
            _stateRegistry.Register(state);
        }

        public void UnregisterState<TState>(TState state) where TState : class
        {
            _stateRegistry.Unregister(state);
        }

        private void RegisterPanels()
        {
            foreach (LoogaMenuPanel panel in _scenePanels)
            {
                RegisterPanel(panel);
            }

            if (!_registerChildrenOnAwake)
                return;

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
                    provider.RegisterStates(_stateRegistry);
                }
            }
        }

        private void UnregisterStateProviders()
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaStateProvider provider)
                {
                    provider.UnregisterStates(_stateRegistry);
                }
            }
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
                return;

            if (state.HasOpenScreens && state.ShowsCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (!state.HasOpenScreens)
            {
                Cursor.visible = _closedCursorVisible;
                Cursor.lockState = _closedLockMode;
            }
        }
    }
}
