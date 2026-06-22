using System;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuInputTriggerPhase
    {
        Started = 0,
        Performed = 1,
        Canceled = 2
    }

    public enum LoogaMenuInputTarget
    {
        Screen = 0,
        ScreenContentEntry = 1,
        Back = 2,
        CloseAll = 3
    }

    public enum LoogaMenuInputOpenBehavior
    {
        Open = 0,
        Toggle = 1,
        CloseAllThenOpen = 2,
        BackIfAnyOpenOtherwiseOpen = 3,
        IgnoreIfAnyOpen = 4
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Input Router")]
    public sealed class LoogaMenuInputRouter : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
        [SerializeField] private LoogaMenuRoot _menuRoot;

        [Header("Input")]
        [Tooltip("Enable this only when no PlayerInput or input bootstrapper is already enabling these actions.")]
        [SerializeField] private bool _manageActionEnabledState;
        [SerializeField] private bool _logRequirementFailures;
        [SerializeField] private LoogaMenuInputBinding[] _bindings = Array.Empty<LoogaMenuInputBinding>();

        private int _lastHandledInputFrame = -1;

        private void OnEnable()
        {
            SubscribeBindings();
        }

        private void OnDisable()
        {
            UnsubscribeBindings();
        }

        private void OnValidate()
        {
            _bindings ??= Array.Empty<LoogaMenuInputBinding>();
        }

        private void SubscribeBindings()
        {
            foreach (LoogaMenuInputBinding binding in _bindings)
            {
                binding?.Subscribe(this, _manageActionEnabledState);
            }
        }

        private void UnsubscribeBindings()
        {
            foreach (LoogaMenuInputBinding binding in _bindings)
            {
                binding?.Unsubscribe(_manageActionEnabledState);
            }
        }

        internal void HandleInput(LoogaMenuInputBinding binding, LoogaMenuInputTriggerPhase phase)
        {
            if (binding == null || phase != binding.TriggerPhase)
                return;

            if (_lastHandledInputFrame == Time.frameCount)
                return;

            LoogaMenuRoot root = ResolveMenuRoot();
            string failureReason = string.Empty;
            if (root == null || !binding.CanOpen(root, out failureReason))
            {
                if (_logRequirementFailures && !string.IsNullOrWhiteSpace(failureReason))
                {
                    Debug.LogWarning(failureReason, this);
                }

                return;
            }

            if (!binding.Execute(root, this))
                return;

            _lastHandledInputFrame = Time.frameCount;
        }

        private LoogaMenuRoot ResolveMenuRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }
    }

    [Serializable]
    public sealed class LoogaMenuInputBinding
    {
        [SerializeField] private string _displayName;
        [SerializeField] private InputActionReference _inputAction;
        [SerializeField] private LoogaMenuInputTriggerPhase _triggerPhase = LoogaMenuInputTriggerPhase.Started;
        [SerializeField] private LoogaMenuInputTarget _target;
        [SerializeField] private LoogaMenuInputOpenBehavior _openBehavior = LoogaMenuInputOpenBehavior.Open;
        [SerializeField] private LoogaMenuRuleSet _requirements;

        [ShowIf(nameof(_target), (int)LoogaMenuInputTarget.Screen)]
        [SerializeField] private LoogaMenuScreenDefinition _screen;

        [ShowIf(nameof(_target), (int)LoogaMenuInputTarget.ScreenContentEntry)]
        [SerializeField] private LoogaMenuScreenContentReference _contentEntry;

        [NonSerialized] private Action<InputAction.CallbackContext> _startedCallback;
        [NonSerialized] private Action<InputAction.CallbackContext> _performedCallback;
        [NonSerialized] private Action<InputAction.CallbackContext> _canceledCallback;
        [NonSerialized] private InputAction _subscribedAction;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
            ? ResolveDefaultDisplayName()
            : _displayName;
        public LoogaMenuInputTriggerPhase TriggerPhase => _triggerPhase;

        internal void Subscribe(LoogaMenuInputRouter router, bool manageActionEnabledState)
        {
            InputAction action = _inputAction != null ? _inputAction.action : null;
            if (router == null || action == null || action == _subscribedAction)
                return;

            _subscribedAction = action;
            _startedCallback = _ => router.HandleInput(this, LoogaMenuInputTriggerPhase.Started);
            _performedCallback = _ => router.HandleInput(this, LoogaMenuInputTriggerPhase.Performed);
            _canceledCallback = _ => router.HandleInput(this, LoogaMenuInputTriggerPhase.Canceled);

            action.started += _startedCallback;
            action.performed += _performedCallback;
            action.canceled += _canceledCallback;

            if (manageActionEnabledState && !action.enabled)
            {
                action.Enable();
            }
        }

        internal void Unsubscribe(bool manageActionEnabledState)
        {
            if (_subscribedAction == null)
                return;

            _subscribedAction.started -= _startedCallback;
            _subscribedAction.performed -= _performedCallback;
            _subscribedAction.canceled -= _canceledCallback;

            if (manageActionEnabledState && _subscribedAction.enabled)
            {
                _subscribedAction.Disable();
            }

            _startedCallback = null;
            _performedCallback = null;
            _canceledCallback = null;
            _subscribedAction = null;
        }

        internal bool CanOpen(LoogaMenuRoot root, out string failureReason)
        {
            failureReason = string.Empty;

            if (_requirements == null)
                return true;

            return _requirements.CanOpen(root != null ? root.BlackboardReader : null, out failureReason);
        }

        internal bool Execute(LoogaMenuRoot root, UnityEngine.Object requester)
        {
            if (root == null)
                return false;

            return _target switch
            {
                LoogaMenuInputTarget.Back => root.Back(),
                LoogaMenuInputTarget.CloseAll => CloseAll(root),
                _ => ExecuteOpenBehavior(root, requester)
            };
        }

        private bool ExecuteOpenBehavior(LoogaMenuRoot root, UnityEngine.Object requester)
        {
            bool hasOpenScreens = root.MenuManager != null && root.MenuManager.OpenScreens.Count > 0;
            bool targetIsOpen = IsTargetOpen(root);

            return _openBehavior switch
            {
                LoogaMenuInputOpenBehavior.Toggle when targetIsOpen => root.Back(),
                LoogaMenuInputOpenBehavior.CloseAllThenOpen => CloseAllThenOpen(root, requester),
                LoogaMenuInputOpenBehavior.BackIfAnyOpenOtherwiseOpen when hasOpenScreens => root.Back(),
                LoogaMenuInputOpenBehavior.IgnoreIfAnyOpen when hasOpenScreens => false,
                _ => OpenTarget(root, requester)
            };
        }

        private bool OpenTarget(LoogaMenuRoot root, UnityEngine.Object requester)
        {
            return _target switch
            {
                LoogaMenuInputTarget.Screen => _screen != null && root.Open(_screen, requester),
                LoogaMenuInputTarget.ScreenContentEntry => _contentEntry != null && _contentEntry.Open(root, requester),
                _ => false
            };
        }

        private bool CloseAllThenOpen(LoogaMenuRoot root, UnityEngine.Object requester)
        {
            root.CloseAll();
            return OpenTarget(root, requester);
        }

        private static bool CloseAll(LoogaMenuRoot root)
        {
            root.CloseAll();
            return true;
        }

        private bool IsTargetOpen(LoogaMenuRoot root)
        {
            if (_target != LoogaMenuInputTarget.Screen || _screen == null || root.MenuManager == null)
                return false;

            foreach (LoogaMenuScreenDefinition openScreen in root.MenuManager.OpenScreens)
            {
                if (openScreen == _screen)
                    return true;
            }

            return false;
        }

        private string ResolveDefaultDisplayName()
        {
            return _target switch
            {
                LoogaMenuInputTarget.Screen when _screen != null => _screen.DisplayName,
                LoogaMenuInputTarget.ScreenContentEntry when _contentEntry != null && _contentEntry.Screen != null =>
                    _contentEntry.Screen.DisplayName,
                LoogaMenuInputTarget.Back => "Back",
                LoogaMenuInputTarget.CloseAll => "Close All",
                _ => "Menu Input"
            };
        }
    }
}
