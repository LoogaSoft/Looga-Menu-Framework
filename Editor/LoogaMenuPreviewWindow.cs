using System.Collections.Generic;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private Vector2 _scroll;

        [MenuItem("Tools/LoogaSoft/Menu/Preview")]
        public static void Open()
        {
            LoogaMenuPreviewWindow window = GetWindow<LoogaMenuPreviewWindow>("Menu Preview");
            window.minSize = new Vector2(360f, 260f);
            window.RefreshScreens();
            window.Show();
        }

        private void OnFocus()
        {
            RefreshScreens();
        }

        private void OnGUI()
        {
            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(panels.Length == 0))
                {
                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    {
                        ResetPreview(panels);
                    }
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    RefreshScreens();
                }
            }

            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox("Open the additive UI scene containing LoogaMenuPanel objects to preview menu screens.",
                    MessageType.Warning);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (LoogaMenuScreenDefinition screen in _screens)
            {
                string label = string.IsNullOrWhiteSpace(screen.DisplayName) ? screen.name : screen.DisplayName;

                using (new EditorGUI.DisabledScope(panels.Length == 0))
                {
                    if (GUILayout.Button(label, GUILayout.Height(28f)))
                    {
                        Preview(screen);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshScreens()
        {
            _screens.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:LoogaMenuScreenDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LoogaMenuScreenDefinition screen = AssetDatabase.LoadAssetAtPath<LoogaMenuScreenDefinition>(path);
                if (screen != null)
                {
                    _screens.Add(screen);
                }
            }
        }

        private static void Preview(LoogaMenuScreenDefinition screen)
        {
            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            LoogaMenuRoot root = Object.FindFirstObjectByType<LoogaMenuRoot>(FindObjectsInactive.Include);
            LoogaMenuPanelDefinition defaultBackgroundPanel = root != null ? root.DefaultBackgroundPanel : null;

            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }

            ShowPanel(screen.GetBackgroundPanel(defaultBackgroundPanel));

            foreach (LoogaMenuScreenPanelEntry entry in screen.DefaultPanels)
            {
                if (entry == null)
                    continue;

                ShowPanel(entry.Panel);
            }

            ShowSingleContentEntry(screen);
            ShowExtensions(root, screen);
        }

        /// <summary>
        /// Previews the initial panel composition contributed by effective extensions.
        /// Screen extensions replace matching root defaults by extension ID.
        /// </summary>
        private static void ShowExtensions(LoogaMenuRoot root, LoogaMenuScreenDefinition screen)
        {
            List<LoogaMenuExtensionDefinition> extensions = new();
            LoogaMenuEditorUtility.ResolveExtensions(root, screen, extensions);

            foreach (LoogaMenuExtensionDefinition extension in extensions)
            {
                if (extension == null || !extension.Enabled)
                    continue;

                if (extension is LoogaMenuActionBarExtension actionBar)
                {
                    ShowPanel(actionBar.Panel);
                    continue;
                }

                if (extension is not LoogaMenuNavigationExtension navigation
                    || !navigation.ActivateOnOpen
                    || navigation.Entries.Length == 0
                    || navigation.Entries[0] == null)
                    continue;

                foreach (LoogaMenuScreenPanelEntry entry in navigation.Entries[0].Panels)
                {
                    if (entry != null)
                    {
                        ShowPanel(entry.Panel);
                    }
                }
            }
        }

        /// <summary>
        /// A screen with one assigned content entry represents one complete,
        /// unambiguous preview state. Include that content so editor previews
        /// match the composition opened through LoogaMenuRoot.OpenContent.
        /// </summary>
        private static void ShowSingleContentEntry(LoogaMenuScreenDefinition screen)
        {
            LoogaMenuScreenContentEntry[] entries = screen.ContentEntries;
            if (entries == null || entries.Length != 1 || entries[0] == null)
                return;

            LoogaMenuScreenContentEntry entry = entries[0];
            if (entry.TargetType == LoogaMenuContentTargetType.Panel)
            {
                ShowPanel(entry.Panel);
                return;
            }

            if (entry.Screen == null)
                return;

            foreach (LoogaMenuScreenPanelEntry nestedEntry in entry.Screen.DefaultPanels)
            {
                if (nestedEntry != null)
                {
                    ShowPanel(nestedEntry.Panel);
                }
            }
        }

        private static void ResetPreview(LoogaMenuPanel[] panels)
        {
            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }
        }

        private static LoogaMenuPanel ShowPanel(LoogaMenuPanelDefinition definition)
        {
            if (definition == null
                || !LoogaMenuEditorUtility.TryFindPanel(definition, out LoogaMenuPanel panelComponent))
                return null;

            panelComponent.Show();
            EditorUtility.SetDirty(panelComponent);
            return panelComponent;
        }

    }
}

