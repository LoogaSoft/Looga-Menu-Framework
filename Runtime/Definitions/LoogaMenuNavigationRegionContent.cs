using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>Provides destination entries for a navigation presenter.</summary>
    public sealed class LoogaMenuNavigationRegionContent : LoogaMenuRegionContent
    {
        [SerializeField, Min(0)] private int _defaultEntryIndex;
        [SerializeField] private LoogaMenuNavigationEntry[] _entries = Array.Empty<LoogaMenuNavigationEntry>();

        public int DefaultEntryIndex => _entries.Length == 0
            ? -1
            : Mathf.Clamp(_defaultEntryIndex, 0, _entries.Length - 1);
        public IReadOnlyList<LoogaMenuNavigationEntry> Entries => _entries;

        public override void CollectPanels(List<LoogaMenuPanelDefinition> panels)
        {
        }
    }
}
