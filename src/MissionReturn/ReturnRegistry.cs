using System;
using System.Collections.Generic;
using System.Linq;
using MGSC;
using SimpleJSON;

namespace NewGamePlus.MissionReturn
{
    /// <summary>
    ///     The shuttles currently in transit, and the sidecar file that keeps them in step with the
    ///     game's save slots. State lives in our own file rather than the game's save because
    ///     ComponentsLayout resolves component types through the game assembly alone, so a mod type in
    ///     the components array deserializes to null and throws on load.
    /// </summary>
    public static class ReturnRegistry
    {
        private const string FileSuffix = "_ngp_returns.dat";

        private static readonly List<PendingReturn> Entries = new List<PendingReturn>();

        public static IReadOnlyList<PendingReturn> All => Entries;

        public static int OccupiedShuttles => Entries.Count;

        public static int AvailableShuttles =>
            Math.Max(0, Plugin.Config.AvailableShuttles - OccupiedShuttles);

        public static bool HasShuttleAvailable => AvailableShuttles > 0;

        public static void Add(PendingReturn entry)
        {
            Entries.Add(entry);
        }

        public static void Remove(PendingReturn entry)
        {
            Entries.Remove(entry);
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        /// <summary>
        ///     The return holding this clone's body, if any. Not simply the first entry with a matching
        ///     profile id: a clone can die, be regrown, fly again and come home while the shuttle from
        ///     the raid it died on is still in transit, so one id can carry two entries.
        /// </summary>
        public static PendingReturn FindLocking(string mercProfileId)
        {
            if (string.IsNullOrEmpty(mercProfileId))
                return null;

            for (var i = 0; i < Entries.Count; i++)
                if (Entries[i].LocksClone && Entries[i].MercProfileId == mercProfileId)
                    return Entries[i];

            return null;
        }

        public static bool IsReturning(Mercenary mercenary)
        {
            if (mercenary == null)
                return false;

            var entry = FindLocking(mercenary.ProfileId);
            return entry != null && entry.Stage == ReturnStage.Returning;
        }

        public static bool IsHoldingFor(Mercenary mercenary)
        {
            return mercenary != null && FindLocking(mercenary.ProfileId) != null;
        }

        private static string FileName(int slot, bool isAutoSave, bool isReport)
        {
            var prefix = isReport ? "report_" : isAutoSave ? "autosave_" : string.Empty;
            return $"{prefix}slot_{slot}{FileSuffix}";
        }

        public static void Save(int slot, bool isAutoSave, bool isReport)
        {
            try
            {
                var fileManager = SingletonMonoBehaviour<FileManager>.Instance;
                var fileName = FileName(slot, isAutoSave, isReport);

                if (Entries.Count == 0)
                {
                    fileManager.RemoveFile(fileName);
                    return;
                }

                var data = new ReturnSaveData { Returns = Entries.ToList() };
                fileManager.SaveFile(fileName, SaveToJSON.CreateNode(data).ToString());
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError("Failed to write the pending-returns sidecar.");
                Plugin.Logger.LogException(ex);
            }
        }

        public static void Load(int slot, bool isAutoSave)
        {
            Entries.Clear();

            try
            {
                var fileManager = SingletonMonoBehaviour<FileManager>.Instance;
                var fileName = FileName(slot, isAutoSave, false);

                if (!fileManager.IsFileExist(fileName))
                    return;

                var json = fileManager.LoadTextFile(fileName);
                if (string.IsNullOrEmpty(json))
                    return;

                var data = new ReturnSaveData();
                data.LoadJSON(JSON.Parse(json));

                foreach (var entry in data.Returns)
                {
                    if (entry == null)
                        continue;

                    // Saved mid-unload means the haul already reached cargo and the clone is already
                    // free, so there is nothing to replay - drop it and release the shuttle.
                    if (entry.Stage == ReturnStage.AwaitingUnload)
                        continue;

                    Entries.Add(entry);
                }

                Plugin.Logger.Log($"Restored {Entries.Count} pending return(s) for slot {slot}.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError("Failed to read the pending-returns sidecar; starting empty.");
                Plugin.Logger.LogException(ex);
                Entries.Clear();
            }
        }

        public static void RemoveFiles(int slot, bool autoSavesOnly)
        {
            try
            {
                var fileManager = SingletonMonoBehaviour<FileManager>.Instance;
                fileManager.RemoveFile(FileName(slot, true, false));

                if (autoSavesOnly)
                    return;

                fileManager.RemoveFile(FileName(slot, false, false));
                fileManager.RemoveFile(FileName(slot, false, true));
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogException(ex);
            }
        }
    }
}
