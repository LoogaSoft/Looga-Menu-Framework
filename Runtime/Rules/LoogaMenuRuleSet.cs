using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Rule Set", menuName = "LoogaSoft/Menu Framework/Rule Set")]
    public sealed class LoogaMenuRuleSet : ScriptableObject
    {
        [SerializeField] private LoogaMenuRuleMode _mode = LoogaMenuRuleMode.AllMustPass;
        [SerializeField] private LoogaBlackboardCondition[] _conditions = Array.Empty<LoogaBlackboardCondition>();

        public LoogaMenuRuleMode Mode => _mode;
        public LoogaBlackboardCondition[] Conditions => _conditions;

        public bool CanOpen(ILoogaStateRegistry states, out string failureReason)
        {
            failureReason = string.Empty;

            if (_conditions == null || _conditions.Length == 0)
                return true;

            bool anyPassed = false;
            LoogaBlackboardCondition firstAssignedCondition = null;
            foreach (LoogaBlackboardCondition condition in _conditions)
            {
                if (condition == null)
                    continue;

                firstAssignedCondition ??= condition;
                bool passed = condition.Evaluate(states);
                anyPassed |= passed;

                if (_mode == LoogaMenuRuleMode.AllMustPass && !passed)
                {
                    failureReason = condition.FailureReason;
                    return false;
                }
            }

            if (_mode == LoogaMenuRuleMode.AnyCanPass && !anyPassed)
            {
                failureReason = firstAssignedCondition != null
                    ? firstAssignedCondition.FailureReason
                    : "No rule condition passed.";
                return false;
            }

            return true;
        }
    }
}
