namespace LoogaSoft.Menu
{
    public interface ILoogaMenuTransitionHandler
    {
        void PlayOpen(LoogaMenuScreenDefinition screen, LoogaMenuView[] views);
        void PlayClose(LoogaMenuScreenDefinition screen, LoogaMenuView[] views);
    }
}
