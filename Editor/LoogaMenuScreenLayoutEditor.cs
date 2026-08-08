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
            DrawBody(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the editable layout fields without creating a nested editor instance.
        /// Screen inspectors use this method for their selected-layout detail view.
        /// </summary>
        internal static void DrawBody(SerializedObject layoutObject)
        {
            EditorGUILayout.PropertyField(layoutObject.FindProperty("_description"));
            EditorGUILayout.PropertyField(layoutObject.FindProperty("_panels"), true);
            LoogaMenuScreenAuthoringGUI.DrawNavigation(
                layoutObject.FindProperty("_navigationOverrides"),
                supportsInheritance: true);
            LoogaMenuScreenAuthoringGUI.DrawActionBar(layoutObject.FindProperty("_actionBar"));
        }
    }
}
