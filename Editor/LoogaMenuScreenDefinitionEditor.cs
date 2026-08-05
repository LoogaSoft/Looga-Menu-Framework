using System.Collections.Generic;
using LoogaSoft.Inspector.Editor;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenDefinition))]
    public sealed class LoogaMenuScreenDefinitionEditor : LoogaEditor
    {
        protected override void DrawBeforeProperties()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Screen",
                "A screen composes reusable panels and evaluates rule assets before it opens.");
        }

        protected override void DrawAfterProperties()
        {
            DrawConfigurations((LoogaMenuScreenDefinition)target);
            DrawValidation((LoogaMenuScreenDefinition)target);
        }

        private void DrawConfigurations(LoogaMenuScreenDefinition screen)
        {
            serializedObject.Update();
            SerializedProperty configurations = serializedObject.FindProperty("_configurations");
            SerializedProperty defaultConfiguration = serializedObject.FindProperty("_defaultConfiguration");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Configurations", EditorStyles.boldLabel);

            if (configurations.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "This screen still uses its legacy panel composition. Migrate it to create an owned default configuration.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_panels"), new GUIContent("Legacy Panels"), true);
            }

            for (int i = 0; i < configurations.arraySize; i++)
            {
                SerializedProperty element = configurations.GetArrayElementAtIndex(i);
                LoogaMenuScreenConfiguration configuration = element.objectReferenceValue as LoogaMenuScreenConfiguration;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(configuration, typeof(LoogaMenuScreenConfiguration), false);

                    bool isDefault = defaultConfiguration.objectReferenceValue == configuration;
                    using (new EditorGUI.DisabledScope(isDefault || configuration == null))
                    {
                        if (GUILayout.Button(isDefault ? "Default" : "Set Default", GUILayout.Width(78f)))
                            defaultConfiguration.objectReferenceValue = configuration;
                    }

                    if (GUILayout.Button("-", GUILayout.Width(22f)))
                    {
                        RemoveConfiguration(configurations, defaultConfiguration, i, configuration);
                        break;
                    }
                }
            }

            if (GUILayout.Button(configurations.arraySize == 0
                    ? "Migrate Current Composition"
                    : "Add Configuration"))
            {
                CreateConfiguration(screen, configurations, defaultConfiguration, configurations.arraySize == 0);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateConfiguration(LoogaMenuScreenDefinition screen, SerializedProperty configurations,
            SerializedProperty defaultConfiguration, bool copyLegacyPanels)
        {
            string assetPath = AssetDatabase.GetAssetPath(screen);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("Save Screen First",
                    "Save the screen as an asset before adding configurations.", "OK");
                return;
            }

            LoogaMenuScreenConfiguration configuration = CreateInstance<LoogaMenuScreenConfiguration>();
            configuration.name = ObjectNames.GetUniqueName(
                System.Array.ConvertAll(screen.Configurations, value => value != null ? value.name : string.Empty),
                copyLegacyPanels ? "Default" : "Configuration");
            AssetDatabase.AddObjectToAsset(configuration, screen);
            Undo.RegisterCreatedObjectUndo(configuration, "Create Menu Screen Configuration");

            SerializedObject configurationObject = new(configuration);
            configurationObject.FindProperty("_stableId").stringValue = System.Guid.NewGuid().ToString("N");
            configurationObject.FindProperty("_displayName").stringValue = configuration.name;
            if (copyLegacyPanels)
                configurationObject.CopyFromSerializedProperty(serializedObject.FindProperty("_panels"));
            configurationObject.ApplyModifiedPropertiesWithoutUndo();

            int index = configurations.arraySize;
            configurations.InsertArrayElementAtIndex(index);
            configurations.GetArrayElementAtIndex(index).objectReferenceValue = configuration;
            if (defaultConfiguration.objectReferenceValue == null)
                defaultConfiguration.objectReferenceValue = configuration;

            EditorUtility.SetDirty(screen);
            EditorUtility.SetDirty(configuration);
            AssetDatabase.SaveAssets();
        }

        private static void RemoveConfiguration(SerializedProperty configurations,
            SerializedProperty defaultConfiguration, int index, LoogaMenuScreenConfiguration configuration)
        {
            SerializedProperty element = configurations.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
                element.objectReferenceValue = null;

            configurations.DeleteArrayElementAtIndex(index);
            if (defaultConfiguration.objectReferenceValue == configuration)
                defaultConfiguration.objectReferenceValue = configurations.arraySize > 0
                    ? configurations.GetArrayElementAtIndex(0).objectReferenceValue
                    : null;

            if (configuration != null && AssetDatabase.IsSubAsset(configuration))
                Undo.DestroyObjectImmediate(configuration);
        }

        private static void DrawValidation(LoogaMenuScreenDefinition screen)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            LoogaMenuRoot root = LoogaMenuEditorUtility.FindMenuRoot();
            if (root == null)
            {
                EditorGUILayout.HelpBox(
                    "Scene panel validation is unavailable because no LoogaMenuRoot is loaded. Open the UI scene, or any scene containing a LoogaMenuRoot, to validate panel references against live scene objects.",
                    MessageType.Info);
                return;
            }

            HashSet<LoogaMenuPanelDefinition> panels = new();
            bool hasIssue = false;
            ValidatePanel("Background", screen.GetBackgroundPanel(root.DefaultBackgroundPanel), panels, ref hasIssue);

            LoogaMenuScreenConfiguration configuration = screen.DefaultConfiguration;
            foreach (LoogaMenuScreenPanelEntry entry in screen.GetPanels(configuration))
            {
                if (entry == null)
                    continue;

                ValidatePanel("Panel", entry.Panel, panels, ref hasIssue);
            }

            List<LoogaMenuExtensionDefinition> extensions = new();
            LoogaMenuEditorUtility.ResolveExtensions(root, screen, configuration, extensions);
            foreach (LoogaMenuExtensionDefinition extension in extensions)
            {
                if (extension == null || !extension.Enabled)
                    continue;

                if (extension is LoogaMenuActionBarExtension actionBar)
                {
                    ValidatePanel("Action Bar", actionBar.Panel, panels, ref hasIssue);
                    continue;
                }

                if (extension is not LoogaMenuNavigationExtension navigation)
                    continue;

                foreach (LoogaMenuNavigationEntry navigationEntry in navigation.Entries)
                {
                    if (navigationEntry == null)
                        continue;

                    // Navigation entries are mutually exclusive, so the same reusable panel may
                    // intentionally appear in more than one entry. Only duplicates within the
                    // active composition are invalid.
                    HashSet<LoogaMenuPanelDefinition> navigationPanels = new(panels);
                    foreach (LoogaMenuScreenPanelEntry entry in navigationEntry.Panels)
                    {
                        if (entry == null)
                            continue;

                        ValidatePanel($"Navigation '{navigationEntry.DisplayName}'", entry.Panel,
                            navigationPanels, ref hasIssue);
                    }
                }
            }

            if (!hasIssue)
            {
                EditorGUILayout.HelpBox("No obvious screen setup issues found.", MessageType.None);
            }
        }

        private static void ValidatePanel(string label, LoogaMenuPanelDefinition panel,
            HashSet<LoogaMenuPanelDefinition> panels, ref bool hasIssue)
        {
            if (panel == null)
                return;

            if (!panels.Add(panel))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox($"{label} panel '{panel.name}' is referenced more than once.", MessageType.Warning);
            }

            if (!LoogaMenuEditorUtility.TryFindPanel(panel, out _))
            {
                hasIssue = true;
                EditorGUILayout.HelpBox($"{label} panel '{panel.name}' has no matching LoogaMenuPanel in the open scene.",
                    MessageType.Info);
            }
        }
    }
}
