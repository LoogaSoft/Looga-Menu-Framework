using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Panel", menuName = "LoogaSoft/Menu/Panel Definition")]
    public sealed class LoogaMenuPanelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Visibility")]
        [SerializeField] private LoogaMenuVisibilityMode _visibilityMode = LoogaMenuVisibilityMode.DisableCanvas;
        [SerializeField] private bool _hideWhenCovered = true;
        [SerializeField] private bool _skipTransitions;
        [SerializeField] private bool _skipOpenSound;
        [SerializeField] private bool _skipCloseSound;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public LoogaMenuVisibilityMode VisibilityMode => _visibilityMode;
        public bool HideWhenCovered => _hideWhenCovered;
        public bool SkipTransitions => _skipTransitions;
        public bool SkipOpenSound => _skipOpenSound;
        public bool SkipCloseSound => _skipCloseSound;
    }
}