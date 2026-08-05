using LoogaSoft.Inspector.Editor;
using LoogaSoft.Menu;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuOpenButton))]
    public sealed class LoogaMenuOpenButtonEditor : LoogaEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty script = serializedObject.FindProperty("m_Script");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(script);

            EditorGUILayout.Space(1f);
            DrawHeaderAttributes(target.GetType());

            SerializedProperty targetProperty = DrawLoogaProperty("_target");

            if ((LoogaMenuOpenButtonTarget)targetProperty.enumValueIndex == LoogaMenuOpenButtonTarget.ScreenContentEntry)
            {
                SerializedProperty contentScreen = DrawLoogaProperty("_contentScreen");
                SerializedProperty contentEntryId = serializedObject.FindProperty("_contentEntryId");
                LoogaMenuContentEntryPopupUtility.Draw(EditorGUILayout.GetControlRect(),
                    contentScreen.objectReferenceValue as LoogaMenuScreenDefinition,
                    contentEntryId);
            }
            else
            {
                SerializedProperty screen = DrawLoogaProperty("_screen");
                LoogaMenuScreenConfigurationPopupUtility.Draw(
                    screen.objectReferenceValue as LoogaMenuScreenDefinition,
                    serializedObject.FindProperty("_configuration"));
            }

            SerializedProperty useActiveMenuRoot = DrawLoogaProperty("_useActiveMenuRoot");

            if (!useActiveMenuRoot.boolValue)
                DrawLoogaProperty("_menuRoot");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
