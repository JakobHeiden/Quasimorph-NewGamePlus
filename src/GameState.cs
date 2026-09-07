using HarmonyLib;
using MGSC;

namespace NewGamePlus
{
    /// <summary>
    ///     Reaches the game's global components from patches the game hands no arguments. The State
    ///     container is captured from ComponentsLayout.CreateGlobalComponents, which runs both when a
    ///     new game starts and when a save is loaded; components are looked up on demand rather than
    ///     cached, so a load that replaces them is picked up.
    /// </summary>
    internal static class GameState
    {
        private static State _container;

        internal static void Capture(State container)
        {
            _container = container;
        }

        internal static T Get<T>() where T : class
        {
            return _container == null ? null : _container.Get<T>();
        }
    }

    [HarmonyPatch(typeof(ComponentsLayout), nameof(ComponentsLayout.CreateGlobalComponents))]
    internal static class ComponentsLayout_CreateGlobalComponents_CaptureState
    {
        private static void Postfix(ComponentsLayout __instance)
        {
            GameState.Capture(__instance._state);
        }
    }
}
