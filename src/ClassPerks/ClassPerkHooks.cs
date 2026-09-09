using System;
using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     Reapplies the level cap to the whole roster on entering space. Perk.NextPerkId is [Save]d, so
    ///     a save written by this mod already carries the right value and this only has to matter when
    ///     the rule itself has changed under an existing save. SpaceStarted is used because it fires
    ///     after both a new game and a load, with the global components in place - and because it is the
    ///     one hook type nothing else in the mod claims, which the game's hook registration requires.
    /// </summary>
    public static class ClassPerkHooks
    {
        [Hook(ModHookType.SpaceStarted)]
        public static void OnSpaceStarted(IModContext context)
        {
            try
            {
                PerkCeiling.SyncAll(context.State.Get<Mercenaries>());
            }
            catch (Exception ex)
            {
                // The game swallows hook exceptions, so tag ours to make them findable in Player.log.
                Plugin.Logger.LogError("Class perk ceiling sync failed.");
                Plugin.Logger.LogException(ex);
            }
        }
    }
}
