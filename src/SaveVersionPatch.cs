using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    // Data.Global.SaveVersion gates both writing and loading saves (see MGSC.SaveManager).
    // Postfixing the getter bumps the version the mod writes and the version it demands on
    // load, so saves made with this mod won't load in vanilla and vice versa.
    [HarmonyPatch(typeof(GlobalSettings), nameof(GlobalSettings.SaveVersion), MethodType.Getter)]
    public static class SaveVersionPatch
    {
        public const int ModdedSaveVersion = 1050;

        public static void Postfix(ref int __result)
        {
            __result = ModdedSaveVersion;
        }
    }
}