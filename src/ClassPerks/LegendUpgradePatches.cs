using HarmonyLib;
using MGSC;

namespace NewGamePlus.ClassPerks
{
    // Turns the Memory Defragmentation class project from a perk swap into a perk upgrade. The
    // department, the project, its cost curve, development time and the memdf_perks_count_to_switch /
    // ModifyLevelLimit caps are all left alone - only the meaning of a perk slot changes, from "replace
    // this perk with one from another class" to "let this perk reach level 4".
    //
    // The pricing falls out for free: MagnumProjectPerkParameterPanel.ModificationsCount scores a slot
    // against the *base* class record, so basic -> legend is not in it and already counts as one paid
    // step, and legend -> basic already counts as a refund.

    /// <summary>
    ///     Keeps the legend id out of MercenaryClassRecord.PerkIds. The value still lands in the
    ///     project's AppliedModifications, which is what LegendUnlock reads, but the class record stays
    ///     all basic ids so that nothing which builds perks from it - ApplyClassForMercenary,
    ///     RestoreSpecificPerks, the class preview on SelectClassScreen - can hand out a legend perk.
    /// </summary>
    [HarmonyPatch(typeof(MagnumProject), "ApplyValueToRecord")]
    internal static class MagnumProject_ApplyValueToRecord_KeepPerkSlotsBasic
    {
        private static bool Prefix(MagnumProjectParameter projectParameter)
        {
            return !LegendUnlock.IsPerkParameter(projectParameter.ParameterType);
        }
    }

    /// <summary>
    ///     Since the class record no longer carries the unlock, a slot's current value has to come from
    ///     the applied modifications instead. Without this the project screen would reopen showing an
    ///     already-upgraded slot as basic, and sell the same upgrade twice.
    /// </summary>
    [HarmonyPatch(typeof(MagnumProject), nameof(MagnumProject.GetParameterCurrentValue))]
    internal static class MagnumProject_GetParameterCurrentValue_ReadUnlock
    {
        private static void Postfix(
            MagnumProject __instance,
            MagnumProjectParameter projectParameter,
            ref object __result)
        {
            if (!LegendUnlock.IsPerkParameter(projectParameter.ParameterType))
                return;

            string applied;
            if (__instance.AppliedModifications.TryGetValue(projectParameter.Id, out applied))
                __result = applied;
        }
    }

    /// <summary>
    ///     Makes the perk slot a two-state toggle instead of opening the perk picker. With only one
    ///     candidate - the legend grade of the perk already in the slot - a modal list would be a window
    ///     with a single entry, while the panel itself already draws current -> next with an arrow
    ///     between them, and hovering either icon gives the full grade comparison for free.
    ///     Routed through AbilitySelected rather than assigning the field so the ModifyLevelLimit guard
    ///     inside it still runs.
    /// </summary>
    [HarmonyPatch(typeof(MagnumProjectPerkParameterPanel), "PerkButtonOnClicked")]
    internal static class MagnumProjectPerkParameterPanel_PerkButtonOnClicked_ToggleUpgrade
    {
        private static bool Prefix(MagnumProjectPerkParameterPanel __instance)
        {
            var target = ToggleTarget(__instance);
            if (target != null)
                __instance.AbilitySelected(target);

            return false;
        }

        /// <summary>
        ///     An upgraded slot toggles back to whatever the base class record holds, which is the exact
        ///     id the upgrade was bought against; anything else toggles up to its legend grade, or to
        ///     nothing at all for the one perk that has no legend record.
        /// </summary>
        private static string ToggleTarget(MagnumProjectPerkParameterPanel panel)
        {
            if (PerkGrade.IsLegend(panel.OriginalAbility))
                return panel._project.GetParameterDefaultValue(panel._projectParameter) as string;

            return PerkGrade.ToLegendId(panel.OriginalAbility);
        }
    }

    /// <summary>Clicking the pending grade cancels back to the slot's applied state.</summary>
    [HarmonyPatch(typeof(MagnumProjectPerkParameterPanel), "NextPerkButtonOnClicked")]
    internal static class MagnumProjectPerkParameterPanel_NextPerkButtonOnClicked_Revert
    {
        private static bool Prefix(MagnumProjectPerkParameterPanel __instance)
        {
            __instance.AbilitySelected(__instance.OriginalAbility);
            return false;
        }
    }

    /// <summary>
    ///     The perk picker is unreachable through the toggle above, but it is still wired to the panel
    ///     and to gamepad navigation. Stubbing its perk branch keeps a stray path from offering the
    ///     other classes' perks the rework removed; the talent and weapon-trait branches are untouched.
    /// </summary>
    [HarmonyPatch(typeof(MagnumProjectSelectAbilityWindow), nameof(MagnumProjectSelectAbilityWindow.Show))]
    internal static class MagnumProjectSelectAbilityWindow_Show_NoPerkSwap
    {
        private static bool Prefix(MagnumProjectParameter parameter)
        {
            return !LegendUnlock.IsPerkParameter(parameter.ParameterType);
        }
    }
}
