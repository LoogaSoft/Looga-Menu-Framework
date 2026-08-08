using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenLayout))]
    public sealed class LoogaMenuScreenLayoutEditor : LoogaEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            LoogaMenuEditorUtility.DrawDefinitionHeader("Screen Layout",
                "A layout defines one panel composition within its owning screen. Layout changes do not add menu history.");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_panels"), true);
            LoogaMenuScreenAuthoringGUI.DrawNavigation(
                serializedObject.FindProperty("_navigationOverrides"),
                supportsInheritance: true);
            LoogaMenuScreenAuthoringGUI.DrawActionBar(serializedObject.FindProperty("_actionBar"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
