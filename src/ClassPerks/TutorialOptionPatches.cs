using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MGSC;

namespace NewGamePlus.ClassPerks
{
    /// <summary>
    ///     Reads the tutorial option as off, now that it holds Vanilla Perk Swaps. This is the only place the
    ///     game reads it, once, when a new campaign first reaches space. Off takes the game's own skip path:
    ///     Jane's opening and her after-skip briefing still play, the tutorial missions are marked done and
    ///     the AnCom PCPU goes into cargo. Data.Global.SkipStartTutorialDialog would force the same path
    ///     without a patch, but it also silences both of Jane's messages.
    /// </summary>
    [HarmonyPatch]
    internal static class SpacemodeTutorial1_launch_AskForTutorialMoment_NeverTutorial
    {
        private static readonly MethodInfo TutorialGetter =
            AccessTools.PropertyGetter(typeof(DifficultyPreset), nameof(DifficultyPreset.Tutorial));

        private static readonly MethodInfo NeverTutorialMethod =
            AccessTools.Method(typeof(SpacemodeTutorial1_launch_AskForTutorialMoment_NeverTutorial),
                nameof(NeverTutorial));

        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(
                AccessTools.Method(typeof(SpacemodeTutorial1_launch), "AskForTutorialMoment"));
        }

        private static bool NeverTutorial(DifficultyPreset preset)
        {
            return false;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var replaced = 0;

            foreach (var instruction in code)
            {
                if (!instruction.Calls(TutorialGetter))
                    continue;

                instruction.opcode = OpCodes.Call;
                instruction.operand = NeverTutorialMethod;
                replaced++;
            }

            if (replaced == 0)
                Plugin.Logger.LogError(
                    "Could not find the tutorial check; a campaign with Vanilla Perk Swaps on will start the tutorial.");

            return code;
        }
    }
}
