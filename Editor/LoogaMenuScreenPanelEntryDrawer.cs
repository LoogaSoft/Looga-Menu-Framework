using System.Collections.Generic;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuScreenPanelEntry))]
    public sealed class LoogaMenuScreenPanelEntryDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, ReorderableList> ParameterLists = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty panel = property.FindPropertyRelative("_panel");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect panelRect = new(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(panelRect, panel, new GUIContent("Panel"));

            Rect parametersRect = new(position.x, panelRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width, LoogaMenuStyledListUtility.GetHeight(parameters));
            LoogaMenuStyledListUtility.Draw(parametersRect, parameters, ParameterLists);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            return EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing
                + LoogaMenuStyledListUtility.GetHeight(parameters);
        }
    }
}
