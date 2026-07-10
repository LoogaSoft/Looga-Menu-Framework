using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomPropertyDrawer(typeof(LoogaMenuScreenContentReference))]
    public sealed class LoogaMenuScreenContentReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty screen = property.FindPropertyRelative("_screen");
            SerializedProperty contentEntryId = property.FindPropertyRelative("_contentEntryId");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect screenRect = new(position.x, position.y, position.width, lineHeight);
            Rect entryRect = new(position.x, screenRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width, lineHeight);

            EditorGUI.PropertyField(screenRect, screen, new GUIContent(
                "Owning Screen",
                "Screen definition that contains the content entry below."));
            LoogaMenuContentEntryPopupUtility.Draw(entryRect,
                screen.objectReferenceValue as LoogaMenuScreenDefinition,
                contentEntryId);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
