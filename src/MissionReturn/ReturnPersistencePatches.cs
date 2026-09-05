using HarmonyLib;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Writes the sidecar alongside the session file, under the same "" / autosave_ / report_
    ///     prefix. Dying in a dungeon reloads the last autosave, so a single file written at "now"
    ///     would describe returns the rewound game knows nothing about.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGame))]
    internal static class SaveManager_SaveGame_Sidecar
    {
        private static void Postfix(SaveManager __instance, bool isAutoSave, bool isReport)
        {
            var metadata = __instance._state.Get<SavedGameMetadata>();
            if (metadata == null || metadata.Slot == -1)
                return;

            ReturnRegistry.Save(metadata.Slot, isAutoSave, isReport);
        }
    }

    /// <summary>Restores the returns belonging to the slot and prefix the game just loaded.</summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadGame))]
    internal static class SaveManager_LoadGame_Sidecar
    {
        private static void Postfix(int slot, bool isAutoSave, ELoadResult __result)
        {
            if (__result != ELoadResult.Success)
                return;

            ReturnRegistry.Load(slot, isAutoSave);
        }
    }

    /// <summary>
    ///     Drops returns left over from the previous run. ProcessStartGame clears the global components
    ///     before both loading a save and starting a new game, so it is the one point both paths share.
    /// </summary>
    [HarmonyPatch(typeof(ComponentsLayout), nameof(ComponentsLayout.RemoveGlobalComponents))]
    internal static class ComponentsLayout_RemoveGlobalComponents_ClearReturns
    {
        private static void Postfix()
        {
            ReturnRegistry.Clear();
        }
    }

    /// <summary>Deletes a slot's sidecars alongside its saves.</summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RemoveSlotSave))]
    internal static class SaveManager_RemoveSlotSave_Sidecar
    {
        private static void Postfix(int slot)
        {
            ReturnRegistry.RemoveFiles(slot, false);
        }
    }

    /// <summary>Deletes a slot's autosave sidecar alongside its autosaves.</summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RemoveAutoSaves))]
    internal static class SaveManager_RemoveAutoSaves_Sidecar
    {
        private static void Postfix(int slot)
        {
            ReturnRegistry.RemoveFiles(slot, true);
        }
    }
}
