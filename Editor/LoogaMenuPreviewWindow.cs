using System.Collections.Generic;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private enum PreviewTab
        {
            Screens,
            Panels
        }

        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly List<LoogaMenuPanelDefinition> _panelDefinitions = new();
        private readonly Vector2[] _scrollPositions = new Vector2[2];
        private PreviewTab _tab;

        [MenuItem("Tools/LoogaSoft/Menu/Preview")]
        public static void Open()
        {
            LoogaMenuPreviewWindow window = GetWindow<LoogaMenuPreviewWindow>("Menu Preview");
            window.minSize = new Vector2(360f, 260f);
            window.RefreshDefinitions();
            window.Show();
        }

        private void OnFocus()
        {
            RefreshDefinitions();
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
                    RefreshDefinitions();
                }
            }

            _tab = (PreviewTab)GUILayout.Toolbar((int)_tab, new[] { "Screens", "Panels" });

            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox("Open the additive UI scene containing LoogaMenuPanel objects to preview definitions.",
                    MessageType.Warning);
            }

            int tabIndex = (int)_tab;
            _scrollPositions[tabIndex] = EditorGUILayout.BeginScrollView(_scrollPositions[tabIndex]);

            if (_tab == PreviewTab.Screens)
            {
                foreach (LoogaMenuScreenDefinition screen in _screens)
                {
                    DrawDefinitionRow(screen, screen.DisplayName, panels.Length > 0, () => Preview(screen));
                }
            }
            else
            {
                foreach (LoogaMenuPanelDefinition panel in _panelDefinitions)
                {
                    DrawDefinitionRow(panel, panel.DisplayName, panels.Length > 0, () => Preview(panel));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshDefinitions()
        {
            _screens.Clear();
            _panelDefinitions.Clear();

            LoadDefinitions(_screens);
            LoadDefinitions(_panelDefinitions);
            _screens.Sort(CompareDefinitions);
            _panelDefinitions.Sort(CompareDefinitions);
        }

        private static void DrawDefinitionRow<T>(T definition, string displayName, bool canPreview,
            System.Action preview) where T : ScriptableObject
        {
            if (definition == null)
                return;

            string label = string.IsNullOrWhiteSpace(displayName) ? definition.name : displayName;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canPreview))
                {
                    if (GUILayout.Button(label, GUILayout.Height(28f)))
                    {
                        preview?.Invoke();
                    }
                }

                if (GUILayout.Button(new GUIContent("Ping", "Ping this definition in the Project window."),
                        EditorStyles.miniButtonLeft, GUILayout.Width(44f), GUILayout.Height(28f)))
                {
                    EditorGUIUtility.PingObject(definition);
                }

                if (GUILayout.Button(new GUIContent("Open", "Select and open this definition."),
                        EditorStyles.miniButtonRight, GUILayout.Width(44f), GUILayout.Height(28f)))
                {
                    OpenDefinition(definition);
                }
            }
        }

        private static void LoadDefinitions<T>(List<T> definitions) where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T definition = AssetDatabase.LoadAssetAtPath<T>(path);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }
        }

        private static int CompareDefinitions<T>(T left, T right) where T : Object
        {
            return string.Compare(left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void OpenDefinition(Object definition)
        {
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            AssetDatabase.OpenAsset(definition);
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

        private static void Preview(LoogaMenuPanelDefinition definition)
        {
            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            ResetPreview(panels);
            ShowPanel(definition);
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

