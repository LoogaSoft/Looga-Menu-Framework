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
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    RefreshScreens();
                }

                GUILayout.FlexibleSpace();
            }

            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox("Open the additive UI scene containing LoogaMenuPanel objects to preview menu screens.",
                    MessageType.Warning);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (LoogaMenuScreenDefinition screen in _screens)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(screen.DisplayName, EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledScope(panels.Length == 0))
                    {
                        if (GUILayout.Button("Preview"))
                        {
                            Preview(screen);
                        }
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
            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }

            ShowPanel(screen.BackgroundPanel, null);

            foreach (LoogaMenuScreenPanelEntry entry in screen.Panels)
            {
                if (entry != null)
                {
                    ShowPanel(entry.Panel, entry.PanelMode);
                }
            }

            ShowPanel(screen.ActionBarPanel, null);
        }

        private static void ShowPanel(LoogaMenuPanelDefinition definition, LoogaMenuPanelMode panelMode)
        {
            if (definition == null
                || !LoogaMenuEditorUtility.TryFindPanel(definition, out LoogaMenuPanel panelComponent))
                return;

            panelComponent.Show(panelMode);
            EditorUtility.SetDirty(panelComponent);
        }
    }
}