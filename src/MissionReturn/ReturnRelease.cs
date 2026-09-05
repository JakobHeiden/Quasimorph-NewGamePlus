using System;
using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     Matures pending returns: hands the held haul to ship cargo and replays the vanilla reward
    ///     screens. Releases one at a time and only while the space map is idle, because the screens
    ///     are singleton views that hide each other, and a second raid finishing by its own route can
    ///     collide with a maturing return. The replay works by restoring
    ///     RaidMetadata.WinCondition.WinConditionsPassed, which ProcessFinishedDungeonData clears:
    ///     AfterRaidBriefingScreen reads it to choose between the dialogue plus statistics window and
    ///     dropping straight to the unload screen.
    /// </summary>
    internal static class ReturnRelease
    {
        private const string CloneReturnedFormat = "{0} has returned from their mission.";
        private const string EmptyShuttleReturnedFormat = "{0}'s shuttle has returned. No survivor aboard.";

        private static int _releaseFrame = -1;

        internal static void Tick(State state)
        {
            if (ReturnRegistry.OccupiedShuttles == 0)
                return;

            var spaceTime = state.Get<SpaceTime>();
            if (spaceTime == null)
                return;

            var idle = !UI.IsAnyShowing(typeof(SpaceHudScreen), typeof(InputLockerScreen));
            var settledSinceRelease = UnityEngine.Time.frameCount != _releaseFrame;

            PendingReturn awaiting = null;
            PendingReturn due = null;

            foreach (var entry in ReturnRegistry.All)
            {
                if (entry.Stage == ReturnStage.AwaitingUnload)
                {
                    awaiting = entry;
                    break;
                }

                if (spaceTime.Time < entry.ReleaseTime)
                    continue;

                if (due == null || entry.ReleaseTime < due.ReleaseTime)
                    due = entry;
            }

            if (awaiting != null)
            {
                if (idle && settledSinceRelease)
                    CompleteUnload(state, awaiting);

                return;
            }

            if (due != null && idle)
                Release(state, due);
        }

        private static void CompleteUnload(State state, PendingReturn awaiting)
        {
            ReturnRegistry.Remove(awaiting);

            var raidMetadata = state.Get<RaidMetadata>();
            if (raidMetadata?.WinCondition != null)
                raidMetadata.WinCondition.WinConditionsPassed = false;
        }

        private static void Release(State state, PendingReturn pending)
        {
            var mercenaries = state.Get<Mercenaries>();
            var merc = mercenaries?.Get(pending.MercProfileId);

            HandBackHaul(pending);

            if (pending.LocksClone && merc != null)
                RestorePriorState(merc, pending, state.Get<SpaceTime>());

            pending.Stage = ReturnStage.AwaitingUnload;
            pending.ClearHaul();
            _releaseFrame = UnityEngine.Time.frameCount;

            SlowTimeForArrival(state.Get<SpaceTime>());
            Notify(pending);

            var showsScreens = merc != null &&
                               (pending.ShouldShowInventory || (pending.MissionSucceeded && pending.HasMission));

            if (!showsScreens)
            {
                ReturnRegistry.Remove(pending);
                return;
            }

            ShowRewardFlow(state, pending, merc);
        }

        private static void RestorePriorState(Mercenary merc, PendingReturn pending, SpaceTime spaceTime)
        {
            var now = spaceTime != null ? spaceTime.Time : pending.ReleaseTime;

            var resumable = pending.PriorState != MercenaryState.None &&
                            pending.PriorState != MercenaryState.Cloning &&
                            pending.PriorState != MercenaryState.InRaid &&
                            pending.PriorStateEndTime > now;

            if (!resumable)
            {
                merc.State = MercenaryState.None;
                return;
            }

            merc.State = pending.PriorState;
            merc.StateStartTime = pending.PriorStateStartTime;
            merc.StateEndTime = pending.PriorStateEndTime;
        }

        internal static void HandBackHaul(PendingReturn pending)
        {
            var spaceGameMode = SingletonMonoBehaviour<SpaceGameMode>.Instance;
            if (spaceGameMode == null)
                return;

            var magnumCargo = spaceGameMode.Get<MagnumCargo>();
            var spaceTime = spaceGameMode.Get<SpaceTime>();
            if (magnumCargo == null || spaceTime == null)
                return;

            Deliver(pending.RewardItems, magnumCargo, spaceTime);
            Deliver(pending.CapsuleItems, magnumCargo, spaceTime);
            Deliver(pending.ShuttleItems, magnumCargo, spaceTime);
        }

        private static void Deliver(List<BasePickupItem> items, MagnumCargo magnumCargo, SpaceTime spaceTime)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                // Held items do not age, so restart the clock rather than charging them for the trip.
                ItemExpireSystem.RefreshExpireTimer(spaceTime, item);
                MagnumCargoSystem.AddCargo(magnumCargo, spaceTime, item, tabFilter: true);
            }

            items.Clear();
        }

        /// <summary>
        ///     Drops the clock so an arrival is not missed at speed. X1 rather than RealTime because
        ///     that is the game's own reaction to something worth noticing, in NewsIcon; RealTime is a
        ///     hundredth of it and would read as a freeze. Never speeds the clock up.
        /// </summary>
        private static void SlowTimeForArrival(SpaceTime spaceTime)
        {
            if (spaceTime == null)
                return;

            var accelerated = spaceTime.TimeScale == TimeScale.X4 ||
                              spaceTime.TimeScale == TimeScale.X10 ||
                              spaceTime.TimeScale == TimeScale.X100;

            if (accelerated)
                spaceTime.TimeScale = TimeScale.X1;
        }

        private static void Notify(PendingReturn pending)
        {
            try
            {
                var format = pending.LocksClone ? CloneReturnedFormat : EmptyShuttleReturnedFormat;
                var name = Localization.Get("spec." + pending.MercProfileId + ".name");

                UI.Staff.NotificationPanel.AddNotification(string.Format(format, name));
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogException(ex);
            }
        }

        private static void ShowRewardFlow(State state, PendingReturn pending, Mercenary merc)
        {
            var raidMetadata = state.Get<RaidMetadata>();
            var spaceship = state.Get<Spaceship>();
            var mission = pending.RebuildMission();

            if (raidMetadata?.WinCondition != null)
                raidMetadata.WinCondition.WinConditionsPassed = pending.MissionSucceeded;

            if (mission == null || spaceship == null)
            {
                if (pending.ShouldShowInventory)
                    UI.Chain<AfterRaidScreen>().Invoke(v => v.Configure(merc)).HideAll().Show().ForbidBack();
                else
                    UI.Chain<SpaceHudScreen>().HideAll().Show();

                return;
            }

            UI.Chain<AfterRaidBriefingScreen>()
                .Invoke(v => v.Configure(merc, mission, pending.BeneficiarySnapshot, pending.VictimSnapshot,
                    spaceship, pending.ShouldShowInventory))
                .SetBackgroundColor(Colors.Transparent)
                .HideAll()
                .Show();
        }
    }
}
