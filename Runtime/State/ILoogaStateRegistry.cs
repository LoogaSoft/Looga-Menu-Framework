namespace LoogaSoft.Menu
{
    public interface ILoogaStateRegistry
    {
        bool TryGet<TState>(out TState state) where TState : class;
        bool TryGetValue(LoogaBlackboardKey key, out LoogaBlackboardValue value);
    }
}
