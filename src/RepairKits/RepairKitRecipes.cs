using System.Collections.Generic;
using System.Linq;
using MGSC;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Makes repair kits need an Empty Box. On the Magnum production line the box is added to each kit's
    ///     recipe. The recipe panel draws ingredients into seven slots and skips any past the last one - skipped
    ///     ingredients are neither shown nor checked, yet production still consumes them - so the box goes first
    ///     to always be visible, and a kit that already used all seven gives up one ingredient to make room. At
    ///     dungeon workbenches the box replaces the ingredients outright, which can leave two recipes for the same
    ///     kit identical, so all but one of those are dropped. Workbench recipe ids are built from their
    ///     ingredients but are deliberately not regenerated, because a workbench saves the id of its recipe.
    /// </summary>
    public static class RepairKitRecipes
    {
        private static readonly Dictionary<string, string> IngredientGivenUpForEmptyBox =
            new Dictionary<string, string>
            {
                ["firearm_repair_kit"] = "plastic"
            };

        public static void RequireEmptyBox(List<RepairRecord> repairKits)
        {
            var repairKitIds = new HashSet<string>(repairKits.Select(repairKit => repairKit.Id));

            AddEmptyBoxToProductionLine(repairKitIds);
            ReplaceWorkbenchIngredientsWithEmptyBox(repairKitIds);
        }

        private static void AddEmptyBoxToProductionLine(HashSet<string> repairKitIds)
        {
            foreach (var receipt in Data.ProduceReceipts)
            {
                if (!repairKitIds.Contains(receipt.OutputItem))
                    continue;

                if (IngredientGivenUpForEmptyBox.TryGetValue(receipt.OutputItem, out var givenUpIngredient))
                    RemoveOne(receipt.OutputItem, receipt.RequiredItems, givenUpIngredient);

                receipt.RequiredItems.Insert(0, new ItemQuantity(EmptyBox.Id, 1));

                Plugin.Logger.Log(
                    $"RepairKitRecipes: production line recipe for '{receipt.OutputItem}' now takes an Empty Box, {receipt.RequiredItems.Sum(item => item.Count)} ingredient(s) in total.");
            }
        }

        private static void RemoveOne(string outputItem, List<ItemQuantity> requiredItems, string itemId)
        {
            var index = requiredItems.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                Plugin.Logger.LogError(
                    $"RepairKitRecipes: production line recipe for '{outputItem}' has no '{itemId}' to give up for the Empty Box.");
                return;
            }

            var remaining = requiredItems[index].Count - 1;
            if (remaining > 0)
                requiredItems[index] = new ItemQuantity(itemId, remaining);
            else
                requiredItems.RemoveAt(index);
        }

        private static void ReplaceWorkbenchIngredientsWithEmptyBox(HashSet<string> repairKitIds)
        {
            var distinctRecipes = new HashSet<string>();
            var duplicates = new List<WorkbenchReceiptRecord>();
            var replacedCount = 0;

            foreach (var receipt in Data.WorkbenchReceipts)
            {
                if (!repairKitIds.Contains(receipt.OutputItem))
                    continue;

                receipt.RequiredItems = new List<ItemQuantity> { new ItemQuantity(EmptyBox.Id, 1) };
                replacedCount++;

                if (!distinctRecipes.Add(WorkbenchRecipeKey(receipt)))
                    duplicates.Add(receipt);
            }

            Data.WorkbenchReceipts.RemoveAll(duplicates.Contains);

            Plugin.Logger.Log(
                $"RepairKitRecipes: {replacedCount} workbench recipe(s) now take only an Empty Box, dropped {duplicates.Count} that became identical.");
        }

        private static string WorkbenchRecipeKey(WorkbenchReceiptRecord receipt)
        {
            var workbenches = string.Join(",", receipt.AllowedWorkbenches.OrderBy(workbench => workbench));
            var perks = string.Join(",", receipt.RequiredPerks.OrderBy(perk => perk.Key).Select(perk => perk.Key + "=" + perk.Value));
            return receipt.OutputItem + "|" + workbenches + "|" + perks;
        }
    }
}
