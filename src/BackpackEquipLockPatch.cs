using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    // Blocks equipping/unequipping the backpack while in a dungeon (ship-only action).
    // "In dungeon" = DungeonGameMode.Instance != null (nulled the instant a raid ends).
    //
    // IsValidItem and BeginDrag are the actual enforcement: every equip/unequip path - drag-and-drop,
    // TakeOrEquip, the context menu's Equip/Unequip/Drop/Disassemble commands - funnels through one of
    // them before touching BackpackSlot. ItemStorage.Remove itself is NOT patched: RegisterItem adds an
    // item to its new storage before removing it from the old one, so vetoing Remove would duplicate the
    // item instead of blocking the move.
    //
    // The context-menu patch at the bottom is UI polish only (hides the menu instead of letting every
    // option silently fail); it's not what enforces the lock.
    internal static class BackpackEquipLockPatch
    {
        internal static bool InDungeon => DungeonGameMode.Instance != null;

        internal static bool IsBackpackSlot(ItemStorage storage)
        {
            return storage != null && storage.Source == ItemStorageSource.BackpackSlot;
        }
    }

    [HarmonyPatch(typeof(ItemStorage), nameof(ItemStorage.IsValidItem))]
    internal static class ItemStorage_IsValidItem_BackpackLock
    {
        private static void Postfix(ItemStorage __instance, ref bool __result)
        {
            if (__result && BackpackEquipLockPatch.IsBackpackSlot(__instance) && BackpackEquipLockPatch.InDungeon)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(DragController), "BeginDrag")]
    internal static class DragController_BeginDrag_BackpackLock
    {
        private static bool Prefix(ItemSlot draggableSlot)
        {
            if (draggableSlot != null && BackpackEquipLockPatch.IsBackpackSlot(draggableSlot.Storage) &&
                BackpackEquipLockPatch.InDungeon)
                return false;
            return true;
        }
    }

    // UI polish only: right-click on the equipped backpack does nothing in a dungeon, instead of opening
    // a menu whose Equip/Unequip/Drop/Disassemble entries would all silently fail via the guards above.
    [HarmonyPatch(typeof(InventoryScreen), "DragControllerShowContextMenuCallback")]
    internal static class InventoryScreen_ShowContextMenu_BackpackLock
    {
        private static bool Prefix(ItemSlot obj)
        {
            if (obj != null && BackpackEquipLockPatch.IsBackpackSlot(obj.Storage) && BackpackEquipLockPatch.InDungeon)
                return false;
            return true;
        }
    }
}
