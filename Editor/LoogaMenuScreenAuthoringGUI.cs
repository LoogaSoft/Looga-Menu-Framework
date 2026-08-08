using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>
    /// Draws the screen and layout controls that are easier to author as explicit menu concepts.
    /// The serialized runtime model stays compact and independent from editor presentation.
    /// </summary>
    internal static class LoogaMenuScreenAuthoringGUI
    {
        private static readonly string[] LayoutNavigationModes = { "Inherit", "Override", "Hidden" };
        private static readonly string[] ActionBarModes = { "Inherit", "Override", "Hidden" };

        public static void DrawNavigation(SerializedProperty layers, bool supportsInheritance)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            DrawNavigationPlacement(layers, LoogaMenuNavigationPlacement.Primary, supportsInheritance);
            DrawNavigationPlacement(layers, LoogaMenuNavigationPlacement.Secondary, supportsInheritance);
        }

        public static void DrawActionBar(SerializedProperty actionBar)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Action Bar", EditorStyles.boldLabel);

            SerializedProperty mode = actionBar.FindPropertyRelative("_mode");
            int selectedMode = LoogaGUILayout.Tabs(
                mode.enumValueIndex,
                ActionBarModes,
                $"{actionBar.serializedObject.targetObject.GetInstanceID()}_{actionBar.propertyPath}_Mode");
            mode.enumValueIndex = selectedMode;

            if ((LoogaMenuActionBarMode)selectedMode != LoogaMenuActionBarMode.Override)
                return;

            SerializedProperty settings = actionBar.FindPropertyRelative("_settings");
            EditorGUILayout.Space(2f);
            LoogaGUILayout.BoxSmall("Action Bar View", () =>
            {
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("_panel"), new GUIContent("Panel"));
            });

            LoogaGUILayout.BoxSmall("Back Action", () => DrawBackAction(settings));
            LoogaGUILayout.BoxSmall("Context Actions", () =>
            {
                SerializedProperty includeCovered = settings.FindPropertyRelative("_includeCoveredPanels");
                EditorGUILayout.PropertyField(includeCovered, new GUIContent(
                    "Include Covered Panels",
                    "Include actions from panels that remain open beneath the active panel."));
                EditorGUILayout.LabelField(
                    "Active panels contribute their available actions at runtime.",
                    EditorStyles.miniLabel);
            });

            SerializedProperty advanced = settings.FindPropertyRelative("_backBindingFallback");
            advanced.isExpanded = LoogaGUILayout.FoldoutSmall(
                "Advanced",
                advanced.isExpanded,
                () =>
                {
                    EditorGUILayout.PropertyField(advanced, new GUIContent("Back Binding Fallback"));
                    EditorGUILayout.PropertyField(
                        settings.FindPropertyRelative("_backSortOrder"),
                        new GUIContent("Back Sort Order"));
                },
                advanced);
        }

        private static void DrawNavigationPlacement(
            SerializedProperty layers,
            LoogaMenuNavigationPlacement placement,
            bool supportsInheritance)
        {
            string title = placement == LoogaMenuNavigationPlacement.Primary
                ? "Primary Navigation"
                : "Secondary Navigation";

            if (!supportsInheritance)
            {
                DrawScreenNavigationPlacement(layers, placement, title);
                return;
            }

            LoogaGUILayout.BoxSmall(title, () => DrawLayoutNavigationPlacement(layers, placement));
        }

        private static void DrawScreenNavigationPlacement(
            SerializedProperty layers,
            LoogaMenuNavigationPlacement placement,
            string title)
        {
            int layerIndex = FindLayer(layers, placement);
            SerializedProperty layer = layerIndex >= 0 ? layers.GetArrayElementAtIndex(layerIndex) : null;
            bool enabled = layer != null && layer.FindPropertyRelative("_visible").boolValue;
            bool expanded = enabled && layer.isExpanded;

            bool nextExpanded = LoogaGUILayout.ToggleFoldoutSmall(
                new GUIContent(title, "Enable this navigation area and edit the entries it contributes."),
                enabled,
                expanded,
                () => DrawNavigationEntries(layer),
                out bool nextEnabled);

            if (nextEnabled != enabled)
            {
                if (layer == null)
                    layer = AddLayer(layers, placement);

                layer.FindPropertyRelative("_visible").boolValue = nextEnabled;
                layer.isExpanded = nextEnabled;
                return;
            }

            if (layer != null)
                layer.isExpanded = nextExpanded;
        }

        private static void DrawLayoutNavigationPlacement(
            SerializedProperty layers,
            LoogaMenuNavigationPlacement placement)
        {
            int layerIndex = FindLayer(layers, placement);
            SerializedProperty layer = layerIndex >= 0 ? layers.GetArrayElementAtIndex(layerIndex) : null;
            int mode = ResolveNavigationMode(layer, true);
            int nextMode = LoogaGUILayout.Tabs(
                mode,
                LayoutNavigationModes,
                $"{layers.serializedObject.targetObject.GetInstanceID()}_{layers.propertyPath}_{placement}");

            if (nextMode != mode)
                layer = ApplyNavigationMode(layers, layerIndex, placement, true, nextMode);

            if (nextMode == 1 && layer != null)
                DrawNavigationEntries(layer);
        }

        private static void DrawNavigationEntries(SerializedProperty layer)
        {
            SerializedProperty entries = layer.FindPropertyRelative("_entries");
            SerializedProperty defaultIndex = layer.FindPropertyRelative("_defaultEntryIndex");

            EditorGUILayout.Space(2f);
            if (entries.arraySize == 0)
            {
                EditorGUILayout.LabelField("No navigation entries.", EditorStyles.miniLabel);
            }

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string summary = BuildEntrySummary(entry);
                if (defaultIndex.intValue == i)
                    summary += "  (Default)";

                int entryIndex = i;
                entry.isExpanded = LoogaGUILayout.FoldoutSmall(
                    summary,
                    entry.isExpanded,
                    () => DrawNavigationEntry(entries, defaultIndex, entryIndex),
                    entry);
            }

            if (GUILayout.Button("Add Entry"))
                AddEntry(entries, defaultIndex);
        }

        private static void DrawNavigationEntry(
            SerializedProperty entries,
            SerializedProperty defaultIndex,
            int index)
        {
            if (index < 0 || index >= entries.arraySize)
                return;

            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("_displayName"),
                new GUIContent("Label"));
            EditorGUILayout.PropertyField(
                entry.FindPropertyRelative("_destination"),
                new GUIContent("Destination"),
                true);

            SerializedProperty requirements = entry.FindPropertyRelative("_requirements");
            requirements.isExpanded = LoogaGUILayout.FoldoutSmall(
                "Advanced",
                requirements.isExpanded,
                () => EditorGUILayout.PropertyField(
                    requirements,
                    new GUIContent("Requirements"),
                    true),
                requirements);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(defaultIndex.intValue == index))
                {
                    if (GUILayout.Button("Set as Default"))
                        defaultIndex.intValue = index;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveEntry(entries, defaultIndex, index);
                    entries.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void DrawBackAction(SerializedProperty settings)
        {
            SerializedProperty showBack = settings.FindPropertyRelative("_showBackAction");
            EditorGUILayout.PropertyField(showBack, new GUIContent("Show Back Action"));
            if (!showBack.boolValue)
                return;

            EditorGUILayout.PropertyField(
                settings.FindPropertyRelative("_backLabel"),
                new GUIContent("Label"));
            EditorGUILayout.PropertyField(
                settings.FindPropertyRelative("_backInputAction"),
                new GUIContent("Input Action"));
        }

        private static int ResolveNavigationMode(SerializedProperty layer, bool supportsInheritance)
        {
            if (layer == null)
                return 0;

            bool visible = layer.FindPropertyRelative("_visible").boolValue;
            if (!supportsInheritance)
                return visible ? 1 : 0;

            return visible ? 1 : 2;
        }

        private static SerializedProperty ApplyNavigationMode(
            SerializedProperty layers,
            int layerIndex,
            LoogaMenuNavigationPlacement placement,
            bool supportsInheritance,
            int mode)
        {
            bool removeLayer = mode == 0;
            if (removeLayer)
            {
                if (layerIndex >= 0)
                    layers.DeleteArrayElementAtIndex(layerIndex);

                return null;
            }

            SerializedProperty layer = layerIndex >= 0
                ? layers.GetArrayElementAtIndex(layerIndex)
                : AddLayer(layers, placement);
            layer.FindPropertyRelative("_visible").boolValue = !supportsInheritance || mode == 1;
            return layer;
        }

        private static SerializedProperty AddLayer(
            SerializedProperty layers,
            LoogaMenuNavigationPlacement placement)
        {
            int index = layers.arraySize;
            layers.InsertArrayElementAtIndex(index);
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            layer.FindPropertyRelative("_placement").enumValueIndex = (int)placement;
            layer.FindPropertyRelative("_visible").boolValue = true;
            layer.FindPropertyRelative("_defaultEntryIndex").intValue = 0;
            layer.FindPropertyRelative("_entries").arraySize = 0;
            return layer;
        }

        private static int FindLayer(
            SerializedProperty layers,
            LoogaMenuNavigationPlacement placement)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.FindPropertyRelative("_placement").enumValueIndex == (int)placement)
                    return i;
            }

            return -1;
        }

        private static void AddEntry(SerializedProperty entries, SerializedProperty defaultIndex)
        {
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_displayName").stringValue = string.Empty;

            SerializedProperty destination = entry.FindPropertyRelative("_destination");
            destination.FindPropertyRelative("_screen").objectReferenceValue = null;
            destination.FindPropertyRelative("_layout").objectReferenceValue = null;
            destination.FindPropertyRelative("_openMode").enumValueIndex = (int)LoogaMenuOpenMode.Replace;
            entry.FindPropertyRelative("_requirements").objectReferenceValue = null;
            entry.isExpanded = true;

            if (entries.arraySize == 1)
                defaultIndex.intValue = 0;
        }

        private static void RemoveEntry(
            SerializedProperty entries,
            SerializedProperty defaultIndex,
            int index)
        {
            entries.DeleteArrayElementAtIndex(index);
            if (entries.arraySize == 0)
            {
                defaultIndex.intValue = 0;
                return;
            }

            if (defaultIndex.intValue > index)
                defaultIndex.intValue--;
            else if (defaultIndex.intValue >= entries.arraySize)
                defaultIndex.intValue = entries.arraySize - 1;
        }

        private static string BuildEntrySummary(SerializedProperty entry)
        {
            string label = entry.FindPropertyRelative("_displayName").stringValue;
            if (string.IsNullOrWhiteSpace(label))
                label = "Unnamed Entry";

            SerializedProperty destination = entry.FindPropertyRelative("_destination");
            LoogaMenuScreenDefinition screen = destination.FindPropertyRelative("_screen").objectReferenceValue
                as LoogaMenuScreenDefinition;
            LoogaMenuScreenLayout layout = destination.FindPropertyRelative("_layout").objectReferenceValue
                as LoogaMenuScreenLayout;

            string target = screen == null ? "No Destination" : screen.name;
            if (screen != null && layout != null)
                target += $" / {layout.name}";

            return $"{label}  ->  {target}";
        }
    }
}
