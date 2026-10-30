using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using NeonVault;

// Visual-only benchmark: does not include AI, scene loading or actual iOS memory.
[InitializeOnLoad]
public static class FactionPerformanceReview
{
    const string Flag = "FactionPerformanceReview.pending";
    const string Output = "D:/Unity/BuildTemp/faction-performance-review.txt";
    static FactionPerformanceReview() { if (SessionState.GetBool(Flag, false)) EditorApplication.update += Tick; }
    public static void Run()
    {
        SessionState.SetBool(Flag, true);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.update -= Tick;
        SessionState.SetBool(Flag, false);
        try
        {
            var result = "Editor visual-only benchmark; NOT iOS; AI, original legacy enemy construction, scene load, GPU timing excluded.\n";
            result += "Faction first instance includes runtime asset and shared mesh setup; subsequent samples use warm cache.\n";
            result += Measure("infected kind 0", false, 0, 0);
            result += Measure("infected kind 7", false, 7, 0);
            result += Measure("Coyote variant 0", true, 1, 0);
            result += Measure("Coyote variant 1", true, 1, 1);
            result += Measure("Red Crow variant 0", true, 2, 0);
            result += Measure("Red Crow variant 1", true, 2, 1);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, result);
            UnityEngine.Debug.Log("FACTION_PERFORMANCE_REVIEW_OK\n" + result);
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Output, ex.ToString()); UnityEngine.Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    static string Measure(string label, bool faction, int kind, int variant)
    {
        const int count = 10;
        var actors = new List<GameObject>();
        long before = Profiler.GetTotalAllocatedMemoryLong();
        var watch = Stopwatch.StartNew();
        double first = 0;
        for (int i = 0; i < count; i++)
        {
            var actor = new GameObject("QA " + label + " " + i);
            actors.Add(actor);
            if (faction) HumanFactionVisual.Create(actor.transform, kind, variant);
            else if (!ImportedEnemyVisual.TryAttach(actor, kind)) throw new Exception("Missing imported enemy prefab: " + kind);
            if (i == 0) first = watch.Elapsed.TotalMilliseconds;
        }
        watch.Stop();
        var root = actors[0];
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        int enabled = 0, skinned = 0, vertices = 0, triangles = 0, materials = 0;
        foreach (var renderer in renderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            enabled++; materials += renderer.sharedMaterials.Length;
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skin) { skinned++; mesh = skin.sharedMesh; }
            else { var filter = renderer.GetComponent<MeshFilter>(); if (filter) mesh = filter.sharedMesh; }
            if (!mesh) continue;
            vertices += mesh.vertexCount;
            for (int s = 0; s < mesh.subMeshCount; s++) triangles += (int)(mesh.GetIndexCount(s) / 3);
        }
        long delta = Profiler.GetTotalAllocatedMemoryLong() - before;
        var text = string.Format("{0}: first_ms={1:F1} warm_avg_ms={2:F1} actors={3} allocated_delta_bytes={4} objects_per_actor={5} renderers={6} skinned={7} material_slots={8} vertices={9} triangles={10}\n",label,first,(watch.Elapsed.TotalMilliseconds-first)/(count-1),count,delta,root.GetComponentsInChildren<Transform>(true).Length,enabled,skinned,materials,vertices,triangles);
        foreach (var actor in actors) UnityEngine.Object.Destroy(actor);
        return text;
    }
}
