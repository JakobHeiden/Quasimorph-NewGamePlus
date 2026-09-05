using HarmonyLib;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Removes the 24h that pass automatically while on mission.
    /// </summary>
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.MissionAddTimeHours), MethodType.Getter)]
    internal static class GlobalSettings_MissionAddTimeHours_Override
    {
        private static void Postfix(ref float __result)
        {
            __result = 0;
        }
    }
}
