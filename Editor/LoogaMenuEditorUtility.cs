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

        public static LoogaMenuView[] FindSceneViews()
        {
            List<LoogaMenuView> views = new();
            foreach (LoogaMenuView view in Resources.FindObjectsOfTypeAll<LoogaMenuView>())
            {
                if (view != null && view.gameObject.scene.IsValid())
                {
                    views.Add(view);
                }
            }

            return views.ToArray();
        }

        public static bool TryFindView(LoogaMenuPanelDefinition panel, out LoogaMenuView view)
        {
            foreach (LoogaMenuView candidate in FindSceneViews())
            {
                if (candidate.Panel == panel)
                {
                    view = candidate;
                    return true;
                }
            }

            view = null;
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
