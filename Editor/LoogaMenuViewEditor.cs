using LoogaSoft.Menu;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuView))]
    public sealed class LoogaMenuViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu View",
                "Assign the panel definition this scene object represents. Screens enable this view through that asset reference.");

            DrawDefaultInspector();
        }
    }
}
