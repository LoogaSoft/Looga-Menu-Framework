using LoogaSoft.Inspector.Editor;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenLayout))]
    public sealed class LoogaMenuScreenLayoutEditor : LoogaEditor
    {
        protected override void DrawBeforeProperties()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Screen Layout",
                "A layout defines one panel composition within its owning screen. Layout changes do not add menu history.");
        }
    }
}
