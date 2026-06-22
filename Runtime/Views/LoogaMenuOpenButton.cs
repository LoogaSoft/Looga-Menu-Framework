using LoogaSoft.Inspector.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuOpenButtonTarget
    {
        Screen = 0,
        ScreenContentEntry = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("LoogaSoft/Menu/Open Menu Button")]
    public sealed class LoogaMenuOpenButton : MonoBehaviour
    {
        [SerializeField] private LoogaMenuOpenButtonTarget _target = LoogaMenuOpenButtonTarget.Screen;
        [SerializeField] private LoogaMenuScreenDefinition _screen;
        [SerializeField] private LoogaMenuScreenDefinition _contentScreen;
        [SerializeField, HideInInspector] private string _contentEntryId;

        [SerializeField] private bool _useActiveMenuRoot = true;
        [HideIf(nameof(_useActiveMenuRoot))]
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
            LoogaMenuRoot root = ResolveMenuRoot();
            if (root == null)
                return;

            if (_target == LoogaMenuOpenButtonTarget.ScreenContentEntry)
            {
                root.OpenContent(_contentScreen, _contentEntryId, this);
                return;
            }

            if (_screen != null)
            {
                root.Open(_screen, this);
            }
        }

        private LoogaMenuRoot ResolveMenuRoot()
        {
            if (!_useActiveMenuRoot && _menuRoot != null)
                return _menuRoot;

            return LoogaMenuRoot.Active;
        }
    }
}
