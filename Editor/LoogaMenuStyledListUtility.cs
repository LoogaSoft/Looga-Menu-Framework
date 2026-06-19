using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuStyledListUtility
    {
        private const float HeaderSizeWidth = 50f;
        private const float HeaderTogglePadding = 4f;
        private const float ElementLeftPadding = 10f;
        private const float ElementTopPadding = 2f;
        private const float ElementHighlightLeftExpansion = 20f;
        private const float ElementHighlightRightExpansion = 6f;

        private static readonly Dictionary<string, ReorderableList> SharedListCache = new();

        public static void Draw(Rect position, SerializedProperty property, Dictionary<string, ReorderableList> cache)
        {
            if (property == null)
                return;

            Event current = Event.current;
            Rect headerRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect sizeRect = new(headerRect.xMax - HeaderSizeWidth, headerRect.y, HeaderSizeWidth, headerRect.height);
            Rect toggleRect = new(
                headerRect.x - HeaderTogglePadding,
                headerRect.y,
                headerRect.width - HeaderSizeWidth + HeaderTogglePadding * 2f,
                headerRect.height);

            EditorGUIUtility.AddCursorRect(toggleRect, MouseCursor.Arrow);
            DrawHeaderHover(toggleRect, current);

            bool expanded = property.isExpanded;
            if (current.type == EventType.MouseDown && current.button == 0 && toggleRect.Contains(current.mousePosition))
            {
                expanded = !expanded;
                property.isExpanded = expanded;
                current.Use();
            }

            GUIContent label = new(property.displayName);
            Vector2 labelSize = EditorStyles.label.CalcSize(label);
            Rect labelRect = toggleRect;
            labelRect.xMin += HeaderTogglePadding;
            Rect arrowRect = new(headerRect.x + labelSize.x + 15f, headerRect.y, 20f, headerRect.height);

            EditorGUI.LabelField(labelRect, label, EditorStyles.label);
            property.isExpanded = EditorGUI.Foldout(arrowRect, expanded, GUIContent.none, true);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.DelayedIntField(sizeRect, property.arraySize);
            if (EditorGUI.EndChangeCheck())
                property.arraySize = Mathf.Max(0, newSize);

            if (!property.isExpanded)
                return;

            ReorderableList list = GetList(property, cache);
            Rect listRect = new(
                position.x,
                headerRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                list.GetHeight());
            list.DoList(listRect);
        }

        public static float GetHeight(SerializedProperty property)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property == null || !property.isExpanded)
                return height;

            ReorderableList list = GetList(property, null);
            return height + EditorGUIUtility.standardVerticalSpacing + list.GetHeight();
        }

        private static ReorderableList GetList(SerializedProperty property, Dictionary<string, ReorderableList> cache)
        {
            Dictionary<string, ReorderableList> targetCache = cache ?? SharedListCache;
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";

            if (targetCache.TryGetValue(key, out ReorderableList list))
                return list;

            list = new ReorderableList(property.serializedObject, property, true, false, true, true)
            {
                headerHeight = 0f,
                drawElementCallback = (rect, index, isActive, _) => DrawElement(rect, property, index, isActive),
                elementHeightCallback = index => GetElementHeight(property, index)
            };
            targetCache[key] = list;
            return list;
        }

        private static void DrawElement(Rect rect, SerializedProperty property, int index, bool isActive)
        {
            if (index < 0 || index >= property.arraySize)
                return;

            int cachedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect highlightRect = rect;
            highlightRect.xMin -= ElementHighlightLeftExpansion;
            highlightRect.xMax += ElementHighlightRightExpansion;

            Event current = Event.current;
            if (current.type == EventType.Repaint && !isActive && highlightRect.Contains(current.mousePosition))
            {
                EditorGUI.DrawRect(highlightRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            }

            rect.y += ElementTopPadding;
            rect.x += ElementLeftPadding;
            rect.width -= ElementLeftPadding;
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element, new GUIContent(element.displayName), true);

            EditorGUI.indentLevel = cachedIndent;
        }

        private static float GetElementHeight(SerializedProperty property, int index)
        {
            if (index < 0 || index >= property.arraySize)
                return EditorGUIUtility.singleLineHeight;

            SerializedProperty element = property.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + ElementTopPadding;
        }

        private static void DrawHeaderHover(Rect toggleRect, Event current)
        {
            if (current.type != EventType.Repaint || !toggleRect.Contains(current.mousePosition))
                return;

            EditorGUI.DrawRect(toggleRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
        }
    }
}
