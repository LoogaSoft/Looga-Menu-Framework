using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Provides the data and panels displayed by one menu region.</summary>
    public abstract class LoogaMenuRegionContent : ScriptableObject
    {
        /// <summary>Adds every panel required by this content.</summary>
        public abstract void CollectPanels(List<LoogaMenuPanelDefinition> panels);
    }

}
