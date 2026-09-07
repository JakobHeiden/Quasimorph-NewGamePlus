using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NewGamePlus.WeaponAndArmorCases
{
    /// <summary>
    ///     One container whose disassembly yield is rolled against the current tech level instead of the
    ///     fixed list the config ships. The roll is written into the shared ItemRecord just before each
    ///     disassembly rather than intercepted, because ItemInteractionSystem.Disassemble reads
    ///     Disassembly straight off that record; nothing in the game displays the list, and it is
    ///     rewritten on every use.
    ///     Picks are weighted towards the cap and drawn without replacement: nearly half of every weapon
    ///     and armour piece in the game sits at tech level 4 or below, so a flat draw would bury the tier
    ///     the player has actually reached.
    /// </summary>
    internal sealed class LootCase
    {
        /// <summary>Content no faction fields - scenery like the fire extinguisher - carries this literal.</summary>
        private const string NoCategory = "none";

        private readonly Func<int> _dropCount;
        private readonly Func<CompositeItemRecord, bool> _isRolledContent;

        private readonly List<ItemRecord> _pool = new List<ItemRecord>();

        private ItemRecord _record;

        internal LootCase(string itemId, Func<int> dropCount, Func<CompositeItemRecord, bool> isRolledContent)
        {
            ItemId = itemId;
            _dropCount = dropCount;
            _isRolledContent = isRolledContent;
        }

        internal string ItemId { get; }

        internal bool Initialize()
        {
            var caseRecord = Data.Items.GetRecord(ItemId) as CompositeItemRecord;
            _record = caseRecord == null ? null : caseRecord.GetRecord<ItemRecord>();
            if (_record == null)
            {
                Plugin.Logger.LogError($"LootCase: no item record '{ItemId}'; leaving its disassembly untouched.");
                return false;
            }

            return true;
        }

        internal void Reroll(int techLevelCap)
        {
            if (_record == null)
                return;

            BuildPool(techLevelCap);

            var entries = new List<ItemQuantity>();
            for (var drawn = 0; drawn < _dropCount() && _pool.Count > 0; drawn++)
            {
                var index = PickWeighted(techLevelCap);
                entries.Add(new ItemQuantity(_pool[index].Id, 1));
                _pool.RemoveAt(index);
            }

            // An empty list would read as "cannot be disassembled" and hide the option entirely, so a
            // cap too low for anything in the pool leaves the config's own list standing.
            if (entries.Count == 0)
                return;

            _record.Disassembly = entries;
        }

        private void BuildPool(int techLevelCap)
        {
            _pool.Clear();

            foreach (var record in Data.Items.Records)
            {
                var composite = record as CompositeItemRecord;
                if (composite == null || record.Id.Contains("_custom") || !_isRolledContent(composite))
                    continue;

                var item = composite.GetRecord<ItemRecord>();
                if (item == null || item.TechLevel < 1 || item.TechLevel > techLevelCap || !IsFielded(item))
                    continue;

                _pool.Add(item);
            }
        }

        private static bool IsFielded(ItemRecord item)
        {
            if (item.Categories == null)
                return false;

            foreach (var category in item.Categories)
                if (category != NoCategory)
                    return true;

            return false;
        }

        private int PickWeighted(int techLevelCap)
        {
            var total = 0f;
            foreach (var item in _pool)
                total += Weight(item, techLevelCap);

            var roll = Random.Range(0f, total);
            for (var index = 0; index < _pool.Count; index++)
            {
                roll -= Weight(_pool[index], techLevelCap);
                if (roll <= 0f)
                    return index;
            }

            return _pool.Count - 1;
        }

        private static float Weight(ItemRecord item, int techLevelCap)
        {
            var falloff = Mathf.Clamp(Plugin.Config.CaseTechLevelFalloff, 0.01f, 1f);
            return Mathf.Pow(falloff, techLevelCap - item.TechLevel);
        }
    }
}
