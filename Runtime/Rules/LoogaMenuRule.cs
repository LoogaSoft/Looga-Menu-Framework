using UnityEngine;

namespace LoogaSoft.Menu
{
    public abstract class LoogaMenuRule : ScriptableObject
    {
        [SerializeField] private string _failureReason;

        public string FailureReason => string.IsNullOrWhiteSpace(_failureReason)
            ? $"{name} failed."
            : _failureReason;

        public abstract bool CanOpen(LoogaMenuOpenContext context, ILoogaStateRegistry states);
    }
}