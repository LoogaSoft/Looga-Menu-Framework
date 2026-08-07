using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;
using UnityEngine;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Read/write contract consumed by any navigation presenter.
    /// </summary>
    public interface ILoogaMenuNavigationExtension
    {
        string Channel { get; }
        IReadOnlyList<LoogaMenuNavigationEntry> Entries { get; }
        int SelectedIndex { get; }
        bool IsActive { get; }
        bool Select(int index);
        bool SelectRelative(int direction);
        bool SetActive(bool active);
    }

    [CreateAssetMenu(
        fileName = "New Navigation Extension",
        menuName = "LoogaSoft/Menu Framework/Extensions/Navigation")]
    public sealed class LoogaMenuNavigationExtension : LoogaMenuExtensionDefinition
    {
        public const string Id = "LoogaSoft.Menu.Navigation";
        public const string DefaultChannel = "Default";

        [SerializeField, Tooltip("Navigation bars use this channel to select one navigation group from the active screen.")]
        private string _channel = DefaultChannel;
        [SerializeField] private bool _activateOnOpen = true;
        [SerializeField] private LoogaMenuNavigationEntry[] _entries = Array.Empty<LoogaMenuNavigationEntry>();

        public override string ExtensionId => $"{Id}.{ResolveChannel(_channel)}";
        public string Channel => ResolveChannel(_channel);
        public bool ActivateOnOpen => _activateOnOpen;
        public LoogaMenuNavigationEntry[] Entries => _entries;

        public override ILoogaMenuExtensionRuntime CreateRuntime()
        {
            return new LoogaMenuNavigationExtensionRuntime(Channel, _entries, _activateOnOpen);
        }

        private void OnValidate()
        {
            _channel = ResolveChannel(_channel);
            _entries ??= Array.Empty<LoogaMenuNavigationEntry>();
            foreach (LoogaMenuNavigationEntry entry in _entries)
            {
                entry?.EnsureStableId();
            }
        }

        private static string ResolveChannel(string channel)
        {
            return string.IsNullOrWhiteSpace(channel) ? DefaultChannel : channel.Trim();
        }
    }

    internal sealed class LoogaMenuNavigationExtensionRuntime :
        ILoogaMenuExtensionRuntime,
        ILoogaMenuNavigationExtension
    {
        private readonly LoogaMenuNavigationEntry[] _entries;
        private readonly bool _activateOnOpen;
        private LoogaMenuExtensionContext _context;
        private int _selectedIndex;

        public LoogaMenuNavigationExtensionRuntime(
            string channel,
            LoogaMenuNavigationEntry[] entries,
            bool activateOnOpen)
        {
            Channel = string.IsNullOrWhiteSpace(channel)
                ? LoogaMenuNavigationExtension.DefaultChannel
                : channel;
            _entries = entries ?? Array.Empty<LoogaMenuNavigationEntry>();
            _activateOnOpen = activateOnOpen;
        }

        public string Channel { get; }
        public IReadOnlyList<LoogaMenuNavigationEntry> Entries => _entries;
        public int SelectedIndex => _entries.Length > 0 ? _selectedIndex : -1;
        public bool IsActive { get; private set; }

        public void Attach(LoogaMenuExtensionContext context)
        {
            _context = context;
            _selectedIndex = FindInitialIndex(context.Configuration?.InitialNavigationEntryId);
            IsActive = _entries.Length > 0
                && (_activateOnOpen || !string.IsNullOrWhiteSpace(context.Configuration?.InitialNavigationEntryId));
        }

        public void Show()
        {
            if (IsActive)
            {
                ShowEntry(CurrentEntry);
            }
        }

        public bool Select(int index)
        {
            if (!IsActive || index < 0 || index >= _entries.Length)
                return false;

            if (_selectedIndex == index)
                return true;

            LoogaMenuNavigationEntry nextEntry = _entries[index];
            if (nextEntry?.Configuration != null)
                return _context.SetConfiguration(nextEntry.Configuration);

            LoogaMenuNavigationEntry previousEntry = CurrentEntry;
            _selectedIndex = index;
            HideEntry(previousEntry);
            ShowEntry(CurrentEntry);
            _context.Refresh();
            return true;
        }

        public bool SelectRelative(int direction)
        {
            if (!IsActive || direction == 0 || _entries.Length == 0)
                return false;

            int nextIndex = (_selectedIndex + Math.Sign(direction) + _entries.Length) % _entries.Length;
            return Select(nextIndex);
        }

        public bool SetActive(bool active)
        {
            if (_entries.Length == 0)
                return false;

            if (IsActive == active)
                return true;

            IsActive = active;
            if (active)
            {
                ShowEntry(CurrentEntry);
            }
            else
            {
                HideEntry(CurrentEntry);
            }

            _context.Refresh();
            return true;
        }

        public void CollectPanels(List<LoogaMenuPanel> panels)
        {
            if (!IsActive)
                return;

            LoogaMenuNavigationEntry entry = CurrentEntry;
            if (entry == null)
                return;

            foreach (LoogaMenuScreenPanelEntry panelEntry in entry.Panels)
            {
                if (panelEntry != null)
                {
                    _context.AddPanel(panelEntry.Panel, panels);
                }
            }
        }

        public bool UsesPanel(LoogaMenuPanelDefinition panel)
        {
            if (!IsActive || panel == null || CurrentEntry == null)
                return false;

            foreach (LoogaMenuScreenPanelEntry entry in CurrentEntry.Panels)
            {
                if (entry != null && entry.Panel == panel)
                    return true;
            }

            return false;
        }

        public bool UsesParameter(LoogaBlackboardKey key)
        {
            if (!IsActive || key == null || CurrentEntry == null)
                return false;

            foreach (LoogaMenuScreenPanelEntry panelEntry in CurrentEntry.Panels)
            {
                if (panelEntry == null)
                    continue;

                foreach (LoogaMenuBlackboardParameter parameter in panelEntry.Parameters)
                {
                    if (parameter != null && parameter.Key == key)
                        return true;
                }
            }

            return false;
        }

        public void ReapplyParameters()
        {
            if (!IsActive || CurrentEntry == null)
                return;

            foreach (LoogaMenuScreenPanelEntry panelEntry in CurrentEntry.Panels)
            {
                if (panelEntry != null)
                {
                    _context.ApplyParameters(panelEntry.Parameters);
                }
            }
        }

        public void Release()
        {
            if (IsActive)
            {
                RemoveEntryParameters(CurrentEntry);
            }

            IsActive = false;
            _context = null;
        }

        private LoogaMenuNavigationEntry CurrentEntry =>
            _selectedIndex >= 0 && _selectedIndex < _entries.Length
                ? _entries[_selectedIndex]
                : null;

        private int FindInitialIndex(string stableId)
        {
            LoogaMenuScreenConfiguration configuration = _context?.Configuration;
            if (configuration != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i]?.Configuration == configuration)
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(stableId))
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i] != null && _entries[i].StableId == stableId)
                        return i;
                }
            }

            return 0;
        }

        private void ShowEntry(LoogaMenuNavigationEntry entry)
        {
            if (entry == null)
                return;

            foreach (LoogaMenuScreenPanelEntry panelEntry in entry.Panels)
            {
                if (panelEntry == null)
                    continue;

                _context.ApplyParameters(panelEntry.Parameters);
                _context.ShowPanel(panelEntry.Panel);
            }
        }

        private void HideEntry(LoogaMenuNavigationEntry entry)
        {
            if (entry == null)
                return;

            foreach (LoogaMenuScreenPanelEntry panelEntry in entry.Panels)
            {
                if (panelEntry != null)
                {
                    _context.HidePanelWhenUnused(panelEntry.Panel);
                }
            }

            RemoveEntryParameters(entry);
        }

        private void RemoveEntryParameters(LoogaMenuNavigationEntry entry)
        {
            if (entry == null)
                return;

            foreach (LoogaMenuScreenPanelEntry panelEntry in entry.Panels)
            {
                if (panelEntry != null)
                {
                    _context.RemoveParameters(panelEntry.Parameters);
                }
            }
        }
    }
}
