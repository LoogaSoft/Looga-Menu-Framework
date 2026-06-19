using System.Collections.Generic;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuScreenContentEntry))]
    public sealed class LoogaMenuScreenContentEntryDrawer : PropertyDrawer
    {
        private const float Gap = 4f;
        private static readonly Dictionary<string, ReorderableList> ParameterLists = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty targetType = property.FindPropertyRelative("_targetType");
            SerializedProperty useCustomDisplayName = property.FindPropertyRelative("_useCustomDisplayName");
            SerializedProperty displayName = property.FindPropertyRelative("_displayName");
            SerializedProperty panel = property.FindPropertyRelative("_panel");
            SerializedProperty screen = property.FindPropertyRelative("_screen");
            SerializedProperty openMode = property.FindPropertyRelative("_openMode");
            SerializedProperty backBehavior = property.FindPropertyRelative("_backBehavior");
            SerializedProperty rules = property.FindPropertyRelative("_rules");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            Rect useCustomNameRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(useCustomNameRect, useCustomDisplayName, new GUIContent("Use Custom Display Name"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawDisplayName(new Rect(position.x, y, position.width, lineHeight), useCustomDisplayName,
                displayName, targetType, panel, screen);

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawTarget(new Rect(position.x, y, position.width, lineHeight), targetType, panel, screen);

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), openMode,
                new GUIContent("Open Mode"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), backBehavior,
                new GUIContent("Back Behavior"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            float rulesHeight = EditorGUI.GetPropertyHeight(rules, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, rulesHeight), rules,
                new GUIContent("Open Requirements"));

            y += rulesHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect parametersRect = new(position.x, y, position.width, LoogaMenuStyledListUtility.GetHeight(parameters));
            LoogaMenuStyledListUtility.Draw(parametersRect, parameters, ParameterLists);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty rules = property.FindPropertyRelative("_rules");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            return lineHeight * 5f
                + spacing * 6f
                + EditorGUI.GetPropertyHeight(rules, true)
                + LoogaMenuStyledListUtility.GetHeight(parameters);
        }

        private static void DrawDisplayName(Rect rect, SerializedProperty useCustomDisplayName,
            SerializedProperty displayName, SerializedProperty targetType, SerializedProperty panel,
            SerializedProperty screen)
        {
            using (new EditorGUI.DisabledScope(!useCustomDisplayName.boolValue))
            {
                if (!useCustomDisplayName.boolValue)
                {
                    displayName.stringValue = ResolveDefaultDisplayName(targetType, panel, screen);
                }

                EditorGUI.PropertyField(rect, displayName, new GUIContent("Display Name"));
            }
        }

        private static void DrawTarget(Rect rect, SerializedProperty targetType, SerializedProperty panel,
            SerializedProperty screen)
        {
            Rect targetContentRect = EditorGUI.PrefixLabel(rect, new GUIContent("Target"));
            float targetTypeWidth = Mathf.Min(116f, targetContentRect.width * 0.34f);
            Rect targetTypeRect = new(targetContentRect.x, targetContentRect.y, targetTypeWidth,
                targetContentRect.height);
            Rect targetRect = new(targetTypeRect.xMax + Gap, rect.y,
                targetContentRect.width - targetTypeWidth - Gap, rect.height);
            bool targetsScreen = (LoogaMenuContentTargetType)targetType.enumValueIndex == LoogaMenuContentTargetType.Screen;

            EditorGUI.PropertyField(targetTypeRect, targetType, GUIContent.none);
            EditorGUI.PropertyField(targetRect, targetsScreen ? screen : panel, GUIContent.none);
        }

        private static string ResolveDefaultDisplayName(SerializedProperty targetType, SerializedProperty panel,
            SerializedProperty screen)
        {
            bool targetsScreen = (LoogaMenuContentTargetType)targetType.enumValueIndex == LoogaMenuContentTargetType.Screen;
            if (targetsScreen)
            {
                return screen.objectReferenceValue is LoogaMenuScreenDefinition screenDefinition
                    ? screenDefinition.DisplayName
                    : "Unassigned Content";
            }

            return panel.objectReferenceValue is LoogaMenuPanelDefinition panelDefinition
                ? panelDefinition.DisplayName
                : "Unassigned Content";
        }
    }
}
