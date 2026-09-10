using System.Collections.Generic;
using HarmonyLib;
using MGSC;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Adds the Empty Box to a disassembled repair kit's yield. Disassemble is the single chokepoint for the
    ///     in-raid inventory, the ship cargo context menu and the Recycling department. The charges are read in
    ///     the prefix because the method spends the disassembled kits' charges before it rolls its yield, and the
    ///     boxes go where the method puts its own yield: into the inventory, or itemsWithoutStorage for the caller
    ///     to drop on the floor or into cargo.
    /// </summary>
    [HarmonyPatch(typeof(ItemInteractionSystem), nameof(ItemInteractionSystem.Disassemble))]
    internal static class ItemInteractionSystem_Disassemble_EmptyBoxFromRepairKit
    {
        private static void Prefix(BasePickupItem item, out EmptyBoxSalvage.KitCharges __state)
        {
            __state = EmptyBoxSalvage.ReadCharges(item);
        }

        private static void Postfix(bool __result, BasePickupItem item, Inventory inventory,
            ref List<BasePickupItem> itemsWithoutStorage, EmptyBoxSalvage.KitCharges __state)
        {
            if (__result && __state != null)
                EmptyBoxSalvage.GiveBack(item, __state, inventory, itemsWithoutStorage);
        }
    }
}
