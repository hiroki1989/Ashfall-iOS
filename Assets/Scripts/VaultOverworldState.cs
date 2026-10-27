using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        public const int OverworldCampCount = 14;
        public const int OverworldChestCount = 500;
        const string OverworldSaveKey = "Vault.Overworld.v1";
        const int OverworldSaveVersion = 2;
        public int LastVisitedCamp { get; private set; }
        public int OverworldMissionClears { get; private set; }
        public int PendingWorldStage { get; private set; } = -1;
        public bool WorldStorySeen { get; private set; }
        public Vector3 SavedOverworldPosition { get; private set; }
        public bool SavedOutside { get; private set; }
        readonly bool[] visitedOverworldCamps = new bool[OverworldCampCount];
        readonly int[] overworldChestOpenedAt = new int[OverworldChestCount];
        readonly HashSet<int> completedOverworldActivities = new HashSet<int>();
        int activeOverworldClueId = -1;
        Vector3 activeOverworldCluePosition;
        public bool HasActiveOverworldClue { get { EnsureOverworldState(); return activeOverworldClueId >= 0; } }
        public Vector3 ActiveOverworldCluePosition { get { EnsureOverworldState(); return activeOverworldCluePosition; } }
        public int ActiveOverworldClueId { get { EnsureOverworldState(); return activeOverworldClueId; } }
        public IReadOnlyList<bool> VisitedOverworldCamps { get { EnsureOverworldState(); return Array.AsReadOnly(visitedOverworldCamps); } }
        public IReadOnlyList<int> OverworldChestOpenedAt { get { EnsureOverworldState(); return Array.AsReadOnly(overworldChestOpenedAt); } }
        bool overworldStateLoaded;

        [Serializable]
        class OverworldSave
        {
            public int version;
            public int lastCamp, missionClears, pendingStage = -1;
            public bool storySeen, outside;
            public Vector3 position;
            public bool[] camps;
            public int[] chestOpenedAt;
            public int[] activities;
            public bool hasActiveClue;
            public int activeClueId;
            public Vector3 activeCluePosition;
        }

        public void EnsureOverworldState()
        {
            if (overworldStateLoaded) return;
            overworldStateLoaded = true;
            visitedOverworldCamps[0] = true;
            for (int i = 0; i < OverworldChestCount; i++) overworldChestOpenedAt[i] = -1;
            if (smoke || !PlayerPrefs.HasKey(OverworldSaveKey)) return;
            try
            {
                var s = JsonUtility.FromJson<OverworldSave>(PlayerPrefs.GetString(OverworldSaveKey));
                if (s == null || (s.version != 1 && s.version != OverworldSaveVersion)) return;
                OverworldMissionClears = Mathf.Max(0, s.missionClears);
                WorldStorySeen = s.storySeen;
                PendingWorldStage = ValidOverworldStage(s.pendingStage) ? s.pendingStage : -1;
                if (s.camps != null)
                    for (int i = 0; i < Math.Min(s.camps.Length, OverworldCampCount); i++) visitedOverworldCamps[i] = s.camps[i];
                visitedOverworldCamps[0] = true;
                LastVisitedCamp = s.lastCamp >= 0 && s.lastCamp < OverworldCampCount && visitedOverworldCamps[s.lastCamp] ? s.lastCamp : 0;
                if (s.chestOpenedAt != null)
                    for (int i = 0; i < Math.Min(s.chestOpenedAt.Length, OverworldChestCount); i++)
                    {
                        int opened = s.chestOpenedAt[i];
                        overworldChestOpenedAt[i] = opened >= 0 && opened <= OverworldMissionClears ? opened : -1;
                    }
                if (s.activities != null)
                    foreach (int id in s.activities) if (id >= 0) completedOverworldActivities.Add(id);
                if (s.hasActiveClue && s.activeClueId >= 0 && completedOverworldActivities.Contains(s.activeClueId) && FiniteOverworldPosition(s.activeCluePosition))
                { activeOverworldClueId = s.activeClueId; activeOverworldCluePosition = s.activeCluePosition; }
                if (s.version == OverworldSaveVersion && FiniteOverworldPosition(s.position))
                {
                    SavedOverworldPosition = s.position;
                    SavedOutside = s.outside;
                }
            }
            catch (Exception e) { Debug.LogWarning("Overworld load failed: " + e.GetType().Name); }
        }

        static bool ValidOverworldStage(int stage) => stage >= 0 && stage < ExpeditionMap.StageCount;
        static bool FiniteOverworldPosition(Vector3 p) =>
            !float.IsNaN(p.x) && !float.IsInfinity(p.x) && Mathf.Abs(p.x) <= 100000f &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && Mathf.Abs(p.y) <= 100000f &&
            !float.IsNaN(p.z) && !float.IsInfinity(p.z) && Mathf.Abs(p.z) <= 100000f;

        public void SaveOverworldState()
        {
            EnsureOverworldState();
            if (smoke) return;
            var s = new OverworldSave
            {
                version = OverworldSaveVersion, lastCamp = LastVisitedCamp,
                missionClears = OverworldMissionClears, pendingStage = PendingWorldStage,
                storySeen = WorldStorySeen, outside = SavedOutside, position = SavedOverworldPosition,
                camps = visitedOverworldCamps, chestOpenedAt = overworldChestOpenedAt,
                activities = new List<int>(completedOverworldActivities).ToArray(),
                hasActiveClue = activeOverworldClueId >= 0,
                activeClueId = activeOverworldClueId, activeCluePosition = activeOverworldCluePosition
            };
            PlayerPrefs.SetString(OverworldSaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        public bool IsCampVisited(int id)
        {
            EnsureOverworldState();
            return id >= 0 && id < OverworldCampCount && visitedOverworldCamps[id];
        }

        public bool IsOverworldActivityCompleted(int id)
        { EnsureOverworldState(); return completedOverworldActivities.Contains(id); }

        public bool CompleteOverworldActivity(int id)
        {
            EnsureOverworldState();
            if (id < 0 || !completedOverworldActivities.Add(id)) return false;
            SaveOverworldState(); return true;
        }

        public bool BeginOverworldClue(int id, Vector3 target)
        {
            EnsureOverworldState();
            if (id < 0 || activeOverworldClueId >= 0 || !FiniteOverworldPosition(target) || !completedOverworldActivities.Add(id)) return false;
            activeOverworldClueId = id; activeOverworldCluePosition = target;
            SaveOverworldState(); return true;
        }

        public bool RecoverOverworldClue(int id)
        {
            EnsureOverworldState();
            if (id < 0 || activeOverworldClueId != id) return false;
            activeOverworldClueId = -1; activeOverworldCluePosition = Vector3.zero;
            SaveOverworldState(); return true;
        }

        public void VisitOverworldCamp(int id)
        {
            EnsureOverworldState();
            if (id < 0 || id >= OverworldCampCount) return;
            bool changed = !visitedOverworldCamps[id] || LastVisitedCamp != id;
            visitedOverworldCamps[id] = true;
            LastVisitedCamp = id;
            if (changed) SaveOverworldState();
        }

        public bool IsOverworldChestAvailable(int id)
        {
            EnsureOverworldState();
            return id >= 0 && id < OverworldChestCount &&
                (overworldChestOpenedAt[id] < 0 || (long)OverworldMissionClears - overworldChestOpenedAt[id] >= 3);
        }

        public bool MarkOverworldChestOpened(int id)
        {
            if (!IsOverworldChestAvailable(id)) return false;
            overworldChestOpenedAt[id] = OverworldMissionClears;
            SaveOverworldState();
            return true;
        }

        public void RegisterOverworldMissionClear()
        {
            EnsureOverworldState();
            // Rebase only at integer exhaustion, retaining every chest's remaining cooldown.
            if (OverworldMissionClears == int.MaxValue)
            {
                int offset = int.MaxValue - 3;
                for (int i = 0; i < OverworldChestCount; i++)
                    if (overworldChestOpenedAt[i] >= 0) overworldChestOpenedAt[i] = Math.Max(0, overworldChestOpenedAt[i] - offset);
                OverworldMissionClears = 3;
            }
            OverworldMissionClears++;
            SaveOverworldState();
        }

        public void SetOverworldReturnStage(int stage)
        {
            EnsureOverworldState();
            PendingWorldStage = ValidOverworldStage(stage) ? stage : -1;
            SaveOverworldState();
        }

        public void RememberOverworldPosition(Vector3 pos, bool outside)
        {
            EnsureOverworldState();
            if (!FiniteOverworldPosition(pos)) return;
            SavedOverworldPosition = pos;
            SavedOutside = outside;
            SaveOverworldState();
        }

        public void CompleteOverworldStory()
        {
            EnsureOverworldState();
            WorldStorySeen = true;
            SaveOverworldState();
        }

        public void ApplyOverworldDeathPenalty()
        {
            EnsureOverworldState();
            Credits = Math.Max(0, Credits) / 2;
            PendingWorldStage = -1;
            SaveProgression();
            SaveOverworldState();
        }
    }
}
