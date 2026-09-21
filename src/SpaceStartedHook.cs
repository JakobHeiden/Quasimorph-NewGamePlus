using System;
using MGSC;
using NewGamePlus.ClassChips;
using NewGamePlus.ClassPerks;

namespace NewGamePlus
{
    /// <summary>
    ///     The mod's one SpaceStarted hook, fanning out to each feature that needs it - the game's hook
    ///     registration runs the second and later method for a ModHookType twice. SpaceStarted fires after
    ///     both a new game and a load, with the global components in place, which is where a rule that has
    ///     changed under an existing save gets reapplied.
    /// </summary>
    public static class SpaceStartedHook
    {
        [Hook(ModHookType.SpaceStarted)]
        public static void OnSpaceStarted(IModContext context)
        {
            var mercenaries = context.State.Get<Mercenaries>();
            Run("Class perk ceiling sync", () => PerkCeiling.SyncAll(mercenaries));
            Run("Class unlock", () => ClassAssignment.UnlockAll(mercenaries));
        }

        private static void Run(string step, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // The game swallows hook exceptions, so tag ours to make them findable in Player.log.
                Plugin.Logger.LogError($"{step} failed.");
                Plugin.Logger.LogException(ex);
            }
        }
    }
}
