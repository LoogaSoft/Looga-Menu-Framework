using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("LoogaSoft/Menu/Menu Panel")]
    public sealed class LoogaMenuPanel : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private LoogaMenuPanelDefinition _panel;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        public LoogaMenuPanelDefinition Panel => _panel;
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
        }

        public void SetCovered(bool covered)
        {
            ResolveReferences(true);

            if (_canvasGroup == null)
                return;

            if (!covered)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                return;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void ResolveReferences()
        {
            ResolveReferences(true);
        }

        private void ResolveReferences(bool canAddMissingComponents)
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null && canAddMissingComponents)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_canvas != null && canAddMissingComponents && _canvas.GetComponent<GraphicRaycaster>() == null)
            {
                _canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}

