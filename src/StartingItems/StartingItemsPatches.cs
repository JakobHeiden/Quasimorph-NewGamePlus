using HarmonyLib;
using MGSC;

namespace NewGamePlus.StartingItems
{
    /// <summary>
    ///     Adds a weapon case, an armor case and three Tourist backpacks to a new game's cargo, on top of
    ///     whatever the difficulty's starting equipment is. The cargo is sorted again afterwards, as the
    ///     original does once its own items are in.
    /// </summary>
    [HarmonyPatch(typeof(MagnumCargoSystem), nameof(MagnumCargoSystem.GenerateStartingItems))]
    internal static class MagnumCargoSystem_GenerateStartingItems_ExtraItems
    {
        private static readonly string[] ExtraItemIds =
        {
            "weapon_container",
            "armor_container",
            "small_backpack_1",
            "small_backpack_1",
            "small_backpack_1"
        };

        private static void Postfix(MagnumCargo cargo, SpaceTime spaceTime)
        {
            foreach (var itemId in ExtraItemIds)
            {
                var item = SingletonMonoBehaviour<ItemFactory>.Instance.CreateForInventory(itemId);
                MagnumCargoSystem.AddCargo(cargo, spaceTime, item, tabFilter: true);
            }

            foreach (var storage in cargo.ShipCargo)
                storage.SortWithExpandByTypeAndName(spaceTime);
        }
    }
}
