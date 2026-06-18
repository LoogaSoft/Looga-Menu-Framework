using System;
using UnityEngine;

namespace LoogaSoft.Menu
{
    public enum LoogaMenuInputBlockPreset
    {
        Custom,
        None,
        BlockAllGameplay,
        InventoryMovementOnly,
        CombatAndEquipment
    }

    [Serializable]
    public struct LoogaMenuGameplayInputBlockPolicy
    {
        public bool blockMovement;
        public bool blockLook;
        public bool blockJump;
        public bool blockMantle;
        public bool blockSprint;
        public bool blockCrouch;
        public bool blockSlide;
        public bool blockFire;
        public bool blockAim;
        public bool blockReload;
        public bool blockInteract;
        public bool blockWeaponSwap;
        public bool blockInventoryHotkeys;

        public static LoogaMenuGameplayInputBlockPolicy None => default;

        public static LoogaMenuGameplayInputBlockPolicy AllGameplay => new()
        {
            blockMovement = true,
            blockLook = true,
            blockJump = true,
            blockMantle = true,
            blockSprint = true,
            blockCrouch = true,
            blockSlide = true,
            blockFire = true,
            blockAim = true,
            blockReload = true,
            blockInteract = true,
            blockWeaponSwap = true,
            blockInventoryHotkeys = true
        };

        public static LoogaMenuGameplayInputBlockPolicy InventoryMovementOnly => new()
        {
            blockLook = true,
            blockMantle = true,
            blockSprint = true,
            blockSlide = true,
            blockFire = true,
            blockAim = true,
            blockReload = true,
            blockInteract = true,
            blockWeaponSwap = true,
            blockInventoryHotkeys = true
        };

        public static LoogaMenuGameplayInputBlockPolicy CombatAndEquipment => new()
        {
            blockFire = true,
            blockAim = true,
            blockReload = true,
            blockWeaponSwap = true,
            blockInventoryHotkeys = true
        };

        public bool BlocksAny =>
            blockMovement ||
            blockLook ||
            blockJump ||
            blockMantle ||
            blockSprint ||
            blockCrouch ||
            blockSlide ||
            blockFire ||
            blockAim ||
            blockReload ||
            blockInteract ||
            blockWeaponSwap ||
            blockInventoryHotkeys;
    }

    [CreateAssetMenu(fileName = "New Menu Input Policy", menuName = "LoogaSoft/Menu/Input Policy")]
    public sealed class LoogaMenuInputPolicy : ScriptableObject
    {
        [Header("Cursor")]
        [SerializeField] private bool _showsCursor = true;
        [SerializeField] private CursorLockMode _cursorLockMode = CursorLockMode.None;

        [Header("Gameplay")]
        [SerializeField] private LoogaMenuInputBlockPreset _inputBlockPreset =
            LoogaMenuInputBlockPreset.BlockAllGameplay;
        [SerializeField] private LoogaMenuGameplayInputBlockPolicy _customInputBlockPolicy =
            LoogaMenuGameplayInputBlockPolicy.AllGameplay;
        [SerializeField] private string _debugLabel;

        public bool ShowsCursor => _showsCursor;
        public CursorLockMode CursorLockMode => _cursorLockMode;
        public LoogaMenuInputBlockPreset InputBlockPreset => _inputBlockPreset;
        public LoogaMenuGameplayInputBlockPolicy InputBlockPolicy => _inputBlockPreset switch
        {
            LoogaMenuInputBlockPreset.None => LoogaMenuGameplayInputBlockPolicy.None,
            LoogaMenuInputBlockPreset.BlockAllGameplay => LoogaMenuGameplayInputBlockPolicy.AllGameplay,
            LoogaMenuInputBlockPreset.InventoryMovementOnly => LoogaMenuGameplayInputBlockPolicy.InventoryMovementOnly,
            LoogaMenuInputBlockPreset.CombatAndEquipment => LoogaMenuGameplayInputBlockPolicy.CombatAndEquipment,
            _ => _customInputBlockPolicy
        };
        public bool BlocksGameplayInput => InputBlockPolicy.BlocksAny;
        public string DebugLabel => string.IsNullOrWhiteSpace(_debugLabel) ? name : _debugLabel;
    }
}
