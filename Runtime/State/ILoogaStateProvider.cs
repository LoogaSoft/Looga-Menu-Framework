namespace LoogaSoft.Menu
{
    public interface ILoogaStateProvider
    {
        void RegisterStates(LoogaStateRegistry registry);
        void UnregisterStates(LoogaStateRegistry registry);
    }
}