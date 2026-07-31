using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenDefinition))]
    public sealed class LoogaMenuScreenDefinitionEditor : LoogaEditor
    {
        protected override void DrawBeforeProperties()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Screen",
                "A screen composes reusable panels and evaluates rule assets before it opens.");
        }

        protected override void DrawAfterProperties()
        {
            DrawValidation((LoogaMenuScreenDefinition)target);
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

            List<LoogaMenuExtensionDefinition> extensions = new();
            LoogaMenuEditorUtility.ResolveExtensions(root, screen, extensions);
            foreach (LoogaMenuExtensionDefinition extension in extensions)
            {
                if (extension == null || !extension.Enabled)
                    continue;

                if (extension is LoogaMenuActionBarExtension actionBar)
                {
                    ValidatePanel("Action Bar", actionBar.Panel, panels, ref hasIssue);
                    continue;
                }

                if (extension is not LoogaMenuNavigationExtension navigation)
                    continue;

                foreach (LoogaMenuNavigationEntry navigationEntry in navigation.Entries)
                {
                    if (navigationEntry == null)
                        continue;

                    // Navigation entries are mutually exclusive, so the same reusable panel may
                    // intentionally appear in more than one entry. Only duplicates within the
                    // active composition are invalid.
                    HashSet<LoogaMenuPanelDefinition> navigationPanels = new(panels);
                    foreach (LoogaMenuScreenPanelEntry entry in navigationEntry.Panels)
                    {
                        if (entry == null)
                            continue;

                        ValidatePanel($"Navigation '{navigationEntry.DisplayName}'", entry.Panel,
                            navigationPanels, ref hasIssue);
                    }
                }
            }

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
