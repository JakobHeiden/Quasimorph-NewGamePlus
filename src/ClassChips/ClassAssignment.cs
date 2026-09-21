using MGSC;

namespace NewGamePlus.ClassChips
{
    /// <summary>
    ///     The rules for putting a class on a clone: every class chips can carry is unlocked from the start, a
    ///     class sits on at most one clone, and assigning it costs one chip of that class from the cargo.
    ///     Who holds a class is read off the roster each time rather than stored, so the rule needs no save
    ///     state, and a class the game strips - a death on a LosePerks difficulty clones the mercenary with no
    ///     class, permanent death removes it from the roster - is free again with no bookkeeping.
    /// </summary>
    internal static class ClassAssignment
    {
        internal static void UnlockAll(Mercenaries mercenaries)
        {
            foreach (var classId in ClassChipStock.ChippableClasses())
                if (!mercenaries.UnlockedClasses.Contains(classId))
                    mercenaries.UnlockedClasses.Add(classId);
        }

        internal static Mercenary HolderOf(Mercenaries mercenaries, string classId, Mercenary except)
        {
            foreach (var mercenary in mercenaries.Values)
                if (mercenary != except && mercenary.MercClassId == classId)
                    return mercenary;

            return null;
        }

        internal static bool CanAssign(Mercenaries mercenaries, MagnumCargo magnumCargo, Mercenary mercenary,
            string classId)
        {
            return !string.IsNullOrEmpty(classId) &&
                   classId != mercenary.MercClassId &&
                   HolderOf(mercenaries, classId, mercenary) == null &&
                   ClassChipStock.Count(magnumCargo, classId) > 0;
        }
    }
}
