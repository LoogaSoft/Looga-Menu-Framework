namespace LoogaSoft.Menu
{
    public interface ILoogaStateRegistry
    {
        bool TryGet<TState>(out TState state) where TState : class;
    }
}
