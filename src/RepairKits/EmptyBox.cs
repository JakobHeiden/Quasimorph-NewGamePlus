using System.Collections.Generic;
using System.Reflection;
using MGSC;
using UnityEngine;

namespace NewGamePlus.RepairKits
{
    /// <summary>
    ///     Adds the Empty Box, the crafting resource every repair kit recipe takes. The game only builds item
    ///     records and their descriptors from its own asset files, so both are cloned at runtime from vanilla tin,
    ///     a plain trash item - price and floor shadow included - and given their own sprites, name, weight and
    ///     stack size.
    ///     As a Resource trash item it gets the craft resource tooltip, and MagnumCargoSystem's recycling hands
    ///     out random Resource items, so it can come out of the Magnum's recycling. Its categories are cleared
    ///     because ItemDropSystem hands out every item whose categories match a loot table.
    /// </summary>
    public static class EmptyBox
    {
        public const string Id = "ngp_empty_box";

        private const string TemplateId = "tin";

        private const string Name = "Empty Box";

        private const string ShortDescription = "Build repair kits from this";

        private const short MaxStack = 3;

        private const float Weight = 0.4f;

        private static readonly MethodInfo ShallowCopy =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Register()
        {
            var template = (Data.Items.GetRecord(TemplateId) as CompositeItemRecord)?.GetRecord<TrashRecord>();
            if (template == null)
            {
                Plugin.Logger.LogError($"EmptyBox: '{TemplateId}' is not a trash item to clone, '{Id}' was not added.");
                return false;
            }

            var record = (TrashRecord)ShallowCopy.Invoke(template, null);
            record.Id = Id;
            record.SubType = TrashSubtype.Resource;
            record.Categories = new List<string>();
            record.Disassembly = new List<ItemQuantity>();
            record.MaxStack = MaxStack;
            record.Weight = Weight;

            var templateDescriptor = template.ItemDesc;
            var descriptor = Object.Instantiate(templateDescriptor);
            descriptor.name = Id;
            descriptor._icon = EmbeddedSprites.Load("generic_repair_kit_inv.png", templateDescriptor.Icon);
            descriptor._smallIcon = EmbeddedSprites.Load("generic_repair_kit_floor.png", templateDescriptor.SmallIcon);
            record.ContentDescriptor = descriptor;

            Data.Items.AddRecord(Id, record);

            foreach (var language in Singleton<Localization>.Instance.db.Values)
            {
                language[$"item.{Id}.name"] = Name;
                language[$"item.{Id}.shortdesc"] = ShortDescription;
            }

            Plugin.Logger.Log($"EmptyBox: added '{Id}', cloned from '{TemplateId}'.");
            return true;
        }
    }
}
