using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    /// <summary>
    /// PS3-grade runtime builders, PBR texture/normal/smoothness configurator, and full-body
    /// Two-Bone Pole IK + finger-grip + walk/crouch/reload/death animator for Raven, Vex, and Malcolm Vale.
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
            public Transform thighL, calfL, footL;
            public Transform thighR, calfR, footR;
            public readonly List<Transform> fingersL = new List<Transform>();
            public readonly List<Transform> fingersR = new List<Transform>();
            public Vector3 pelvisRestPos;
            public readonly Dictionary<Transform, Quaternion> restLocalRot = new Dictionary<Transform, Quaternion>();
        }

        struct TexSpec
        {
            public string tex, normal, metallicGloss, ao;
            public float metallic, smoothness, bump, aoStrength;
            public Color tint;
        }

        static readonly Dictionary<string, TexSpec> BossTexTable = new Dictionary<string, TexSpec>
        {
            { "Skin", new TexSpec { tex = "{k}_body", normal = "{k}_skin_n", ao = "{k}_skin_ao", metallic = 0f, smoothness = 0.24f, bump = 0.65f, aoStrength = 0.85f, tint = Color.white } },
            { "HeadPBR", new TexSpec { tex = "{k}_head_pbr_a", normal = "{k}_head_pbr_n", metallicGloss = "{k}_head_pbr_m", ao = "{k}_head_pbr_ao", bump = 0.95f, aoStrength = 0.90f, tint = Color.white } },
            { "OutfitPBR", new TexSpec { tex = "{k}_outfit_pbr_a", normal = "{k}_outfit_pbr_n", metallicGloss = "{k}_outfit_pbr_m", ao = "{k}_outfit_pbr_ao", bump = 0.95f, aoStrength = 0.90f, tint = Color.white } },
        };

        public static CustomRigPose AttachBossModel(GameObject root, int faction, out DetailedHeroEquipment3D.WeaponSockets gun)
        {
            string key = faction == 1 ? "Raven" : "Vex";
            string lower = faction == 1 ? "raven" : "vex";
            var prefab = Resources.Load<GameObject>("Characters/" + key + "/" + key);
            if (!prefab) { gun = null; return null; }

            var model = Object.Instantiate(prefab, root.transform, false);
            model.name = key + " Blender Boss Model";
            model.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            ApplyBossTextures(model, key, lower, root);
            ConfigureSkinnedRenderers(model);

            var pose = CaptureRigPose(model.transform);
            gun = DetailedHeroEquipment3D.BuildRifle(root.transform, DetailedHeroMaterials3D.Get);
            PoseCustomRig(pose, gun, Vector3.forward, true, -1f, false, false, false, -1f, 0f, faction);
            return pose;
        }

        static void ConfigureSkinnedRenderers(GameObject model)
        {
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (Application.isMobilePlatform)
                {
                    skin.quality = SkinQuality.Bone2;
                    skin.updateWhenOffscreen = false;
                    skin.shadowCastingMode = ShadowCastingMode.Off;
                    skin.receiveShadows = false;
                }
                else
                {
                    skin.updateWhenOffscreen = true;
                }
            }
        }

        static void ApplyTextureSet(
            GameObject target, string texDir, Dictionary<string, TexSpec> table,
            System.Func<string, string> resolveTexName, ResourceOwner owner)
        {
            var cloned = new Dictionary<Material, Material>();
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (!src) continue;
                    if (!cloned.TryGetValue(src, out var dst))
                    {
                        dst = new Material(src) { name = src.name + " (HQ)" };
                        owner.owned.Add(dst);
                        int idx = src.name.IndexOf('_');
                        string suffix = idx >= 0 && idx + 1 < src.name.Length ? src.name.Substring(idx + 1) : src.name;
                        int dot = suffix.IndexOf('.');
                        if (dot >= 0) suffix = suffix.Substring(0, dot);
                        int spIdx = suffix.IndexOf(" (Instance)");
                        if (spIdx >= 0) suffix = suffix.Substring(0, spIdx);
                        if (table.TryGetValue(suffix, out var sp))
                            ApplyTexSpec(dst, texDir, sp, resolveTexName);
                        cloned[src] = dst;
                    }
                    mats[i] = dst;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static void ApplyTexSpec(Material mat, string texDir, TexSpec sp, System.Func<string, string> resolveTexName)
        {
            string texName = resolveTexName(sp.tex);
            if (!string.IsNullOrEmpty(texName))
            {
                var tex = Resources.Load<Texture2D>(texDir + texName);
                if (tex && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            string normName = resolveTexName(sp.normal);
            if (!string.IsNullOrEmpty(normName))
            {
                var nrm = Resources.Load<Texture2D>(texDir + normName);
                if (nrm && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", nrm);
                    mat.EnableKeyword("_NORMALMAP");
                    if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", sp.bump);
                }
            }
            string mgName = resolveTexName(sp.metallicGloss);
            if (!string.IsNullOrEmpty(mgName))
            {
                var mg = Resources.Load<Texture2D>(texDir + mgName);
                if (mg && mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", mg);
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                    if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 1.0f);
                }
            }
            string aoName = resolveTexName(sp.ao);
            if (!string.IsNullOrEmpty(aoName))
            {
                var ao = Resources.Load<Texture2D>(texDir + aoName);
                if (ao && mat.HasProperty("_OcclusionMap"))
                {
                    mat.SetTexture("_OcclusionMap", ao);
                    if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", sp.aoStrength > 0f ? sp.aoStrength : 0.90f);
                }
            }
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", sp.tint);
            if (string.IsNullOrEmpty(mgName))
            {
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", sp.metallic);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", sp.smoothness);
            }
        }

        static void ApplyBossTextures(GameObject model, string key, string lower, GameObject ownerGo)
        {
            var owner = ownerGo.GetComponent<ResourceOwner>() ?? ownerGo.AddComponent<ResourceOwner>();
            ApplyTextureSet(model, "Characters/" + key + "/Textures/", BossTexTable, p => string.IsNullOrEmpty(p) ? null : p.Replace("{k}", lower), owner);
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
                footL = FindBone("foot_l"),
                thighR = FindBone("thigh_r"),
                calfR = FindBone("calf_r"),
                footR = FindBone("foot_r"),
            };

            string[] fingerPrefixes = { "index_01", "index_02", "middle_01", "middle_02", "ring_01", "ring_02", "pinky_01", "pinky_02" };
            foreach (string p in fingerPrefixes)
            {
                var fl = FindBone(p + "_l"); if (fl) pose.fingersL.Add(fl);
                var fr = FindBone(p + "_r"); if (fr) pose.fingersR.Add(fr);
            }
            var tl = FindBone("thumb_01_l"); if (tl) pose.fingersL.Add(tl);
            var tr = FindBone("thumb_01_r"); if (tr) pose.fingersR.Add(tr);

            if (pose.pelvis) pose.pelvisRestPos = pose.pelvis.localPosition;
            Transform[] tracked = {
                pose.pelvis, pose.spine02, pose.spine03, pose.head,
                pose.upperArmL, pose.lowerArmL, pose.handL,
                pose.upperArmR, pose.lowerArmR, pose.handR,
                pose.thighL, pose.calfL, pose.footL,
                pose.thighR, pose.calfR, pose.footR
            };
            foreach (var b in tracked)
                if (b) pose.restLocalRot[b] = b.localRotation;
            foreach (var f in pose.fingersL) pose.restLocalRot[f] = f.localRotation;
            foreach (var f in pose.fingersR) pose.restLocalRot[f] = f.localRotation;
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

            float crouchT = crouch ? 1f : roll ? 0.65f : 0f;
            float rollTilt = roll ? Mathf.Sin(Time.time * 24f) * 16f : 0f;
            float leanZ = Mathf.Clamp(-lateralVel * 4.5f, -15f, 15f) + rollTilt;

            if (deathTime >= 0f)
            {
                float buckle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deathTime / 0.28f));
                float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((deathTime - 0.12f) / 0.68f));
                float side = faction == 2 ? -1f : 1f;
                pose.modelRoot.localRotation = Quaternion.Euler(fall * 78f, side * fall * 10f, side * fall * 22f);
                pose.modelRoot.localPosition = new Vector3(side * fall * 0.06f, 0.03f - buckle * 0.10f + fall * 0.20f, -fall * 0.25f);
                if (pose.pelvis) pose.pelvis.localPosition = pose.pelvisRestPos;
                if (pose.spine02) pose.spine02.rotation = Quaternion.AngleAxis(-buckle * 14f, rootRight) * pose.spine02.rotation;
                if (pose.thighL) pose.thighL.rotation = Quaternion.AngleAxis(-buckle * 34f, rootRight) * pose.thighL.rotation;
                if (pose.calfL) pose.calfL.rotation = Quaternion.AngleAxis(buckle * 58f, rootRight) * pose.calfL.rotation;
                if (pose.thighR) pose.thighR.rotation = Quaternion.AngleAxis(-buckle * 22f, rootRight) * pose.thighR.rotation;
                if (pose.calfR) pose.calfR.rotation = Quaternion.AngleAxis(buckle * 42f, rootRight) * pose.calfR.rotation;
                if (gun != null && gun.root) gun.root.gameObject.SetActive(false);
                return;
            }

            if (gun != null && gun.root && !gun.root.gameObject.activeSelf) gun.root.gameObject.SetActive(true);
            pose.modelRoot.localRotation = Quaternion.Euler(14f * crouchT, 0f, leanZ);
            pose.modelRoot.localPosition = new Vector3(0f, 0.03f - 0.20f * crouchT, 0f);

            // Anatomical sagittal gait cycle:
            // Positive AngleAxis around +rootRight swings limb backward (-Z); negative swings forward (+Z).
            // Knees strictly bend backward (+AngleAxis on calf around +rootRight) to prevent reverse hyperextension.
            float stride = phase >= 0f ? Mathf.Sin(phase * Mathf.PI * 2f) : 0f;
            float liftL = phase >= 0f ? Mathf.Max(0f, Mathf.Cos(phase * Mathf.PI * 2f)) : 0f;
            float liftR = phase >= 0f ? Mathf.Max(0f, -Mathf.Cos(phase * Mathf.PI * 2f)) : 0f;
            float bob = phase >= 0f ? Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f)) * 0.024f : Mathf.Sin(Time.time * 2.2f) * 0.006f;
            if (pose.pelvis) pose.pelvis.localPosition = pose.pelvisRestPos + new Vector3(0f, bob, 0f);
            if (pose.spine02) pose.spine02.rotation = Quaternion.AngleAxis(firing ? 22f : 8f, rootUp) * pose.spine02.rotation;
            if (pose.head) pose.head.rotation = Quaternion.AngleAxis(firing ? -18f : -6f, rootUp) * pose.head.rotation;

            void PoseLeg(Transform thigh, Transform calf, Transform foot, float thighDeg, float kneeDeg)
            {
                if (!thigh) return;
                thigh.rotation = Quaternion.AngleAxis(thighDeg, rootRight) * thigh.rotation;
                if (calf) calf.rotation = Quaternion.AngleAxis(kneeDeg, rootRight) * calf.rotation;
                if (foot) foot.rotation = Quaternion.AngleAxis(-(thighDeg + kneeDeg) * 0.85f, rootRight) * foot.rotation;
            }

            float thL = stride * 24f - crouchT * 40f - 4f;
            float thR = -stride * 24f - crouchT * 36f + 4f;
            float knL = 10f + liftL * 34f + crouchT * 72f;
            float knR = 10f + liftR * 34f + crouchT * 68f;
            PoseLeg(pose.thighL, pose.calfL, pose.footL, thL, knL);
            PoseLeg(pose.thighR, pose.calfR, pose.footR, thR, knR);

            if (gun == null || !gun.root || !gun.trigger || !gun.support) return;

            float scale = pose.modelRoot.lossyScale.y;
            if (scale < 0.01f) scale = 1f;
            gun.root.localScale = Vector3.one * 0.80f;

            float reloadSwing = reloading ? Mathf.Sin(Time.time * 14f) : 0f;
            float pitch = firing ? (reloading ? 18f + reloadSwing * 10f : 0f) : 22f + stride * 7f;
            float yaw = firing ? (reloading ? -18f + reloadSwing * 8f : 0f) : -20f + stride * 5f;
            gun.root.rotation = rootRot * Quaternion.Euler(pitch, yaw, reloading ? 14f : 0f);

            Vector3 torsoAnchor = pose.spine03 ? pose.spine03.position : (pose.modelRoot.position + rootUp * (1.28f * scale));
            Vector3 desiredAnchor = firing && gun.stock
                ? (pose.upperArmR ? pose.upperArmR.position : torsoAnchor + rootRight * (0.16f * scale)) + forwardDir * (0.04f * scale) - rootUp * (0.05f * scale)
                : torsoAnchor + (forwardDir * 0.16f + rootRight * 0.10f - rootUp * 0.09f) * scale;
            Vector3 socketPos = (firing && gun.stock) ? gun.stock.position : gun.trigger.position;
            gun.root.position += desiredAnchor - socketPos;

            // Two-bone analytical pole-vector IK so elbows always bend naturally down/outward without twisting
            Vector3 rTarget = gun.trigger.position + rootUp * (0.020f * scale);
            Vector3 rPole = torsoAnchor + (rootRight * 0.34f - rootUp * 0.42f - forwardDir * 0.22f) * scale;
            SolveTwoBoneIK(pose.upperArmR, pose.lowerArmR, pose.handR, rTarget, rPole);

            Vector3 lTarget = gun.support.position - gun.root.forward * (0.045f * scale) + rootUp * (0.015f * scale)
                + (reloading ? (-rootUp * 0.08f + reloadSwing * rootRight * 0.04f) * scale : Vector3.zero);
            Vector3 lPole = torsoAnchor + (-rootRight * 0.30f - rootUp * 0.42f - forwardDir * 0.16f) * scale;
            SolveTwoBoneIK(pose.upperArmL, pose.lowerArmL, pose.handL, lTarget, lPole);

            // Align wrists to cradle fore-end / grip naturally, then curl fingers inward
            if (pose.handR) pose.handR.rotation = Quaternion.AngleAxis(24f, gun.root.forward) * pose.handR.rotation;
            if (pose.handL) pose.handL.rotation = Quaternion.AngleAxis(-58f, gun.root.forward) * pose.handL.rotation;
            CurlFingers(pose.fingersR, -1f, 54f);
            CurlFingers(pose.fingersL, 1f, 54f);
        }

        static void SolveTwoBoneIK(Transform upper, Transform lower, Transform end, Vector3 targetPos, Vector3 polePos)
        {
            if (!upper || !lower || !end) return;
            Vector3 rootPos = upper.position;
            float lenA = Vector3.Distance(rootPos, lower.position);
            float lenB = Vector3.Distance(lower.position, end.position);
            if (lenA < 0.01f || lenB < 0.01f) return;

            Vector3 toTarget = targetPos - rootPos;
            float dist = Mathf.Clamp(toTarget.magnitude, 0.04f, (lenA + lenB) * 0.985f);
            Vector3 dir = toTarget.sqrMagnitude > 0.00001f ? toTarget.normalized : Vector3.forward;

            Vector3 bendPlane = polePos - rootPos;
            bendPlane -= dir * Vector3.Dot(bendPlane, dir);
            if (bendPlane.sqrMagnitude < 0.0001f) bendPlane = Vector3.down;
            bendPlane.Normalize();

            float cosA = Mathf.Clamp((lenA * lenA + dist * dist - lenB * lenB) / (2f * lenA * dist), -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));
            Vector3 elbowPos = rootPos + (dir * cosA + bendPlane * sinA) * lenA;

            Vector3 curUpper = lower.position - upper.position;
            Vector3 desUpper = elbowPos - upper.position;
            if (curUpper.sqrMagnitude > 0.00001f && desUpper.sqrMagnitude > 0.00001f)
                upper.rotation = Quaternion.FromToRotation(curUpper.normalized, desUpper.normalized) * upper.rotation;

            Vector3 curLower = end.position - lower.position;
            Vector3 desLower = targetPos - lower.position;
            if (curLower.sqrMagnitude > 0.00001f && desLower.sqrMagnitude > 0.00001f)
                lower.rotation = Quaternion.FromToRotation(curLower.normalized, desLower.normalized) * lower.rotation;
        }

        static void CurlFingers(List<Transform> fingers, float signX, float deg)
        {
            for (int i = 0; i < fingers.Count; i++)
                if (fingers[i])
                    fingers[i].localRotation *= Quaternion.Euler(deg * signX, 0f, 0f);
        }

        static readonly Dictionary<string, TexSpec> ValeBossTexTable = new Dictionary<string, TexSpec>
        {
            { "body",      new TexSpec { tex = "vale_body_a",      normal = "vale_body_n",      metallicGloss = "vale_body_m",      ao = "vale_body_ao",      bump = 0.90f, aoStrength = 0.88f, tint = Color.white } },
            { "waistcoat", new TexSpec { tex = "vale_waistcoat_a", normal = "vale_waistcoat_n", metallicGloss = "vale_waistcoat_m", ao = "vale_waistcoat_ao", bump = 0.95f, aoStrength = 0.90f, tint = Color.white } },
            { "pants",     new TexSpec { tex = "vale_pants_a",     normal = "vale_pants_n",     metallicGloss = "vale_pants_m",     ao = "vale_pants_ao",     bump = 0.90f, aoStrength = 0.88f, tint = Color.white } },
        };

        /// <summary>
        /// Customizes the imported bald/skinhead RaiderBossNPC / Enemy_11 model for Malcolm Vale:
        /// removes sunglasses, applies 4-view painted veteran PBR maps, and mounts only the lower-face bronze respirator.
        /// </summary>
        public static void CustomizeImportedVale(Transform root, Transform model, Transform actor, float targetHeight)
        {
            if (!root || !model || !actor) return;
            var owner = root.GetComponent<ResourceOwner>() ?? root.gameObject.AddComponent<ResourceOwner>();
            const string texDir = "Characters/ValeBoss/Textures/";

            var cloned = new Dictionary<Material, Material>();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "polySurface45" || r.name.IndexOf("glass", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    r.enabled = false; // Expose Malcolm Vale's eyes & scarred right brow!
                    continue;
                }
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (!src) continue;
                    string lower = src.name.ToLowerInvariant();
                    string key = lower.Contains("body") ? "body" : lower.Contains("waistcoat") ? "waistcoat" : lower.Contains("pants") ? "pants" : null;
                    if (key == null) continue;
                    if (!cloned.TryGetValue(src, out var dst))
                    {
                        dst = new Material(src) { name = src.name + " (ValeHQ)" };
                        owner.owned.Add(dst);
                        ApplyTexSpec(dst, texDir, ValeBossTexTable[key], s => s);
                        cloned[src] = dst;
                    }
                    mats[i] = dst;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }

            // Attach ONLY the bronze dual-canister half-respirator (leaves scarred brow & eyes clear)
            Transform head = null;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                if (bone.name == "Head_M" || bone.name == "head") { head = bone; break; }
            var propsPrefab = Resources.Load<GameObject>("Characters/ValeProps/ValeProps");
            if (head && propsPrefab)
            {
                var propsRoot = Object.Instantiate(propsPrefab);
                propsRoot.name = "ValeHalfMaskTemp";
                ApplyValeTextures(propsRoot, owner);
                Transform maskObj = null;
                foreach (var t in propsRoot.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Vale_HeadProps") { maskObj = t; break; }
                if (maskObj)
                {
                    var socket = new GameObject("Vale half respirator socket").transform;
                    socket.SetParent(head, true);
                    // Skin_1 head/jaw protrudes farther forward than CommonBody; place mask cleanly on nose/mouth bridge
                    socket.position = head.position + actor.up * (targetHeight * -0.014f) + actor.forward * (targetHeight * 0.022f);
                    socket.rotation = actor.rotation;
                    Vector3 ls = head.lossyScale;
                    float scaleFactor = (targetHeight / 1.80f);
                    socket.localScale = new Vector3(
                        scaleFactor / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                        scaleFactor / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
                        scaleFactor / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
                    maskObj.SetParent(socket, false);
                    maskObj.localPosition = Vector3.zero;
                    maskObj.localRotation = Quaternion.identity;
                    maskObj.localScale = Vector3.one;
                }
                if (Application.isPlaying) Object.Destroy(propsRoot);
                else Object.DestroyImmediate(propsRoot);
            }
        }

        // ---------------- Malcolm Vale: Keeps Existing DetailedHero3D + CommonBody Skeleton + Attaches PS3 ValeProps ----------------
        public static void BuildValeLook(DetailedHero3D body, SkinnedMeshRenderer[] armorParts, SkinnedMeshRenderer common, GameObject root)
        {
            if (!body) return;
            var owner = root.GetComponent<ResourceOwner>() ?? root.gameObject.AddComponent<ResourceOwner>();

            // 1) Recolor existing Warden articulated armor slots to matte weathered trench/trouser tones + veteran face
            if (armorParts != null)
            {
                TintSlot(armorParts.Length > 1 ? armorParts[1] : null, new Color(0.28f, 0.21f, 0.17f), owner);
                TintSlot(armorParts.Length > 2 ? armorParts[2] : null, new Color(0.26f, 0.20f, 0.16f), owner);
                TintSlot(armorParts.Length > 3 ? armorParts[3] : null, new Color(0.22f, 0.19f, 0.17f), owner);
                TintSlot(armorParts.Length > 4 ? armorParts[4] : null, new Color(0.18f, 0.14f, 0.11f), owner);
            }
            ApplyValeVeteranFace(common, owner);

            // 2) Instantiate Blender-sculpted & PBR-textured ValeProps and attach each group onto live DetailedHero3D bones
            var propsPrefab = Resources.Load<GameObject>("Characters/ValeProps/ValeProps");
            if (!propsPrefab) return;
            var propsRoot = Object.Instantiate(propsPrefab);
            propsRoot.name = "ValePropsTemp";
            ApplyValeTextures(propsRoot, owner);

            void AttachSocket(string childName, Transform bone)
            {
                if (!bone) return;
                Transform found = null;
                foreach (var t in propsRoot.GetComponentsInChildren<Transform>(true))
                    if (t.name == childName) { found = t; break; }
                if (!found) return;
                found.SetParent(bone, false);
                found.localPosition = Vector3.zero;
                found.localRotation = Quaternion.identity;
                found.localScale = Vector3.one;
            }

            AttachSocket("Vale_HeadProps", body.head);
            AttachSocket("Vale_ChestProps", body.chest);
            AttachSocket("Vale_ShoulderL", body.leftUpperArm);
            AttachSocket("Vale_ShoulderR", body.rightUpperArm);
            AttachSocket("Vale_PelvisProps", body.pelvis);

            if (Application.isPlaying) Object.Destroy(propsRoot);
            else Object.DestroyImmediate(propsRoot);
        }

        static readonly Dictionary<string, TexSpec> ValeTexTable = new Dictionary<string, TexSpec>
        {
            { "AtlasPBR", new TexSpec { tex = "vale_props_pbr_a", normal = "vale_props_pbr_n", metallicGloss = "vale_props_pbr_m", ao = "vale_props_pbr_ao", bump = 0.95f, aoStrength = 0.90f, tint = Color.white } },
        };

        static void ApplyValeTextures(GameObject propsRoot, ResourceOwner owner) =>
            ApplyTextureSet(propsRoot, "Characters/ValeProps/Textures/", ValeTexTable, s => s, owner);

        static void ApplyValeVeteranFace(SkinnedMeshRenderer common, ResourceOwner owner)
        {
            if (!common) return;
            var faceTex = Resources.Load<Texture2D>("Characters/ValeProps/Textures/vale_face");
            var faceNorm = Resources.Load<Texture2D>("Characters/ValeProps/Textures/vale_face_n");
            if (!faceTex) return;
            foreach (var smr in common.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name != "Common body / approved shared head") continue;
                var mats = smr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i]) continue;
                    var m = new Material(mats[i]) { name = mats[i].name + " (ValeFace)" };
                    owner.owned.Add(m);
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", faceTex);
                    if (faceNorm && m.HasProperty("_BumpMap"))
                    {
                        m.SetTexture("_BumpMap", faceNorm);
                        m.EnableKeyword("_NORMALMAP");
                        if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0.55f);
                    }
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.14f);
                    mats[i] = m;
                    changed = true;
                }
                if (changed) smr.sharedMaterials = mats;
                break;
            }
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
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.04f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.16f);
                mats[i] = m;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = mats;
        }
    }
}
