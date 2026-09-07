using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.Recipes
{
    /// <summary>
    ///     Rewrites production line recipes in place once the game's tables are loaded. Both ration packs
    ///     were a single plastic, which the wrapper left behind by eating one already paid for, so the line
    ///     produced food out of nothing but time.
    /// </summary>
    public static class RecipeOverrides
    {
        private static readonly Dictionary<string, List<ItemQuantity>> RequiredItems =
            new Dictionary<string, List<ItemQuantity>>
            {
                // Moo-Moo
                ["ration_pack_4"] = new List<ItemQuantity>
                {
                    new ItemQuantity("water_bottle_1", 1),
                    new ItemQuantity("plastic", 3)
                },

                // Chick-Chick
                ["ration_pack_3"] = new List<ItemQuantity>
                {
                    new ItemQuantity("water_bottle_1", 1),
                    new ItemQuantity("rags", 3)
                }
            };

        public static void Apply()
        {
            foreach (var entry in RequiredItems)
            {
                var receipt = Data.ProduceReceipts.Get(entry.Key);

                if (receipt == null)
                {
                    Plugin.Logger.LogError("RecipeOverrides: no production recipe outputs '" + entry.Key +
                                           "'; its cost is unchanged.");
                    continue;
                }

                receipt.RequiredItems = entry.Value;
            }
        }
    }
}
