using System;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Per-frame driver for the delay, and the one place the mod's space-mode hooks live - the
    ///     game's hook registration invokes the second and later method for a given ModHookType twice,
    ///     so there is exactly one method per type here. Ticking on SpaceUpdateAfterGameLoop rather
    ///     than the Before variant lands it immediately after MercenarySystem.Update has flipped the
    ///     borrowed Cloning state back to None, leaving no frame in which a returning clone is unlocked.
    /// </summary>
    public static class ReturnHooks
    {
        /// <summary>
        ///     Suppressing the briefing screen happens during SpaceGameMode.Initialization, before
        ///     _initialized is set, so the space HUD cannot be raised until the next frame.
        /// </summary>
        internal static bool ShowSpaceHudNextFrame;

        [Hook(ModHookType.SpaceUpdateAfterGameLoop)]
        public static void OnSpaceUpdate(IModContext context)
        {
            try
            {
                if (ShowSpaceHudNextFrame)
                {
                    ShowSpaceHudNextFrame = false;
                    UI.Chain<SpaceHudScreen>().HideAll().Show();
                }

                ReturnRelease.Tick(context.State);
                ShuttleCounterWidget.Refresh();
            }
            catch (Exception ex)
            {
                // The game swallows hook exceptions, so tag ours to make them findable in Player.log.
                Plugin.Logger.LogError("Mission-return update failed.");
                Plugin.Logger.LogException(ex);
            }
        }

        [Hook(ModHookType.SpaceFinished)]
        public static void OnSpaceFinished(IModContext context)
        {
            ShowSpaceHudNextFrame = false;
            ShuttleCounterWidget.Release();
            TravelGate.Forget();
        }
    }
}
