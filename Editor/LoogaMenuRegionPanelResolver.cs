using System;
using System.Collections.Generic;

namespace LoogaSoft.Menu.Editor
{
    /// <summary>Collects region panels for editor previews without creating transient runtime assets.</summary>
    internal static class LoogaMenuRegionPanelResolver
    {
        private static readonly List<LoogaMenuRegionContent> Contents = new();

        public static void Collect(
            LoogaMenuRoot root,
            LoogaMenuScreenDefinition screen,
            LoogaMenuScreenLayout layout,
            LoogaMenuRegionDefinition region,
            List<LoogaMenuPanelDefinition> destination)
        {
            destination.Clear();
            Contents.Clear();
            if (region == null)
                return;

            Add(region.DefaultContent);
            LoogaMenuContextDefinition context = root != null
                ? root.ActiveContext ?? root.DefaultContext
                : null;
            Apply(context?.RegionOverrides, region);
            Apply(screen?.RegionOverrides, region);
            Apply(screen?.ResolveLayout(layout)?.RegionOverrides, region);

            foreach (LoogaMenuRegionContent content in Contents)
                content.CollectPanels(destination);

            Contents.Clear();
        }

        private static void Apply(
            LoogaMenuRegionOverride[] overrides,
            LoogaMenuRegionDefinition region)
        {
            foreach (LoogaMenuRegionOverride regionOverride in overrides
                ?? Array.Empty<LoogaMenuRegionOverride>())
            {
                if (regionOverride == null || regionOverride.Region != region)
                    continue;

                switch (regionOverride.Mode)
                {
                    case LoogaMenuRegionMode.Override:
                        Contents.Clear();
                        Add(regionOverride.Content);
                        break;
                    case LoogaMenuRegionMode.Hide:
                        Contents.Clear();
                        break;
                    case LoogaMenuRegionMode.Add:
                        Add(regionOverride.Content);
                        break;
                }

                return;
            }
        }

        private static void Add(LoogaMenuRegionContent content)
        {
            if (content != null)
                Contents.Add(content);
        }
    }
}
