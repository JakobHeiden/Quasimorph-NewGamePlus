using System;
using System.Collections.Generic;
using MGSC;

namespace NewGamePlus.MissionReturn
{
    public enum ReturnStage
    {
        /// <summary>In transit: clone locked, haul held, shuttle occupied.</summary>
        Returning = 0,

        /// <summary>Haul handed back and screens shown; the shuttle stays occupied until they close.</summary>
        AwaitingUnload = 1
    }

    /// <summary>
    ///     One shuttle's delayed homecoming: the haul it carries and a snapshot of everything the
    ///     vanilla reward screens need, since the live Mission is removed the moment the raid resolves.
    ///     Written with the game's own [Save] reflection rather than Newtonsoft, because that is what
    ///     round-trips <see cref="BasePickupItem" /> polymorphically.
    ///     <see cref="LocksClone" /> is false after a death: MercDied has already replaced that profile
    ///     id with a fresh clone on the game's vat timer, so the return owns only the capsule.
    /// </summary>
    public class PendingReturn
    {
        [Save] public string MercProfileId = string.Empty;
        [Save] public ReturnStage Stage = ReturnStage.Returning;
        [Save] public DateTime ReleaseTime;

        [Save] public MercenaryState PriorState;
        [Save] public DateTime PriorStateStartTime;
        [Save] public DateTime PriorStateEndTime;

        [Save] public bool ShouldShowInventory = true;
        [Save] public bool MissionSucceeded;
        [Save] public bool LocksClone;

        [Save] public bool HasMission;
        [Save] public bool IsStoryMission;
        [Save] public string MissionStationId = string.Empty;
        [Save] public string MissionStoryId = string.Empty;
        [Save] public string BeneficiaryFactionId = string.Empty;
        [Save] public string VictimFactionId = string.Empty;
        [Save] public ProceduralMissionType ProcMissionType;
        [Save] public int ProcMissionNameIndex;

        [Save] public float BeneficiaryReputation;
        [Save] public int BeneficiaryPower;
        [Save] public float BeneficiaryTechLevel;
        [Save] public float VictimReputation;
        [Save] public int VictimPower;
        [Save] public float VictimTechLevel;

        [Save] public List<BasePickupItem> RewardItemsExample = new List<BasePickupItem>();

        [Save] public List<BasePickupItem> RewardItems = new List<BasePickupItem>();
        [Save] public List<BasePickupItem> CapsuleItems = new List<BasePickupItem>();
        [Save] public List<BasePickupItem> ShuttleItems = new List<BasePickupItem>();

        public FactionStatsSnapshot BeneficiarySnapshot => new FactionStatsSnapshot
        {
            Reputation = BeneficiaryReputation, Power = BeneficiaryPower, TechLevel = BeneficiaryTechLevel
        };

        public FactionStatsSnapshot VictimSnapshot => new FactionStatsSnapshot
        {
            Reputation = VictimReputation, Power = VictimPower, TechLevel = VictimTechLevel
        };

        public void CaptureMission(Mission mission)
        {
            HasMission = mission != null;
            if (mission == null)
                return;

            IsStoryMission = mission.IsStoryMission;
            MissionStationId = mission.StationId ?? string.Empty;
            MissionStoryId = mission.StoryId ?? string.Empty;
            BeneficiaryFactionId = mission.BeneficiaryFactionId ?? string.Empty;
            VictimFactionId = mission.VictimFactionId ?? string.Empty;
            ProcMissionType = mission.ProcMissionType;
            ProcMissionNameIndex = mission.ProcMissionNameIndex;
            RewardItemsExample = new List<BasePickupItem>(mission.RewardItemsExample);
        }

        public void CaptureSnapshots(FactionStatsSnapshot beneficiary, FactionStatsSnapshot victim)
        {
            BeneficiaryReputation = beneficiary.Reputation;
            BeneficiaryPower = beneficiary.Power;
            BeneficiaryTechLevel = beneficiary.TechLevel;
            VictimReputation = victim.Reputation;
            VictimPower = victim.Power;
            VictimTechLevel = victim.TechLevel;
        }

        /// <summary>A detached Mission carrying only the fields the after-raid screens read.</summary>
        public Mission RebuildMission()
        {
            if (!HasMission)
                return null;

            return new Mission
            {
                IsStoryMission = IsStoryMission,
                StationId = MissionStationId,
                StoryId = MissionStoryId,
                BeneficiaryFactionId = BeneficiaryFactionId,
                VictimFactionId = VictimFactionId,
                ProcMissionType = ProcMissionType,
                ProcMissionNameIndex = ProcMissionNameIndex,
                RewardItemsExample = RewardItemsExample
            };
        }

        public void ClearHaul()
        {
            RewardItems = new List<BasePickupItem>();
            CapsuleItems = new List<BasePickupItem>();
            ShuttleItems = new List<BasePickupItem>();
        }
    }

    /// <summary>Sidecar file root.</summary>
    public class ReturnSaveData
    {
        [Save] public List<PendingReturn> Returns = new List<PendingReturn>();
    }
}
