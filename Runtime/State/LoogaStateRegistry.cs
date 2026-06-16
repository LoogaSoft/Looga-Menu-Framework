using System;
using System.Collections.Generic;

namespace LoogaSoft.Menu
{
    public sealed class LoogaStateRegistry : ILoogaStateRegistry
    {
        private readonly Dictionary<Type, object> _states = new();

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

        public void Clear()
        {
            _states.Clear();
        }
    }
}
