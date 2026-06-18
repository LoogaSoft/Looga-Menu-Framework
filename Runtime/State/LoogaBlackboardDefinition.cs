using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    [CreateAssetMenu(fileName = "New Menu Blackboard", menuName = "LoogaSoft/Menu/Blackboard/Definition")]
    public sealed class LoogaBlackboardDefinition : ScriptableObject
    {
        [SerializeField] private LoogaBlackboardKey[] _keys = Array.Empty<LoogaBlackboardKey>();

        public LoogaBlackboardKey[] Keys => _keys;
    }
}
