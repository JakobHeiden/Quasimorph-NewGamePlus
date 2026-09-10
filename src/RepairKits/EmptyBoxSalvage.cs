using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Makes a disassembled repair kit yield nothing but its Empty Box, with the kit's remaining charges out of
    ///     its full charges as the chance. Each kit's own yield is replaced by a single Empty Box entry of count
    ///     zero: an empty list would read as "cannot be disassembled" and hide the option, while a zero count
    ///     leaves ItemInteractionSystem.Disassemble nothing to roll, so the box added afterwards is the whole
    ///     yield. Nothing in the game displays the list. A stack keeps one pooled charge total rather than charges per kit, so
    ///     the pool is dealt out full kit first - the same order ItemInteractionSystem.Disassemble takes charges
    ///     from a partly disassembled stack. The Recycling department multiplies a stack's count by its bonus
    ///     without adding charges, so its bonus kits come out empty and never return a box.
    /// </summary>
    public static class EmptyBoxSalvage
    {
        private static readonly HashSet<string> RepairKitIds = new HashSet<string>();

        public static void Initialize(List<RepairRecord> repairKits)
        {
            RepairKitIds.Clear();
            foreach (var repairKit in repairKits)
            {
                var disassemblyRecord = Data.Items.GetSimpleRecord<ItemRecord>(repairKit.Id);
                if (disassemblyRecord == null || !disassemblyRecord.CanDisassembly)
                    continue;

                disassemblyRecord.Disassembly = new List<ItemQuantity> { new ItemQuantity(EmptyBox.Id, 0) };
                RepairKitIds.Add(repairKit.Id);
            }

            Plugin.Logger.Log($"EmptyBoxSalvage: {RepairKitIds.Count} repair kit(s) return an Empty Box on disassembly.");
        }

        public static KitCharges ReadCharges(BasePickupItem item)
        {
            if (item == null || !RepairKitIds.Contains(item.Id))
                return null;

            var usable = item.Comp<UsableItemComponent>();
            return usable == null ? null : new KitCharges(item.StackCount, usable.CurrentUsageValue, usable.MaxUsageValue);
        }

        public static void GiveBack(BasePickupItem item, KitCharges before, Inventory inventory,
            List<BasePickupItem> itemsWithoutStorage)
        {
            var chargesLeft = item.StackCount == 0 ? 0 : Mathf.Max(0, item.Comp<UsableItemComponent>().CurrentUsageValue);
            var disassembledCharges = before.Charges - chargesLeft;

            var boxes = 0;
            for (var kit = before.StackCount - item.StackCount; kit > 0; kit--)
            {
                var kitCharges = Mathf.Clamp(disassembledCharges, 0, before.ChargesPerKit);
                disassembledCharges -= kitCharges;

                if (Random.Range(0, before.ChargesPerKit) < kitCharges)
                    boxes++;
            }

            while (boxes > 0)
            {
                var box = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(EmptyBox.Id);
                box.StackCount = (short)Mathf.Min(boxes, box.MaxStack);
                boxes -= box.StackCount;
                box.ExaminedItem = false;

                if (inventory == null || !inventory.TakeOrEquip(box))
                    itemsWithoutStorage.Add(box);
            }
        }

        public sealed class KitCharges
        {
            public KitCharges(int stackCount, int charges, int chargesPerKit)
            {
                StackCount = stackCount;
                Charges = charges;
                ChargesPerKit = chargesPerKit;
            }

            public int StackCount { get; }

            public int Charges { get; }

            public int ChargesPerKit { get; }
        }
    }
}
