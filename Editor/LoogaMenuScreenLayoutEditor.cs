using System;
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

            DrawBody(serializedObject, propertyName => DrawLoogaProperty(propertyName));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the editable layout fields without creating a nested editor instance.
        /// Screen inspectors use this method for their selected-layout detail view.
        /// </summary>
        internal static void DrawBody(SerializedObject layoutObject, Action<string> drawProperty = null)
        {
            if (drawProperty != null)
            {
                drawProperty("_description");
                drawProperty("_panels");
            }
            else
            {
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_description"));
                LoogaGUILayout.PropertyField(layoutObject.FindProperty("_panels"), true);
            }

            LoogaMenuScreenAuthoringGUI.DrawRegions(layoutObject.FindProperty("_regionOverrides"));
        }
    }
}
