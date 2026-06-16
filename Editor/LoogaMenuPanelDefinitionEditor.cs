using LoogaSoft.Menu;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuPanelDefinition))]
    public sealed class LoogaMenuPanelDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Menu Panel",
                "A panel is one reusable UI piece, such as Stockpile, Loadout, Action Bar, or a shared background.");

            DrawDefaultInspector();
        }
    }
}
