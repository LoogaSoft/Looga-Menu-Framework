using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private const float ConfigurationRowHeight = 22f;
        private const float SceneButtonOuterInsetPixels = 2f;
        private const float SceneIconInsetPixels = 2f;
        private const float HeaderArrowTextGapPixels = 4f;

        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, bool> _screenFoldouts = new();
        private static GUIContent _selectSceneContent;
        private static GUIContent _selectSceneTooltipContent;
        private static GUIStyle _configurationButtonStyle;
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
                        ResetPreview(panels);
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshDefinitions();
            }

            if (panels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Open the UI scene that contains the LoogaMenuPanel objects to preview screens.",
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (LoogaMenuScreenDefinition screen in _screens)
                DrawScreen(screen, panels);
            EditorGUILayout.EndScrollView();
        }

        private void DrawScreen(LoogaMenuScreenDefinition screen, LoogaMenuPanel[] scenePanels)
        {
            if (screen == null)
                return;

            int layoutCount = CountLayouts(screen.Layouts);
            bool hasLayoutFoldout = layoutCount > 1;
            bool expanded = hasLayoutFoldout
                && _screenFoldouts.TryGetValue(screen, out bool current)
                && current;

            using (LoogaEditorFoldouts.BeginLargeFoldoutLayout(
                       expanded,
                       out Rect headerRect,
                       out Rect clickRect))
            {
                string countSuffix = hasLayoutFoldout ? $" ({layoutCount})" : string.Empty;
                GUIContent label = new(
                    screen.DisplayName + countSuffix,
                    hasLayoutFoldout
                        ? "Left-click to show layouts. Right-click to open and ping the screen definition."
                        : "Left-click to preview. Right-click to open and ping the screen definition.");

                bool canPreview = scenePanels.Length > 0;
                bool canSelect = HasSceneObjects(screen, screen.DefaultLayout, scenePanels);
                if (DrawScreenHeader(
                        headerRect,
                        clickRect,
                        label,
                        screen,
                        hasLayoutFoldout,
                        expanded,
                        canPreview,
                        canSelect,
                        () => Preview(screen, screen.DefaultLayout),
                        () => SelectSceneObjects(screen, screen.DefaultLayout, scenePanels)))
                {
                    expanded = !expanded;
                }

                if (hasLayoutFoldout)
                    _screenFoldouts[screen] = expanded;

                if (!expanded)
                    return;

                foreach (LoogaMenuScreenLayout layout in screen.Layouts)
                {
                    if (layout == null)
                        continue;

                    string suffix = layout == screen.DefaultLayout ? " (Default)" : string.Empty;
                    DrawConfigurationRow(
                        layout,
                        layout.DisplayName + suffix,
                        canPreview,
                        HasSceneObjects(screen, layout, scenePanels),
                        () => Preview(screen, layout),
                        () => SelectSceneObjects(screen, layout, scenePanels));
                }
            }
        }

        private static bool DrawScreenHeader(
            Rect headerRect,
            Rect clickRect,
            GUIContent label,
            ScriptableObject definition,
            bool showFoldout,
            bool expanded,
            bool canPreview,
            bool canSelect,
            Action preview,
            Action selectSceneObjects)
        {
            headerRect = LoogaEditorStyle.PixelSnap(headerRect);
            clickRect = LoogaEditorStyle.PixelSnap(clickRect);

            float outerInset = LoogaEditorStyle.Pixels(SceneButtonOuterInsetPixels);
            float sceneButtonSize = Mathf.Max(0f, headerRect.height - outerInset * 2f);
            Rect sceneButtonRect = new(
                headerRect.xMax - outerInset - sceneButtonSize,
                headerRect.yMin + outerInset,
                sceneButtonSize,
                sceneButtonSize);
            sceneButtonRect = LoogaEditorStyle.PixelSnap(sceneButtonRect);

            Rect contentRect = LoogaEditorFoldouts.GetLargeFoldoutHeaderContentRect(headerRect, false);
            Rect arrowRect = LoogaEditorFoldouts.GetLargeFoldoutArrowRect(headerRect);
            if (showFoldout)
            {
                arrowRect.x = contentRect.xMin;
                contentRect.xMin = arrowRect.xMax + LoogaEditorStyle.Pixels(HeaderArrowTextGapPixels);
            }

            contentRect.xMax = sceneButtonRect.xMin - outerInset;

            Rect interactiveRect = clickRect;
            interactiveRect.xMax = sceneButtonRect.xMin - outerInset;
            Event current = Event.current;
            if (interactiveRect.Contains(current.mousePosition))
                LoogaEditorFoldouts.DrawHoverRect(interactiveRect);

            GUI.Label(contentRect, label, EditorStyles.boldLabel);
            if (showFoldout)
                LoogaEditorStyle.DrawFoldoutTriangle(arrowRect, expanded);

            DrawSceneSelectionButton(sceneButtonRect, canSelect, selectSceneObjects);

            if (HandleContextClick(interactiveRect, definition))
                return false;

            if (current.type != EventType.MouseDown
                || current.button != 0
                || !interactiveRect.Contains(current.mousePosition))
            {
                return false;
            }

            if (showFoldout)
            {
                current.Use();
                return true;
            }

            if (canPreview)
            {
                preview?.Invoke();
                current.Use();
            }

            return false;
        }

        private static void DrawConfigurationRow(
            LoogaMenuScreenLayout layout,
            string displayName,
            bool canPreview,
            bool canSelect,
            Action preview,
            Action selectSceneObjects)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, ConfigurationRowHeight);
            rowRect.yMax += EditorGUIUtility.standardVerticalSpacing;
            rowRect = LoogaEditorStyle.PixelSnap(rowRect);
            float outerInset = LoogaEditorStyle.Pixels(SceneButtonOuterInsetPixels);
            float sceneButtonSize = Mathf.Max(0f, rowRect.height - outerInset * 2f);
            Rect sceneButtonRect = new(
                rowRect.xMax - outerInset - sceneButtonSize,
                rowRect.yMin + outerInset,
                sceneButtonSize,
                sceneButtonSize);
            sceneButtonRect = LoogaEditorStyle.PixelSnap(sceneButtonRect);

            GUIContent content = new(
                string.IsNullOrWhiteSpace(displayName) ? layout.name : displayName,
                "Left-click to preview. Right-click to open and ping the layout definition.");
            GUIStyle buttonStyle = GetConfigurationButtonStyle(sceneButtonSize + outerInset * 2f);
            bool pointerOverSceneButton = sceneButtonRect.Contains(Event.current.mousePosition);
            using (new EditorGUI.DisabledScope(!canPreview && !pointerOverSceneButton))
            {
                if (pointerOverSceneButton)
                {
                    GUI.Box(rowRect, content, buttonStyle);
                }
                else if (GUI.Button(rowRect, content, buttonStyle))
                {
                    preview?.Invoke();
                }
            }

            DrawSceneSelectionButton(sceneButtonRect, canSelect, selectSceneObjects);
            HandleContextClick(rowRect, layout);
        }

        private static void DrawSceneSelectionButton(Rect rect, bool canSelect, Action selectSceneObjects)
        {
            GUIContent content = GetSelectSceneContent();
            GUIContent tooltipContent = GetSelectSceneTooltipContent();
            tooltipContent.tooltip = canSelect
                ? "Select the corresponding panel object(s) in the hierarchy."
                : "No corresponding panel objects are loaded in the open scene.";

            bool clicked;
            using (new EditorGUI.DisabledScope(!canSelect))
            {
                clicked = GUI.Button(rect, tooltipContent, EditorStyles.miniButton);

                if (Event.current.type == EventType.Repaint && content.image != null)
                {
                    float iconInset = LoogaEditorStyle.Pixels(SceneIconInsetPixels);
                    Rect iconRect = new(
                        rect.xMin + iconInset,
                        rect.yMin + iconInset,
                        Mathf.Max(0f, rect.width - iconInset * 2f),
                        Mathf.Max(0f, rect.height - iconInset * 2f));
                    GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit, true);
                }
            }

            if (clicked)
                selectSceneObjects?.Invoke();
        }

        private static GUIContent GetSelectSceneContent()
        {
            if (_selectSceneContent != null)
                return _selectSceneContent;

            string iconName = EditorGUIUtility.isProSkin
                ? "d_scenevis_visible_hover"
                : "scenevis_visible_hover";
            Texture icon = EditorGUIUtility.IconContent(iconName).image;
            icon ??= EditorGUIUtility.IconContent("ViewToolZoom").image;
            _selectSceneContent = new GUIContent(icon);
            return _selectSceneContent;
        }

        private static GUIContent GetSelectSceneTooltipContent()
        {
            return _selectSceneTooltipContent ??= new GUIContent();
        }

        private static GUIStyle GetConfigurationButtonStyle(float reservedRightWidth)
        {
            _configurationButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft
            };

            _configurationButtonStyle.padding.left = Mathf.Max(6, EditorStyles.miniButton.padding.left);
            _configurationButtonStyle.padding.right = Mathf.CeilToInt(reservedRightWidth);
            return _configurationButtonStyle;
        }

        private static bool HandleContextClick(Rect rect, UnityEngine.Object definition)
        {
            Event current = Event.current;
            if (current.type != EventType.ContextClick || !rect.Contains(current.mousePosition))
                return false;

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            AssetDatabase.OpenAsset(definition);
            current.Use();
            return true;
        }

        private static int CountLayouts(LoogaMenuScreenLayout[] layouts)
        {
            if (layouts == null)
                return 0;

            int count = 0;
            foreach (LoogaMenuScreenLayout layout in layouts)
            {
                if (layout != null)
                    count++;
            }

            return count;
        }

        private void RefreshDefinitions()
        {
            _screens.Clear();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LoogaMenuScreenDefinition)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LoogaMenuScreenDefinition screen = AssetDatabase.LoadAssetAtPath<LoogaMenuScreenDefinition>(path);
                if (screen != null)
                    _screens.Add(screen);
            }

            _screens.Sort((left, right) => string.Compare(
                left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            return FindSceneObjects(screen, layout, scenePanels).Count > 0;
        }

        private static void SelectSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            List<GameObject> sceneObjects = FindSceneObjects(screen, layout, scenePanels);
            if (sceneObjects.Count == 0)
                return;

            UnityEngine.Object[] selection = new UnityEngine.Object[sceneObjects.Count];
            for (int i = 0; i < sceneObjects.Count; i++)
                selection[i] = sceneObjects[i];

            Selection.objects = selection;
            Selection.activeObject = sceneObjects[0];
            EditorGUIUtility.PingObject(sceneObjects[0]);
        }

        private static List<GameObject> FindSceneObjects(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuPanel[] scenePanels)
        {
            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            List<GameObject> sceneObjects = new();
            foreach (LoogaMenuPanel panel in scenePanels)
            {
                if (panel != null && panel.Panel != null && definitions.Contains(panel.Panel))
                    sceneObjects.Add(panel.gameObject);
            }

            return sceneObjects;
        }

        private static HashSet<LoogaMenuPanelDefinition> CollectPanelDefinitions(
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout)
        {
            HashSet<LoogaMenuPanelDefinition> definitions = new();
            if (screen == null)
                return definitions;

            layout = screen.ResolveLayout(layout);
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(layout))
            {
                if (entry?.Panel != null)
                    definitions.Add(entry.Panel);
            }

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            LoogaMenuStructureProfile structure = root != null
                ? root.Structure
                : LoogaMenuStructureEditorUtility.FindStructure();
            if (structure == null)
                return definitions;

            List<LoogaMenuPanelDefinition> regionPanels = new();
            foreach (LoogaMenuRegionDefinition region in structure.Regions)
            {
                LoogaMenuRegionContent content = screen.ResolveRegion(layout, region);
                if (content == null)
                    continue;

                regionPanels.Clear();
                content.CollectPanels(regionPanels);
                foreach (LoogaMenuPanelDefinition panel in regionPanels)
                {
                    if (panel != null)
                        definitions.Add(panel);
                }
            }

            return definitions;
        }

        private static void Preview(LoogaMenuScreenDefinition screen, LoogaMenuScreenLayout layout)
        {
            LoogaMenuPanel[] panels = LoogaMenuEditorUtility.FindScenePanels();
            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }

            HashSet<LoogaMenuPanelDefinition> definitions = CollectPanelDefinitions(screen, layout);
            foreach (LoogaMenuPanelDefinition definition in definitions)
                ShowPanel(definition);
        }

        private static void ResetPreview(LoogaMenuPanel[] panels)
        {
            foreach (LoogaMenuPanel panel in panels)
            {
                panel.Hide();
                EditorUtility.SetDirty(panel);
            }
        }

        private static void ShowPanel(LoogaMenuPanelDefinition definition)
        {
            if (definition == null
                || !LoogaMenuEditorUtility.TryFindPanel(definition, out LoogaMenuPanel panel))
            {
                return;
            }

            panel.Show();
            EditorUtility.SetDirty(panel);
        }
    }
}
