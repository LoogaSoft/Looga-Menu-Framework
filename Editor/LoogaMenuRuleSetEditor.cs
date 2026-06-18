using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuRuleSet))]
    public sealed class LoogaMenuRuleSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Rule Set",
                "A rule set gates menu screens and content entries using typed blackboard conditions.");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_conditions"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
