using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public interface ILoogaMenuNavigation
    {
        LoogaMenuRegionDefinition Region { get; }
        IReadOnlyList<LoogaMenuNavigationEntry> Entries { get; }
        int SelectedIndex { get; }
        bool Select(int index);
        bool SelectRelative(int direction);
    }

    [Serializable]
    public sealed class LoogaMenuNavigationEntry
    {
        [SerializeField] private string _displayName;
        [SerializeField] private LoogaMenuDestination _destination = new();
        [SerializeField] private LoogaMenuRuleSet _requirements;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? "Navigation Entry" : _displayName;
        public LoogaMenuDestination Destination => _destination;
        public LoogaMenuRuleSet Requirements => _requirements;

        internal static LoogaMenuNavigationEntry Create(
            string displayName,
            LoogaMenuDestination destination,
            LoogaMenuRuleSet requirements = null)
        {
            return new LoogaMenuNavigationEntry
            {
                _displayName = displayName,
                _destination = destination,
                _requirements = requirements
            };
        }
    }
}
