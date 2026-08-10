using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Displays one or more authored menu panels in a region.</summary>
    public sealed class LoogaMenuPanelRegionContent : LoogaMenuRegionContent
    {
        [SerializeField] private LoogaMenuPanelDefinition[] _panels = Array.Empty<LoogaMenuPanelDefinition>();

        public IReadOnlyList<LoogaMenuPanelDefinition> Panels => _panels;

        public override void CollectPanels(List<LoogaMenuPanelDefinition> panels)
        {
            if (panels == null)
                return;

            foreach (LoogaMenuPanelDefinition panel in _panels ?? Array.Empty<LoogaMenuPanelDefinition>())
            {
                if (panel != null && !panels.Contains(panel))
                    panels.Add(panel);
            }
        }
    }
}
