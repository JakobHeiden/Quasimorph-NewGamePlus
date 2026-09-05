using HarmonyLib;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Decides whether a launch is refused for want of a shuttle. A peaceful station visit costs
    ///     none, and is told apart by having no Mission attached to the prepare screen.
    /// </summary>
    internal static class ShuttleGate
    {
        private const string NoShuttlesMessage = "No shuttle available.";

        internal static bool Blocks(PrepareRaidScreen screen)
        {
            if (screen == null || screen._mission == null || ReturnRegistry.HasShuttleAvailable)
                return false;

            UI.Chain<AlertDialogWindow>().Invoke(v => v.Configure(NoShuttlesMessage)).Show();
            return true;
        }
    }

    /// <summary>
    ///     Blocks the launch button. PrepareRaidScreen is the only route into a raid the player drives
    ///     - station missions, Bramfatura invasions and peaceful visits all funnel through it - so this
    ///     and the confirm dialog below cover every case. Scenario-scripted launches are left alone,
    ///     since blocking a story beat would strand it with no way to retry.
    /// </summary>
    [HarmonyPatch(typeof(PrepareRaidScreen), "StartOperationButtonOnClick")]
    internal static class PrepareRaidScreen_StartOperation_ShuttleGate
    {
        private static bool Prefix(PrepareRaidScreen __instance)
        {
            return !ShuttleGate.Blocks(__instance);
        }
    }

    /// <summary>Blocks the second commit path, the "you packed nothing" confirmation.</summary>
    [HarmonyPatch(typeof(PrepareRaidScreen), "ConfirmStartMissionDialog")]
    internal static class PrepareRaidScreen_ConfirmStart_ShuttleGate
    {
        private static bool Prefix(PrepareRaidScreen __instance, ConfirmDialogWindow.Option obj)
        {
            if (obj != ConfirmDialogWindow.Option.Yes)
                return true;

            return !ShuttleGate.Blocks(__instance);
        }
    }

    /// <summary>
    ///     Forces peaceful stations to open the fast-trade screen instead of sending a clone down.
    ///     Only the station window and the options page read this flag, so the effect is exactly
    ///     "station visits are always trade"; the toggle in options now displays as permanently on.
    ///     Story-mandated physical visits are unaffected - those go through CanVisitStation's separate
    ///     ignoreFastTrade.
    /// </summary>
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.FastTrade), MethodType.Getter)]
    internal static class GameSettings_FastTrade_ForceOn
    {
        private static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }
}
