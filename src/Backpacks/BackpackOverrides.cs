using System;
using MGSC;

namespace NewGamePlus.Backpacks
{
    // Makes backpacks unbreakable and their contents weightless, moving the load onto the backpack's own
    // weight instead. A backpack without a config entry defaults to what vanilla would weigh full.
    public static class BackpackOverrides
    {
        public static void Apply(ModConfig config, string configPath)
        {
            var count = 0;
            var isConfigChanged = false;

            foreach (var record in Data.Items.Records)
            {
                var composite = record as CompositeItemRecord;
                if (composite == null)
                    continue;

                var backpack = composite.GetRecord<BackpackRecord>();
                if (backpack == null)
                    continue;

                BackpackConfig backpackConfig;
                if (!config.Backpacks.TryGetValue(backpack.Id, out backpackConfig))
                {
                    backpackConfig = new BackpackConfig { Weight = VanillaWeightFilledWithOneKiloItems(backpack) };
                    config.Backpacks[backpack.Id] = backpackConfig;
                    isConfigChanged = true;
                }

                backpack.Weight = backpackConfig.Weight;
                backpack.BackpackWeightMult = 0;
                backpack.Unbreakable = true;
                count++;
            }

            if (isConfigChanged)
            {
                Plugin.Logger.Log("BackpackOverrides: found new backpack id(s), added default entries to config.json.");
                config.Save(configPath);
            }

            Plugin.Logger.Log(
                $"BackpackOverrides: applied per-backpack Weight/BackpackWeightMult (Unbreakable={true}) to {count} backpack record(s).");
        }

        // Base Height, so difficulty presets that enlarge backpacks don't raise the default.
        private static float VanillaWeightFilledWithOneKiloItems(BackpackRecord backpack)
        {
            var slots = backpack.Width * backpack.Height;
            var weight = backpack.Weight + slots * backpack.BackpackWeightMult;
            return (float)Math.Round(weight, 1);
        }
    }
}
