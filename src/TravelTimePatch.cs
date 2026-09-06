using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    /// <summary>
    ///     Scales spaceship travel duration. Travel hours are distance times a single global factor, so scaling that
    ///     factor covers the starmap estimate, the tooltip and the actual flight alike.
    /// </summary>
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.DistanceToHours), MethodType.Getter)]
    internal static class GlobalSettings_DistanceToHours_Override
    {
        private static void Postfix(ref double __result)
        {
            __result *= Plugin.Config.TravelTimeMultiplier;
        }
    }
}
