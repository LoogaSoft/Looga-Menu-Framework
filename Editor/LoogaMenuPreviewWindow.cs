using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    public sealed class LoogaMenuPreviewWindow : EditorWindow
    {
        private const float RevealIconSizePixels = 18f;
        private const float RevealButtonInsetPixels = 5f;
        private const float RevealButtonGapPixels = 2f;
        private const float ConfigurationTextInsetPixels = 8f;
        private const float ExpandedScreenBottomPaddingPixels = 7f;
        private const float HeaderArrowTextGapPixels = 4f;
        private const string HierarchyIconPath =
            "Packages/com.loogasoft.loogamenuframework/Editor/Icons/Remix/node-tree.png";
        private const string DefinitionIconPath =
            "Packages/com.loogasoft.loogamenuframework/Editor/Icons/Remix/file-settings-fill.png";

        private readonly List<LoogaMenuScreenDefinition> _screens = new();
        private readonly Dictionary<LoogaMenuScreenDefinition, bool> _screenFoldouts = new();
        private static Texture _hierarchyIcon;
        private static Texture _definitionIcon;
        private static GUIContent _buttonTooltipContent;
        private static GUIStyle _configurationButtonStyle;
        private static GUIStyle _sceneButtonStyle;
        private Vector2 _scrollPosition;

        [MenuItem("LoogaSoft/Menu Framework/Menu Preview")]
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

                float configurationRowHeight = LoogaEditorFoldouts.GetLargeFoldoutHeaderHeight();
                Rect configurationBlock = EditorGUILayout.GetControlRect(
                    false,
                    configurationRowHeight * layoutCount);
                int configurationIndex = 0;
                foreach (LoogaMenuScreenLayout layout in screen.Layouts)
                {
                    if (layout == null)
                        continue;

                    Rect rowRect = new(
                        configurationBlock.x,
                        configurationBlock.y + configurationRowHeight * configurationIndex,
                        configurationBlock.width,
                        configurationRowHeight);
                    string suffix = layout == screen.DefaultLayout ? " (Default)" : string.Empty;
                    DrawConfigurationRow(
                        rowRect,
                        layout,
                        layout.DisplayName + suffix,
                        canPreview,
                        HasSceneObjects(screen, layout, scenePanels),
                        () => Preview(screen, layout),
                        () => SelectSceneObjects(screen, layout, scenePanels));
                    configurationIndex++;
                }

                EditorGUILayout.Space(LoogaEditorStyle.Pixels(ExpandedScreenBottomPaddingPixels));
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

            float outerInset = LoogaEditorStyle.Pixels(RevealButtonInsetPixels);
            GetRevealButtonRects(
                headerRect,
                outerInset,
                out Rect hierarchyButtonRect,
                out Rect definitionButtonRect);

            Rect contentRect = LoogaEditorFoldouts.GetLargeFoldoutHeaderContentRect(headerRect, false);
            Rect arrowRect = LoogaEditorFoldouts.GetLargeFoldoutArrowRect(headerRect);
            if (showFoldout)
            {
                arrowRect.x = contentRect.xMin;
                contentRect.xMin = arrowRect.xMax + LoogaEditorStyle.Pixels(HeaderArrowTextGapPixels);
            }

            contentRect.xMax = hierarchyButtonRect.xMin - outerInset;

            Rect interactiveRect = clickRect;
            interactiveRect.xMax = hierarchyButtonRect.xMin - outerInset;
            Event current = Event.current;
            if (clickRect.Contains(current.mousePosition))
                LoogaEditorFoldouts.DrawHoverRect(clickRect);

            GUI.Label(contentRect, label, EditorStyles.boldLabel);
            if (showFoldout)
                LoogaEditorStyle.DrawFoldoutTriangle(arrowRect, expanded);

            DrawRevealButton(
                hierarchyButtonRect,
                GetHierarchyIcon(),
                canSelect,
                canSelect
                    ? "Select Scene Object"
                    : "No corresponding panel objects are loaded in the open scene.",
                selectSceneObjects);
            DrawRevealButton(
                definitionButtonRect,
                GetDefinitionIcon(),
                definition != null,
                "Ping Definition Asset",
                () => SelectDefinitionAsset(definition));

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
            Rect rowRect,
            LoogaMenuScreenLayout layout,
            string displayName,
            bool canPreview,
            bool canSelect,
            Action preview,
            Action selectSceneObjects)
        {
            rowRect = LoogaEditorStyle.PixelSnap(rowRect);
            float outerInset = LoogaEditorStyle.Pixels(RevealButtonInsetPixels);
            GetRevealButtonRects(
                rowRect,
                outerInset,
                out Rect hierarchyButtonRect,
                out Rect definitionButtonRect);

            GUIContent content = new(
                string.IsNullOrWhiteSpace(displayName) ? layout.name : displayName,
                "Left-click to preview. Right-click to open and ping the layout definition.");
            float reservedRightWidth = rowRect.xMax - hierarchyButtonRect.xMin + outerInset;
            GUIStyle buttonStyle = GetConfigurationButtonStyle(reservedRightWidth);
            bool pointerOverRevealButton = hierarchyButtonRect.Contains(Event.current.mousePosition)
                                           || definitionButtonRect.Contains(Event.current.mousePosition);
            using (new EditorGUI.DisabledScope(!canPreview && !pointerOverRevealButton))
            {
                if (pointerOverRevealButton)
                {
                    GUI.Box(rowRect, content, buttonStyle);
                }
                else if (GUI.Button(rowRect, content, buttonStyle))
                {
                    preview?.Invoke();
                }
            }

            DrawRevealButton(
                hierarchyButtonRect,
                GetHierarchyIcon(),
                canSelect,
                canSelect
                    ? "Select Scene Object"
                    : "No corresponding panel objects are loaded in the open scene.",
                selectSceneObjects);
            DrawRevealButton(
                definitionButtonRect,
                GetDefinitionIcon(),
                layout != null,
                "Ping Definition Asset",
                () => SelectDefinitionAsset(layout));
            HandleContextClick(rowRect, layout);
        }

        private static void DrawRevealButton(
            Rect rect,
            Texture icon,
            bool enabled,
            string tooltip,
            Action action)
        {
            GUIContent tooltipContent = GetButtonTooltipContent();
            tooltipContent.tooltip = tooltip;

            bool clicked;
            using (new EditorGUI.DisabledScope(!enabled))
            {
                clicked = GUI.Button(rect, tooltipContent, GetRevealButtonStyle());

                if (Event.current.type == EventType.Repaint && icon != null)
                {
                    float iconSize = Mathf.Min(
                        LoogaEditorStyle.Pixels(RevealIconSizePixels),
                        Mathf.Min(rect.width, rect.height));
                    Rect iconRect = new(
                        rect.center.x - iconSize * 0.5f,
                        rect.center.y - iconSize * 0.5f,
                        iconSize,
                        iconSize);
                    iconRect = LoogaEditorStyle.PixelSnap(iconRect);
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }
            }

            if (clicked)
                action?.Invoke();
        }

        private static Texture GetHierarchyIcon()
        {
            if (_hierarchyIcon != null)
                return _hierarchyIcon;

            _hierarchyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(HierarchyIconPath);
            _hierarchyIcon ??= EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image;
            return _hierarchyIcon;
        }

        private static Texture GetDefinitionIcon()
        {
            if (_definitionIcon != null)
                return _definitionIcon;

            _definitionIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(DefinitionIconPath);
            _definitionIcon ??= EditorGUIUtility.IconContent("ScriptableObject Icon").image;
            return _definitionIcon;
        }

        private static GUIContent GetButtonTooltipContent()
        {
            return _buttonTooltipContent ??= new GUIContent();
        }

        private static GUIStyle GetConfigurationButtonStyle(float reservedRightWidth)
        {
            _configurationButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 0f,
                fixedHeight = 0f,
                stretchWidth = true,
                stretchHeight = true
            };

            _configurationButtonStyle.padding.left = Mathf.CeilToInt(
                LoogaEditorStyle.Pixels(ConfigurationTextInsetPixels));
            _configurationButtonStyle.padding.right = Mathf.CeilToInt(reservedRightWidth);
            return _configurationButtonStyle;
        }

        private static GUIStyle GetRevealButtonStyle()
        {
            return _sceneButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 0f,
                fixedHeight = 0f,
                stretchWidth = true,
                stretchHeight = true,
                padding = new RectOffset()
            };
        }

        private static void GetRevealButtonRects(
            Rect rowRect,
            float outerInset,
            out Rect hierarchyButtonRect,
            out Rect definitionButtonRect)
        {
            float size = Mathf.Max(0f, rowRect.height - outerInset * 2f);
            float gap = LoogaEditorStyle.Pixels(RevealButtonGapPixels);
            definitionButtonRect = LoogaEditorStyle.PixelSnap(new Rect(
                rowRect.xMax - outerInset - size,
                rowRect.center.y - size * 0.5f,
                size,
                size));
            hierarchyButtonRect = definitionButtonRect;
            hierarchyButtonRect.x = definitionButtonRect.xMin - gap - size;
            hierarchyButtonRect = LoogaEditorStyle.PixelSnap(hierarchyButtonRect);
        }

        private static void SelectDefinitionAsset(UnityEngine.Object definition)
        {
            if (definition == null)
                return;

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
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
