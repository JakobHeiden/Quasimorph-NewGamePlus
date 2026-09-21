using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.ClassChips
{
    /// <summary>
    ///     Counts, takes and hands out class chips in the Magnum's cargo. A chip's class is not its item id but
    ///     DatadiskComponent.UnlockId, rolled from the record's UnlockIds when the item is created, so the game's
    ///     id-based sweeps such as ItemInteractionSystem.RemoveSpecificItem cannot pick a chip by class.
    ///     Only the ship's own storages count, not mercenary inventories, so a chip is never pulled out of the
    ///     backpack of a clone that is away on a mission.
    /// </summary>
    internal static class ClassChipStock
    {
        internal static int Count(MagnumCargo magnumCargo, string classId)
        {
            var count = 0;
            foreach (var storage in MagnumCargoSystem.GetAvailableCargoStorages(magnumCargo))
                foreach (var item in storage.Items)
                    if (ClassOf(item) == classId)
                        count++;

            return count;
        }

        internal static bool TryTake(MagnumCargo magnumCargo, string classId)
        {
            foreach (var storage in MagnumCargoSystem.GetAvailableCargoStorages(magnumCargo))
                foreach (var item in storage.Items)
                    if (ClassOf(item) == classId)
                    {
                        storage.Remove(item);
                        return true;
                    }

            return false;
        }

        internal static void Give(MagnumCargo magnumCargo, SpaceTime spaceTime, string classId)
        {
            var chipId = ChipIdFor(classId);
            if (chipId == null)
            {
                Plugin.Logger.LogError($"No class chip can carry class '{classId}'; none handed out.");
                return;
            }

            var chip = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(chipId);
            chip.Comp<DatadiskComponent>().SetUnlockId(classId);
            MagnumCargoSystem.AddCargo(magnumCargo, spaceTime, chip, tabFilter: true);
        }

        internal static IEnumerable<string> ChippableClasses()
        {
            foreach (var chip in ChipRecords())
                foreach (var classId in chip.Value.UnlockIds)
                    yield return classId;
        }

        private static string ClassOf(BasePickupItem item)
        {
            if (!item.Is<DatadiskRecord>() ||
                item.Record<DatadiskRecord>().UnlockType != DatadiskUnlockType.MercenaryClass)
                return null;

            return item.Comp<DatadiskComponent>()?.UnlockId;
        }

        private static string ChipIdFor(string classId)
        {
            foreach (var chip in ChipRecords())
                if (chip.Value.UnlockIds.Contains(classId))
                    return chip.Key;

            return null;
        }

        private static IEnumerable<KeyValuePair<string, DatadiskRecord>> ChipRecords()
        {
            foreach (var record in Data.Items.Records)
            {
                var composite = record as CompositeItemRecord;
                var datadisk = composite?.GetRecord<DatadiskRecord>();
                if (datadisk != null && datadisk.UnlockType == DatadiskUnlockType.MercenaryClass &&
                    datadisk.UnlockIds != null)
                    yield return new KeyValuePair<string, DatadiskRecord>(composite.Id, datadisk);
            }
        }
    }
}
