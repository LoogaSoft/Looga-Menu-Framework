using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Rule Set", menuName = "LoogaSoft/Menu/Rule Set")]
    public sealed class LoogaMenuRuleSet : ScriptableObject
    {
        [SerializeField] private LoogaMenuRuleMode _mode = LoogaMenuRuleMode.AllMustPass;
        [SerializeField] private LoogaMenuRule[] _rules = Array.Empty<LoogaMenuRule>();

        public LoogaMenuRuleMode Mode => _mode;
        public LoogaMenuRule[] Rules => _rules;

        public bool CanOpen(LoogaMenuOpenContext context, ILoogaStateRegistry states,
            out LoogaMenuRule failedRule)
        {
            failedRule = null;

            if (_rules == null || _rules.Length == 0)
                return true;

            bool anyPassed = false;

            foreach (LoogaMenuRule rule in _rules)
            {
                if (rule == null)
                    continue;

                bool passed = rule.CanOpen(context, states);
                anyPassed |= passed;

                if (_mode == LoogaMenuRuleMode.AllMustPass && !passed)
                {
                    failedRule = rule;
                    return false;
                }
            }

            if (_mode == LoogaMenuRuleMode.AnyCanPass && !anyPassed)
            {
                failedRule = FirstAssignedRule();
                return false;
            }

            return true;
        }

        private LoogaMenuRule FirstAssignedRule()
        {
            if (_rules == null)
                return null;

            foreach (LoogaMenuRule rule in _rules)
            {
                if (rule != null)
                    return rule;
            }

            return null;
        }
    }
}
