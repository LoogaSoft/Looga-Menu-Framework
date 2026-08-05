using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuScreenConfigurationPopupUtility
    {
        public static void Draw(LoogaMenuScreenDefinition screen, SerializedProperty configurationProperty)
        {
            if (configurationProperty == null)
                return;

            if (screen == null || screen.Configurations == null || screen.Configurations.Length == 0)
            {
                configurationProperty.objectReferenceValue = null;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Configuration", null, typeof(LoogaMenuScreenConfiguration), false);
                return;
            }

            string[] labels = new string[screen.Configurations.Length + 1];
            LoogaMenuScreenConfiguration defaultConfiguration = screen.DefaultConfiguration;
            labels[0] = defaultConfiguration != null
                ? $"Default ({defaultConfiguration.DisplayName})"
                : "Default";
            int selectedIndex = 0;

            for (int i = 0; i < screen.Configurations.Length; i++)
            {
                LoogaMenuScreenConfiguration configuration = screen.Configurations[i];
                labels[i + 1] = configuration != null ? configuration.DisplayName : "Missing Configuration";
                if (configurationProperty.objectReferenceValue == configuration)
                    selectedIndex = i + 1;
            }

            int nextIndex = EditorGUILayout.Popup("Configuration", selectedIndex, labels);
            configurationProperty.objectReferenceValue = nextIndex == 0 ? null : screen.Configurations[nextIndex - 1];
        }
    }
}
