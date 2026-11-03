using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Exercises the same batch, building bounds and camera-ray predicate as the
// outdoor world; no game saves or player preferences are touched.
public static class WorldAssetNavigationOcclusionReview
{
    static readonly BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Static;
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    public static void Run()
    {
        string output = "D:/Unity/BuildTemp/WorldAssetNavigationOcclusionReview";
        Directory.CreateDirectory(output);
        var report = new StringBuilder();
        var watch = Stopwatch.StartNew();
        try
        {
            var geo = new OverworldGeography();
            int sites = 0, buildings = 0;
            foreach (var group in new[] { geo.Camps, geo.Entrances, geo.Chests })
                foreach (var site in group) { Check(!geo.Blocked(site, .4f), "blocked site " + site); sites++; }
            // Road and building geometry share this real geography's collision
            // query, including the footprint used to place added city prefabs.
            for (int x = -40; x <= 40; x += 5) for (int z = -40; z <= 40; z += 5)
            {
                var chunk = geo.GetChunk(x, z);
                foreach (var rect in chunk.Buildings)
                {
                    Check(geo.Blocked(new Vector3(rect.center.x, 0, rect.center.y), .4f), "building centre not blocked " + rect);
                    buildings++;
                }
                foreach (var road in geo.GetRoadSegmentsForChunk(x, z))
                {
                    if (road.BridgeDeck) continue;
                    Vector3 middle = (road.A + road.B) * .5f;
                    Check(!geo.Blocked(middle, .4f), "road blocked " + middle);
                }
            }
            Check(buildings > 100, "insufficient building coverage");
            var prefabs = Resources.LoadAll<GameObject>("ImportedWorld/RuinedCityBuildingsV2");
            Check(prefabs.Length == 6, "expected six ruined city prefabs, got " + prefabs.Length);
            var cameraObject = new GameObject("Occlusion review camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 18;
            camera.transform.position = new Vector3(0, 8, -16);
            camera.transform.LookAt(new Vector3(0, 1, 5));
            var hides = typeof(ImportedWorldBatch).GetMethod("Hides", Instance);
            var faded = typeof(ImportedWorldBatch).GetMethod("Faded", Static);
            Check(hides != null && faded != null, "batch occlusion methods unavailable");
            int groups = 0, parts = 0;
            foreach (var source in prefabs)
            {
                var root = new GameObject("Review batch");
                var sourceBounds = ImportedWorldBatch.SourceBounds(source);
                float scale = Mathf.Min(10f / sourceBounds.size.x, 10f / sourceBounds.size.z, 8f / sourceBounds.size.y);
                var pos = new Vector3(-sourceBounds.center.x * scale, -sourceBounds.min.y * scale, -sourceBounds.center.z * scale);
                ImportedWorldBatch.Add(root.transform, source, pos, Quaternion.identity, Vector3.one * scale, true);
                var batch = root.GetComponent<ImportedWorldBatch>();
                var bounds = (IList)typeof(ImportedWorldBatch).GetField("buildingBounds", Instance).GetValue(batch);
                var ownerBounds = (Bounds)bounds[0];
                Check(ownerBounds.size.x <= 10.01f && ownerBounds.size.z <= 10.01f, "prefab spills outside collision footprint " + source.name);
                var dictionary = (IDictionary)typeof(ImportedWorldBatch).GetField("groups", Instance).GetValue(batch);
                foreach (DictionaryEntry pair in dictionary)
                {
                    object group = pair.Value; var type = group.GetType();
                    var owners = (IList)type.GetField("owners").GetValue(group);
                    foreach (int owner in owners) { Check(owner == 0, "part does not share building owner " + source.name); parts++; }
                    var material = (Material)type.GetField("material").GetValue(group);
                    var ghost = (Material)faded.Invoke(null, new object[] { material });
                    Check(ghost.shader && ghost.shader.name.Contains("BuildingGhost") && ghost.color.a > .05f && ghost.color.a < .8f, "not translucent " + source.name);
                    groups++;
                }
                Check(dictionary.Count > 0, "no batch groups " + source.name);
                // Test actual production Hides predicate against a camera and
                // points on either side of the real prefab bounds.
                var centre = ownerBounds.center;
                camera.transform.position = new Vector3(centre.x, ownerBounds.max.y + 2, ownerBounds.min.z - 12);
                var behind = new Vector3(centre.x, Mathf.Max(.8f, ownerBounds.min.y + ownerBounds.size.y * .2f), ownerBounds.max.z + 2);
                camera.transform.LookAt(behind);
                bool obscured = (bool)hides.Invoke(batch, new object[] { ownerBounds, behind, camera });
                Check(obscured, "camera-ray does not fade behind building " + source.name);
                var front = new Vector3(centre.x, behind.y, ownerBounds.min.z - 1);
                Check(!(bool)hides.Invoke(batch, new object[] { ownerBounds, front, camera }), "fades in front of building " + source.name);
                UnityEngine.Object.DestroyImmediate(root);
            }
            UnityEngine.Object.DestroyImmediate(cameraObject);
            report.AppendLine("PASS checks=" + checks + " sites=" + sites + " buildingFootprints=" + buildings + " prefabs=" + prefabs.Length + " groups=" + groups + " ownerParts=" + parts + " elapsedMs=" + watch.ElapsedMilliseconds);
            report.AppendLine("CPU timing is Editor-only; mobile FPS and live gameplay remain separate checks.");
            File.WriteAllText(output + "/result.txt", report.ToString());
            UnityEngine.Debug.Log(report.ToString()); EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(output + "/result.txt", "FAIL checks=" + checks + "\n" + error);
            UnityEngine.Debug.LogException(error); EditorApplication.Exit(1);
        }
    }
}
