using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    /// <summary>
    /// Runtime builders and custom-rig combat animators for the three human faction bosses:
    /// Raven (Red Claw / Blender HQ), Vex (Coyote / Blender HQ), and Malcolm Vale (articulated Trenchcoat Warden variant).
    /// </summary>
    internal static class FactionBossVisuals
    {
        sealed class ResourceOwner : MonoBehaviour
        {
            public readonly List<Object> owned = new List<Object>();
            void OnDestroy()
            {
                for (int i = 0; i < owned.Count; i++)
                    if (owned[i]) Object.Destroy(owned[i]);
            }
        }

        internal sealed class CustomRigPose
        {
            public Transform modelRoot;
            public Transform pelvis, spine02, spine03, head;
            public Transform upperArmL, lowerArmL, handL;
            public Transform upperArmR, lowerArmR, handR;
            public Transform thighL, calfL, thighR, calfR;
            public Vector3 pelvisRestPos;
            public readonly Dictionary<Transform, Quaternion> restLocalRot = new Dictionary<Transform, Quaternion>();
        }

        struct TexSpec
        {
            public string tex, normal;
            public float metallic, smoothness, bump, emissiveGreen;
            public Color tint;
        }

        static readonly Dictionary<string, TexSpec> BossTexTable = new Dictionary<string, TexSpec>
        {
            { "Skin", new TexSpec { tex = "{k}_body", normal = null, metallic = 0f, smoothness = 0.25f, tint = Color.white } },
            { "Leather", new TexSpec { tex = "iona_leather", normal = "iona_leather_n", metallic = 0.08f, smoothness = 0.42f, bump = 0.9f, tint = Color.white } },
            { "Brass", new TexSpec { tex = "iona_brass", normal = "iona_brass_n", metallic = 0.85f, smoothness = 0.65f, bump = 0.85f, tint = Color.white } },
            { "Pants", new TexSpec { tex = "iona_pants", normal = "iona_pants_n", metallic = 0f, smoothness = 0.14f, bump = 0.8f, tint = Color.white } },
            { "Hair", new TexSpec { tex = "iona_hair", normal = null, metallic = 0f, smoothness = 0.20f, tint = Color.white } },
            { "Coat", new TexSpec { tex = "{k}_coat", normal = "{k}_coat_n", metallic = 0f, smoothness = 0.18f, bump = 0.95f, tint = Color.white } },
            { "Scarf", new TexSpec { tex = "{k}_scarf", normal = "{k}_scarf_n", metallic = 0f, smoothness = 0.14f, bump = 0.9f, tint = Color.white } },
            { "Armor", new TexSpec { tex = "{k}_armor", normal = "{k}_armor_n", metallic = 0.58f, smoothness = 0.55f, bump = 0.9f, tint = Color.white } },
            { "Emblem", new TexSpec { tex = "{k}_emblem", normal = "{k}_emblem_n", metallic = 0.48f, smoothness = 0.52f, bump = 0.85f, tint = Color.white } },
            { "Claw", new TexSpec { tex = "raven_claw", normal = null, metallic = 0.92f, smoothness = 0.72f, tint = Color.white } },
            { "Medallion", new TexSpec { tex = "vex_medallion", normal = null, metallic = 0.82f, smoothness = 0.66f, tint = Color.white } },
            { "Bone", new TexSpec { tex = "vex_bone", normal = null, metallic = 0.04f, smoothness = 0.52f, tint = Color.white } },
            { "Lens", new TexSpec { tex = "{k}_lens", normal = null, metallic = 0.35f, smoothness = 0.92f, emissiveGreen = 0.22f, tint = Color.white } },
            { "Canister", new TexSpec { tex = "{k}_canister_cap", normal = null, metallic = 0.84f, smoothness = 0.62f, tint = Color.white } },
            { "Vent", new TexSpec { tex = "{k}_vent", normal = null, metallic = 0.70f, smoothness = 0.58f, tint = Color.white } },
        };

        public static CustomRigPose AttachBossModel(GameObject root, int faction, out DetailedHeroEquipment3D.WeaponSockets gun)
        {
            string key = faction == 1 ? "Raven" : "Vex";
            string lower = faction == 1 ? "raven" : "vex";
            var prefab = Resources.Load<GameObject>("Characters/" + key + "/" + key);
            if (!prefab) { gun = null; return null; }

            var model = Object.Instantiate(prefab, root.transform, false);
            model.name = key + " Blender Boss Model";
            // Blender FBX (axis_forward=-Z, axis_up=Y) exports character front facing +Z in Unity,
            // matching Foe.transform.forward directly (0 deg). Lift +0.03m to stay flush on floor.
            model.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            ApplyBossTextures(model, key, lower, root);
            if (Application.isMobilePlatform)
            {
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.quality = SkinQuality.Bone2;
                    skin.updateWhenOffscreen = false;
                    skin.shadowCastingMode = ShadowCastingMode.Off;
                    skin.receiveShadows = false;
                }
            }
            else
            {
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    skin.updateWhenOffscreen = true;
            }

            var pose = CaptureRigPose(model.transform);
            gun = DetailedHeroEquipment3D.BuildRifle(root.transform, DetailedHeroMaterials3D.Get);
            PoseCustomRig(pose, gun, Vector3.forward, true, 0f, false, false, false, -1f);
            return pose;
        }

        static void ApplyBossTextures(GameObject model, string key, string lower, GameObject ownerGo)
        {
            var owner = ownerGo.GetComponent<ResourceOwner>() ?? ownerGo.AddComponent<ResourceOwner>();
            string texDir = "Characters/" + key + "/Textures/";
            var clonedMats = new Dictionary<Material, Material>();

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (!src) continue;
                    if (!clonedMats.TryGetValue(src, out var dst))
                    {
                        dst = new Material(src) { name = src.name + " (Boss)" };
                        owner.owned.Add(dst);
                        ConfigureMaterialFromName(dst, src.name, texDir, lower);
                        clonedMats[src] = dst;
                    }
                    mats[i] = dst;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static void ConfigureMaterialFromName(Material mat, string matName, string texDir, string lower)
        {
            string suffix = null;
            int idx = matName.IndexOf('_');
            if (idx >= 0 && idx + 1 < matName.Length) suffix = matName.Substring(idx + 1);
            if (string.IsNullOrEmpty(suffix) || !BossTexTable.TryGetValue(suffix, out var spec)) return;

            string Resolve(string pattern)
            {
                if (string.IsNullOrEmpty(pattern)) return null;
                if (lower == "raven" && pattern == "{k}_emblem") return "raven_slashes";
                if (lower == "raven" && pattern == "{k}_scarf") return "raven_crimson";
                if (lower == "vex" && pattern == "{k}_armor") return "vex_metal";
                return pattern.Replace("{k}", lower);
            }

            string texName = Resolve(spec.tex);
            if (!string.IsNullOrEmpty(texName))
            {
                var tex = Resources.Load<Texture2D>(texDir + texName);
                if (tex && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            string normName = Resolve(spec.normal);
            if (!string.IsNullOrEmpty(normName))
            {
                var nrm = Resources.Load<Texture2D>(texDir + normName);
                if (nrm && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", nrm);
                    mat.EnableKeyword("_NORMALMAP");
                    if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", spec.bump);
                }
            }
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", spec.tint);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", spec.metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", spec.smoothness);
            if (spec.emissiveGreen > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.10f, spec.emissiveGreen, 0.08f));
            }
        }

        static CustomRigPose CaptureRigPose(Transform modelRoot)
        {
            var byName = new Dictionary<string, Transform>();
            foreach (var smr in modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.bones == null) continue;
                foreach (var b in smr.bones)
                    if (b && !byName.ContainsKey(b.name.ToLowerInvariant()))
                        byName[b.name.ToLowerInvariant()] = b;
            }
            foreach (var t in modelRoot.GetComponentsInChildren<Transform>(true))
                if (t && !byName.ContainsKey(t.name.ToLowerInvariant()))
                    byName[t.name.ToLowerInvariant()] = t;

            Transform FindBone(string name)
            {
                string key = name.ToLowerInvariant();
                if (byName.TryGetValue(key, out var exact)) return exact;
                foreach (var kv in byName)
                    if (kv.Key.EndsWith(key)) return kv.Value;
                return null;
            }

            var pose = new CustomRigPose
            {
                modelRoot = modelRoot,
                pelvis = FindBone("pelvis"),
                spine02 = FindBone("spine_02"),
                spine03 = FindBone("spine_03"),
                head = FindBone("head"),
                upperArmL = FindBone("upperarm_l"),
                lowerArmL = FindBone("lowerarm_l"),
                handL = FindBone("hand_l"),
                upperArmR = FindBone("upperarm_r"),
                lowerArmR = FindBone("lowerarm_r"),
                handR = FindBone("hand_r"),
                thighL = FindBone("thigh_l"),
                calfL = FindBone("calf_l"),
                thighR = FindBone("thigh_r"),
                calfR = FindBone("calf_r"),
            };

            if (pose.pelvis) pose.pelvisRestPos = pose.pelvis.localPosition;
            Transform[] tracked = {
                pose.pelvis, pose.spine02, pose.spine03, pose.head,
                pose.upperArmL, pose.lowerArmL, pose.handL,
                pose.upperArmR, pose.lowerArmR, pose.handR,
                pose.thighL, pose.calfL, pose.thighR, pose.calfR
            };
            foreach (var b in tracked)
                if (b) pose.restLocalRot[b] = b.localRotation;
            return pose;
        }

        public static void PoseCustomRig(
            CustomRigPose pose, DetailedHeroEquipment3D.WeaponSockets gun,
            Vector3 forwardDir, bool firing, float phase,
            bool crouch, bool roll, bool reloading, float deathTime, float lateralVel = 0f, int faction = 1)
        {
            if (pose == null || !pose.modelRoot) return;
            foreach (var kv in pose.restLocalRot)
                if (kv.Key) kv.Key.localRotation = kv.Value;

            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude < 0.0001f) forwardDir = Vector3.forward;
            forwardDir.Normalize();
            Quaternion rootRot = Quaternion.LookRotation(forwardDir);
            Vector3 rootRight = rootRot * Vector3.right;
            Vector3 rootUp = Vector3.up;

            // Ground offset & lean
            float crouchT = crouch ? 1f : roll ? 0.65f : 0f;
            float rollTilt = roll ? Mathf.Sin(Time.time * 24f) * 16f : 0f;
            float leanZ = Mathf.Clamp(-lateralVel * 4.5f, -15f, 15f) + rollTilt;

            if (deathTime >= 0f)
            {
                float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((deathTime - 0.10f) / 0.65f));
                float side = faction == 2 ? -1f : 1f;
                pose.modelRoot.localRotation = Quaternion.Euler(fall * 78f, side * fall * 10f, side * fall * 20f);
                pose.modelRoot.localPosition = new Vector3(0f, 0.03f + fall * 0.14f, -fall * 0.18f);
                if (pose.spine02) pose.spine02.rotation = Quaternion.AngleAxis(fall * 14f, rootRight) * pose.spine02.rotation;
                if (gun != null && gun.root) gun.root.gameObject.SetActive(false);
                return;
            }

            if (gun != null && gun.root && !gun.root.gameObject.activeSelf) gun.root.gameObject.SetActive(true);
            pose.modelRoot.localRotation = Quaternion.Euler(18f * crouchT, 0f, leanZ);
            pose.modelRoot.localPosition = new Vector3(0f, 0.03f - 0.18f * crouchT, 0f);

            // Combat stance twist & walking stride (in actor's root frame so it is independent of local bone axis!)
            float stride = phase >= 0f ? Mathf.Sin(phase * Mathf.PI * 2f) : 0f;
            float bob = phase >= 0f ? Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f)) * 0.025f : Mathf.Sin(Time.time * 2.2f) * 0.008f;
            if (pose.pelvis) pose.pelvis.localPosition = pose.pelvisRestPos + new Vector3(0f, bob, 0f);
            if (pose.spine02) pose.spine02.rotation = Quaternion.AngleAxis(firing ? 24f : 10f, rootUp) * pose.spine02.rotation;
            if (pose.head) pose.head.rotation = Quaternion.AngleAxis(firing ? -20f : -8f, rootUp) * pose.head.rotation;

            void SwingLeg(Transform thigh, Transform calf, float angleDeg)
            {
                if (!thigh) return;
                thigh.rotation = Quaternion.AngleAxis(angleDeg, rootRight) * thigh.rotation;
                if (calf)
                {
                    float kneeBend = Mathf.Max(4f, -angleDeg * 0.85f + crouchT * 38f);
                    calf.rotation = Quaternion.AngleAxis(kneeBend, rootRight) * calf.rotation;
                }
            }
            SwingLeg(pose.thighL, pose.calfL, stride * 24f - crouchT * 36f);
            SwingLeg(pose.thighR, pose.calfR, -stride * 24f - crouchT * 32f);

            if (gun == null || !gun.root || !gun.trigger || !gun.support) return;

            float scale = pose.modelRoot.lossyScale.y;
            if (scale < 0.01f) scale = 1f;
            gun.root.localScale = Vector3.one * 0.80f;

            // Rifle orientation & placement on chest/shoulder
            float pitch = firing ? (reloading ? 16f : 0f) : 24f + stride * 8f;
            float yaw = firing ? (reloading ? -20f : 0f) : -20f + stride * 5f;
            gun.root.rotation = rootRot * Quaternion.Euler(pitch, yaw, reloading ? 12f : 0f);
            Vector3 torsoAnchor = pose.spine03 ? pose.spine03.position : (pose.modelRoot.position + rootUp * (1.28f * scale));
            Vector3 desiredTrigger = firing && gun.stock
                ? (pose.upperArmR ? pose.upperArmR.position : torsoAnchor + rootRight * (0.16f * scale)) + forwardDir * (0.04f * scale) - rootUp * (0.05f * scale)
                : torsoAnchor + (forwardDir * 0.16f + rootRight * 0.10f - rootUp * 0.08f) * scale;
            Vector3 socketPos = (firing && gun.stock) ? gun.stock.position : gun.trigger.position;
            gun.root.position += desiredTrigger - socketPos;

            // Axis-agnostic two-segment arm aiming toward the trigger and fore-end grips
            AimLimbSegment(pose.upperArmR, pose.lowerArmR,
                (pose.upperArmR ? pose.upperArmR.position : torsoAnchor) + (rootRight * 0.13f - rootUp * 0.15f + forwardDir * 0.08f) * scale);
            AimLimbSegment(pose.lowerArmR, pose.handR, gun.trigger.position + rootUp * (0.025f * scale));

            Vector3 supportTarget = gun.support.position - gun.root.forward * (0.045f * scale) + rootUp * (0.015f * scale);
            AimLimbSegment(pose.upperArmL, pose.lowerArmL,
                (pose.upperArmL ? pose.upperArmL.position : torsoAnchor) + (-rootRight * 0.09f - rootUp * 0.17f + forwardDir * 0.13f) * scale);
            AimLimbSegment(pose.lowerArmL, pose.handL, supportTarget);
        }

        static void AimLimbSegment(Transform segment, Transform child, Vector3 worldTarget)
        {
            if (!segment || !child) return;
            Vector3 current = child.position - segment.position;
            Vector3 desired = worldTarget - segment.position;
            if (current.sqrMagnitude < 0.0001f || desired.sqrMagnitude < 0.0001f) return;
            segment.rotation = Quaternion.FromToRotation(current.normalized, desired.normalized) * segment.rotation;
        }

        // ---------------- Malcolm Vale (Portrait-aligned Trenchcoat + Half-Respirator on Existing Rig) ----------------
        public static void BuildValeLook(DetailedHero3D body, SkinnedMeshRenderer[] armorParts, SkinnedMeshRenderer common, GameObject root)
        {
            if (!body) return;
            var owner = root.GetComponent<ResourceOwner>() ?? root.AddComponent<ResourceOwner>();

            Material MakeMat(string baseKey, string name, Color tint)
            {
                var mat = new Material(DetailedHeroMaterials3D.Get(baseKey)) { name = "Vale / " + name };
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                owner.owned.Add(mat);
                return mat;
            }

            // 1) Recolor shared Warden armor slots to Vale's dark weathered charcoal-brown trenchcoat & dark trousers
            var coatMat = MakeMat("ClothDark", "Trenchcoat", new Color(0.32f, 0.24f, 0.18f));
            var coatTrimMat = MakeMat("LeatherDark", "CoatLapel", new Color(0.26f, 0.19f, 0.14f));
            var pantsMat = MakeMat("ClothDark", "Pants", new Color(0.22f, 0.20f, 0.17f));
            var scarfMat = MakeMat("Cloth", "CrimsonScarf", new Color(0.42f, 0.12f, 0.11f));
            var bronzeMat = MakeMat("Brass", "RespiratorBronze", new Color(0.56f, 0.42f, 0.25f));
            var darkMetal = MakeMat("ProductSteel", "MaskGrille", new Color(0.20f, 0.20f, 0.21f));
            var leather = MakeMat("Leather", "HarnessLeather", new Color(0.32f, 0.22f, 0.14f));
            var scarMat = MakeMat("Cloth", "BrowScar", new Color(0.46f, 0.17f, 0.15f));

            if (armorParts != null)
            {
                TintSlot(armorParts.Length > 1 ? armorParts[1] : null, new Color(0.46f, 0.35f, 0.26f), owner);
                TintSlot(armorParts.Length > 2 ? armorParts[2] : null, new Color(0.44f, 0.34f, 0.25f), owner);
                TintSlot(armorParts.Length > 3 ? armorParts[3] : null, new Color(0.32f, 0.29f, 0.26f), owner);
                TintSlot(armorParts.Length > 4 ? armorParts[4] : null, new Color(0.26f, 0.21f, 0.17f), owner);
            }

            // 2) Bare Head: Keep full CommonBody approved head + Right-brow scar + Half-Respirator (NO Compact on h so legacy head/hair stays hidden!)
            var h = body.head;
            if (h)
            {
                var valeHeadAccessories = new GameObject("Vale head accessories").transform;
                valeHeadAccessories.SetParent(h, false);

                // Diagonal scar directly above right eye/brow (portrait-vale-v8 signature)
                DetailedHeroArsenal3D.Rod("Vale right brow scar", valeHeadAccessories, scarMat, new Vector3(0.022f, 0.068f, 0.102f), new Vector3(0.044f, 0.098f, 0.094f), 0.0034f);

                // Lower-face Bronze Dual-Canister Half-Respirator (NO goggles -> weathered eyes & brows stay bare!)
                var maskRoot = new GameObject("Vale half respirator").transform;
                maskRoot.SetParent(valeHeadAccessories, false);
                maskRoot.localPosition = new Vector3(0f, 0.028f, 0.094f);
                maskRoot.localScale = Vector3.one * 0.88f;

                // Sculpted bronze nose-mouth cup + slatted center intake
                DetailedHeroArsenal3D.Shell("Respirator center cup", maskRoot, bronzeMat, new Vector3(0f, 0.004f, 0.014f), new Vector2(0.092f, 0.098f), 0.034f);
                var vent = DetailedHeroArsenal3D.Tube("Respirator intake rim", maskRoot, darkMetal, new Vector3(0f, -0.012f, 0.036f), 0.022f, 0.022f, 0.72f, 0.020f, 20);
                for (int r = -1; r <= 1; r++)
                    DetailedHeroArsenal3D.Rod("Intake slat", vent, darkMetal, new Vector3(-0.014f, r * 0.006f, 0.021f), new Vector3(0.014f, r * 0.006f, 0.021f), 0.0018f);

                // Twin dual-side bronze filter canisters angled out on cheeks + rear harness straps
                for (int s = -1; s <= 1; s += 2)
                {
                    var can = new GameObject(s < 0 ? "Left canister" : "Right canister").transform;
                    can.SetParent(maskRoot, false);
                    can.localPosition = new Vector3(s * 0.048f, -0.014f, 0.006f);
                    can.localRotation = Quaternion.Euler(14f, s * 46f, 0f);
                    DetailedHeroArsenal3D.Tube("Filter canister drum", can, bronzeMat, Vector3.zero, 0.031f, 0.030f, 0.85f, 0.030f, 24);
                    DetailedHeroArsenal3D.Tube("Filter core plate", can, darkMetal, new Vector3(0f, 0f, 0.024f), 0.025f, 0.005f, 0.08f, -1f, 20);

                    // Upper & lower leather head straps wrapping back around temples & jawline
                    DetailedHeroArsenal3D.Line("Upper mask strap", valeHeadAccessories, leather, 0.0042f,
                        new Vector3(s * 0.040f, 0.064f, 0.098f),
                        new Vector3(s * 0.088f, 0.084f, 0.020f),
                        new Vector3(s * 0.068f, 0.090f, -0.068f));
                    DetailedHeroArsenal3D.Line("Lower mask strap", valeHeadAccessories, leather, 0.0042f,
                        new Vector3(s * 0.052f, 0.014f, 0.082f),
                        new Vector3(s * 0.082f, 0.022f, 0.005f),
                        new Vector3(s * 0.062f, 0.028f, -0.070f));
                }
                DetailedHeroArsenal3D.Compact(valeHeadAccessories);
            }

            // 3) Chest: Deep Crimson Neck Scarf + Popped Trenchcoat Collar + Flat Flush Lapels + Brass Buttons + Right D-Ring + Diagonal Strap
            var ch = body.chest;
            if (ch)
            {
                // Wrapped crimson bandana scarf tucked around neck (no Shell steel rim!)
                var sc = new GameObject("Vale crimson neck scarf").transform;
                sc.SetParent(ch, false);
                sc.localPosition = new Vector3(0f, 0.215f, 0.012f);
                var n1 = DetailedHeroArsenal3D.Tube("Scarf wrap lower", sc, scarfMat, Vector3.zero, 0.108f, 0.055f, 0.68f, 0.096f, 28);
                n1.localRotation = Quaternion.Euler(94f, 0f, 0f);
                AddClothDome("Scarf front fold", sc, scarfMat, new Vector3(0f, -0.055f, 0.092f), new Vector3(0.155f, 0.125f, 0.048f));

                // Popped trenchcoat high collar around back & sides of neck
                var colRoot = new GameObject("Vale trenchcoat collar").transform;
                colRoot.SetParent(ch, false);
                colRoot.localPosition = new Vector3(0f, 0.228f, -0.012f);
                colRoot.localRotation = Quaternion.Euler(78f, 0f, 0f);
                DetailedHeroArsenal3D.Tube("High coat collar", colRoot, coatMat, Vector3.zero, 0.142f, 0.092f, 0.84f, 0.132f, 28);

                // Flat flush chest lapels (thin conformal panels angled across upper chest) + brass buttons + right D-ring
                for (int s = -1; s <= 1; s += 2)
                {
                    var lapel = AddClothDome(s < 0 ? "Left lapel" : "Right lapel", ch, coatTrimMat,
                        new Vector3(s * 0.092f, 0.125f, 0.128f), new Vector3(0.095f, 0.155f, 0.022f));
                    lapel.localRotation = Quaternion.Euler(-10f, s * 22f, s * -16f);

                    DetailedHeroArsenal3D.Tube("Lapel brass button", ch, bronzeMat, new Vector3(s * 0.105f, 0.155f, 0.148f), 0.012f, 0.008f, 0.05f, -1f, 16);
                    DetailedHeroArsenal3D.Tube("Coat brass button", ch, bronzeMat, new Vector3(s * 0.055f, 0.01f, 0.158f), 0.012f, 0.008f, 0.05f, -1f, 16);
                }
                // Right chest (+X) D-ring tab from lapel (visible clearly on portrait-vale-v8)
                DetailedHeroArsenal3D.Rod("Right chest D-ring tab", ch, pantsMat, new Vector3(0.085f, 0.145f, 0.156f), new Vector3(0.085f, 0.092f, 0.156f), 0.011f);
                DetailedHeroArsenal3D.Tube("Right chest brass D-ring", ch, bronzeMat, new Vector3(0.085f, 0.082f, 0.162f), 0.018f, 0.007f, 0.62f, -1f, 20);

                // Diagonal cross-chest leather strap + brass buckle slider
                DetailedHeroArsenal3D.Line("Cross chest leather strap", ch, leather, 0.015f,
                    new Vector3(-0.075f, 0.19f, 0.132f), new Vector3(0.0f, 0.10f, 0.152f), new Vector3(0.115f, -0.06f, 0.138f));
                DetailedHeroArsenal3D.Tube("Chest strap brass buckle", ch, bronzeMat, new Vector3(-0.018f, 0.115f, 0.155f), 0.022f, 0.010f, 0.55f, -1f, 8);
            }

            // 4) Smooth draped shoulder capelets hugging upper arms cleanly (no Shell rivets)
            void BuildShoulderCapelet(Transform arm, int side)
            {
                if (!arm) return;
                for (int layer = 0; layer < 2; layer++)
                {
                    var flap = AddClothDome("Vale shoulder capelet " + layer, arm, layer == 0 ? coatMat : coatTrimMat,
                        new Vector3(side * (0.016f + layer * 0.010f), -0.020f - layer * 0.036f, 0f),
                        new Vector3(0.032f, 0.095f, 0.155f + layer * 0.015f));
                    flap.localRotation = Quaternion.Euler(0f, 0f, side * -(12f + layer * 8f));
                }
            }
            BuildShoulderCapelet(body.leftUpperArm, -1);
            BuildShoulderCapelet(body.rightUpperArm, 1);

            // 5) Pelvis / Waist: Heavy Leather Belt + Big Brass Buckle + Crimson Waist Sash Tie
            var p = body.pelvis;
            if (p)
            {
                var belt = DetailedHeroArsenal3D.Tube("Vale waist belt", p, leather, new Vector3(0f, 0.01f, 0f), 0.172f, 0.042f, 0.85f, 0.168f, 28);
                belt.localRotation = Quaternion.Euler(90f, 0f, 0f);
                DetailedHeroArsenal3D.Tube("Vale brass belt buckle", p, bronzeMat, new Vector3(0f, -0.012f, 0.158f), 0.034f, 0.014f, 0.52f, -1f, 4);

                // Crimson cloth waist sash tie dangling down right hip
                DetailedHeroArsenal3D.Rod("Crimson waist sash knot", p, scarfMat, new Vector3(-0.058f, 0.005f, 0.156f), new Vector3(-0.068f, -0.145f, 0.162f), 0.022f);
                DetailedHeroArsenal3D.Rod("Crimson waist sash tail", p, scarfMat, new Vector3(-0.042f, 0.005f, 0.156f), new Vector3(-0.048f, -0.115f, 0.164f), 0.018f);
            }
        }

        static Transform AddClothDome(string name, Transform parent, Material mat, Vector3 localPos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            const int rings = 10, segs = 16;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                float phi = v * Mathf.PI;
                float sinPhi = Mathf.Sin(phi), cosPhi = -Mathf.Cos(phi);
                for (int s = 0; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    float theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(theta) * sinPhi * scale.x * 0.5f, cosPhi * scale.y * 0.5f, Mathf.Sin(theta) * sinPhi * scale.z * 0.5f));
                    uvs.Add(new Vector2(u, v));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int a = r * (segs + 1) + s, b = a + segs + 1;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var owner = parent ? parent.GetComponentInParent<ResourceOwner>() : null;
            if (owner) owner.owned.Add(mesh);
            return go.transform;
        }

        static void TintSlot(SkinnedMeshRenderer renderer, Color tint, ResourceOwner owner)
        {
            if (!renderer) return;
            var mats = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                var m = new Material(mats[i]) { name = mats[i].name + " (Vale)" };
                owner.owned.Add(m);
                if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
                mats[i] = m;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = mats;
        }
    }
}
