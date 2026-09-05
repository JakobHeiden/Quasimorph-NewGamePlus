using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NewGamePlus
{
    public class BackpackConfig
    {
        public float Weight = 15f;
    }

    public class ModConfig
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public Dictionary<string, BackpackConfig> Backpacks { get; set; } = new Dictionary<string, BackpackConfig>();

        /// <summary>Counted in space time, which only advances in space mode.</summary>
        public double ReturnDelayHours { get; set; } = 120.0;

        public int AvailableShuttles { get; set; } = 3;

        /// <summary>Off by default: post-mission story dialogue consumes triggers, so deferring it can
        ///     reorder narrative.</summary>
        public bool DelayStoryMissions { get; set; } = false;

        /// <summary>Offset from the top centre of the space HUD, in canvas units.</summary>
        public float ShuttleCounterX { get; set; } = 0f;

        public float ShuttleCounterY { get; set; } = -12f;

        public static ModConfig LoadConfig(string configPath)
        {
            ModConfig config;

            if (File.Exists(configPath))
                try
                {
                    var sourceJson = File.ReadAllText(configPath);

                    config = JsonConvert.DeserializeObject<ModConfig>(sourceJson, SerializerSettings);

                    //Add any new elements that have been added since the last mod version the user had.
                    var upgradeConfig = JsonConvert.SerializeObject(config, SerializerSettings);

                    if (upgradeConfig != sourceJson)
                    {
                        Plugin.Logger.Log("Updating config with missing elements");
                        //re-write
                        File.WriteAllText(configPath, upgradeConfig);
                    }


                    return config;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError("Error parsing configuration.  Ignoring config file and using defaults");
                    Plugin.Logger.LogException(ex);

                    //Not overwriting in case the user just made a typo.
                    config = new ModConfig();
                    return config;
                }

            config = new ModConfig();

            var json = JsonConvert.SerializeObject(config, SerializerSettings);
            File.WriteAllText(configPath, json);

            return config;
        }

        public void Save(string configPath)
        {
            var json = JsonConvert.SerializeObject(this, SerializerSettings);
            File.WriteAllText(configPath, json);
        }
    }
}
