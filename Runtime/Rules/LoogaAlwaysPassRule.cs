namespace LoogaSoft.Menu
{
    [UnityEngine.CreateAssetMenu(fileName = "Always Pass Rule", menuName = "LoogaSoft/Menu/Rules/Always Pass")]
    public sealed class LoogaAlwaysPassRule : LoogaMenuRule
    {
        public override bool CanOpen(LoogaMenuOpenContext context, ILoogaStateRegistry states)
        {
            return true;
        }
    }
}
