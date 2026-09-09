using MGSC;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Rewrites the repair kit tables in place once the game's tables are loaded, folding a kit's
    ///     max-durability restore into its ordinary durability restore. Kits are the repair items with a
    ///     positive MaxCapacity: BreakableItemComponent.Restore reads that field as an adjustment to the
    ///     item's permanent max-durability penalty, so a positive value restores max durability and a
    ///     negative one — every other repair item — costs it. Those are left alone. Rewriting the record
    ///     rather than patching the repair call keeps the item tooltip, which reads the same two fields,
    ///     showing the merged number.
    /// </summary>
    public static class RepairKitOverrides
    {
        public static void Apply()
        {
            var count = 0;

            foreach (var record in Data.Items.Records)
            {
                var composite = record as CompositeItemRecord;
                if (composite == null)
                    continue;

                var repair = composite.GetRecord<RepairRecord>();
                if (repair == null || repair.MaxCapacity <= 0)
                    continue;

                var mergedRestoreAmount = repair.RestoreAmount + repair.MaxCapacity;

                Plugin.Logger.Log(
                    $"RepairKitOverrides: '{repair.Id}' restored {repair.RestoreAmount} durability and {repair.MaxCapacity} max durability, now restores {mergedRestoreAmount} durability only.");

                repair.RestoreAmount = mergedRestoreAmount;
                repair.MaxCapacity = 0;
                count++;
            }

            Plugin.Logger.Log($"RepairKitOverrides: reworked {count} repair kit record(s).");
        }
    }
}
