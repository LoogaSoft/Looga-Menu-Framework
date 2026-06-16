using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [Serializable]
    public sealed class LoogaMenuScreenPanelEntry
    {
        [SerializeField] private LoogaMenuPanelDefinition _panel;
        [SerializeField] private LoogaMenuOpenMode _openMode = LoogaMenuOpenMode.KeepPreviousVisible;
        [SerializeField] private LoogaMenuPanelMode _panelMode;
        [SerializeField] private bool _required = true;

        public LoogaMenuPanelDefinition Panel => _panel;
        public LoogaMenuOpenMode OpenMode => _openMode;
        public LoogaMenuPanelMode PanelMode => _panelMode;
        public bool Required => _required;
    }
}
