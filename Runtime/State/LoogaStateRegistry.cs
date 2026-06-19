using LoogaSoft.Blackboard;

namespace LoogaSoft.Menu
{
    /// <summary>
    /// Menu-owned runtime blackboard. Kept for API compatibility while rules depend on blackboard interfaces.
    /// </summary>
    public sealed class LoogaStateRegistry : ILoogaStateRegistry, ILoogaBlackboardWriter
    {
        private readonly LoogaBlackboard _blackboard = new();

        public LoogaStateRegistry()
        {
            LoogaBlackboardRegistry.SetActive(_blackboard);
        }

        public void SetValue(LoogaBlackboardKey key, LoogaBlackboardValue value)
        {
            _blackboard.SetValue(key, value);
        }

        public void SetBool(LoogaBlackboardKey key, bool value)
        {
            SetValue(key, LoogaBlackboardValue.Bool(value));
        }

        public void SetInt(LoogaBlackboardKey key, int value)
        {
            SetValue(key, LoogaBlackboardValue.Int(value));
        }

        public void SetFloat(LoogaBlackboardKey key, float value)
        {
            SetValue(key, LoogaBlackboardValue.Float(value));
        }

        public void SetString(LoogaBlackboardKey key, string value)
        {
            SetValue(key, LoogaBlackboardValue.String(value));
        }

        public bool TryGetValue(LoogaBlackboardKey key, out LoogaBlackboardValue value)
        {
            return _blackboard.TryGetValue(key, out value);
        }

        public void RemoveValue(LoogaBlackboardKey key)
        {
            _blackboard.RemoveValue(key);
        }

        public void Clear()
        {
            _blackboard.Clear();
        }
    }
}
