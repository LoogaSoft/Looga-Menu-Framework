using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaBlackboardDefinition))]
    public sealed class LoogaBlackboardDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _keys;
        private LoogaBlackboardValueType _newKeyType;

        private void OnEnable()
        {
            _keys = serializedObject.FindProperty("_keys");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Blackboard",
                "Defines typed state keys that rules can read at runtime. The asset stores definitions only, not live state.");

            EditorGUILayout.PropertyField(_keys);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Create Key", EditorStyles.boldLabel);
            _newKeyType = (LoogaBlackboardValueType)EditorGUILayout.EnumPopup("Type", _newKeyType);

            if (GUILayout.Button("Create Key"))
            {
                CreateKey();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateKey()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Blackboard Key",
                $"New {_newKeyType} Key",
                "asset",
                "Choose where to save the new blackboard key.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            LoogaBlackboardKey key = CreateInstance<LoogaBlackboardKey>();
            SerializedObject keyObject = new(key);
            keyObject.FindProperty("_valueType").enumValueIndex = (int)_newKeyType;
            keyObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(key, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);

            serializedObject.Update();
            AddKeyReference(key);
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(key);
        }

        private void AddKeyReference(LoogaBlackboardKey key)
        {
            if (key == null)
                return;

            for (int i = 0; i < _keys.arraySize; i++)
            {
                if (_keys.GetArrayElementAtIndex(i).objectReferenceValue == key)
                    return;
            }

            _keys.InsertArrayElementAtIndex(_keys.arraySize);
            _keys.GetArrayElementAtIndex(_keys.arraySize - 1).objectReferenceValue = key;
        }
    }
}
