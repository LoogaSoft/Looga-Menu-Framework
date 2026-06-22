using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuOpenButton))]
    public sealed class LoogaMenuOpenButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty target = serializedObject.FindProperty("_target");
            EditorGUILayout.PropertyField(target);

            if ((LoogaMenuOpenButtonTarget)target.enumValueIndex == LoogaMenuOpenButtonTarget.ScreenContentEntry)
            {
                SerializedProperty contentScreen = serializedObject.FindProperty("_contentScreen");
                SerializedProperty contentEntryId = serializedObject.FindProperty("_contentEntryId");
                EditorGUILayout.PropertyField(contentScreen, new GUIContent("Content Screen"));
                LoogaMenuContentEntryPopupUtility.Draw(EditorGUILayout.GetControlRect(),
                    contentScreen.objectReferenceValue as LoogaMenuScreenDefinition,
                    contentEntryId);
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_screen"));
            }

            SerializedProperty useActiveMenuRoot = serializedObject.FindProperty("_useActiveMenuRoot");
            EditorGUILayout.PropertyField(useActiveMenuRoot, new GUIContent("Use Active Menu Root"));

            if (!useActiveMenuRoot.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_menuRoot"), new GUIContent("Menu Root"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
