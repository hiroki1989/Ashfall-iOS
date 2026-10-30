using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Lightweight landmarks for nearby travel activities. They borrow the city's
    // existing props, and are unloaded with the surrounding streamed area.
    public sealed class OverworldActivityVisuals : MonoBehaviour
    {
        VaultGame game;
        readonly Dictionary<int, GameObject> visible = new Dictionary<int, GameObject>();
        readonly Dictionary<int,int> visibleTypes = new Dictionary<int,int>();
        readonly HashSet<int> wanted = new HashSet<int>();
        readonly List<int> expired = new List<int>();
        readonly GameObject[] source = new GameObject[3];
        float nextRefresh;

        public void Initialize(VaultGame owner)
        {
            game = owner;
            source[0] = Resources.Load<GameObject>("ImportedWorld/props/pbr_box_supply");
            source[1] = Resources.Load<GameObject>("ImportedWorld/props/OxygenTank");
            source[2] = Resources.Load<GameObject>("ImportedWorld/props/pbr_box_ammo");
        }

        public void OpenReward(int id)
        {
            if(!visible.TryGetValue(id,out var chest)||!chest)return;
            visible.Remove(id);visibleTypes.Remove(id);
            EquipmentChestVisual.Open(chest);Destroy(chest,2f);
        }

        void Update()
        {
            if (game == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .6f;
            wanted.Clear();
            var sites = game.NearbyOverworldActivitySites;
            if (sites != null) foreach (var site in sites)
            {
                if (site.completed || (site.position - game.PlayerPosition).sqrMagnitude > 150f * 150f) continue;
                wanted.Add(site.id);
                if (visible.TryGetValue(site.id,out var existing))
                {
                    if (visibleTypes.TryGetValue(site.id,out int oldType) && oldType==site.type) continue;
                    Destroy(existing); visible.Remove(site.id); visibleTypes.Remove(site.id);
                }
                visibleTypes[site.id]=site.type;
                if (site.type == 5)
                {
                    var chest = new GameObject("Travel activity reward / " + site.id);
                    chest.transform.SetParent(transform, false);
                    chest.transform.position = new Vector3(site.position.x, 0, site.position.z);
                    chest.transform.rotation = Quaternion.Euler(0, site.id % 4 * 90, 0);
                    EquipmentChestVisual.Create(chest.transform, site.id >= 100100 && site.id < 100150);
                    visible.Add(site.id, chest);
                    continue;
                }
                int kind = site.type == 4 ? 1 : Mathf.Clamp(site.type, 0, 2);
                GameObject marker = source[kind] != null
                    ? Instantiate(source[kind], transform)
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Travel activity / " + site.id;
                if (marker.transform.parent != transform) marker.transform.SetParent(transform, false);
                marker.transform.position = new Vector3(site.position.x, 0, site.position.z);
                marker.transform.rotation = Quaternion.Euler(0, site.id % 4 * 90, 0);
                var renderers = marker.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (longest > .01f)
                    {
                        float target = kind == 1 ? 1.7f : 1.2f;
                        marker.transform.localScale *= target / longest;
                        bounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                        marker.transform.position += Vector3.up * (.02f - bounds.min.y);
                    }
                }
                // Props are visual clues only. The event's explicit proximity check
                // handles interaction, so imported colliders cannot trap the player.
                foreach (var collider in marker.GetComponentsInChildren<Collider>()) collider.enabled = false;
                if (source[kind] == null) marker.transform.localScale = new Vector3(.8f, .4f, .8f);
                ObjectiveGroundRing.Create(marker.transform, site.position, 1.15f);
                visible.Add(site.id, marker);
            }
            expired.Clear();
            foreach (var pair in visible) if (!wanted.Contains(pair.Key)) expired.Add(pair.Key);
            foreach (int id in expired) { Destroy(visible[id]); visible.Remove(id); visibleTypes.Remove(id); }
        }
    }

    // A single ground-level circular line marks interactive objectives without
    // obscuring the prop or adding a bright vertical object to the scene.
    static class ObjectiveGroundRing
    {
        static Material material;
        public static void Create(Transform parent, Vector3 center, float radius)
        {
            if (!material)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (!shader) return;
                material = new Material(shader) { name = "Objective ring / orange" };
            }
            var go = new GameObject("Objective ground ring");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 64;
            line.widthMultiplier = .07f;
            line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(1f, .43f, .055f, .9f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * radius,
                    center.y + .08f, center.z + Mathf.Sin(angle) * radius));
            }
        }
    }
}
