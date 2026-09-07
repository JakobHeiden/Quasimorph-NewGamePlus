using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace NewGamePlus.Food
{
    // Removes the calorie cost of activating a trigger perk, leaving movement as the only drain on
    // satiety. The cost is computed inline at three independent sites, all floored by Mathf.Max(.., 1),
    // so no amount of data editing can reach zero - each site needs its own patch.

    /// <summary>
    ///     Drops the Starvation.Restore call that opens ApplyPerkTrigger and reruns the rest of it. The
    ///     deduction is the method's first statement and the remainder is four lines of public API, so
    ///     replacing the body is clearer than transpiling the deduction out of it.
    /// </summary>
    [HarmonyPatch(typeof(PerkSystem), nameof(PerkSystem.ApplyPerkTrigger))]
    internal static class PerkSystem_ApplyPerkTrigger_NoCalorieCost
    {
        private static bool Prefix(CreatureData creatureData, Perk perk)
        {
            var cooldown = PerkSystem.GetPerkModifiedCooldown(creatureData, perk.Get("ICooldown").IntVal);

            creatureData.EffectsController.Add(
                new PerkTrigger(
                    FormatHelper.ClearPerkGrades(perk.PerkId),
                    perk.Get("IDuration").IntVal,
                    cooldown,
                    perk.Get("BActionPointDuration").BoolVal),
                false);

            return false;
        }
    }

    /// <summary>
    ///     Lifts the "can you afford the activation" gate now that activation is free. Passive and rank
    ///     perks already returned true unconditionally, so this only affects trigger perks: they fire and
    ///     keep earning experience while starving.
    /// </summary>
    [HarmonyPatch(typeof(PerkSystem), nameof(PerkSystem.MeetsActivationConditions))]
    internal static class PerkSystem_MeetsActivationConditions_NoCalorieGate
    {
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    /// <summary>
    ///     Zeroes the activation cost the perk tooltip recomputes for itself. The tooltip already skips
    ///     the calorie segment when the cost is not positive, so zeroing drops it from the activation row
    ///     entirely and leaves the rest of the row, and every translation, untouched.
    ///     Anchored on the "IActivation" lookup: the field read that follows it is the no-mercenary path
    ///     (research and project screens), the Mathf.Max floor after that is the mercenary path. If an
    ///     anchor ever moves the tooltip keeps showing a cost that is no longer charged, rather than the
    ///     patch breaking the screen.
    /// </summary>
    [HarmonyPatch(typeof(TooltipFactory), "BuildRegularPerkDetails", typeof(PerkRecord), typeof(Mercenary),
        typeof(string), typeof(bool))]
    internal static class TooltipFactory_BuildRegularPerkDetails_ZeroActivationCost
    {
        private static readonly FieldInfo IntValField =
            AccessTools.Field(typeof(PerkParameter), nameof(PerkParameter.IntVal));

        private static readonly MethodInfo MathfMax =
            AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), new[] { typeof(float), typeof(float) });

        private static readonly MethodInfo ZeroCostInt =
            AccessTools.Method(typeof(TooltipFactory_BuildRegularPerkDetails_ZeroActivationCost), nameof(ZeroCost),
                new[] { typeof(PerkParameter) });

        private static readonly MethodInfo ZeroCostFloat =
            AccessTools.Method(typeof(TooltipFactory_BuildRegularPerkDetails_ZeroActivationCost), nameof(ZeroCost),
                new[] { typeof(float), typeof(float) });

        private static int ZeroCost(PerkParameter activation)
        {
            return 0;
        }

        private static float ZeroCost(float scaledCost, float floor)
        {
            return 0f;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            var activation = code.FindIndex(instruction =>
                instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == "IActivation");

            var intVal = activation < 0
                ? -1
                : code.FindIndex(activation, instruction => instruction.LoadsField(IntValField));

            var floor = intVal < 0
                ? -1
                : code.FindIndex(intVal, instruction => instruction.Calls(MathfMax));

            if (floor < 0)
            {
                Plugin.Logger.LogError(
                    "Could not zero the perk activation cost in the tooltip; it will keep showing a cost that is no longer charged.");
                return code;
            }

            code[intVal].opcode = OpCodes.Call;
            code[intVal].operand = ZeroCostInt;

            code[floor].operand = ZeroCostFloat;

            return code;
        }
    }
}
