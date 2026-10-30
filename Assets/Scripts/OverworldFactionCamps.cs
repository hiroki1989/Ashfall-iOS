using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const int FactionCampReadyBase = 100000;
        const int FactionCampClaimedBase = 100100;
        sealed class FactionCamp
        {
            public int id, faction;
            public Vector3 position;
            public readonly List<WorldEnemySpawn> guards = new List<WorldEnemySpawn>();
            public bool spawned;
            public GameObject visual;
        }
        readonly List<FactionCamp> factionCamps = new List<FactionCamp>();
        Material campMetal, campWood, campRed, campSand;

        Material CampMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color, enableInstancing = true };
            materials.Add(material);
            return material;
        }

        void CampPart(Transform root, PrimitiveType shape, Vector3 localPosition, Vector3 scale, Material material, float yaw = 0)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.transform.SetParent(root, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            // Scenery is decorative: neither the road nor guard positions acquire blockers.
            var collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        void BuildFactionCampVisual(FactionCamp camp)
        {
            if (campMetal == null)
            {
                campMetal = CampMaterial(new Color(.19f, .19f, .18f));
                campWood = CampMaterial(new Color(.34f, .25f, .17f));
                campRed = CampMaterial(new Color(.54f, .10f, .08f));
                campSand = CampMaterial(new Color(.58f, .46f, .28f));
            }
            var root = new GameObject("Faction camp " + camp.id);
            root.transform.SetParent(world, false);
            root.transform.position = new Vector3(camp.position.x, 0, camp.position.z);
            camp.visual = root;
            // Two flanks leave the centre and all four guard positions open.
            for (int side = -1; side <= 1; side += 2)
            {
                var offset = new Vector3(side * 7, .65f, 0);
                var location = camp.position + offset;
                if (WorldGeography.Blocked(location, 1.8f) || WorldEntranceBuildings.Blocked(location, 1.8f, WorldGeography)) continue;
                float x = side * 7;
                if (camp.faction == 1) // Red Claw: red banners and fortified scrap.
                {
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, .72f, 0), new Vector3(2.4f, 1.35f, .25f), campMetal, side * 12);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, 1.35f, 0), new Vector3(2.5f, .13f, .35f), campRed, side * 12);
                    CampPart(root.transform, PrimitiveType.Cylinder, new Vector3(x + side * .8f, 1.75f, -1.7f), new Vector3(.11f, 1.75f, .11f), campMetal);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, 2.65f, -1.7f), new Vector3(1.55f, .9f, .08f), campRed);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, 2.65f, -1.75f), new Vector3(.14f, .65f, .04f), campMetal, 25);
                }
                else // Dusk Coyotes: canvas, supply crates, and ochre pennants.
                {
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, .45f, 0), new Vector3(1.25f, .9f, 1.15f), campWood, side * 10);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x + side * .7f, .33f, 1.25f), new Vector3(.8f, .65f, .8f), campMetal);
                    CampPart(root.transform, PrimitiveType.Cylinder, new Vector3(x, 1.6f, -1.6f), new Vector3(.1f, 1.6f, .1f), campWood);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x + side * .65f, 2.45f, -1.6f), new Vector3(1.3f, .65f, .07f), campSand);
                    CampPart(root.transform, PrimitiveType.Cube, new Vector3(x, 1.4f, 1.6f), new Vector3(2.4f, .12f, 1.3f), campSand, side * 8);
                }
            }
        }


        // Called after the regular overworld population is generated.
        void CreateFactionCamps()
        {
            factionCamps.Clear();
            campMetal = campWood = campRed = campSand = null;
            for (int i = 0; i < 50; i++)
            {
                int x = -45 + (i % 10) * 10, z = -40 + (i / 10) * 20;
                var random = new System.Random(17437 + i * 7919);
                Vector3 p = Vector3.zero;
                bool found = false;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    float along = z * 100 + 15 + (float)random.NextDouble() * 70;
                    var candidate = new Vector3(WorldGeography.StreetX(x, along) + 5, .65f, along);
                    bool safe = Mathf.Abs(candidate.x - WorldGeography.RiverX(candidate.z)) >= 40 || WorldGeography.Bridge(candidate.z);
                    foreach (var camp in WorldGeography.Camps) if ((camp - candidate).sqrMagnitude < 50 * 50) { safe = false; break; }
                    foreach (var entrance in WorldGeography.Entrances) if ((entrance - candidate).sqrMagnitude < 35 * 35) { safe = false; break; }
                    if (safe && !WorldGeography.Blocked(candidate, 1f) && !WorldEntranceBuildings.Blocked(candidate, 1f, WorldGeography)) { p = candidate; found = true; break; }
                }
                if (found) factionCamps.Add(new FactionCamp { id = i, faction = i % 2 + 1, position = p });
            }
        }

        // Called after streaming; guard spawn records use the same population cap and streaming as normal enemies.
        void TickFactionCamps()
        {
            if (!IsOverworld || player == null) return;
            foreach (var camp in factionCamps)
            {
                float distance = (camp.position - PlayerPosition).sqrMagnitude;
                if (camp.visual != null && distance > 145 * 145) { Destroy(camp.visual); camp.visual = null; }
                else if (camp.visual == null && distance < 120 * 120) BuildFactionCampVisual(camp);
                if (IsOverworldActivityCompleted(FactionCampReadyBase + camp.id)) continue;
                if (!camp.spawned && (camp.position - PlayerPosition).sqrMagnitude < 100 * 100)
                {
                    camp.spawned = true;
                    int guardCount = rng.Next(10, 21);
                    for (int member = 0; member < guardCount; member++)
                    {
                        float angle = member * Mathf.PI * 2f / guardCount;
                        float radius = 5f + (member % 2) * 3f;
                        var p = camp.position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                        if (WorldGeography.Blocked(p, .8f) || WorldEntranceBuildings.Blocked(p, .8f, WorldGeography)) p = camp.position;
                        var spawn = new WorldEnemySpawn { position = p, kind = 10, level = Mathf.Max(1, DebtLevel) };
                        camp.guards.Add(spawn); worldEnemySpawns.Add(spawn);
                    }
                }
                if (!camp.spawned) continue;
                bool allDefeated = camp.guards.Count > 0;
                foreach (var guard in camp.guards)
                {
                    if (!guard.defeated) allDefeated = false;
                    var foe = guard.active;
                    if (foe == null || foe.GetComponentInChildren<HumanFactionVisual>() != null) continue;
                    foreach (var renderer in foe.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                    var human = HumanFactionVisual.Create(foe.transform, camp.faction, foe.UseAssaultRifle ? 0 : 1);
                    human.transform.localScale=Vector3.one*1.12f;
                    human.transform.localPosition = Vector3.down * .03f;
                }
                if (allDefeated)
                {
                    CompleteOverworldActivity(FactionCampReadyBase + camp.id);
                    Notify("CAMP CLEARED / open the reward chest", "野営地制圧 / 報酬箱を開けよう");
                }
            }
        }

        void AddFactionCampRewards()
        {
            foreach (var camp in factionCamps)
                if (IsOverworldActivityCompleted(FactionCampReadyBase + camp.id) && !IsOverworldActivityCompleted(FactionCampClaimedBase + camp.id))
                    nearbyOverworldActivities.Add(new OverworldActivitySite { id = FactionCampClaimedBase + camp.id, type = 5, position = camp.position });
        }
        static bool IsFactionCampReward(int id) => id >= FactionCampClaimedBase && id < FactionCampClaimedBase + 50;
    }
}
