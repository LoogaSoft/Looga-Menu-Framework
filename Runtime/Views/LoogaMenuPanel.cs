using System;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("LoogaSoft/Menu/Menu Panel")]
    public sealed class LoogaMenuPanel : MonoBehaviour
    {
        [TooltipBox("Assign the panel definition this scene object represents. Screens enable this panel through that asset reference.")]
        [SerializeField] private LoogaMenuPanelDefinition _panel;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private bool _isCovered;
        private bool _isVisible;

        public LoogaMenuPanelDefinition Panel => _panel;
        public bool IsCovered => _isCovered;
        public bool IsVisible => _isVisible;

        public event Action<bool> CoveredChanged;
        public event Action<bool> VisibilityChanged;

        public Canvas Canvas
        {
            get
            {
                ResolveReferences();
                return _canvas;
            }
        }
        public CanvasGroup CanvasGroup
        {
            get
            {
                ResolveReferences();
                return _canvasGroup;
            }
        }
        public RectTransform RectTransform => transform as RectTransform;

        private void Awake()
        {
            ResolveReferences(true);
        }

        private void OnValidate()
        {
            ResolveReferences(false);
        }

        public void Show()
        {
            ResolveReferences(true);

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (_canvas != null)
            {
                _canvas.enabled = true;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            SetVisible(true);
            SetCoveredState(false);
        }

        public void Hide()
        {
            ResolveReferences(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_canvas != null)
            {
                _canvas.enabled = false;
            }

            SetVisible(false);
            SetCoveredState(false);
        }

        public void SetCovered(bool covered)
        {
            ResolveReferences(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = !covered;
                _canvasGroup.blocksRaycasts = !covered;
            }

            SetCoveredState(covered);
        }

        private void SetCoveredState(bool covered)
        {
            if (_isCovered == covered)
                return;

            _isCovered = covered;
            CoveredChanged?.Invoke(covered);
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible)
                return;

            _isVisible = visible;
            VisibilityChanged?.Invoke(visible);
        }

        private void ResolveReferences()
        {
            ResolveReferences(true);
        }

        private void ResolveReferences(bool logMissingComponents)
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (logMissingComponents && _canvasGroup == null)
            {
                Debug.LogWarning($"{name} is missing a {nameof(CanvasGroup)}. Add one to the menu panel object.", this);
            }

            if (logMissingComponents && _canvas != null && _canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning($"{_canvas.name} is missing a {nameof(GraphicRaycaster)}. Add one if this panel needs pointer interaction.", _canvas);
            }
        }
    }
}
