using System.Collections.Generic;
using LoogaSoft.Blackboard;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenDefinition))]
    public sealed class LoogaMenuScreenDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LoogaMenuScreenDefinition screen = (LoogaMenuScreenDefinition)target;
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Screen",
                "A screen composes reusable panels and evaluates rule assets before it opens.");

            LoogaMenuEditorUtility.DrawDisplayName(serializedObject);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_panels"));
            DrawPanelReference(serializedObject.FindProperty("_backgroundPanelMode"),
                serializedObject.FindProperty("_backgroundPanel"), "Background Panel");
            DrawPanelReference(serializedObject.FindProperty("_actionBarPanelMode"),
                serializedObject.FindProperty("_actionBarPanel"), "Action Bar Panel");

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rules"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_inputPolicy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_closeAsGroupOnBack"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_closeExistingScreens"));
            serializedObject.ApplyModifiedProperties();

            DrawValidation(screen);
        }

        private static void DrawPanelReference(SerializedProperty mode, SerializedProperty panel, string label)
        {
            EditorGUILayout.PropertyField(mode, new GUIContent($"{label} Source"));

            if ((LoogaMenuPanelReferenceMode)mode.enumValueIndex != LoogaMenuPanelReferenceMode.Override)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(panel, new GUIContent(label));
            EditorGUI.indentLevel--;
        }

        private static void DrawValidation(LoogaMenuScreenDefinition screen)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            HashSet<LoogaMenuPanelDefinition> panels = new();
            bool hasIssue = false;
            LoogaMenuRoot root = Object.FindFirstObjectByType<LoogaMenuRoot>(FindObjectsInactive.Include);
            LoogaMenuPanelDefinition defaultBackgroundPanel = root != null ? root.DefaultBackgroundPanel : null;
            LoogaMenuPanelDefinition defaultActionBarPanel = root != null ? root.DefaultActionBarPanel : null;

            ValidatePanel("Background", screen.GetBackgroundPanel(defaultBackgroundPanel), panels, ref hasIssue);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null)
                    continue;

                ValidatePanel("Panel", entry.Panel, panels, ref hasIssue);
            }

            ValidatePanel("Action Bar", screen.GetActionBarPanel(defaultActionBarPanel), panels, ref hasIssue);

            if (!hasIssue)
            {
                EditorGUILayout.HelpBox("No obvious screen setup issues found.", MessageType.None);
            }
        }

        private static void ValidatePanel(string label, LoogaMenuPanelDefinition panel,
            HashSet<LoogaMenuPanelDefinition> panels, ref bool hasIssue)
        {
            if (panel == null)
                return;

            if (!panels.Add(panel))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox($"{label} panel '{panel.name}' is referenced more than once.", MessageType.Warning);
            }

            if (!LoogaMenuEditorUtility.TryFindPanel(panel, out _))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox($"{label} panel '{panel.name}' has no matching LoogaMenuPanel in the open scene.",
                    MessageType.Info);
            }
        }
    }

    [CustomPropertyDrawer(typeof(LoogaMenuScreenPanelEntry))]
    public sealed class LoogaMenuScreenPanelEntryDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty panel = property.FindPropertyRelative("_panel");
            SerializedProperty openMode = property.FindPropertyRelative("_openMode");
            SerializedProperty missingBehavior = property.FindPropertyRelative("_missingPanelBehavior");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            Rect panelRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(panelRect, panel, new GUIContent("Panel"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect openModeRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(openModeRect, openMode, new GUIContent("Open Mode"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect missingRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(missingRect, missingBehavior, new GUIContent("Missing Panel"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect parametersRect = new(position.x, y,
                position.width, EditorGUI.GetPropertyHeight(parameters, true));
            EditorGUI.PropertyField(parametersRect, parameters);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            return EditorGUIUtility.singleLineHeight * 3f
                + EditorGUIUtility.standardVerticalSpacing * 3f
                + EditorGUI.GetPropertyHeight(parameters, true);
        }
    }

    [CustomPropertyDrawer(typeof(LoogaMenuBlackboardParameter))]
    public sealed class LoogaMenuBlackboardParameterDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty key = property.FindPropertyRelative("_key");
            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float keyWidth = Mathf.Max(120f, row.width * 0.52f);
            Rect keyRect = new(row.x, row.y, keyWidth, row.height);
            Rect valueRect = new(keyRect.xMax + Gap, row.y, row.width - keyWidth - Gap, row.height);

            EditorGUI.PropertyField(keyRect, key, GUIContent.none);
            DrawValueField(valueRect, property, key.objectReferenceValue as LoogaBlackboardKey);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static void DrawValueField(Rect rect, SerializedProperty property, LoogaBlackboardKey key)
        {
            if (key == null)
            {
                EditorGUI.LabelField(rect, "No Key");
                return;
            }

            SerializedProperty value = key.ValueType switch
            {
                LoogaBlackboardValueType.Bool => property.FindPropertyRelative("_boolValue"),
                LoogaBlackboardValueType.Int => property.FindPropertyRelative("_intValue"),
                LoogaBlackboardValueType.Float => property.FindPropertyRelative("_floatValue"),
                LoogaBlackboardValueType.String => property.FindPropertyRelative("_stringValue"),
                _ => null
            };

            if (value != null)
            {
                EditorGUI.PropertyField(rect, value, GUIContent.none);
            }
        }
    }
}
