using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenDefinition))]
    public sealed class LoogaMenuScreenDefinitionEditor : LoogaEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Screen",
                "A screen is one menu destination. Its layouts change composition without adding history.");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));
            DrawLayouts((LoogaMenuScreenDefinition)target);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_navigation"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_actionBar"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_backgroundPanelMode"));
            if ((LoogaMenuPanelReferenceMode)serializedObject.FindProperty("_backgroundPanelMode").enumValueIndex
                == LoogaMenuPanelReferenceMode.Override)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_backgroundPanel"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rules"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_inputPolicy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultOpenMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_missingPanelBehavior"));
            serializedObject.ApplyModifiedProperties();

            DrawValidation((LoogaMenuScreenDefinition)target);
        }

        private void DrawLayouts(LoogaMenuScreenDefinition screen)
        {
            SerializedProperty layouts = serializedObject.FindProperty("_layouts");
            SerializedProperty defaultLayout = serializedObject.FindProperty("_defaultLayout");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layouts", EditorStyles.boldLabel);
            if (layouts.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add a layout before opening this screen. A layout owns the screen's panel composition.",
                    MessageType.Info);
            }

            for (int i = 0; i < layouts.arraySize; i++)
            {
                SerializedProperty element = layouts.GetArrayElementAtIndex(i);
                LoogaMenuScreenLayout layout = element.objectReferenceValue as LoogaMenuScreenLayout;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(layout, typeof(LoogaMenuScreenLayout), false);

                    bool isDefault = defaultLayout.objectReferenceValue == layout;
                    using (new EditorGUI.DisabledScope(isDefault || layout == null))
                    {
                        if (GUILayout.Button(isDefault ? "Default" : "Set Default", GUILayout.Width(78f)))
                            defaultLayout.objectReferenceValue = layout;
                    }

                    if (GUILayout.Button("-", GUILayout.Width(22f)))
                    {
                        RemoveLayout(layouts, defaultLayout, i, layout);
                        return;
                    }
                }
            }

            if (GUILayout.Button("Add Layout"))
                CreateLayout(screen, layouts, defaultLayout);
        }

        private void CreateLayout(
            LoogaMenuScreenDefinition screen,
            SerializedProperty layouts,
            SerializedProperty defaultLayout)
        {
            string assetPath = AssetDatabase.GetAssetPath(screen);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("Save Screen First",
                    "Save the screen asset before adding layouts.", "OK");
                return;
            }

            LoogaMenuScreenLayout layout = CreateInstance<LoogaMenuScreenLayout>();
            LoogaMenuScreenLayout[] existingLayouts = screen.Layouts ?? System.Array.Empty<LoogaMenuScreenLayout>();
            layout.name = ObjectNames.GetUniqueName(
                System.Array.ConvertAll(existingLayouts, value => value != null ? value.name : string.Empty),
                layouts.arraySize == 0 ? "Default" : "Layout");
            AssetDatabase.AddObjectToAsset(layout, screen);
            Undo.RegisterCreatedObjectUndo(layout, "Create Menu Screen Layout");

            int index = layouts.arraySize;
            layouts.InsertArrayElementAtIndex(index);
            layouts.GetArrayElementAtIndex(index).objectReferenceValue = layout;
            if (defaultLayout.objectReferenceValue == null)
                defaultLayout.objectReferenceValue = layout;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            GUIUtility.ExitGUI();
        }

        private void RemoveLayout(
            SerializedProperty layouts,
            SerializedProperty defaultLayout,
            int index,
            LoogaMenuScreenLayout layout)
        {
            SerializedProperty element = layouts.GetArrayElementAtIndex(index);
            element.objectReferenceValue = null;
            layouts.DeleteArrayElementAtIndex(index);
            if (defaultLayout.objectReferenceValue == layout)
            {
                defaultLayout.objectReferenceValue = layouts.arraySize > 0
                    ? layouts.GetArrayElementAtIndex(0).objectReferenceValue
                    : null;
            }

            serializedObject.ApplyModifiedProperties();
            if (layout != null && AssetDatabase.IsSubAsset(layout))
                Undo.DestroyObjectImmediate(layout);

            AssetDatabase.SaveAssets();
            GUIUtility.ExitGUI();
        }

        private static void DrawValidation(LoogaMenuScreenDefinition screen)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            bool hasIssue = false;
            if (screen.Layouts == null || screen.Layouts.Length == 0)
            {
                hasIssue = true;
                EditorGUILayout.HelpBox("The screen has no layout.", MessageType.Error);
            }

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root == null)
            {
                EditorGUILayout.HelpBox(
                    "Open a scene with a LoogaMenuRoot to validate panel registrations.",
                    MessageType.Info);
                return;
            }

            foreach (LoogaMenuScreenLayout layout in screen.Layouts ?? System.Array.Empty<LoogaMenuScreenLayout>())
            {
                if (layout == null)
                    continue;

                HashSet<LoogaMenuPanelDefinition> panels = new();
                ValidatePanel($"{layout.DisplayName} background",
                    screen.GetBackgroundPanel(root.DefaultBackgroundPanel), panels, ref hasIssue);
                foreach (LoogaMenuScreenPanelEntry entry in layout.Panels)
                {
                    if (entry != null)
                        ValidatePanel(layout.DisplayName, entry.Panel, panels, ref hasIssue);
                }
            }

            if (!hasIssue)
                EditorGUILayout.HelpBox("No obvious screen setup issues found.", MessageType.None);
        }

        private static void ValidatePanel(
            string label,
            LoogaMenuPanelDefinition panel,
            HashSet<LoogaMenuPanelDefinition> panels,
            ref bool hasIssue)
        {
            if (panel == null)
                return;

            if (!panels.Add(panel))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    $"{label} references panel '{panel.name}' more than once.",
                    MessageType.Warning);
            }

            if (!LoogaMenuEditorUtility.TryFindPanel(panel, out _))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox(
                    $"{label} panel '{panel.name}' has no matching scene component.",
                    MessageType.Info);
            }
        }
    }
}
