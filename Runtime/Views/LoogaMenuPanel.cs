using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LoogaSoft/Menu/Menu Panel")]
    public sealed class LoogaMenuPanel : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private LoogaMenuPanelDefinition _panel;

        [Header("Scene References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;

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

        private LoogaMenuPanelMode _activeMode;

        private void Awake()
        {
            ResolveReferences(true);
        }

        private void OnValidate()
        {
            ResolveReferences(false);
        }

        public void Show(LoogaMenuPanelMode panelMode)
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

            ApplyPanelMode(panelMode);
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

            LoogaMenuVisibilityMode visibilityMode = _panel != null
                ? _panel.VisibilityMode
                : LoogaMenuVisibilityMode.DisableCanvas;

            if (visibilityMode == LoogaMenuVisibilityMode.DeactivateGameObject)
            {
                gameObject.SetActive(false);
                return;
            }

            if (visibilityMode == LoogaMenuVisibilityMode.DisableCanvas && _canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        public void SetCovered(bool covered)
        {
            ResolveReferences(true);

            if (_canvasGroup == null)
                return;

            bool hideWhenCovered = _panel == null || _panel.HideWhenCovered;
            _canvasGroup.interactable = !covered;
            _canvasGroup.blocksRaycasts = !covered;
            _canvasGroup.alpha = covered && hideWhenCovered ? 0f : 1f;
        }

        private void ApplyPanelMode(LoogaMenuPanelMode panelMode)
        {
            if (_activeMode == panelMode)
                return;

            _activeMode = panelMode;

            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILoogaMenuPanelModeReceiver receiver)
                {
                    receiver.ApplyPanelMode(panelMode);
                }
            }
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