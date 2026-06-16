using LoogaSoft.Menu;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuPanel))]
    public sealed class LoogaMenuPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Panel",
                "Assign the panel definition this scene object represents. Screens enable this panel through that asset reference.");

            DrawDefaultInspector();
        }
    }
}