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
        readonly HashSet<int> wanted = new HashSet<int>();
        readonly List<int> expired = new List<int>();
        readonly GameObject[] source = new GameObject[3];
        float nextRefresh;

        public void Initialize(VaultGame owner)
        {
            game = owner;
            source[0] = Resources.Load<GameObject>("ImportedWorld/props/pbr_box_ammo");
            source[1] = Resources.Load<GameObject>("ImportedWorld/props/rack");
            source[2] = Resources.Load<GameObject>("ImportedWorld/props/pbr_box_ammo");
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
                if (visible.ContainsKey(site.id)) continue;
                int kind = Mathf.Clamp(site.type, 0, 2);
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
                visible.Add(site.id, marker);
            }
            expired.Clear();
            foreach (var pair in visible) if (!wanted.Contains(pair.Key)) expired.Add(pair.Key);
            foreach (int id in expired) { Destroy(visible[id]); visible.Remove(id); }
        }
    }
}
