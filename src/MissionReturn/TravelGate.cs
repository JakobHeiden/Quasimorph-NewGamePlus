using HarmonyLib;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Grounds the ship while any shuttle is still out. Holds the starmap's departure button so
    ///     the hover patches can recognise it without a view lookup on every button in the game.
    ///     The gate is applied to the button and to its click handler rather than to
    ///     TravelMetadata.CanTravel, which looks like the natural switch but is a plain field that
    ///     story scripts also write - taking it over would clobber their travel locks.
    /// </summary>
    internal static class TravelGate
    {
        internal const string GroundedMessage = "All shuttles must be docked before travelling.";

        private static CommonButton _departureButton;
        private static bool _showingTooltip;

        internal static bool Grounded => ReturnRegistry.OccupiedShuttles > 0;

        /// <summary>
        ///     Only the jump into Bramfatura is grounded. The way out has to stay open or a player who
        ///     jumped before launching a mission would be held there until the Bramfatura counter
        ///     drained and the stay scenario ejected them.
        /// </summary>
        internal static bool GroundsBramJump(MagnumDepartment department)
        {
            return Grounded && department != null && !department._travelMetadata.IsInBramfatura;
        }

        internal static void Track(CommonButton departureButton)
        {
            _departureButton = departureButton;
        }

        internal static void Forget()
        {
            HideTooltip();
            _departureButton = null;
        }

        internal static bool IsDepartureButton(CommonButton button)
        {
            return button != null && ReferenceEquals(button, _departureButton);
        }

        internal static void ShowTooltip()
        {
            var factory = SingletonMonoBehaviour<TooltipFactory>.Instance;
            if (factory == null || _showingTooltip)
                return;

            _showingTooltip = true;
            factory.ShowSimpleTextTooltip(GroundedMessage);
        }

        internal static void HideTooltip()
        {
            if (!_showingTooltip)
                return;

            _showingTooltip = false;

            var factory = SingletonMonoBehaviour<TooltipFactory>.Instance;
            if (factory != null)
                factory.HideSimpleTextTooltip();
        }
    }

    /// <summary>
    ///     Puts the departure button into the game's own disabled styling while shuttles are out. Only
    ///     ever tightens what the vanilla refresh decided, so a travel lock set by a story still holds.
    /// </summary>
    [HarmonyPatch(typeof(StarmapScreen), "RefreshActionButtons")]
    internal static class StarmapScreen_RefreshActionButtons_TravelGate
    {
        private static void Postfix(StarmapScreen __instance)
        {
            TravelGate.Track(__instance._departureButton);

            if (TravelGate.Grounded)
                __instance._departureButton.SetInteractable(false);
        }
    }

    /// <summary>Refuses the departure itself, since a disabled button is a visual state, not a guard.</summary>
    [HarmonyPatch(typeof(StarmapScreen), "DepartureButtonOnClick")]
    internal static class StarmapScreen_DepartureButtonOnClick_TravelGate
    {
        private static bool Prefix()
        {
            return !TravelGate.Grounded;
        }
    }

    /// <summary>
    ///     Explains the grounded departure button on hover. Patched on CommonButton because that is
    ///     where the pointer handlers live; the reference check keeps it to the one button.
    /// </summary>
    [HarmonyPatch(typeof(CommonButton), nameof(CommonButton.OnPointerEnter))]
    internal static class CommonButton_OnPointerEnter_TravelGate
    {
        private static void Postfix(CommonButton __instance)
        {
            if (TravelGate.Grounded && TravelGate.IsDepartureButton(__instance))
                TravelGate.ShowTooltip();
        }
    }

    [HarmonyPatch(typeof(CommonButton), nameof(CommonButton.OnPointerExit))]
    internal static class CommonButton_OnPointerExit_TravelGate
    {
        private static void Postfix(CommonButton __instance)
        {
            if (TravelGate.IsDepartureButton(__instance))
                TravelGate.HideTooltip();
        }
    }

    /// <summary>
    ///     Grounds the Bramfatura engine, which is the real way around the starmap gate: inside
    ///     Bramfatura every hop is free, because GetTravelHoursBetweenPoints multiplies the distance
    ///     by zero there. Denying CanJump also drives the UI, since the department reports it as
    ///     IsOnCooldown and the fast-access panel fades and refuses clicks on that alone.
    /// </summary>
    [HarmonyPatch(typeof(BramEngineDepartment), nameof(BramEngineDepartment.CanJump), MethodType.Getter)]
    internal static class BramEngineDepartment_CanJump_TravelGate
    {
        private static void Postfix(BramEngineDepartment __instance, ref bool __result)
        {
            if (__result && TravelGate.GroundsBramJump(__instance))
                __result = false;
        }
    }

    /// <summary>
    ///     Gives the engine's status line a reason. Left alone when the engine is genuinely cooling
    ///     down, since that readout is the more useful of the two.
    /// </summary>
    [HarmonyPatch(typeof(BramEngineDepartment), nameof(BramEngineDepartment.GetDescription))]
    internal static class BramEngineDepartment_GetDescription_TravelGate
    {
        private static void Postfix(BramEngineDepartment __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) && __instance.IsActiveDepartment() &&
                TravelGate.GroundsBramJump(__instance))
                __result = TravelGate.GroundedMessage;
        }
    }
}
