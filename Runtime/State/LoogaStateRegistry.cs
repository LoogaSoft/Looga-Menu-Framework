using System;
using System.Collections.Generic;
using LoogaSoft.Blackboard;

namespace LoogaSoft.Menu
{
    public sealed class LoogaStateRegistry : ILoogaStateRegistry
    {
        private readonly Dictionary<Type, object> _states = new();
        private readonly LoogaBlackboard _blackboard = new();

        public LoogaStateRegistry()
        {
            LoogaBlackboardRuntimeRegistry.SetActive(_blackboard);
        }

        public void Register<TState>(TState state) where TState : class
        {
            if (state == null)
                return;

            _states[typeof(TState)] = state;
        }

        public void Unregister<TState>(TState state) where TState : class
        {
            if (state == null)
                return;

            Type type = typeof(TState);
            if (_states.TryGetValue(type, out object current) && ReferenceEquals(current, state))
            {
                _states.Remove(type);
            }
        }

        public bool TryGet<TState>(out TState state) where TState : class
        {
            if (_states.TryGetValue(typeof(TState), out object value) && value is TState typedState)
            {
                state = typedState;
                return true;
            }

            state = null;
            return false;
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
            _states.Clear();
            _blackboard.Clear();
        }
    }
}
