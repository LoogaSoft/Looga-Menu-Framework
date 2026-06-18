using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Blackboard Key", menuName = "LoogaSoft/Menu/Blackboard/Key")]
    public sealed class LoogaBlackboardKey : ScriptableObject
    {
        [SerializeField] private bool _useCustomDisplayName;
        [SerializeField] private string _displayName;
        [SerializeField] private LoogaBlackboardValueType _valueType;
        [SerializeField, TextArea] private string _description;

        public string DisplayName => _useCustomDisplayName && !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : name;
        public LoogaBlackboardValueType ValueType => _valueType;
        public string Description => _description;

        private void OnValidate()
        {
            if (!_useCustomDisplayName)
            {
                _displayName = name;
            }
        }
    }
}
