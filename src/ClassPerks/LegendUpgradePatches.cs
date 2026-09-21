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
    //
    // With Vanilla Perk Swaps on, every patch here steps aside and the project swaps perks as in the
    // base game.

    /// <summary>
    ///     Keeps the legend id out of MercenaryClassRecord.PerkIds. The value still lands in the
    ///     project's AppliedModifications, which is what LegendUnlock reads, but the class record stays
    ///     all basic ids so that nothing which builds perks from it - ApplyClassForMercenary,
    ///     RestoreSpecificPerks, the class preview on SelectClassScreen - can hand out a legend perk.
    ///     With Vanilla Perk Swaps on, swapped perks are written through as in the base game, but a legend
    ///     id is still held back: only the rework ever stores one, and written through it would become the
    ///     class's starting perk. That happens to a campaign started before the option existed, whose
    ///     tutorial setting now reads as the option.
    /// </summary>
    [HarmonyPatch(typeof(MagnumProject), "ApplyValueToRecord")]
    internal static class MagnumProject_ApplyValueToRecord_KeepPerkSlotsBasic
    {
        private static bool Prefix(MagnumProjectParameter projectParameter, string value)
        {
            if (!LegendUnlock.IsPerkParameter(projectParameter.ParameterType))
                return true;

            return VanillaPerkSwaps.IsOn && !PerkGrade.IsLegend(value);
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
            if (VanillaPerkSwaps.IsOn || !LegendUnlock.IsPerkParameter(projectParameter.ParameterType))
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
            if (VanillaPerkSwaps.IsOn)
                return true;

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
            if (VanillaPerkSwaps.IsOn)
                return true;

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
            return VanillaPerkSwaps.IsOn || !LegendUnlock.IsPerkParameter(parameter.ParameterType);
        }
    }
}
