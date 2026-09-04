using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    [HarmonyPatch(typeof(MainMenuScreen), nameof(MainMenuScreen.Awake))]
    public static class ExamplePatch
    {
        public static void Prefix(MainMenuScreen __instance)
        {
            Plugin.Logger.Log("--- main menu awake");
        }
    }
}