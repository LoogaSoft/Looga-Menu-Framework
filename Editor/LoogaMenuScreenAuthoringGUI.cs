using System;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>Draws configured region overrides for screens and layouts.</summary>
    internal static class LoogaMenuScreenAuthoringGUI
    {
        private static readonly string[] RegionModes = { "Inherit", "Override", "Hide" };

        public static void DrawRegions(SerializedProperty overrides)
        {
            LoogaMenuStructureProfile structure = LoogaMenuStructureEditorUtility.FindStructure();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Regions", EditorStyles.boldLabel);

            if (structure == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign or create a Menu Structure Profile before authoring regions.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField("Structure", structure, typeof(LoogaMenuStructureProfile), false);
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(46f)))
                    AssetDatabase.OpenAsset(structure);
            }

            foreach (LoogaMenuRegionDefinition region in structure.Regions)
            {
                if (region != null)
                    DrawRegion(overrides, region);
            }
        }

        private static void DrawRegion(SerializedProperty overrides, LoogaMenuRegionDefinition region)
        {
            int index = FindOverride(overrides, region);
            SerializedProperty regionOverride = index >= 0
                ? overrides.GetArrayElementAtIndex(index)
                : null;
            int mode = regionOverride != null
                ? regionOverride.FindPropertyRelative("_mode").enumValueIndex
                : (int)LoogaMenuRegionMode.Inherit;

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(region.DisplayName, EditorStyles.label);
            int nextMode = LoogaGUILayout.Tabs(
                mode,
                RegionModes,
                $"{overrides.serializedObject.targetObject.GetInstanceID()}_{region.GetInstanceID()}");

            if (nextMode != mode)
            {
                regionOverride ??= AddOverride(overrides, region);
                regionOverride.FindPropertyRelative("_mode").enumValueIndex = nextMode;
            }

            if ((LoogaMenuRegionMode)nextMode != LoogaMenuRegionMode.Override)
                return;

            regionOverride ??= AddOverride(overrides, region);
            SerializedProperty content = regionOverride.FindPropertyRelative("_content");
            LoogaMenuRegionContent contentAsset = content.objectReferenceValue as LoogaMenuRegionContent;
            if (contentAsset == null || contentAsset.GetType() != region.ContentType)
            {
                contentAsset = CreateContent(overrides.serializedObject.targetObject, region);
                content.objectReferenceValue = contentAsset;
                overrides.serializedObject.ApplyModifiedProperties();
            }

            using (new EditorGUI.IndentLevelScope())
                DrawContent(contentAsset);
        }

        internal static void DrawContent(LoogaMenuRegionContent content)
        {
            if (content == null)
                return;

            SerializedObject contentObject = new(content);
            contentObject.UpdateIfRequiredOrScript();

            if (content is LoogaMenuNavigationRegionContent)
            {
                DrawNavigationContent(contentObject);
            }
            else if (content is LoogaMenuActionRegionContent)
            {
                DrawActionContent(contentObject);
            }
            else
            {
                LoogaGUILayout.PropertyField(contentObject.FindProperty("_panels"), true);
            }

            contentObject.ApplyModifiedProperties();
        }

        private static void DrawNavigationContent(SerializedObject contentObject)
        {
            SerializedProperty entries = contentObject.FindProperty("_entries");
            SerializedProperty defaultIndex = contentObject.FindProperty("_defaultEntryIndex");
            LoogaGUILayout.PropertyField(entries, new GUIContent("Entries"), true);
            if (entries.arraySize > 0)
            {
                defaultIndex.intValue = EditorGUILayout.IntSlider(
                    new GUIContent("Default Entry"),
                    defaultIndex.intValue,
                    0,
                    entries.arraySize - 1);
            }
        }

        private static void DrawActionContent(SerializedObject contentObject)
        {
            LoogaGUILayout.PropertyField(contentObject.FindProperty("_panel"));
            SerializedProperty showBack = contentObject.FindProperty("_showBackAction");
            LoogaGUILayout.PropertyField(showBack);
            if (showBack.boolValue)
            {
                LoogaGUILayout.PropertyField(contentObject.FindProperty("_backLabel"));
                LoogaGUILayout.PropertyField(contentObject.FindProperty("_backInputAction"));
            }

            LoogaGUILayout.PropertyField(contentObject.FindProperty("_includeCoveredPanels"));
            SerializedProperty advanced = contentObject.FindProperty("_backBindingFallback");
            advanced.isExpanded = LoogaGUILayout.FoldoutSmall(
                "Advanced",
                advanced.isExpanded,
                () =>
                {
                    LoogaGUILayout.PropertyField(advanced);
                    LoogaGUILayout.PropertyField(contentObject.FindProperty("_backSortOrder"));
                },
                advanced);
        }

        private static int FindOverride(SerializedProperty overrides, LoogaMenuRegionDefinition region)
        {
            for (int i = 0; i < overrides.arraySize; i++)
            {
                SerializedProperty candidate = overrides.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("_region").objectReferenceValue == region)
                    return i;
            }

            return -1;
        }

        private static SerializedProperty AddOverride(
            SerializedProperty overrides,
            LoogaMenuRegionDefinition region)
        {
            int index = overrides.arraySize;
            overrides.InsertArrayElementAtIndex(index);
            SerializedProperty regionOverride = overrides.GetArrayElementAtIndex(index);
            regionOverride.FindPropertyRelative("_region").objectReferenceValue = region;
            regionOverride.FindPropertyRelative("_mode").enumValueIndex = (int)LoogaMenuRegionMode.Inherit;
            regionOverride.FindPropertyRelative("_content").objectReferenceValue = null;
            return regionOverride;
        }

        private static LoogaMenuRegionContent CreateContent(
            UnityEngine.Object owner,
            LoogaMenuRegionDefinition region)
        {
            LoogaMenuRegionContent content =
                ScriptableObject.CreateInstance(region.ContentType) as LoogaMenuRegionContent;
            content.name = region.DisplayName;
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            if (!string.IsNullOrWhiteSpace(ownerPath))
                AssetDatabase.AddObjectToAsset(content, owner);

            Undo.RegisterCreatedObjectUndo(content, "Create Menu Region Content");
            EditorUtility.SetDirty(owner);
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            return content;
        }
    }

    internal static class LoogaMenuStructureEditorUtility
    {
        public static LoogaMenuStructureProfile FindStructure()
        {
            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root != null && root.Structure != null)
                return root.Structure;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LoogaMenuStructureProfile)}");
            if (guids.Length != 1)
                return null;

            return AssetDatabase.LoadAssetAtPath<LoogaMenuStructureProfile>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
