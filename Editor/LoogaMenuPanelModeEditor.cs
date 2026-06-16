using LoogaSoft.Menu;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuPanelMode), true)]
    public sealed class LoogaMenuPanelModeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LoogaMenuEditorUtility.DrawDefinitionHeader("Panel Mode",
                "A panel mode adjusts how a reusable menu panel behaves inside a specific screen.");

            LoogaMenuEditorUtility.DrawDisplayName(serializedObject);
            DrawPropertiesExcluding(serializedObject, "m_Script", "_useCustomDisplayName", "_displayName");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
