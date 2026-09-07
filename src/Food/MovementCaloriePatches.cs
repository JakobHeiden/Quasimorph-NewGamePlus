using HarmonyLib;
using MGSC;

namespace NewGamePlus.Food
{
    // Reprices movement: what one action point costs in calories, and whether carried weight adds to it.
    // Both are read from a single place each and are recomputed by every screen that previews them, so
    // overriding them at the source keeps the health screen, the equipment tooltip and the actual
    // deduction in agreement without patching any of them.

    /// <summary>Calories a sneaking clone spends per action point.</summary>
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.SlowMoveStarvationMult), MethodType.Getter)]
    internal static class GlobalSettings_SlowMoveStarvationMult_Override
    {
        private static void Postfix(ref float __result)
        {
            __result = Plugin.Config.SlowMoveCaloriesPerAction;
        }
    }

    /// <summary>Calories a walking clone spends per action point.</summary>
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.NormalMoveStarvationMult), MethodType.Getter)]
    internal static class GlobalSettings_NormalMoveStarvationMult_Override
    {
        private static void Postfix(ref float __result)
        {
            __result = Plugin.Config.NormalMoveCaloriesPerAction;
        }
    }

    /// <summary>Calories a running clone spends per action point.</summary>
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.RunMoveStarvationMult), MethodType.Getter)]
    internal static class GlobalSettings_RunMoveStarvationMult_Override
    {
        private static void Postfix(ref float __result)
        {
            __result = Plugin.Config.RunMoveCaloriesPerAction;
        }
    }

    /// <summary>
    ///     Drops the satiety drain that carried weight adds to every action point. Only the satiety helper
    ///     is zeroed, not the weight behind it, so weight still costs dodge, throwback and carry capacity.
    /// </summary>
    [HarmonyPatch(typeof(CreatureData), nameof(CreatureData.GetItemsWeightSatietyDrain))]
    internal static class CreatureData_GetItemsWeightSatietyDrain_NoWeightCost
    {
        private static bool Prefix(ref float __result)
        {
            __result = 0f;
            return false;
        }
    }
}
