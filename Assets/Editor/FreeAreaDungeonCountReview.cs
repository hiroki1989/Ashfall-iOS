using System;
using System.IO;
using System.Text;
using NeonVault;
using UnityEditor;
using UnityEngine;

public static class FreeAreaDungeonCountReview
{
    public static void Run()
    {
        try
        {
            var geo = new OverworldGeography();
            var center = geo.Camps[0];
            int lv1 = 0, lv2 = 0, outsideReach = 0;
            var report = new StringBuilder();
            report.AppendLine("Free area dungeon count (geography / access analysis)");
            report.AppendLine("ID | distance from camp (m) | base level | faction | name | inside 600m");
            for (int id = 0; id < ExpeditionMap.StageCount; ++id)
            {
                Vector3 delta = geo.Entrances[id] - center; delta.y = 0;
                float distance = delta.magnitude;
                if (distance > 603.0f) continue;
                var stage = ExpeditionMap.Get(id, 1);
                // Faction headquarters require conquest milestones and are excluded from
                // the initially accessible count; ordinary stages can be entered when
                // their level is no higher than the player's current level.
                bool ordinary = id < ExpeditionMap.RegularStageCount;
                bool inside = distance <= 600f;
                if (ordinary && inside && stage.level == 1) lv1++;
                if (ordinary && inside && stage.level <= 2) lv2++;
                if (ordinary && !inside && stage.level <= 2) outsideReach++;
                report.AppendLine(string.Format("{0} | {1:F2} | {2} | {3} | {4} | {5}", id, distance, stage.level, ExpeditionMap.FactionForStage(id), stage.japaneseName, inside));
            }
            report.AppendLine("Level 1 initially enterable inside 600m: " + lv1);
            report.AppendLine("Level 2 enterable inside 600m: " + lv2);
            report.AppendLine("Potential extra within interaction reach (600-603m): " + outsideReach);
            report.AppendLine("Analysis does not verify unobstructed path or building door interaction.");
            const string file = "D:/Unity/BuildTemp/free-area-dungeon-count.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, report.ToString(), Encoding.UTF8);
            Debug.Log("FREE_AREA_DUNGEON_COUNT_DONE: " + file);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            throw;
        }
    }
}
