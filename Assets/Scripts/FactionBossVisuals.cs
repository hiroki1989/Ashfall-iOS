using System.Collections.Generic;
using System.Reflection;
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
        // A single geometry-free copy of the player's joints supplies the same
        // gait and weapon pose to every imported human, evaluated one at a time.
        static DetailedHero3D playerPoseDriver;
        static Transform PlayerJoint(string name, Transform parent, Vector3 position)
        {
            var joint = new GameObject(name).transform; joint.SetParent(parent, false); joint.localPosition = position; return joint;
        }
        static DetailedHero3D PlayerPoseDriver()
        {
            if (playerPoseDriver) return playerPoseDriver;
            var root = new GameObject("Shared human pose driver");
            root.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying) Object.DontDestroyOnLoad(root);
            var h = root.AddComponent<DetailedHero3D>();
            var rig = PlayerJoint("rig", root.transform, Vector3.zero);
            typeof(DetailedHero3D).GetField("rig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(h, rig);
            h.pelvis = PlayerJoint("pelvis", rig, new Vector3(0,.98f,0));
            h.chest = PlayerJoint("chest", rig, new Vector3(0,1.25f,0));
            h.head = PlayerJoint("head", h.chest, new Vector3(0,.30f,0));
            for (int side=-1;side<=1;side+=2)
            {
                var upper=PlayerJoint("upper arm",h.chest,new Vector3(side*.18f,.177f,0));
                var fore=PlayerJoint("forearm",upper,new Vector3(0,-DetailedHero3D.UpperArmLength,0));
                var hand=PlayerJoint("hand",fore,new Vector3(0,-DetailedHero3D.ForearmLength,0));
                var thigh=PlayerJoint("thigh",h.pelvis,new Vector3(side*.105f,-.025f,0));
                var shin=PlayerJoint("shin",thigh,new Vector3(0,-.438f,0));
                var foot=PlayerJoint("foot",shin,new Vector3(0,-.416f,0));
                var toe=PlayerJoint("toe",foot,new Vector3(0,-.089f,.12f));
                if(side<0){h.leftUpperArm=upper;h.leftForearm=fore;h.leftHand=hand;h.leftThigh=thigh;h.leftShin=shin;h.leftFoot=foot;h.leftToe=toe;}
                else{h.rightUpperArm=upper;h.rightForearm=fore;h.rightHand=hand;h.rightThigh=thigh;h.rightShin=shin;h.rightFoot=foot;h.rightToe=toe;}
            }
            playerPoseDriver=h; return h;
        }
        static void MatchDirection(Transform bone, Transform child, Transform reference, Transform referenceChild)
        {
            if(!bone||!child||!reference||!referenceChild)return;
            Vector3 from=child.position-bone.position, to=referenceChild.position-reference.position;
            if(from.sqrMagnitude>1e-8f&&to.sqrMagnitude>1e-8f)
                bone.rotation=Quaternion.FromToRotation(from,to)*bone.rotation;
        }
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
            // Player-motion retarget data, captured once in the authored bind pose.
            public Transform handTipL, handTipR;
            public Vector3 modelRestLocalPos;
            public Quaternion modelRestLocalRot = Quaternion.identity;
            public Vector3 restFootLocalL, restFootLocalR;
            public readonly Dictionary<Transform, Quaternion> retarget = new Dictionary<Transform, Quaternion>();
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
            { "MeshyPBR", new TexSpec { tex = "{k}_meshy_a", normal = "{k}_meshy_n", metallicGloss = "{k}_meshy_m", bump = 0.95f, tint = Color.white } },
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

        // Ordinary faction enemies use their own authored mesh and the same
        // articulated rig and poses as the existing human combatants.
        public static CustomRigPose AttachSoldierModel(GameObject root, int faction, int variant, out DetailedHeroEquipment3D.WeaponSockets gun)
        {
            string key = faction == 1 ? "Redcrow" : "Coyote";
            var prefab = Resources.Load<GameObject>("Characters/" + key + "/" + key);
            if (!prefab) { gun = null; return null; }
            var model = Object.Instantiate(prefab, root.transform, false);
            model.name = "Faction soldier model";
            model.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ApplyBossTextures(model, key, key.ToLowerInvariant(), root);
            ConfigureSkinnedRenderers(model);
            var pose = CaptureRigPose(model.transform);
            if (!pose.pelvis || !pose.upperArmL || !pose.upperArmR || !pose.thighL || !pose.thighR)
            {
                Object.Destroy(model); gun = null; return null;
            }
            gun = variant == 1 ? DetailedHeroEquipment3D.BuildPistol(root.transform, DetailedHeroMaterials3D.Get)
                               : DetailedHeroEquipment3D.BuildRifle(root.transform, DetailedHeroMaterials3D.Get);
            return pose;
        }

        public static CustomRigPose CaptureSoldierPose(Transform root)
        {
            var model = root.Find("Faction soldier model");
            return model ? CaptureRigPose(model) : null;
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

        internal static CustomRigPose CaptureRigPose(Transform modelRoot, bool includeFingers = true)
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
                // Unreal-style names (Raven, Vex, soldiers) with Mixamo fallbacks (Veil).
                pelvis = FindBone("pelvis") ?? FindBone("hips"),
                spine02 = FindBone("spine_02") ?? FindBone("spine1"),
                spine03 = FindBone("spine_03") ?? FindBone("spine2"),
                head = FindBone("head"),
                upperArmL = FindBone("upperarm_l") ?? FindBone("leftarm"),
                lowerArmL = FindBone("lowerarm_l") ?? FindBone("leftforearm"),
                handL = FindBone("hand_l") ?? FindBone("lefthand"),
                upperArmR = FindBone("upperarm_r") ?? FindBone("rightarm"),
                lowerArmR = FindBone("lowerarm_r") ?? FindBone("rightforearm"),
                handR = FindBone("hand_r") ?? FindBone("righthand"),
                thighL = FindBone("thigh_l") ?? FindBone("leftupleg"),
                calfL = FindBone("calf_l") ?? FindBone("leftleg"),
                footL = FindBone("foot_l") ?? FindBone("leftfoot"),
                thighR = FindBone("thigh_r") ?? FindBone("rightupleg"),
                calfR = FindBone("calf_r") ?? FindBone("rightleg"),
                footR = FindBone("foot_r") ?? FindBone("rightfoot"),
                handTipL = FindBone("middle_01_l") ?? FindBone("lefthandmiddle1"),
                handTipR = FindBone("middle_01_r") ?? FindBone("righthandmiddle1"),
            };

            if (includeFingers)
            {
                string[] fingerPrefixes = { "index_01", "index_02", "middle_01", "middle_02", "ring_01", "ring_02", "pinky_01", "pinky_02" };
                foreach (string p in fingerPrefixes)
                {
                    var fl = FindBone(p + "_l"); if (fl) pose.fingersL.Add(fl);
                    var fr = FindBone(p + "_r"); if (fr) pose.fingersR.Add(fr);
                }
                var tl = FindBone("thumb_01_l"); if (tl) pose.fingersL.Add(tl);
                var tr = FindBone("thumb_01_r"); if (tr) pose.fingersR.Add(tr);
            }

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
            CaptureRetargetRest(pose);
            return pose;
        }

        // The player's reference skeleton has identity joint rotations in its rest pose:
        // limbs point straight down, spine/neck straight up, facing +Z.
        static void CaptureRetargetRest(CustomRigPose pose)
        {
            var m = pose.modelRoot;
            pose.modelRestLocalPos = m.localPosition;
            pose.modelRestLocalRot = m.localRotation;
            Quaternion frame = m.rotation * Quaternion.Inverse(m.localRotation);
            Quaternion inv = Quaternion.Inverse(frame);
            void Add(Transform bone, Transform child, Vector3 playerDir)
            {
                if (!bone) return;
                Quaternion rel = inv * bone.rotation;
                Quaternion align = Quaternion.identity;
                if (child)
                {
                    Vector3 d = inv * (child.position - bone.position);
                    if (d.sqrMagnitude > 1e-8f) align = Quaternion.FromToRotation(d.normalized, playerDir);
                }
                pose.retarget[bone] = align * rel;
            }
            Add(pose.pelvis, pose.spine03, Vector3.up);
            Add(pose.spine03, pose.head, Vector3.up);
            Add(pose.head, null, Vector3.up);
            Add(pose.upperArmL, pose.lowerArmL, Vector3.down); Add(pose.lowerArmL, pose.handL, Vector3.down); Add(pose.handL, pose.handTipL, Vector3.down);
            Add(pose.upperArmR, pose.lowerArmR, Vector3.down); Add(pose.lowerArmR, pose.handR, Vector3.down); Add(pose.handR, pose.handTipR, Vector3.down);
            Add(pose.thighL, pose.calfL, Vector3.down); Add(pose.calfL, pose.footL, Vector3.down); Add(pose.footL, null, Vector3.forward);
            Add(pose.thighR, pose.calfR, Vector3.down); Add(pose.calfR, pose.footR, Vector3.down); Add(pose.footR, null, Vector3.forward);
            if (pose.footL) pose.restFootLocalL = m.InverseTransformPoint(pose.footL.position);
            if (pose.footR) pose.restFootLocalR = m.InverseTransformPoint(pose.footR.position);
        }

        static void CopyPlayerJoint(CustomRigPose pose, Transform bone, Transform reference)
        {
            if (bone && reference && pose.retarget.TryGetValue(bone, out var r))
                bone.rotation = reference.rotation * r;
        }

        public static void PoseCustomRig(
            CustomRigPose pose, DetailedHeroEquipment3D.WeaponSockets gun,
            Vector3 forwardDir, bool firing, float phase,
            bool crouch, bool roll, bool reloading, float deathTime, float lateralVel = 0f, int faction = 1, bool handgun = false,
            Vector3 velocity = default, float moveBlend = 0f)
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
            if (!crouch && !roll && !reloading && gun != null && gun.root && gun.trigger && gun.support)
            {
                PoseLikePlayer(pose, gun, forwardDir, firing, phase, handgun, velocity, moveBlend);
                return;
            }
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
            Vector3 desiredAnchor = handgun
                ? torsoAnchor + (forwardDir * 0.48f + rootRight * 0.12f - rootUp * 0.10f) * scale
                : firing && gun.stock
                ? (pose.upperArmR ? pose.upperArmR.position : torsoAnchor + rootRight * (0.16f * scale)) + forwardDir * (0.04f * scale) - rootUp * (0.05f * scale)
                : torsoAnchor + (forwardDir * 0.16f + rootRight * 0.10f - rootUp * 0.09f) * scale;
            Vector3 socketPos = (!handgun && firing && gun.stock) ? gun.stock.position : gun.trigger.position;
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

        // Drives an authored human rig with exactly the player's motion. The player's own
        // pose solver (DetailedHeroPlayer3D.Pose) runs on a geometry-free copy of the player
        // skeleton scaled to this character; every joint (pelvis, chest, head, arms, hands,
        // legs, feet) is copied, then the wrists are solved onto the same weapon sockets.
        internal static void PoseLikePlayer(CustomRigPose pose, DetailedHeroEquipment3D.WeaponSockets gun, Vector3 forward,
            bool firing, float phase, bool handgun, Vector3 velocity, float blend, bool curlFingers = true)
        {
            var h=PlayerPoseDriver();
            var m=pose.modelRoot;
            foreach (var kv in pose.restLocalRot) if (kv.Key) kv.Key.localRotation = kv.Value;
            m.localPosition=pose.modelRestLocalPos; m.localRotation=pose.modelRestLocalRot;
            if (pose.pelvis) pose.pelvis.localPosition = pose.pelvisRestPos;
            Vector3 ground=m.parent ? m.parent.position : m.position;
            float shoulder=(pose.upperArmL.position.y+pose.upperArmR.position.y)*.5f-ground.y;
            float hs=Mathf.Clamp(shoulder/1.427f,.4f,3f);
            h.transform.SetPositionAndRotation(ground, Quaternion.identity);
            h.transform.localScale=Vector3.one*hs;
            DetailedHeroPlayer3D.Pose(h,gun,handgun,firing,forward,phase,velocity,blend);

            // Body: copy every joint rotation in parent-first order.
            CopyPlayerJoint(pose,pose.pelvis,h.pelvis);
            if (pose.pelvis) pose.pelvis.position += Vector3.up*(h.pelvis.position.y-ground.y-.98f*hs);
            CopyPlayerJoint(pose,pose.spine03,h.chest);
            CopyPlayerJoint(pose,pose.head,h.head);
            CopyPlayerJoint(pose,pose.upperArmL,h.leftUpperArm); CopyPlayerJoint(pose,pose.lowerArmL,h.leftForearm);
            CopyPlayerJoint(pose,pose.upperArmR,h.rightUpperArm); CopyPlayerJoint(pose,pose.lowerArmR,h.rightForearm);
            CopyPlayerJoint(pose,pose.thighL,h.leftThigh); CopyPlayerJoint(pose,pose.calfL,h.leftShin); CopyPlayerJoint(pose,pose.footL,h.leftFoot);
            CopyPlayerJoint(pose,pose.thighR,h.rightThigh); CopyPlayerJoint(pose,pose.calfR,h.rightShin); CopyPlayerJoint(pose,pose.footR,h.rightFoot);

            // Keep the planted foot on the floor (limb proportions differ from the player).
            if (pose.footL && pose.footR)
            {
                float dl=pose.footL.position.y-m.TransformPoint(pose.restFootLocalL).y;
                float dr=pose.footR.position.y-m.TransformPoint(pose.restFootLocalR).y;
                m.position-=Vector3.up*Mathf.Min(dl,dr);
            }

            // Weapon: player's world size, and the same placement relative to the shoulders.
            if (gun==null || !gun.root) return;
            Transform anchor=!handgun&&firing&&gun.stock?gun.stock:gun.trigger;
            Vector3 before=anchor.position;
            float parentScale=gun.root.parent?Mathf.Max(.001f,gun.root.parent.lossyScale.y):1f;
            gun.root.localScale=Vector3.one*(.78f*hs/parentScale);
            gun.root.position+=before-anchor.position;
            Vector3 shoulders=(pose.upperArmL.position+pose.upperArmR.position)*.5f;
            Vector3 playerShoulders=(h.leftUpperArm.position+h.rightUpperArm.position)*.5f;
            gun.root.position+=shoulders-playerShoulders;

            // Hands on the weapon with the player's elbow direction and grip orientation.
            Vector3 wrist=gun.trigger.position+gun.root.up*(.065f*hs)-gun.root.forward*(.01f*hs);
            Vector3 support=handgun?wrist-gun.root.right*(.045f*hs):gun.support.position-gun.root.forward*(.065f*hs)-gun.root.up*(.01f*hs);
            SolveTwoBoneIK(pose.upperArmR,pose.lowerArmR,pose.handR,wrist,pose.upperArmR.position+(h.rightForearm.position-h.rightUpperArm.position));
            SolveTwoBoneIK(pose.upperArmL,pose.lowerArmL,pose.handL,support,pose.upperArmL.position+(h.leftForearm.position-h.leftUpperArm.position));
            CopyPlayerJoint(pose,pose.handR,h.rightHand);
            CopyPlayerJoint(pose,pose.handL,h.leftHand);
            if (curlFingers) { CurlFingers(pose.fingersR,-1f,54f); CurlFingers(pose.fingersL,1f,54f); }
        }

        /// <summary>
        /// Poses Malcolm Vale during the opening hub conversation (portrait stance: left forearm bent across
        /// waist/belt, right arm relaxed at side) and blends smoothly into an unarmed walking gait on departure.
        /// </summary>
        internal static void PoseValeOpeningAndWalk(CustomRigPose pose, Vector3 forward, float phase, float moveBlend)
        {
            if (pose == null || !pose.modelRoot) return;
            var h = PlayerPoseDriver();
            var m = pose.modelRoot;
            foreach (var kv in pose.restLocalRot) if (kv.Key) kv.Key.localRotation = kv.Value;
            m.localPosition = pose.modelRestLocalPos;
            m.localRotation = pose.modelRestLocalRot;
            if (pose.pelvis) pose.pelvis.localPosition = pose.pelvisRestPos;

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 ground = m.parent ? m.parent.position : m.position;
            float shoulder = (pose.upperArmL.position.y + pose.upperArmR.position.y) * 0.5f - ground.y;
            float hs = Mathf.Clamp(shoulder / 1.427f, 0.4f, 3f);
            h.transform.SetPositionAndRotation(ground, Quaternion.identity);
            h.transform.localScale = Vector3.one * hs;

            float blend = Mathf.Clamp01(moveBlend);
            h.SetPose("lowrifle", forward, phase);
            h.SetLocomotion(forward * (blend * 2.5f), blend, false, phase);
            h.head.localRotation = Quaternion.Lerp(Quaternion.Euler(3f, -4f, 0f), Quaternion.Euler(2f, 0f, 0f), blend);

            CopyPlayerJoint(pose, pose.pelvis, h.pelvis);
            if (pose.pelvis) pose.pelvis.position += Vector3.up * (h.pelvis.position.y - ground.y - 0.98f * hs);
            CopyPlayerJoint(pose, pose.spine03, h.chest);
            CopyPlayerJoint(pose, pose.head, h.head);
            CopyPlayerJoint(pose, pose.thighL, h.leftThigh);
            CopyPlayerJoint(pose, pose.calfL, h.leftShin);
            CopyPlayerJoint(pose, pose.footL, h.leftFoot);
            CopyPlayerJoint(pose, pose.thighR, h.rightThigh);
            CopyPlayerJoint(pose, pose.calfR, h.rightShin);
            CopyPlayerJoint(pose, pose.footR, h.rightFoot);

            if (pose.footL && pose.footR)
            {
                float dl = pose.footL.position.y - m.TransformPoint(pose.restFootLocalL).y;
                float dr = pose.footR.position.y - m.TransformPoint(pose.restFootLocalR).y;
                m.position -= Vector3.up * Mathf.Min(dl, dr);
            }

            Vector3 rootRight = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 rootUp = Vector3.up;
            Vector3 pelvisPos = pose.pelvis ? pose.pelvis.position : ground + rootUp * (0.98f * hs);
            float cosPhase = phase < 0f ? 0f : Mathf.Cos(phase * Mathf.PI * 2f);

            Vector3 lIdleTarget = pelvisPos + forward * (0.21f * hs) - rootRight * (0.04f * hs) + rootUp * (0.05f * hs);
            Vector3 lIdlePole = pose.upperArmL.position - rootRight * (0.38f * hs) - rootUp * (0.34f * hs) - forward * (0.18f * hs);

            Vector3 rIdleTarget = pelvisPos + rootRight * (0.25f * hs) + forward * (0.04f * hs) - rootUp * (0.11f * hs);
            Vector3 rIdlePole = pose.upperArmR.position + rootRight * (0.28f * hs) - rootUp * (0.35f * hs) - forward * (0.26f * hs);

            Vector3 lWalkTarget = pelvisPos - rootRight * (0.26f * hs) + forward * ((0.03f - cosPhase * 0.24f) * hs) - rootUp * (0.08f * hs);
            Vector3 lWalkPole = pose.upperArmL.position - rootRight * (0.24f * hs) - rootUp * (0.35f * hs) - forward * (0.25f * hs);

            Vector3 rWalkTarget = pelvisPos + rootRight * (0.26f * hs) + forward * ((0.03f + cosPhase * 0.24f) * hs) - rootUp * (0.08f * hs);
            Vector3 rWalkPole = pose.upperArmR.position + rootRight * (0.24f * hs) - rootUp * (0.35f * hs) - forward * (0.25f * hs);

            SolveTwoBoneIK(pose.upperArmL, pose.lowerArmL, pose.handL,
                Vector3.Lerp(lIdleTarget, lWalkTarget, blend),
                Vector3.Lerp(lIdlePole, lWalkPole, blend));
            SolveTwoBoneIK(pose.upperArmR, pose.lowerArmR, pose.handR,
                Vector3.Lerp(rIdleTarget, rWalkTarget, blend),
                Vector3.Lerp(rIdlePole, rWalkPole, blend));
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
