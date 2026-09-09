using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     Reads and rewrites the grade suffix of a perk id. A perk's four levels are four separate
    ///     PerkRecords chained by NextPerkId, distinguished only by an "_basic"/"_advanced"/"_master"/
    ///     "_legend" suffix, so deriving one grade from another is string work.
    ///     The game's own ParseHelper.GetGradeByPerkId and FormatHelper.ClearPerkGrades match on
    ///     IndexOf/Replace anywhere in the id; these match on the suffix, which is where a grade
    ///     actually sits, and so cannot mistake a tag that merely contains a grade word.
    /// </summary>
    internal static class PerkGrade
    {
        internal const string MasterSuffix = "_master";
        internal const string LegendSuffix = "_legend";

        private static readonly string[] Suffixes = { "_basic", "_advanced", MasterSuffix, LegendSuffix };

        internal static bool TryGetTag(string perkId, out string tag)
        {
            if (!string.IsNullOrEmpty(perkId))
                foreach (var suffix in Suffixes)
                    if (perkId.EndsWith(suffix))
                    {
                        tag = perkId.Substring(0, perkId.Length - suffix.Length);
                        return true;
                    }

            tag = null;
            return false;
        }

        internal static bool IsMaster(string perkId)
        {
            return !string.IsNullOrEmpty(perkId) && perkId.EndsWith(MasterSuffix);
        }

        internal static bool IsLegend(string perkId)
        {
            return !string.IsNullOrEmpty(perkId) && perkId.EndsWith(LegendSuffix);
        }

        /// <summary>Null when the perk has no legend record, as elemental_resist does not.</summary>
        internal static string ToLegendId(string perkId)
        {
            string tag;
            if (!TryGetTag(perkId, out tag))
                return null;

            var legendId = tag + LegendSuffix;
            return Data.Perks.GetRecord(legendId) == null ? null : legendId;
        }

        internal static bool SharesTag(string perkId, string tag)
        {
            string perkTag;
            return TryGetTag(perkId, out perkTag) && perkTag == tag;
        }
    }
}
