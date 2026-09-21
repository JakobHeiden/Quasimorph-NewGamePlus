using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MGSC;
using NewGamePlus.ClassChips;

namespace NewGamePlus.StartingRoster
{
    /// <summary>
    ///     Starts a new game with extra clones and extra classes on top of what the difficulty grants, up to
    ///     the same ceiling the game clamps its own counts to: the length of its start lists. Picks continue
    ///     the game's own order - the rest of the start list, or its random pool when the difficulty
    ///     randomises. Runs ahead of MercenarySystem_FillStartMercsAndClasses_StartingChips so the extra
    ///     classes are paid out as chips too; only classes a chip can carry are picked, never one already
    ///     chosen.
    /// </summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.FillStartMercsAndClasses))]
    [HarmonyPriority(Priority.High)]
    internal static class MercenarySystem_FillStartMercsAndClasses_ExtraClonesAndClasses
    {
        private static void Postfix(SpaceTime spaceTime, MagnumProjects magnumProjects,
            MagnumProgression magnumProgression, Difficulty difficulty, Mercenaries mercenaries,
            PerkFactory perkFactory)
        {
            var extraMercenaries = PickExtras(
                difficulty.Preset.RndMercsAtStart
                    ? Data.MercenaryProfiles.Ids.Where(id => !id.EndsWith("_boss") && !id.EndsWith("_custom"))
                    : Data.Global.StartMercenaries,
                difficulty.Preset.RndMercsAtStart, mercenaries.UnlockedMercenaries,
                Plugin.Config.ExtraStartingClones, Data.Global.StartMercenaries.Count);
            foreach (var profileId in extraMercenaries)
            {
                mercenaries.UnlockedMercenaries.Add(profileId);
                MercenarySystem.CloneMercenary(spaceTime, magnumProjects, magnumProgression, mercenaries, profileId,
                    true, difficulty, perkFactory);
            }

            var chippableClasses = new HashSet<string>(ClassChipStock.ChippableClasses());
            var extraClasses = PickExtras(
                (difficulty.Preset.RndClassesAtStart ? (IEnumerable<string>)chippableClasses : Data.Global.StartClasses)
                .Where(chippableClasses.Contains),
                difficulty.Preset.RndClassesAtStart, mercenaries.UnlockedClasses,
                Plugin.Config.ExtraStartingClassChips, Data.Global.StartClasses.Count);
            mercenaries.UnlockedClasses.AddRange(extraClasses);
        }

        private static List<string> PickExtras(IEnumerable<string> pool, bool shuffle,
            ICollection<string> alreadyPicked, int extraCount, int totalCap)
        {
            var candidates = pool.Where(id => !alreadyPicked.Contains(id)).Distinct().ToList();
            if (shuffle)
                candidates.Shuffle();

            return candidates.Take(System.Math.Min(extraCount, totalCap - alreadyPicked.Count)).ToList();
        }
    }
}
