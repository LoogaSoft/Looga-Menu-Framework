using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private const float PreviewButtonHeight = 26f;
        private const float PreviewButtonLeftPadding = 10f;
        private const float PreviewButtonRightPadding = 10f;
        private const float PreviewButtonTriangleCenterInset = 14f;
        private const float PreviewButtonTriangleSide = 8f;

        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, bool> _screenFoldouts = new();
        private static GUIStyle _previewButtonStyle;
        private static GUIStyle _previewDisclosureButtonStyle;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/LoogaSoft/Menu/Preview")]
        public static void Open()
        {
            LoogaMenuPreviewWindow window = GetWindow<LoogaMenuPreviewWindow>("Menu Preview");
            window.minSize = new Vector2(360f, 260f);
            window.RefreshDefinitions();
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnFocus()
        {
            RefreshDefinitions();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove)
                Repaint();

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

            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox("Open the additive UI scene containing LoogaMenuPanel objects to preview definitions.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (LoogaMenuScreenDefinition screen in _screens)
            {
                DrawScreen(screen, panels.Length > 0);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawScreen(LoogaMenuScreenDefinition screen, bool canPreview)
        {
            if (screen == null)
                return;

            LoogaMenuScreenConfiguration[] configurations = screen.Configurations;
            int configurationCount = CountConfigurations(configurations);
            if (configurationCount == 0)
            {
                DrawDefinitionRow(screen, screen.DisplayName, canPreview, () => Preview(screen, null));
                return;
            }

            bool expanded = _screenFoldouts.TryGetValue(screen, out bool current) && current;
            string label = $"{screen.DisplayName} ({configurationCount})";
            GUIContent content = new(label,
                "Left-click to show or hide configurations. Right-click to open and ping the screen definition.");
            Rect row = EditorGUILayout.GetControlRect(false, PreviewButtonHeight);
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && row.Contains(currentEvent.mousePosition))
            {
                OpenDefinition(screen);
                currentEvent.Use();
                return;
            }

            if (DrawPreviewButton(row, content, true, expanded))
                expanded = !expanded;

            _screenFoldouts[screen] = expanded;
            if (!expanded)
                return;

            foreach (LoogaMenuScreenConfiguration configuration in configurations)
            {
                if (configuration == null)
                    continue;

                string suffix = configuration == screen.DefaultConfiguration ? " (Default)" : string.Empty;
                DrawDefinitionRow(configuration, configuration.DisplayName + suffix, canPreview,
                    () => Preview(screen, configuration), 18f);
            }
        }

        private static int CountConfigurations(LoogaMenuScreenConfiguration[] configurations)
        {
            if (configurations == null)
                return 0;

            int count = 0;
            foreach (LoogaMenuScreenConfiguration configuration in configurations)
            {
                if (configuration != null)
                    count++;
            }

            return count;
        }

        private void RefreshDefinitions()
        {
            _screens.Clear();

            LoadDefinitions(_screens);
            _screens.Sort(CompareDefinitions);
        }

        private static void DrawDefinitionRow<T>(T definition, string displayName, bool canPreview,
            System.Action preview, float leftInset = 0f) where T : ScriptableObject
        {
            if (definition == null)
                return;

            string label = string.IsNullOrWhiteSpace(displayName) ? definition.name : displayName;
            GUIContent content = new(label, "Left-click to preview. Right-click to open and ping the definition.");
            Rect buttonRect = EditorGUILayout.GetControlRect(false, PreviewButtonHeight);
            buttonRect.xMin += leftInset;
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown
                && currentEvent.button == 1
                && buttonRect.Contains(currentEvent.mousePosition))
            {
                OpenDefinition(definition);
                currentEvent.Use();
                return;
            }

            using (new EditorGUI.DisabledScope(!canPreview))
            {
                if (DrawPreviewButton(buttonRect, content))
                {
                    preview?.Invoke();
                }
            }
        }

        private static bool DrawPreviewButton(Rect rect, GUIContent content, bool showDisclosure = false,
            bool expanded = false)
        {
            rect = LoogaEditorStyle.PixelSnap(rect);
            GUI.Box(rect, GUIContent.none, LoogaEditorFoldouts.SmallFoldoutBoxStyle);

            if (GUI.enabled && rect.Contains(Event.current.mousePosition))
                LoogaEditorFoldouts.DrawHoverRect(rect);

            GUIStyle style = GetPreviewButtonStyle(showDisclosure);
            bool clicked = GUI.Button(rect, content, style);
            if (showDisclosure)
                DrawDisclosureTriangle(rect, expanded);

            return clicked;
        }

        private static GUIStyle GetPreviewButtonStyle(bool showDisclosure)
        {
            GUIStyle style = showDisclosure ? _previewDisclosureButtonStyle : _previewButtonStyle;
            if (style != null)
            {
                style.padding.left = showDisclosure
                    ? GetDisclosureTextPadding()
                    : Mathf.RoundToInt(PreviewButtonLeftPadding);
                return style;
            }

            style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(
                    showDisclosure ? GetDisclosureTextPadding() : Mathf.RoundToInt(PreviewButtonLeftPadding),
                    Mathf.RoundToInt(PreviewButtonRightPadding),
                    0,
                    0)
            };
            style.normal.textColor = LoogaEditorStyle.TextColor;
            style.hover.textColor = LoogaEditorStyle.TextColor;
            style.active.textColor = LoogaEditorStyle.TextColor;
            style.focused.textColor = LoogaEditorStyle.TextColor;

            if (showDisclosure)
                _previewDisclosureButtonStyle = style;
            else
                _previewButtonStyle = style;

            return style;
        }

        private static int GetDisclosureTextPadding()
        {
            float centerInset = LoogaEditorStyle.Pixels(PreviewButtonTriangleCenterInset);
            float side = LoogaEditorStyle.Pixels(PreviewButtonTriangleSide);
            float altitude = side * Mathf.Sqrt(3f) * 0.5f;
            float triangleLeft = centerInset - side * 0.5f;
            float triangleRight = centerInset + altitude * 2f / 3f;
            float railGap = Mathf.Max(0f, triangleLeft - LoogaEditorStyle.AccentRailWidth);
            return Mathf.CeilToInt(triangleRight + railGap);
        }

        private static void DrawDisclosureTriangle(Rect row, bool expanded)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float side = LoogaEditorStyle.Pixels(PreviewButtonTriangleSide);
            float altitude = side * Mathf.Sqrt(3f) * 0.5f;
            Vector2 center = new(
                LoogaEditorStyle.PixelSnapValue(row.xMin
                    + LoogaEditorStyle.Pixels(PreviewButtonTriangleCenterInset)),
                LoogaEditorStyle.PixelSnapValue(row.center.y));

            Vector3[] points = expanded
                ? new[]
                {
                    new Vector3(center.x - side * 0.5f, center.y - altitude / 3f, 0f),
                    new Vector3(center.x + side * 0.5f, center.y - altitude / 3f, 0f),
                    new Vector3(center.x, center.y + altitude * 2f / 3f, 0f)
                }
                : new[]
                {
                    new Vector3(center.x - altitude / 3f, center.y - side * 0.5f, 0f),
                    new Vector3(center.x - altitude / 3f, center.y + side * 0.5f, 0f),
                    new Vector3(center.x + altitude * 2f / 3f, center.y, 0f)
                };

            Color previousColor = Handles.color;
            Handles.color = LoogaEditorStyle.ArrowColor;
            Handles.BeginGUI();
            Handles.DrawAAConvexPolygon(points);
            Handles.EndGUI();
            Handles.color = previousColor;
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

        private static void Preview(LoogaMenuScreenDefinition screen, LoogaMenuScreenConfiguration configuration)
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

            configuration = screen.ResolveConfiguration(configuration);
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(configuration))
            {
                if (entry == null)
                    continue;

                ShowPanel(entry.Panel);
            }

            ShowSingleContentEntry(screen);
            ShowExtensions(root, screen, configuration);
        }

        /// <summary>
        /// Previews the initial panel composition contributed by effective extensions.
        /// Screen extensions replace matching root defaults by extension ID.
        /// </summary>
        private static void ShowExtensions(LoogaMenuRoot root, LoogaMenuScreenDefinition screen,
            LoogaMenuScreenConfiguration configuration)
        {
            List<LoogaMenuExtensionDefinition> extensions = new();
            LoogaMenuEditorUtility.ResolveExtensions(root, screen, configuration, extensions);

            foreach (LoogaMenuExtensionDefinition extension in extensions)
            {
                if (extension == null || !extension.Enabled)
                    continue;

                if (extension is LoogaMenuActionBarExtension actionBar)
                {
                    ShowPanel(actionBar.Panel);
                    continue;
                }

                if (extension is not LoogaMenuNavigationExtension navigation || navigation.Entries.Length == 0)
                    continue;

                LoogaMenuNavigationEntry initialEntry = ResolveInitialNavigationEntry(navigation, configuration);
                if (initialEntry == null)
                    continue;

                foreach (LoogaMenuScreenPanelEntry entry in initialEntry.Panels)
                {
                    if (entry != null)
                    {
                        ShowPanel(entry.Panel);
                    }
                }
            }
        }

        private static LoogaMenuNavigationEntry ResolveInitialNavigationEntry(
            LoogaMenuNavigationExtension navigation, LoogaMenuScreenConfiguration configuration)
        {
            string initialId = configuration?.InitialNavigationEntryId;
            if (!string.IsNullOrWhiteSpace(initialId))
            {
                foreach (LoogaMenuNavigationEntry entry in navigation.Entries)
                {
                    if (entry != null && entry.StableId == initialId)
                        return entry;
                }
            }

            return navigation.ActivateOnOpen ? navigation.Entries[0] : null;
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

            foreach (LoogaMenuScreenPanelEntry nestedEntry in entry.Screen.GetPanels(entry.Screen.DefaultConfiguration))
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

