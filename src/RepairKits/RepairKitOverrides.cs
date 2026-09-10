using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Rewrites the repair kit tables in place once the game's tables are loaded, folding a kit's
    ///     max-durability restore into its ordinary durability restore. Rewriting the record rather than
    ///     patching the repair call keeps the item tooltip, which reads the same two fields, showing the
    ///     merged number.
    /// </summary>
    public static class RepairKitOverrides
    {
        public static void Apply(List<RepairRecord> repairKits)
        {
            foreach (var repairKit in repairKits)
            {
                var mergedRestoreAmount = repairKit.RestoreAmount + repairKit.MaxCapacity;

                Plugin.Logger.Log(
                    $"RepairKitOverrides: '{repairKit.Id}' restored {repairKit.RestoreAmount} durability and {repairKit.MaxCapacity} max durability, now restores {mergedRestoreAmount} durability only.");

                repairKit.RestoreAmount = mergedRestoreAmount;
                repairKit.MaxCapacity = 0;
            }

            Plugin.Logger.Log($"RepairKitOverrides: reworked {repairKits.Count} repair kit record(s).");
        }
    }
}
