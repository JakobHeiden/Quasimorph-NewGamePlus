using HarmonyLib;
using MGSC;

namespace NewGamePlus.WeaponAndArmorCases
{
    /// <summary>
    ///     The single chokepoint for every disassembly route - the in-raid inventory, the ship cargo
    ///     context menu and the Recycling department all funnel through here - so one prefix covers them
    ///     all. Forcing "guaranteed" on a rolled case makes its contents come out whole rather than each
    ///     surviving Data.Global.SpawnItemOnDisassembleChance, which the Recycling department already
    ///     did for every item; the two routes now agree.
    /// </summary>
    [HarmonyPatch(typeof(ItemInteractionSystem), nameof(ItemInteractionSystem.Disassemble))]
    internal static class ItemInteractionSystem_Disassemble_LootCase
    {
        private static void Prefix(BasePickupItem item, ref bool guaranteed)
        {
            if (LootCases.Reroll(item))
                guaranteed = true;
        }
    }
}
