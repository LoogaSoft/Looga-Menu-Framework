using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public abstract class LoogaMenuPanelMode : ScriptableObject
    {
        [LoogaBoxGroup("Identity")]
        [TooltipBox("A panel mode adjusts how a reusable menu panel behaves inside a specific screen.")]
        [SerializeField] private bool _useCustomDisplayName;
        [ShowIf(nameof(_useCustomDisplayName))]
        [LoogaBoxGroupEnd]
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
