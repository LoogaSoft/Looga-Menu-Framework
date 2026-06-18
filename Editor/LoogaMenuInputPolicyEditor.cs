using LoogaSoft.Menu;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuInputPolicy))]
    public sealed class LoogaMenuInputPolicyEditor : UnityEditor.Editor
    {
        private SerializedProperty _showsCursor;
        private SerializedProperty _cursorLockMode;
        private SerializedProperty _inputBlockPreset;
        private SerializedProperty _customInputBlockPolicy;
        private SerializedProperty _debugLabel;

        private void OnEnable()
        {
            _showsCursor = serializedObject.FindProperty("_showsCursor");
            _cursorLockMode = serializedObject.FindProperty("_cursorLockMode");
            _inputBlockPreset = serializedObject.FindProperty("_inputBlockPreset");
            _customInputBlockPolicy = serializedObject.FindProperty("_customInputBlockPolicy");
            _debugLabel = serializedObject.FindProperty("_debugLabel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_showsCursor);
            EditorGUILayout.PropertyField(_cursorLockMode);

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_inputBlockPreset);

            if ((LoogaMenuInputBlockPreset)_inputBlockPreset.enumValueIndex == LoogaMenuInputBlockPreset.Custom)
            {
                EditorGUILayout.PropertyField(_customInputBlockPolicy);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(_debugLabel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
