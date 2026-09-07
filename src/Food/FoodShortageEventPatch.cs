using HarmonyLib;
using MGSC;

namespace NewGamePlus.Food
{
    /// <summary>
    ///     Suppresses the food shortage event, which strips a level of its food, drink and alcohol during
    ///     generation and would leave a raid with nothing to eat. Clearing the drawn event id covers the
    ///     briefing dialogue as well as the despawn, and costs the raid no other event: the draw that
    ///     returned this one had already ruled the rest out. Events are matched on the item classes they
    ///     despawn rather than by id, so the ammo, goods and medicine shortages are left alone and any
    ///     further food shortage the game adds is caught too.
    /// </summary>
    [HarmonyPatch(typeof(IngameEventSystem), nameof(IngameEventSystem.RandomizeDungeonEvent))]
    internal static class IngameEventSystem_RandomizeDungeonEvent_NoFoodShortage
    {
        private static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
                return;

            ItemDespawnEventRecord despawn;

            if (Data.Events.TryGet(__result, out despawn) && despawn.ItemsClasses != null &&
                despawn.ItemsClasses.Contains(ItemClass.Food))
                __result = string.Empty;
        }
    }
}
