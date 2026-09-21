using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     Holds class perks at level 3 by blanking NextPerkId on the mercenary's own Perk instances,
    ///     and restores it once the class project unlocks that perk's slot.
    ///     Every level-up path already treats an empty NextPerkId as "maxed out" - RaisePerkAction skips
    ///     the perk before it can even earn experience, AddExpToMultiplePerks stops chaining overflow,
    ///     LevelUpPerk returns early - so borrowing the game's own vocabulary for the cap costs one
    ///     assignment instead of a guard on each caller. It is also the only safe place to intervene:
    ///     suppressing the level-up itself while NextPerkId still names the legend record leaves
    ///     AddExpToMultiplePerks queueing overflow experience under an id no perk in the list carries,
    ///     and its while loop never drains.
    ///     Perk.NextPerkId is [Save]d, so the cap survives a save/load and travels with the instances a
    ///     clone inherits; the syncs exist to catch the moments the value should change.
    ///     With Vanilla Perk Swaps on nothing is capped, so a sync also releases the caps a campaign
    ///     started before the option existed still carries.
    /// </summary>
    internal static class PerkCeiling
    {
        internal static void SyncAll(Mercenaries mercenaries)
        {
            if (mercenaries == null)
                return;

            foreach (var mercenary in mercenaries.Values)
                Sync(mercenary);
        }

        internal static void Sync(Mercenary mercenary)
        {
            if (mercenary?.CreatureData?.Perks == null)
                return;

            foreach (var perk in mercenary.CreatureData.Perks)
            {
                if (!IsCappable(perk))
                    continue;

                string tag;
                if (!PerkGrade.TryGetTag(perk.PerkId, out tag))
                    continue;

                var record = Data.Perks.GetRecord(perk.PerkId);
                if (record == null)
                    continue;

                perk.NextPerkId = VanillaPerkSwaps.IsOn || LegendUnlock.IsUnlocked(mercenary.MercClassId, tag)
                    ? record.NextPerkId
                    : string.Empty;
            }
        }

        /// <summary>
        ///     Only graded class perks are capped. Talents, ranks, ultimates and active implants sit in
        ///     the same list but progress on their own tracks - ranks in particular are already gated by
        ///     the Battle Experience department.
        /// </summary>
        private static bool IsCappable(Perk perk)
        {
            if (perk == null || !PerkGrade.IsMaster(perk.PerkId))
                return false;

            switch (perk.PerkType)
            {
                case PerkType.Talent:
                case PerkType.Rank:
                case PerkType.Ultimate:
                case PerkType.ActiveImplant:
                    return false;
                default:
                    return true;
            }
        }
    }
}
