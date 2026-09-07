using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace NewGamePlus.WeaponAndArmorCases
{
    /// <summary>
    ///     The containers whose disassembly is rolled rather than fixed, and the tech level they are
    ///     rolled against. There is no global tech level in the game - every reading of it is a
    ///     faction's own CurrentTechLevel - so the average across the unlocked factions stands in for
    ///     "how advanced the sector is right now".
    /// </summary>
    internal static class LootCases
    {
        private static readonly List<LootCase> Cases = new List<LootCase>();

        private static readonly HashSet<string> ExcludedWeaponIds = new HashSet<string>();

        internal static void Initialize()
        {
            Cases.Clear();

            ExcludedWeaponIds.Clear();
            if (Plugin.Config.WeaponCaseExcludedIds != null)
                foreach (var itemId in Plugin.Config.WeaponCaseExcludedIds)
                    ExcludedWeaponIds.Add(itemId);

            Register(new LootCase("weapon_container", () => Plugin.Config.WeaponCaseWeapons, IsWeapon));
            Register(new LootCase("armor_container", () => Plugin.Config.ArmorCasePieces, IsArmorPiece));
        }

        private static void Register(LootCase lootCase)
        {
            if (lootCase.Initialize())
                Cases.Add(lootCase);
        }

        /// <summary>True when the item is a rolled container, meaning its yield must not be rolled again
        ///     against the game's per-item spawn chance.</summary>
        internal static bool Reroll(BasePickupItem item)
        {
            if (item == null)
                return false;

            var factions = GameState.Get<Factions>();
            if (factions == null)
                return false;

            foreach (var lootCase in Cases)
                if (lootCase.ItemId == item.Id)
                {
                    lootCase.Reroll(TechLevelCap(factions));
                    return true;
                }

            return false;
        }

        /// <summary>
        ///     Locked factions never gain tech - FactionSystem.TechLevelTick skips them - so counting
        ///     them would peg the average to their starting level for the whole run.
        /// </summary>
        private static int TechLevelCap(Factions factions)
        {
            var total = 0;
            var counted = 0;

            foreach (var faction in factions.Values)
            {
                if (!factions.IsEnabledFaction(faction))
                    continue;

                total += faction.CurrentTechLevel;
                counted++;
            }

            var average = counted == 0 ? 1 : Mathf.RoundToInt(total / (float) counted);
            return Mathf.Clamp(average + Plugin.Config.CaseTechLevelBonus, 1, Data.Global.MaxTechLevel);
        }

        /// <summary>
        ///     Melee and ranged both. Quasimorph weapons are ruled out, as are the monsters' own natural
        ///     weapons and the severed-limb weapons, which are the weapon facet of an augmentation item
        ///     rather than anything a case could hold.
        ///     WeaponSubClass.Tool is the game's marker for repurposed industrial gear - nail guns,
        ///     wrenches, crowbars, shovels, discthrowers, the fire extinguisher. The scrap-metal melee
        ///     weapons are junk of the same kind but carry no flag that separates them from a service
        ///     knife or a police baton, so they are excluded by id from config instead.
        /// </summary>
        private static bool IsWeapon(CompositeItemRecord composite)
        {
            var weapon = composite.GetRecord<WeaponRecord>();

            return weapon != null
                   && !weapon.IsImplicit
                   && weapon.WeaponSubClass != WeaponSubClass.Quasi
                   && weapon.WeaponSubClass != WeaponSubClass.Tool
                   && !ExcludedWeaponIds.Contains(weapon.Id)
                   && composite.GetRecord<AugmentationRecord>() == null;
        }

        /// <summary>
        ///     The four worn armour slots. IArmorRecord is what separates them from vests, the fifth
        ///     ResistRecord slot, which are load-bearing rigs rather than armour. ArmorClass.Cloth is
        ///     the game's own clothing tier - 131 of the 399 pieces, 91 of them at tech level 1 - and is
        ///     what keeps overalls and lab coats out of an Armor Case.
        /// </summary>
        private static bool IsArmorPiece(CompositeItemRecord composite)
        {
            var armor = composite.GetRecord<ResistRecord>() as IArmorRecord;

            return armor != null
                   && armor.ArmorSubClass != ArmorSubClass.Quasi
                   && armor.ArmorClass != ArmorClass.Cloth
                   && armor.ArmorClass != ArmorClass.HardCloth;
        }
    }
}
