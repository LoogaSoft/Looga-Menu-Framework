using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Panel", menuName = "LoogaSoft/Menu Framework/Panel Definition")]
    public sealed class LoogaMenuPanelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Feedback")]
        [SerializeField] private bool _skipTransitions;
        [SerializeField] private bool _skipOpenSound;
        [SerializeField] private bool _skipCloseSound;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public string Description => _description;
        public bool SkipTransitions => _skipTransitions;
        public bool SkipOpenSound => _skipOpenSound;
        public bool SkipCloseSound => _skipCloseSound;

        private void OnValidate()
        {
            if (!_useCustomDisplayName)
            {
                _displayName = name;
            }
        }
    }
}

