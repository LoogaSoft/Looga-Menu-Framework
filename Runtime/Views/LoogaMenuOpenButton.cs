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
        [SerializeField] private int _contentEntryIndex;
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
            if (root == null)
                return;

            if (_target == LoogaMenuOpenButtonTarget.ScreenContentEntry)
            {
                if (_contentScreen == null
                    || _contentEntryIndex < 0
                    || _contentEntryIndex >= _contentScreen.ContentEntries.Length)
                    return;

                root.OpenContent(_contentScreen.ContentEntries[_contentEntryIndex], this);
                return;
            }

            if (_screen != null)
            {
                root.Open(_screen, this);
            }
        }
    }
}
