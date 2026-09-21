using HarmonyLib;
using MGSC;

namespace NewGamePlus.ClassChips
{
    // Turns class chips from one-time unlocks into the price of an assignment, and makes a class exclusive
    // to one clone. The gate that refuses an assignment sits on SelectClassScreen, the only place a player
    // assigns a class; the chip changes hands in ApplyClassForMercenary, which every assignment goes through.

    /// <summary>
    ///     Pays for an assignment with a chip and refunds the class it replaces. Booked here rather than on the
    ///     screen so the tutorial's scripted assignment is charged like any other. Cloning puts a class back
    ///     through Mercenary.SetMercClass directly and never passes through here, so a clone keeps its class
    ///     for free.
    /// </summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.ApplyClassForMercenary))]
    internal static class MercenarySystem_ApplyClassForMercenary_TradeChips
    {
        private static void Prefix(Mercenary mercenary, string classId, out string __state)
        {
            __state = mercenary.MercClassId;
            if (classId == mercenary.MercClassId)
                return;

            var magnumCargo = GameState.Get<MagnumCargo>();
            if (magnumCargo != null && !ClassChipStock.TryTake(magnumCargo, classId))
                Plugin.Logger.LogError($"Class '{classId}' assigned with no chip in the cargo to pay for it.");
        }

        private static void Postfix(string classId, string __state)
        {
            if (string.IsNullOrEmpty(__state) || __state == classId)
                return;

            var magnumCargo = GameState.Get<MagnumCargo>();
            var spaceTime = GameState.Get<SpaceTime>();
            if (magnumCargo != null && spaceTime != null)
                ClassChipStock.Give(magnumCargo, spaceTime, __state);
        }
    }

    /// <summary>
    ///     Hands out a new game's starting classes as one chip each. The difficulty's class count and its
    ///     random-classes option still pick which classes those are. MagnumCargo is not among the parameters
    ///     but is already in place: GenerateStartingItems runs right before.
    /// </summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.FillStartMercsAndClasses))]
    internal static class MercenarySystem_FillStartMercsAndClasses_StartingChips
    {
        private static void Postfix(SpaceTime spaceTime, Mercenaries mercenaries)
        {
            var magnumCargo = GameState.Get<MagnumCargo>();
            foreach (var classId in mercenaries.UnlockedClasses)
                ClassChipStock.Give(magnumCargo, spaceTime, classId);
        }
    }

    /// <summary>
    ///     Suffixes each class with what decides whether this clone can take it: the clone that already holds
    ///     it, or how many chips the cargo has for it. MercenaryClassPanel.Initialize rewrites the caption on
    ///     every OnEnable, so a pooled panel never carries a stale suffix.
    /// </summary>
    [HarmonyPatch(typeof(SelectClassScreen), "OnEnable")]
    internal static class SelectClassScreen_OnEnable_LabelAvailability
    {
        private static void Postfix(SelectClassScreen __instance)
        {
            var magnumCargo = GameState.Get<MagnumCargo>();
            foreach (var panel in __instance._panels)
            {
                var classId = panel._mercClassId;
                if (classId == __instance._merc.MercClassId)
                    continue;

                var holder = ClassAssignment.HolderOf(__instance._mercenaries, classId, __instance._merc);
                panel._mercClassCaption.text += holder != null
                    ? $" ({Localization.Get($"spec.{holder.ProfileId}.name")})"
                    : $" x{ClassChipStock.Count(magnumCargo, classId)}";
            }
        }
    }

    /// <summary>Keeps the select button dead for a class this clone cannot take, while still previewing its perks.</summary>
    [HarmonyPatch(typeof(SelectClassScreen), "PanelOnSelectClass")]
    internal static class SelectClassScreen_PanelOnSelectClass_DisableUnavailable
    {
        private static void Postfix(SelectClassScreen __instance)
        {
            if (!SelectClassGate.CanAssign(__instance, __instance._selectedClassId))
                __instance._selectClassButton.SetInteractable(false);
        }
    }

    /// <summary>A double-click would otherwise turn the button back on on its way to pressing it.</summary>
    [HarmonyPatch(typeof(SelectClassScreen), "PanelOnForceSelectClass")]
    internal static class SelectClassScreen_PanelOnForceSelectClass_BlockUnavailable
    {
        private static bool Prefix(SelectClassScreen __instance, string arg2)
        {
            return SelectClassGate.CanAssign(__instance, arg2);
        }
    }

    /// <summary>
    ///     The gate itself, for every route to the button including gamepad confirm. A class change only opens
    ///     the confirmation dialog from here, so refusing here also covers ConfirmChangeClassDialog.
    /// </summary>
    [HarmonyPatch(typeof(SelectClassScreen), "SelectClassButtonOnClick")]
    internal static class SelectClassScreen_SelectClassButtonOnClick_Gate
    {
        private static bool Prefix(SelectClassScreen __instance)
        {
            return SelectClassGate.CanAssign(__instance, __instance._selectedClassId);
        }
    }

    internal static class SelectClassGate
    {
        internal static bool CanAssign(SelectClassScreen screen, string classId)
        {
            return ClassAssignment.CanAssign(screen._mercenaries, GameState.Get<MagnumCargo>(), screen._merc, classId);
        }
    }

    /// <summary>
    ///     A class chip is never "already unlocked": every class starts unlocked and the chip is spent on
    ///     assignment instead. This clears the red status icon on the cargo slot and the matching tooltip line,
    ///     the only two callers. The cargo context menu checks UnlockedClasses on its own and keeps hiding the
    ///     Unlock command, which would now do nothing.
    /// </summary>
    [HarmonyPatch(typeof(ItemInteractionSystem), nameof(ItemInteractionSystem.IsAlreadyUnlockedDatadisk))]
    internal static class ItemInteractionSystem_IsAlreadyUnlockedDatadisk_ClassChipsStayUseful
    {
        private static void Postfix(DatadiskRecord record, ref bool __result)
        {
            if (record.UnlockType == DatadiskUnlockType.MercenaryClass)
                __result = false;
        }
    }
}
