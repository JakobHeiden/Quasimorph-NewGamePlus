using System;
using System.Collections.Generic;
using HarmonyLib;
using MGSC;

namespace NewGamePlus.Food
{
    // Keeps calories out of the two loot channels that ration a raid: the level's generated loot, and the
    // inventories enemies spawn carrying. Both pick items through ItemDropSystem, and both are filtered on
    // the item's own StarvationValue rather than on its ItemClass, because the classes do not divide along
    // the line that matters - Drink carries calories too, while water is a healing item and a medical
    // crafting reagent that happens to share its class. Anything carrying Qmorphos reduction is exempt as
    // well, being worth more as a corruption sink than the calories it also carries: that is every Alcohol,
    // and ron_blood, which holds the largest reduction in the game on a Food record where no class rule
    // reaches it.

    internal static class FoodFilter
    {
        private static readonly HashSet<string> ExemptIds = new HashSet<string> { "ron_blood" };

        internal static bool IsWithheld(CompositeItemRecord record)
        {
            var consumable = record?.GetRecord<ConsumableRecord>();

            return consumable != null && consumable.StarvationValue > 0 &&
                   consumable.ItemClass != ItemClass.Alcohol && !ExemptIds.Contains(record.Id);
        }

        internal static bool IsWithheld(string itemId)
        {
            return IsWithheld(Data.Items.GetRecord(itemId) as CompositeItemRecord);
        }
    }

    /// <summary>
    ///     Takes calorie items out of a level's generated loot. Filtering the candidate list that feeds the
    ///     points draw, rather than the draw itself, means the budget is spent on everything else instead of
    ///     going unspent, so a level carries as much loot as before and none of it is edible.
    /// </summary>
    [HarmonyPatch(typeof(ItemDropSystem), "PrepareItemDropRecords", typeof(List<ItemRecord>), typeof(ItemsPrices))]
    internal static class ItemDropSystem_PrepareItemDropRecords_NoFood
    {
        private static void Prefix(ref List<ItemRecord> items)
        {
            items = items.FindAll(record => !FoodFilter.IsWithheld(record.Id));
        }
    }

    /// <summary>
    ///     Marks the window in which a creature's equipment is rolled, so the shared item randomizer can
    ///     refuse calories for that roll alone. A finalizer clears the flag rather than a postfix, because a
    ///     throw inside generation would otherwise leave it set and silently strip food from mission
    ///     rewards, which draw through the same randomizer.
    /// </summary>
    [HarmonyPatch(typeof(CreatureSystem), nameof(CreatureSystem.GenerateEquipment))]
    internal static class CreatureSystem_GenerateEquipment_NoFood
    {
        internal static bool RollingEquipment;

        private static void Prefix()
        {
            RollingEquipment = true;
        }

        private static void Finalizer()
        {
            RollingEquipment = false;
        }
    }

    /// <summary>
    ///     Refuses calorie items while equipment is being rolled. A mob's AdditItemClasses table gives Food
    ///     its lowest weight, but the randomizer weights per item rather than per class and food has the
    ///     largest low tech roster in the game, so it wins roughly a tenth of every roll. Rejecting it here
    ///     rather than editing the table leaves GrantedItems alone, so the mobs meant to carry roasted meat
    ///     still carry it.
    /// </summary>
    [HarmonyPatch(typeof(ItemDropSystem), nameof(ItemDropSystem.Randomize))]
    internal static class ItemDropSystem_Randomize_NoFoodOnCreatures
    {
        private static void Prefix(ref Func<CompositeItemRecord, bool> condition)
        {
            if (!CreatureSystem_GenerateEquipment_NoFood.RollingEquipment)
                return;

            var allowed = condition;

            condition = record => !FoodFilter.IsWithheld(record) && (allowed == null || allowed(record));
        }
    }
}
