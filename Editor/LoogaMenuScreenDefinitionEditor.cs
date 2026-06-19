using System.Collections.Generic;
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

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root == null)
            {
                EditorGUILayout.HelpBox(
                    "Scene panel validation is unavailable because no LoogaMenuRoot is loaded. Open the UI scene, or any scene containing a LoogaMenuRoot, to validate panel references against live scene objects.",
                    MessageType.Info);
                return;
            }

            HashSet<LoogaMenuPanelDefinition> panels = new();
            bool hasIssue = false;
            ValidatePanel("Background", screen.GetBackgroundPanel(root.DefaultBackgroundPanel), panels, ref hasIssue);

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry == null)
                    continue;

                ValidatePanel("Panel", entry.Panel, panels, ref hasIssue);
            }

            ValidatePanel("Action Bar", screen.GetActionBarPanel(root.DefaultActionBarPanel), panels, ref hasIssue);

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
}
