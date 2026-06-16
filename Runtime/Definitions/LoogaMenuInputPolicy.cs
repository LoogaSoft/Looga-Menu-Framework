using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Input Policy", menuName = "LoogaSoft/Menu/Input Policy")]
    public sealed class LoogaMenuInputPolicy : ScriptableObject
    {
        [Header("Cursor")]
        [SerializeField] private bool _showsCursor = true;
        [SerializeField] private CursorLockMode _cursorLockMode = CursorLockMode.None;

        [Header("Gameplay")]
        [SerializeField] private bool _blocksGameplayInput = true;
        [SerializeField] private string _debugLabel;

        public bool ShowsCursor => _showsCursor;
        public CursorLockMode CursorLockMode => _cursorLockMode;
        public bool BlocksGameplayInput => _blocksGameplayInput;
        public string DebugLabel => string.IsNullOrWhiteSpace(_debugLabel) ? name : _debugLabel;
    }
}