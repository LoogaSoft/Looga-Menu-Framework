using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Activates an authored region presenter when the menu resolves content for its shared slot.
    /// Keep this component on an active parent so the presenter itself can remain inactive at rest.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Region Object Activator")]
    public sealed class LoogaMenuRegionObjectActivator : MonoBehaviour
    {
        [InspectorName("Shared Slot")]
        [SerializeField] private LoogaMenuRegionDefinition _region;

        [Tooltip("The presenter root that this component activates when the shared slot has content.")]
        [SerializeField] private GameObject _target;

        private LoogaMenuManager _manager;

        private void OnEnable()
        {
            TryBindManager();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindManager();
        }

        private void Update()
        {
            LoogaMenuManager activeManager = LoogaMenuRoot.Active?.MenuManager;
            if (activeManager != _manager)
                TryBindManager();
        }

        private void OnValidate()
        {
            if (_target == gameObject)
            {
                Debug.LogWarning(
                    "A menu region object activator must be outside the target that it controls.",
                    this);
            }
        }

        private void TryBindManager()
        {
            LoogaMenuManager manager = LoogaMenuRoot.Active?.MenuManager;
            if (manager == _manager)
                return;

            UnbindManager();
            _manager = manager;
            if (_manager != null)
                _manager.StateChanged += OnMenuStateChanged;

            Refresh();
        }

        private void UnbindManager()
        {
            if (_manager != null)
                _manager.StateChanged -= OnMenuStateChanged;

            _manager = null;
        }

        private void OnMenuStateChanged(LoogaMenuState state)
        {
            _ = state;
            Refresh();
        }

        private void Refresh()
        {
            if (_target == null || _target == gameObject)
                return;

            bool shouldBeActive = _manager?.ResolveRegionContent(_region) != null;
            if (_target.activeSelf != shouldBeActive)
                _target.SetActive(shouldBeActive);
        }
    }
}
