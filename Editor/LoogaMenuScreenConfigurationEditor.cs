using LoogaSoft.Inspector.Editor;
using UnityEditor;

namespace LoogaSoft.Menu.Editor
{
    [CustomEditor(typeof(LoogaMenuScreenConfiguration))]
    public sealed class LoogaMenuScreenConfigurationEditor : LoogaEditor
    {
        protected override void DrawBeforeProperties()
        {
            LoogaMenuEditorUtility.DrawDefinitionHeader("Screen Configuration",
                "A configuration defines one named panel and extension composition within its owning screen.");
        }
    }
}
