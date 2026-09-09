using System.Collections.Generic;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace NewGamePlus.ClassPerks
{
    // Keeps PerkCeiling applied at every moment a mercenary's perks or the class's unlocks can change,
    // then covers the two places that read the cap from somewhere other than Perk.NextPerkId.

    /// <summary>
    ///     A perk that just reached master must be capped before the next action can feed it experience.
    ///     DoLevelUpPerks is the single funnel every level-up path ends in, and it carries the mercenary.
    /// </summary>
    [HarmonyPatch(typeof(PerkSystem), "DoLevelUpPerks")]
    internal static class PerkSystem_DoLevelUpPerks_ApplyCeiling
    {
        private static void Postfix(Mercenary mercenary)
        {
            PerkCeiling.Sync(mercenary);
        }
    }

    /// <summary>Perks built fresh from the class record start uncapped; cap them on the spot.</summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.ApplyClassForMercenary))]
    internal static class MercenarySystem_ApplyClassForMercenary_ApplyCeiling
    {
        private static void Postfix(Mercenary mercenary)
        {
            PerkCeiling.Sync(mercenary);
        }
    }

    /// <summary>
    ///     A clone either inherits the previous body's Perk instances, which already carry the cap, or
    ///     gets fresh ones on a difficulty that loses perks. The whole roster is swept rather than the
    ///     one mercenary because CloneMercenary replaces it in the list instead of returning it.
    /// </summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.CloneMercenary))]
    internal static class MercenarySystem_CloneMercenary_ApplyCeiling
    {
        private static void Postfix(Mercenaries mercenaries)
        {
            PerkCeiling.SyncAll(mercenaries);
        }
    }

    /// <summary>
    ///     Releases the cap the moment a class project completes, for mercenaries already sitting at
    ///     master as well as future ones.
    /// </summary>
    [HarmonyPatch(typeof(MagnumDevelopmentSystem), nameof(MagnumDevelopmentSystem.ProjectFinished))]
    internal static class MagnumDevelopmentSystem_ProjectFinished_ReleaseCeiling
    {
        private static void Postfix(MagnumProject project, Mercenaries mercenaries)
        {
            if (project.ProjectType == MagnumProjectType.MercenaryClass)
                PerkCeiling.SyncAll(mercenaries);
        }
    }

    /// <summary>
    ///     The training centre picks on grade alone and never looks at NextPerkId, so it would keep
    ///     selecting a capped perk: the mercenary would train forever for nothing, and the completion
    ///     notification would look up a record for an empty id. Rerun the original selection with the
    ///     cap honoured, which also lets it fall through to a perk whose slot has been unlocked.
    /// </summary>
    [HarmonyPatch(typeof(MercenarySystem), nameof(MercenarySystem.GetPerkForTraining))]
    internal static class MercenarySystem_GetPerkForTraining_SkipCapped
    {
        private static bool Prefix(MagnumProgression magnumSpaceship, Mercenary mercenary, ref Perk __result)
        {
            var levelLimit = Mathf.Min(magnumSpaceship.TrainingCenterPerkLevelLimit, 4f);
            Perk trainable = null;
            var lowestLevel = int.MaxValue;

            foreach (var perk in mercenary.CreatureData.Perks)
            {
                if (perk.PerkType == PerkType.Talent || perk.PerkType == PerkType.Rank ||
                    perk.PerkType == PerkType.Ultimate)
                    continue;

                if (string.IsNullOrEmpty(perk.NextPerkId))
                    continue;

                int level;
                string perkTag;
                string grade;
                ParseHelper.GetGradeByPerkId(perk.PerkId, out level, out perkTag, out grade);

                if (level >= levelLimit || level > lowestLevel)
                    continue;

                lowestLevel = level;
                trainable = perk;
            }

            __result = trainable;
            return false;
        }
    }

    /// <summary>
    ///     The tooltip reads NextPerkId off the PerkRecord rather than the mercenary's instance, so a
    ///     capped perk would still advertise progress towards legend. Drop the experience bar and say
    ///     why, mirroring how the Rank branch above it reports the Battle Experience department's limit.
    ///     The cap is re-read from the mercenary's own perk instead of the project so that the tooltip
    ///     can never disagree with what actually blocks the level-up.
    /// </summary>
    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.BuildPerkTooltip), typeof(PerkRecord),
        typeof(Mercenary), typeof(PerkTooltipMode), typeof(int), typeof(bool))]
    internal static class TooltipFactory_BuildPerkTooltip_ShowCeiling
    {
        private static readonly Dictionary<Localization.Lang, string> LimitReached =
            new Dictionary<Localization.Lang, string>
            {
                [Localization.Lang.EnglishUS] = "Perk upgrade limit reached",
                [Localization.Lang.Russian] = "Достигнут лимит улучшения перка",
                [Localization.Lang.German] = "Perk-Verbesserungslimit erreicht",
                [Localization.Lang.French] = "Limite d'amélioration de l'aptitude atteinte",
                [Localization.Lang.Spanish] = "Límite de mejora de la habilidad alcanzado",
                [Localization.Lang.Polish] = "Osiągnięto limit ulepszania perku",
                [Localization.Lang.Turkish] = "Yetenek geliştirme limitine ulaşıldı",
                [Localization.Lang.BrazilianPortugal] = "Limite de aprimoramento de perk alcançado",
                [Localization.Lang.Korean] = "특성 강화 한도 도달",
                [Localization.Lang.Japanese] = "パークアップグレードの上限に到達しました",
                [Localization.Lang.ChineseSimp] = "特长升级已达上限"
            };

        private static void Postfix(TooltipFactory __instance, PerkRecord perkRecord, Mercenary mercenary)
        {
            if (mercenary == null || string.IsNullOrEmpty(perkRecord.NextPerkId) ||
                !PerkGrade.IsMaster(perkRecord.Id))
                return;

            if (!IsCapped(mercenary, perkRecord.Id))
                return;

            __instance._expBlock.gameObject.SetActive(false);
            __instance.AddPanelToTooltip().SetMultilineName(Text()).SetNameColor(Colors.DarkYellow);
        }

        private static bool IsCapped(Mercenary mercenary, string perkId)
        {
            foreach (var perk in mercenary.CreatureData.Perks)
                if (perk.PerkId == perkId)
                    return string.IsNullOrEmpty(perk.NextPerkId);

            return false;
        }

        private static string Text()
        {
            string text;
            return LimitReached.TryGetValue(Singleton<Localization>.Instance.CurrentLang, out text)
                ? text
                : LimitReached[Localization.Lang.EnglishUS];
        }
    }
}
