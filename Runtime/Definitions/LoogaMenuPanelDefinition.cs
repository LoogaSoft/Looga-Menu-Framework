using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Panel", menuName = "LoogaSoft/Menu Framework/Panel Definition")]
    public sealed class LoogaMenuPanelDefinition : ScriptableObject
    {
        [LoogaBoxGroup("Identity")]
        [TooltipBox("A panel is one reusable UI piece, such as Stockpile, Loadout, Action Bar, or a shared background.")]
        [SerializeField] private bool _useCustomDisplayName;
        [ShowIf(nameof(_useCustomDisplayName))]
        [SerializeField] private string _displayName;
        [LoogaBoxGroupEnd]
        [SerializeField, TextArea] private string _description;

        [LoogaBoxGroup("Feedback")]
        [SerializeField] private bool _skipTransitions;
        [SerializeField] private bool _skipOpenSound;
        [LoogaBoxGroupEnd]
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
