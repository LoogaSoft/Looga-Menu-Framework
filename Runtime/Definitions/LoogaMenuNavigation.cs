using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuNavigationPlacement
    {
        Primary = 0,
        Secondary = 1
    }

    public interface ILoogaMenuNavigation
    {
        LoogaMenuNavigationPlacement Placement { get; }
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
    }

    [Serializable]
    public sealed class LoogaMenuNavigationLayer
    {
        [SerializeField] private LoogaMenuNavigationPlacement _placement;
        [SerializeField] private bool _visible = true;
        [SerializeField, Min(0)] private int _defaultEntryIndex;
        [SerializeField] private LoogaMenuNavigationEntry[] _entries = Array.Empty<LoogaMenuNavigationEntry>();

        public LoogaMenuNavigationPlacement Placement => _placement;
        public bool Visible => _visible;
        public int DefaultEntryIndex => _entries.Length == 0 ? -1 : Mathf.Clamp(_defaultEntryIndex, 0, _entries.Length - 1);
        public LoogaMenuNavigationEntry[] Entries => _entries;
    }
}
