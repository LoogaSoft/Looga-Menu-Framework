using System.Collections.Generic;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuEditorUtility
    {
        public static LoogaMenuRoot FindMenuRoot()
        {
            foreach (LoogaMenuRoot root in Resources.FindObjectsOfTypeAll<LoogaMenuRoot>())
            {
                if (root != null && root.gameObject.scene.IsValid())
                    return root;
            }

            return null;
        }

        public static LoogaMenuPanel[] FindScenePanels()
        {
            List<LoogaMenuPanel> panels = new();
            foreach (LoogaMenuPanel panel in Resources.FindObjectsOfTypeAll<LoogaMenuPanel>())
            {
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    panels.Add(panel);
                }
            }

            return panels.ToArray();
        }

        public static bool TryFindPanel(LoogaMenuPanelDefinition definition, out LoogaMenuPanel panel)
        {
            foreach (LoogaMenuPanel candidate in FindScenePanels())
            {
                if (candidate.Panel == definition)
                {
                    panel = candidate;
                    return true;
                }
            }

            panel = null;
            return false;
        }

        public static void DrawDefinitionHeader(string title, string helpText)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(helpText))
            {
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
            }
        }
    }
}