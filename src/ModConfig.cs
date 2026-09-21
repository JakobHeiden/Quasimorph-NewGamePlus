using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NewGamePlus
{
    public class BackpackConfig
    {
        public float Weight;
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

        /// <summary>Multiplies the hours a spaceship flight between space objects costs.</summary>
        public double TravelTimeMultiplier { get; set; } = 1.0 / 3.0;

        /// <summary>Off by default: post-mission story dialogue consumes triggers, so deferring it can
        ///     reorder narrative.</summary>
        public bool DelayStoryMissions { get; set; } = false;

        /// <summary>Weapons rolled into a Weapons Case each time one is taken apart.</summary>
        public int WeaponCaseWeapons { get; set; } = 4;

        /// <summary>Scrap-metal melee weapons, which the game's data does not otherwise distinguish from
        ///     a service knife or a police baton. Repurposed tools are already excluded by their
        ///     WeaponSubClass and need no entry here.</summary>
        public List<string> WeaponCaseExcludedIds { get; set; } = new List<string>
        {
            "trash_pipe_1",
            "trash_club_1",
            "trash_axe_1",
            "trash_blade_1",
            "trash_fist_1",
            "bone_knife"
        };

        /// <summary>Armour pieces rolled into an Armor Case each time one is taken apart.</summary>
        public int ArmorCasePieces { get; set; } = 4;

        /// <summary>Added to the average tech level of the unlocked factions to cap what a rolled case
        ///     can yield. The cap is clamped to the game's maximum tech level.</summary>
        public int CaseTechLevelBonus { get; set; } = 2;

        /// <summary>How much less likely a pick is per tech level below the cap. 1 draws flat across the
        ///     whole pool, which at a high cap is mostly early-game gear.</summary>
        public float CaseTechLevelFalloff { get; set; } = 0.6f;

        /// <summary>Calories spent per action point while sneaking. One action point is one tile, and
        ///     sneaking grants one per turn.</summary>
        public float SlowMoveCaloriesPerAction { get; set; } = 10f;

        /// <summary>Calories spent per action point while walking, which grants two per turn.</summary>
        public float NormalMoveCaloriesPerAction { get; set; } = 5f;

        /// <summary>Calories spent per action point while running, which grants three per turn.</summary>
        public float RunMoveCaloriesPerAction { get; set; } = 20f;

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
