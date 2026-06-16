using UnityEngine;

namespace LoogaSoft.Menu
{
    public abstract class LoogaMenuPanelMode : ScriptableObject
    {
        [SerializeField] private string _displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    }
}