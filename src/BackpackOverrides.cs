using MGSC;

namespace NewGamePlus
{
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
                    backpackConfig = new BackpackConfig();
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
    }
}