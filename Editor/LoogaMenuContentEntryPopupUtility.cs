using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuContentEntryPopupUtility
    {
        public static void Draw(Rect rect, LoogaMenuScreenDefinition screen, SerializedProperty contentEntryId)
        {
            if (screen == null)
            {
                EditorGUI.LabelField(rect, "Content Entry", "Assign a screen first.");
                return;
            }

            LoogaMenuScreenContentEntry[] entries = screen.ContentEntries;
            if (entries == null || entries.Length == 0)
            {
                EditorGUI.LabelField(rect, "Content Entry", "No content entries.");
                return;
            }

            string[] labels = new string[entries.Length];
            int selectedIndex = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                labels[i] = GetContentEntryLabel(entries[i], i);
                if (entries[i] != null && entries[i].StableId == contentEntryId.stringValue)
                {
                    selectedIndex = i;
                }
            }

            int newIndex = EditorGUI.Popup(rect, "Content Entry", selectedIndex, labels);
            if (newIndex >= 0 && newIndex < entries.Length && entries[newIndex] != null)
            {
                contentEntryId.stringValue = entries[newIndex].StableId;
            }
        }

        private static string GetContentEntryLabel(LoogaMenuScreenContentEntry entry, int index)
        {
            if (entry == null)
                return $"Entry {index}";

            if (!string.IsNullOrWhiteSpace(entry.DisplayName))
                return entry.DisplayName;

            if (entry.TargetType == LoogaMenuContentTargetType.Screen)
            {
                return entry.Screen != null ? entry.Screen.DisplayName : $"Entry {index} (Missing Screen)";
            }

            return entry.Panel != null ? entry.Panel.DisplayName : $"Entry {index} (Missing Panel)";
        }
    }
}
