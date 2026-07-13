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
            LoogaMenuPanelDefinition defaultActionBarPanel = root != null ? root.DefaultActionBarPanel : null;

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

            ShowPanel(screen.GetActionBarPanel(defaultActionBarPanel));
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

