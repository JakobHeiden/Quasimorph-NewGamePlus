using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     Where a class's legend unlocks are stored and how they are read back.
    ///     A finished MercenaryClass project records each changed perk slot in
    ///     MagnumProject.AppliedModifications as parameterId -> perkId; the rework writes the legend
    ///     grade of the slot's own perk there to mean "this slot may reach level 4". That dictionary is
    ///     [Save]d with the project and replayed on load, so the unlock persists with no state of our
    ///     own - which matters, because a mod type cannot be added to the save's global components
    ///     without failing the load outright.
    ///     The value is deliberately not written through to the class record: MagnumProject
    ///     .ApplyValueToRecord is suppressed for perk slots, leaving MercenaryClassRecord.PerkIds all
    ///     basic ids. Nothing downstream can then instantiate or display a legend perk by accident.
    /// </summary>
    internal static class LegendUnlock
    {
        internal static bool IsPerkParameter(MagnumProjectParameterType parameterType)
        {
            return parameterType >= MagnumProjectParameterType.Perk0 &&
                   parameterType <= MagnumProjectParameterType.Perk5;
        }

        internal static MagnumProject GetClassProject(string mercClassId)
        {
            if (string.IsNullOrEmpty(mercClassId))
                return null;

            var projects = GameState.Get<MagnumProjects>();
            if (projects == null)
                return null;

            var project = projects.Get(mercClassId);
            return project != null && project.ProjectType == MagnumProjectType.MercenaryClass ? project : null;
        }

        internal static bool IsUnlocked(string mercClassId, string perkTag)
        {
            var project = GetClassProject(mercClassId);
            if (project == null)
                return false;

            foreach (var applied in project.AppliedModifications)
                if (PerkGrade.IsLegend(applied.Value) && PerkGrade.SharesTag(applied.Value, perkTag))
                    return true;

            return false;
        }
    }
}
