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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_panels"), new GUIContent("Default Panels"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_contentEntries"), new GUIContent("Content Entries"));
            DrawPanelReference(serializedObject.FindProperty("_backgroundPanelMode"),
                serializedObject.FindProperty("_backgroundPanel"), "Background Panel");
            DrawPanelReference(serializedObject.FindProperty("_actionBarPanelMode"),
                serializedObject.FindProperty("_actionBarPanel"), "Action Bar Panel");

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rules"), new GUIContent("Open Requirements"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_inputPolicy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_missingPanelBehavior"),
                new GUIContent("Missing Panel Behavior"));
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

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
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
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            Rect panelRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(panelRect, panel, new GUIContent("Panel"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect parametersRect = new(position.x, y,
                position.width, EditorGUI.GetPropertyHeight(parameters, true));
            EditorGUI.PropertyField(parametersRect, parameters);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            return EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing
                + EditorGUI.GetPropertyHeight(parameters, true);
        }
    }

    [CustomPropertyDrawer(typeof(LoogaMenuScreenContentEntry))]
    public sealed class LoogaMenuScreenContentEntryDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty targetType = property.FindPropertyRelative("_targetType");
            SerializedProperty panel = property.FindPropertyRelative("_panel");
            SerializedProperty screen = property.FindPropertyRelative("_screen");
            SerializedProperty openMode = property.FindPropertyRelative("_openMode");
            SerializedProperty backBehavior = property.FindPropertyRelative("_backBehavior");
            SerializedProperty rules = property.FindPropertyRelative("_rules");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            Rect targetRow = new(position.x, y, position.width, lineHeight);
            Rect targetContentRect = EditorGUI.PrefixLabel(targetRow, new GUIContent("Target"));
            float targetTypeWidth = Mathf.Min(116f, targetContentRect.width * 0.34f);
            Rect targetTypeRect = new(targetContentRect.x, targetContentRect.y, targetTypeWidth, targetContentRect.height);
            Rect targetRect = new(targetTypeRect.xMax + Gap, targetRow.y,
                targetContentRect.width - targetTypeWidth - Gap, targetRow.height);
            bool targetsScreen = (LoogaMenuContentTargetType)targetType.enumValueIndex == LoogaMenuContentTargetType.Screen;
            EditorGUI.PropertyField(targetTypeRect, targetType, GUIContent.none);
            EditorGUI.PropertyField(targetRect, targetsScreen ? screen : panel, GUIContent.none);

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect openModeRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(openModeRect, openMode, new GUIContent("Open Mode"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect backBehaviorRect = new(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(backBehaviorRect, backBehavior, new GUIContent("Back Behavior"));

            y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            float rulesHeight = EditorGUI.GetPropertyHeight(rules, true);
            Rect rulesRect = new(position.x, y, position.width, rulesHeight);
            EditorGUI.PropertyField(rulesRect, rules, new GUIContent("Open Requirements"));

            y += rulesHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect parametersRect = new(position.x, y, position.width, EditorGUI.GetPropertyHeight(parameters, true));
            EditorGUI.PropertyField(parametersRect, parameters);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty rules = property.FindPropertyRelative("_rules");
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            return lineHeight * 3f
                + spacing * 4f
                + EditorGUI.GetPropertyHeight(rules, true)
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
                SerializedProperty contentEntryIndex = serializedObject.FindProperty("_contentEntryIndex");
                EditorGUILayout.PropertyField(contentScreen, new GUIContent("Content Screen"));
                DrawContentEntryPopup(contentScreen.objectReferenceValue as LoogaMenuScreenDefinition, contentEntryIndex);
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_screen"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_menuRoot"));
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawContentEntryPopup(LoogaMenuScreenDefinition screen, SerializedProperty index)
        {
            if (screen == null)
            {
                EditorGUILayout.HelpBox("Assign a screen definition to choose one of its content entries.", MessageType.Info);
                return;
            }

            LoogaMenuScreenContentEntry[] entries = screen.ContentEntries;
            if (entries == null || entries.Length == 0)
            {
                EditorGUILayout.HelpBox($"'{screen.DisplayName}' has no content entries.", MessageType.Warning);
                return;
            }

            string[] labels = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                labels[i] = GetContentEntryLabel(entries[i], i);
            }

            index.intValue = Mathf.Clamp(index.intValue, 0, entries.Length - 1);
            index.intValue = EditorGUILayout.Popup("Content Entry", index.intValue, labels);
        }

        private static string GetContentEntryLabel(LoogaMenuScreenContentEntry entry, int index)
        {
            if (entry == null)
                return $"Entry {index}";

            if (entry.TargetType == LoogaMenuContentTargetType.Screen)
            {
                return entry.Screen != null ? entry.Screen.DisplayName : $"Entry {index} (Missing Screen)";
            }

            return entry.Panel != null ? entry.Panel.DisplayName : $"Entry {index} (Missing Panel)";
        }
    }
}
