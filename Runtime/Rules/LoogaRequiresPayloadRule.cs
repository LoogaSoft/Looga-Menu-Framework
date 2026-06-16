using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "Requires Payload Rule", menuName = "LoogaSoft/Menu/Rules/Requires Payload")]
    public sealed class LoogaRequiresPayloadRule : LoogaMenuRule
    {
        public override bool CanOpen(LoogaMenuOpenContext context, ILoogaStateRegistry states)
        {
            return context != null && context.Payload != null;
        }
    }
}