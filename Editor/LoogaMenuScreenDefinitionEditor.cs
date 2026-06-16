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
            LoogaMenuScreenDefinition screen = (LoogaMenuScreenDefinition)target;
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Screen",
                "A screen composes reusable panels and evaluates rule assets before it opens.");

            DrawDefaultInspector();
            DrawValidation(screen);
        }

        private static void DrawValidation(LoogaMenuScreenDefinition screen)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            HashSet<LoogaMenuPanelDefinition> panels = new();
            bool hasIssue = false;

            ValidatePanel("Background", screen.BackgroundPanel, panels, ref hasIssue);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry == null)
                    continue;

                ValidatePanel("Panel", entry.Panel, panels, ref hasIssue);
            }

            ValidatePanel("Action Bar", screen.ActionBarPanel, panels, ref hasIssue);

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

            if (!LoogaMenuEditorUtility.TryFindView(panel, out _))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox($"{label} panel '{panel.name}' has no matching LoogaMenuView in the open scene.",
                    MessageType.Info);
            }
        }
    }
}
