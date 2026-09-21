using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     The "Vanilla Perk Swaps" difficulty option, choosing between the base game's class perks and
    ///     the rework in this folder. It takes over the tutorial option rather than adding its own, because
    ///     the difficulty screen reads and writes every option as a property of DifficultyPreset, which a mod
    ///     cannot extend. The tutorial option already has everything this one needs: it is [Save]d with the
    ///     session, locked once a campaign has started, kept across the in-game preset switch, and its
    ///     presets default to on for Easy and Normal and off for Hard. The tutorial itself is always skipped,
    ///     see SpacemodeTutorial1_launch_AskForTutorialMoment_NeverTutorial.
    /// </summary>
    internal static class VanillaPerkSwaps
    {
        private const string Label = "Vanilla Perk Swaps";

        /// <summary>Off before a campaign has been started or loaded, when there is no Difficulty to read.</summary>
        internal static bool IsOn => GameState.Get<Difficulty>()?.Preset?.Tutorial ?? false;

        internal static void Register()
        {
            foreach (var language in Singleton<Localization>.Instance.db.Values)
                language["ui.difficulty.Tutorial"] = Label;
        }
    }
}
