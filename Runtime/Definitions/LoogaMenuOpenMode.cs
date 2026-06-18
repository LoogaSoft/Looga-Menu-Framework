namespace LoogaSoft.Menu
{
    public enum LoogaMenuOpenMode
    {
        Replace = 0,
        AddAlongside = 1,
        Overlay = 2
    }

    public enum LoogaMenuContentTargetType
    {
        Panel = 0,
        Screen = 1
    }

    public enum LoogaMenuContentBackBehavior
    {
        CloseThisEntry = 0,
        ReturnToParent = 1,
        CloseParent = 2,
        CloseWholeFlow = 3
    }

    public enum LoogaMenuMissingPanelBehavior
    {
        Ignore = 0,
        Warn = 1,
        BlockOpen = 2
    }

}
