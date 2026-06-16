using UnityEngine;

namespace LoogaSoft.Menu
{
    public abstract class LoogaMenuPanelMode : ScriptableObject
    {
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;

        protected virtual void OnValidate()
        {
            if (!_useCustomDisplayName)
            {
                _displayName = name;
            }
        }
    }
}

