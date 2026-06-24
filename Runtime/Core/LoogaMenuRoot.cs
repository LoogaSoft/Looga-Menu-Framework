using System.Collections;
using LoogaSoft.Blackboard;
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
#if UNITY_EDITOR
        [SerializeField] private bool _reapplyClosedCursorAtEndOfFrameInEditor = true;
#endif

        private LoogaBlackboard _ownedBlackboard;
        private ILoogaBlackboardReader _blackboardReader;
        private ILoogaBlackboardWriter _blackboardWriter;
        private LoogaMenuManager _menuManager;
#if UNITY_EDITOR
        private Coroutine _editorCursorReapplyRoutine;
#endif

        public static LoogaMenuRoot Active { get; private set; }
        public LoogaMenuManager MenuManager => _menuManager;
        public ILoogaBlackboardReader BlackboardReader => _blackboardReader;
        public ILoogaBlackboardWriter BlackboardWriter => _blackboardWriter;
        public LoogaMenuPanelDefinition DefaultBackgroundPanel => _defaultBackgroundPanel;
        public LoogaMenuPanelDefinition DefaultActionBarPanel => _defaultActionBarPanel;

        private void Awake()
        {
            Active = this;
            ResolveBlackboard();
            _menuManager = new LoogaMenuManager(_blackboardReader, _blackboardWriter, _defaultBackgroundPanel, _defaultActionBarPanel);
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
#if UNITY_EDITOR
            StopEditorCursorReapply();
#endif

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

        /// <summary>
        /// Opens a content entry by its stable ID, opening the owning screen first if needed.
        /// </summary>
        public bool OpenContent(LoogaMenuScreenDefinition screen, string contentEntryId,
            UnityEngine.Object requester = null, object payload = null)
        {
            return _menuManager != null && _menuManager.OpenContent(screen, contentEntryId, requester, payload);
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
                return;

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
                return;

            if (state.HasOpenScreens && state.ShowsCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (!state.HasOpenScreens)
            {
                ApplyClosedCursorState();
#if UNITY_EDITOR
                QueueEditorClosedCursorReapply();
#endif
            }
        }

        private void ApplyClosedCursorState()
        {
            Cursor.visible = _closedCursorVisible;
            Cursor.lockState = _closedLockMode;
        }

#if UNITY_EDITOR
        private void QueueEditorClosedCursorReapply()
        {
            if (!_reapplyClosedCursorAtEndOfFrameInEditor)
                return;

            StopEditorCursorReapply();
            _editorCursorReapplyRoutine = StartCoroutine(ReapplyClosedCursorAtEndOfFrame());
        }

        private void StopEditorCursorReapply()
        {
            if (_editorCursorReapplyRoutine == null)
                return;

            StopCoroutine(_editorCursorReapplyRoutine);
            _editorCursorReapplyRoutine = null;
        }

        private IEnumerator ReapplyClosedCursorAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            _editorCursorReapplyRoutine = null;

            if (!_controlCursor || _menuManager == null || _menuManager.OpenScreens.Count > 0)
                yield break;

            ApplyClosedCursorState();
        }
#endif
    }
}
