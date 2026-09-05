using System.Collections.Generic;
using HarmonyLib;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Scratch state shared by the patches that intercept one MissionSystem.ProcessFinishedDungeonData
    ///     call. Everything the delay touches happens inside that single method, in this order:
    ///     ReturnShuttleItems -> MissionFinishedByPlayer -> RemoveSpecificItem -> RestoreStateAfterMission
    ///     -> AfterRaidBriefingScreen.Configure/Show, so a static set by the prefix and cleared by the
    ///     postfix is enough to carry the decision between them.
    /// </summary>
    internal static class ReturnInterceptor
    {
        internal static bool Active;
        internal static PendingReturn Pending;

        /// <summary>Fetch objective left on the vanilla path so the turn-in can still find it in cargo.</summary>
        internal static string TurnInItemId;

        internal static void Reset()
        {
            Active = false;
            Pending = null;
            TurnInItemId = null;
        }
    }

    /// <summary>
    ///     Decides whether a finished raid becomes a delayed return, and if a clone came home parks it
    ///     in MercenaryState.Cloning, the game's own "untouchable clone" state: MercenaryPanel hides
    ///     its backpack, class and implant icons and SelectMercenaryScreen refuses to deploy it.
    ///     The deadline is stamped in the postfix rather than the prefix because the method advances
    ///     the space clock by Data.Global.MissionAddTimeHours in between, which would put a deadline
    ///     measured on entry in the past before the first tick ran.
    /// </summary>
    [HarmonyPatch(typeof(MissionSystem), nameof(MissionSystem.ProcessFinishedDungeonData))]
    internal static class MissionSystem_ProcessFinishedDungeonData_Delay
    {
        private static void Prefix(
            Missions missions,
            Mercenaries mercenaries,
            RaidMetadata raidMetadata,
            DungeonFinishedData finishedData)
        {
            ReturnInterceptor.Reset();

            if (Plugin.Config.ReturnDelayHours <= 0.0)
                return;

            var endedARaid = finishedData != null &&
                             (finishedData.Reason == GameFinishedReason.MissionFinished ||
                              finishedData.Reason == GameFinishedReason.PlayerDead);

            if (!endedARaid)
                return;

            var merc = mercenaries?.MercenaryInRaid;
            if (merc == null)
                return;

            var mission = missions?.Get(raidMetadata);
            if (mission != null && mission.IsStoryMission && !Plugin.Config.DelayStoryMissions)
                return;

            var succeeded = finishedData.Reason == GameFinishedReason.MissionFinished &&
                            raidMetadata?.WinCondition != null &&
                            raidMetadata.WinCondition.WinConditionsPassed;

            var survived = !merc.CreatureData.Health.Dead;
            var bodyCameBack = survived && finishedData.DeathReason != HealthInfo.DeathReason.VictoryDeath;

            ReturnInterceptor.Active = true;
            ReturnInterceptor.Pending = new PendingReturn
            {
                MercProfileId = merc.ProfileId,
                MissionSucceeded = succeeded,
                LocksClone = survived,
                ShouldShowInventory = bodyCameBack
            };

            var isFetchWin = succeeded &&
                             raidMetadata.WinCondition.WinCondition == WinCondition.ItemInInventoryById &&
                             raidMetadata.WinCondition.WinConditionParameters.Count > 0;

            if (isFetchWin)
                ReturnInterceptor.TurnInItemId = raidMetadata.WinCondition.WinConditionParameters[0];
        }

        private static void Postfix(Mercenaries mercenaries, SpaceTime spaceTime)
        {
            if (!ReturnInterceptor.Active)
            {
                ReturnInterceptor.Reset();
                return;
            }

            var pending = ReturnInterceptor.Pending;
            pending.ReleaseTime = spaceTime.Time.AddHours(Plugin.Config.ReturnDelayHours);

            var merc = pending.LocksClone ? mercenaries.Get(pending.MercProfileId) : null;

            if (pending.LocksClone && merc == null)
            {
                Plugin.Logger.LogError("Clone '" + pending.MercProfileId +
                                       "' vanished during mission wrap-up; handing its haul back immediately.");
                ReturnRelease.HandBackHaul(pending);
                ReturnInterceptor.Reset();
                return;
            }

            if (merc != null)
            {
                pending.PriorState = merc.State;
                pending.PriorStateStartTime = merc.StateStartTime;
                pending.PriorStateEndTime = merc.StateEndTime;

                merc.State = MercenaryState.Cloning;
                merc.StateStartTime = spaceTime.Time;
                merc.StateEndTime = pending.ReleaseTime;
            }

            ReturnRegistry.Add(pending);
            Plugin.Logger.Log("Shuttle returning until " + pending.ReleaseTime + " (clone '" +
                              pending.MercProfileId + "', won=" + pending.MissionSucceeded +
                              ", survived=" + pending.LocksClone + ").");

            ReturnInterceptor.Reset();
        }
    }

    /// <summary>
    ///     Moves the drop capsule and the shuttle/elevator cargo into the pending return instead of
    ///     into ship cargo, leaving the vanilla body to clear whatever remains. The haul is taken out
    ///     of the game entirely rather than parked in place because both departments own a single
    ///     shared storage: the next mission would launch with it aboard, ArsenalScreen exposes the
    ///     shuttle in space, and MissionWinCondition counts both toward the next fetch objective.
    /// </summary>
    [HarmonyPatch(typeof(MagnumCargoSystem), nameof(MagnumCargoSystem.ReturnShuttleItems))]
    internal static class MagnumCargoSystem_ReturnShuttleItems_Divert
    {
        private static void Prefix(MagnumProgression magnumSpaceship)
        {
            if (!ReturnInterceptor.Active || magnumSpaceship == null)
                return;

            var pending = ReturnInterceptor.Pending;

            Divert(magnumSpaceship.GetDepartment<AutonomousCapsuleDepartment>()?.CapsuleStorage,
                pending.CapsuleItems);
            Divert(magnumSpaceship.GetDepartment<ShuttleCargoDepartment>()?.ShuttleCargo,
                pending.ShuttleItems);
        }

        private static void Divert(ItemStorage storage, List<BasePickupItem> destination)
        {
            if (storage == null)
                return;

            foreach (var item in new List<BasePickupItem>(storage.Items))
            {
                if (item == null || item.Id == ReturnInterceptor.TurnInItemId)
                    continue;

                storage.Remove(item);
                item.Storage = null;
                item.ExaminedItem = false;
                destination.Add(item);
            }
        }
    }

    /// <summary>
    ///     Holds the mission's reward items back by emptying the list the reward loop reads. Everything
    ///     else MissionFinishedByPlayer does - reputation, station capture, trade points - still runs.
    /// </summary>
    [HarmonyPatch(typeof(MissionSystem), nameof(MissionSystem.MissionFinishedByPlayer))]
    internal static class MissionSystem_MissionFinishedByPlayer_HoldRewards
    {
        private static void Prefix(Mission mission)
        {
            if (!ReturnInterceptor.Active || mission == null)
                return;

            ReturnInterceptor.Pending.RewardItems = new List<BasePickupItem>(mission.RewardItems);
            mission.RewardItems = new List<BasePickupItem>();
        }
    }

    /// <summary>
    ///     Hides clones whose haul is still in transit from a fetch turn-in, which otherwise deletes its
    ///     count out of every mercenary's inventory with no notion of who earned what. Swapping the
    ///     argument for a stand-in works because the method only reads Values.
    /// </summary>
    [HarmonyPatch(typeof(ItemInteractionSystem), nameof(ItemInteractionSystem.RemoveSpecificItem))]
    internal static class ItemInteractionSystem_RemoveSpecificItem_SkipReturning
    {
        private static void Prefix(ref Mercenaries mercenaries)
        {
            if (mercenaries == null || ReturnRegistry.OccupiedShuttles == 0)
                return;

            List<Mercenary> reachable = null;
            foreach (var mercenary in mercenaries.Values)
                if (ReturnRegistry.IsHoldingFor(mercenary))
                {
                    if (reachable == null)
                        reachable = new List<Mercenary>(mercenaries.Values);

                    reachable.Remove(mercenary);
                }

            if (reachable != null)
                mercenaries = new Mercenaries { Values = reachable };
        }
    }

    /// <summary>
    ///     Captures the mission and faction deltas the reward screens need, since both are local to
    ///     ProcessFinishedDungeonData and the Mission itself is removed before the return matures.
    ///     ShouldShowInventory is decided in the prefix instead, because this screen is never
    ///     configured on the PlayerDead path.
    /// </summary>
    [HarmonyPatch(typeof(AfterRaidBriefingScreen), nameof(AfterRaidBriefingScreen.Configure))]
    internal static class AfterRaidBriefingScreen_Configure_Capture
    {
        private static void Postfix(
            Mission mission,
            FactionStatsSnapshot beneficiarySnapshot,
            FactionStatsSnapshot victimSnapshot)
        {
            if (!ReturnInterceptor.Active)
                return;

            ReturnInterceptor.Pending.CaptureMission(mission);
            ReturnInterceptor.Pending.CaptureSnapshots(beneficiarySnapshot, victimSnapshot);
        }
    }

    /// <summary>
    ///     Stops the briefing screen before its coroutine starts and raises the space HUD next frame.
    ///     Hiding the screen after the fact would be too late: StartCoroutine runs the body up to the
    ///     first yield synchronously, flashing the post-mission dialogue.
    /// </summary>
    [HarmonyPatch(typeof(AfterRaidBriefingScreen), "OnEnable")]
    internal static class AfterRaidBriefingScreen_OnEnable_Suppress
    {
        private static bool Prefix()
        {
            if (!ReturnInterceptor.Active)
                return true;

            ReturnHooks.ShowSpaceHudNextFrame = true;
            return false;
        }
    }

    /// <summary>
    ///     Swallows the "clone is ready" notification for a body we parked in Cloning ourselves, since
    ///     the release takes over in the same frame. A clone genuinely regrown after a death shares the
    ///     profile id but keeps its own notification.
    /// </summary>
    [HarmonyPatch(typeof(NotificationPanel), nameof(NotificationPanel.AddMercFinishedActivityNotify))]
    internal static class NotificationPanel_MercFinished_Suppress
    {
        private static bool Prefix(string mercId, MercenaryState state)
        {
            return state != MercenaryState.Cloning || ReturnRegistry.FindLocking(mercId) == null;
        }
    }
}
