using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    // Vanilla save version is read from data files and stored in GlobalSettings. This intercepts reading it from there.
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