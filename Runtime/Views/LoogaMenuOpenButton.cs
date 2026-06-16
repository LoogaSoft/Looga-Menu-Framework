using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("LoogaSoft/Menu/Open Menu Button")]
    public sealed class LoogaMenuOpenButton : MonoBehaviour
    {
        [SerializeField] private LoogaMenuScreenDefinition _screen;
        [SerializeField] private LoogaMenuRoot _menuRoot;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Open);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Open);
            }
        }

        private void Open()
        {
            LoogaMenuRoot root = _menuRoot != null ? _menuRoot : LoogaMenuRoot.Active;
            if (root == null || _screen == null)
                return;

            root.Open(_screen, this);
        }
    }
}
