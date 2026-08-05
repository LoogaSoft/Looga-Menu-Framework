using System.Collections.Generic;
using System;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuEditorUtility
    {
        public static LoogaMenuRoot FindMenuRoot()
        {
            foreach (LoogaMenuRoot root in Resources.FindObjectsOfTypeAll<LoogaMenuRoot>())
            {
                if (root != null && root.gameObject.scene.IsValid())
                    return root;
            }

            return null;
        }

        public static LoogaMenuPanel[] FindScenePanels()
        {
            List<LoogaMenuPanel> panels = new();
            foreach (LoogaMenuPanel panel in Resources.FindObjectsOfTypeAll<LoogaMenuPanel>())
            {
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    panels.Add(panel);
                }
            }

            return panels.ToArray();
        }

        public static bool TryFindPanel(LoogaMenuPanelDefinition definition, out LoogaMenuPanel panel)
        {
            foreach (LoogaMenuPanel candidate in FindScenePanels())
            {
                if (candidate.Panel == definition)
                {
                    panel = candidate;
                    return true;
                }
            }

            panel = null;
            return false;
        }

        /// <summary>
        /// Resolves the effective extension set using the same root-default and
        /// screen-override rules as the runtime menu manager.
        /// </summary>
        public static void ResolveExtensions(LoogaMenuRoot root, LoogaMenuScreenDefinition screen,
            List<LoogaMenuExtensionDefinition> destination)
        {
            ResolveExtensions(root, screen, screen?.DefaultConfiguration, destination);
        }

        public static void ResolveExtensions(LoogaMenuRoot root, LoogaMenuScreenDefinition screen,
            LoogaMenuScreenConfiguration configuration, List<LoogaMenuExtensionDefinition> destination)
        {
            destination.Clear();

            Dictionary<string, int> indicesById = new(StringComparer.Ordinal);
            AddOrReplaceExtensions(root != null ? root.DefaultExtensions : null, destination, indicesById);
            AddOrReplaceExtensions(screen != null ? screen.Extensions : null, destination, indicesById);
            AddOrReplaceExtensions(configuration != null ? configuration.Extensions : null, destination, indicesById);
        }

        public static void DrawDefinitionHeader(string title, string helpText)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(helpText))
            {
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
            }
        }

        public static void DrawDisplayName(SerializedObject serializedObject)
        {
            SerializedProperty useCustomDisplayName = serializedObject.FindProperty("_useCustomDisplayName");
            SerializedProperty displayName = serializedObject.FindProperty("_displayName");

            if (useCustomDisplayName == null || displayName == null)
                return;

            if (!useCustomDisplayName.boolValue)
            {
                displayName.stringValue = serializedObject.targetObject.name;
            }

            EditorGUILayout.PropertyField(useCustomDisplayName);

            using (new EditorGUI.DisabledScope(!useCustomDisplayName.boolValue))
            {
                EditorGUILayout.PropertyField(displayName);
            }
        }

        private static void AddOrReplaceExtensions(IEnumerable<LoogaMenuExtensionDefinition> definitions,
            List<LoogaMenuExtensionDefinition> destination, Dictionary<string, int> indicesById)
        {
            if (definitions == null)
                return;

            foreach (LoogaMenuExtensionDefinition definition in definitions)
            {
                if (definition == null)
                    continue;

                string id = string.IsNullOrWhiteSpace(definition.ExtensionId)
                    ? definition.GetType().FullName
                    : definition.ExtensionId;

                if (indicesById.TryGetValue(id, out int index))
                {
                    destination[index] = definition;
                    continue;
                }

                indicesById.Add(id, destination.Count);
                destination.Add(definition);
            }
        }
    }
}

