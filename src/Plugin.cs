using System.IO;
using HarmonyLib;
using MGSC;
using NewGamePlus.Backpacks;

namespace NewGamePlus
{
    public static class Plugin
    {
        public static ConfigDirectories ConfigDirectories = new ConfigDirectories();

        public static Logger Logger = new Logger();

        public static ModConfig Config { get; private set; }

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfig(IModContext context)
        {
            Directory.CreateDirectory(ConfigDirectories.ModPersistenceFolder);

            Config = ModConfig.LoadConfig(ConfigDirectories.ConfigPath);

            new Harmony("Entengummitiger_" + ConfigDirectories.ModAssemblyName).PatchAll();

            BackpackOverrides.Apply(Config, ConfigDirectories.ConfigPath);
        }
    }
}