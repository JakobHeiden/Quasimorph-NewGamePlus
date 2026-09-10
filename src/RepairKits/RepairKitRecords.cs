using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Finds the game's repair kits among all repair items. A kit is a RepairRecord with a positive
    ///     MaxCapacity: BreakableItemComponent.Restore reads that field as an adjustment to the item's
    ///     permanent max-durability penalty, so a positive value restores max durability, while every other
    ///     repair item spends it and carries a negative value. Kits have to be collected before
    ///     RepairKitOverrides runs, because it clears the very field they are recognised by.
    /// </summary>
    public static class RepairKitRecords
    {
        public static List<RepairRecord> FindAll()
        {
            var repairKits = new List<RepairRecord>();

            foreach (var record in Data.Items.Records)
            {
                var composite = record as CompositeItemRecord;
                if (composite == null)
                    continue;

                var repair = composite.GetRecord<RepairRecord>();
                if (repair == null || repair.MaxCapacity <= 0)
                    continue;

                repairKits.Add(repair);
            }

            return repairKits;
        }
    }
}
