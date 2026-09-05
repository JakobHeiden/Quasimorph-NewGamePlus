using System;
using HarmonyLib;
using MGSC;
using UnityEngine.EventSystems;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Retitles the hover tooltip of a clone that is returning rather than cloning, which borrows
    ///     MercenaryState.Cloning for its lock. The state name is read in exactly one place in the
    ///     game, so replacing this handler for our own clones leaves every other one untouched.
    /// </summary>
    [HarmonyPatch(typeof(MercenaryStateIcon), nameof(MercenaryStateIcon.OnPointerEnter))]
    internal static class MercenaryStateIcon_OnPointerEnter_ReturningLabel
    {
        private const string ReturningLabel = "Returning from mission";

        private static bool Prefix(MercenaryStateIcon __instance, PointerEventData eventData)
        {
            if (!ReturnRegistry.IsReturning(__instance._mercenary))
                return true;

            __instance._selectionBorder.gameObject.SetActive(true);

            if (__instance._createdTooltip)
                return false;

            __instance._createdTooltip = true;

            var remaining = FormatHelper.ToLocalizedDaysAndHours(
                __instance._mercenary.StateEndTime - __instance._spaceTime.Time);

            SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip(
                ReturningLabel + Environment.NewLine + remaining.WrapInColor(Colors.White));

            return false;
        }
    }
}
