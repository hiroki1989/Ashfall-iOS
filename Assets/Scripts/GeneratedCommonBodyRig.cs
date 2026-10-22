using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // The plain-clothes base is a single authored figure. Equipment remains a separate
    // layer; this binder never chooses a different body for a particular armor slot.
    public static class GeneratedCommonBodyRig
    {
        const string Root = "Art/GeneratedBase/";
        const string Candidate = "arm-rig-candidate";

        public static SkinnedMeshRenderer Create(DetailedHero3D hero)
        {
            if (!hero) return null;
            // The repaired base uses fixed surface UVs and the approved shared head.
            // An explicit zero retains the legacy body for rollback.
            if (PlayerPrefs.GetInt("Vault.GeneratedCommonBody.Experimental", 1) == 0)
                return null;
            var prefab = Resources.Load<GameObject>(Root + "continuous-arm");
            var atlas = Resources.Load<Texture2D>(Root + "continuous-arm-atlas");
            if (!prefab || !atlas)
            {
                prefab = Resources.Load<GameObject>(Root + "surface-fixed-candidate");
                atlas = Resources.Load<Texture2D>(Root + "surface-fixed-candidate-atlas");
            }
            bool fixedSurfaceUv = prefab && atlas;
            if (!fixedSurfaceUv)
            {
                prefab = Resources.Load<GameObject>(Root + Candidate);
                atlas = Resources.Load<Texture2D>(Root + Candidate + "-atlas");
            }
            bool articulatedSource = prefab && atlas;
            if (!articulatedSource)
            {
                prefab = Resources.Load<GameObject>(Root + "body");
                atlas = Resources.Load<Texture2D>(Root + "body-atlas");
            }
            if (!prefab || !atlas) return null;
            var instance = Object.Instantiate(prefab, hero.transform, false);
            instance.name = "Common plain-clothes body / source";
            var filter = instance.GetComponentInChildren<MeshFilter>(true);
            var sourceRenderer = filter ? filter.GetComponent<MeshRenderer>() : null;
            if (!sourceRenderer || !filter.sharedMesh || !filter.sharedMesh.isReadable)
            {
                DestroySource(instance);
                Debug.LogError("Common body mesh is missing or not readable");
                return null;
            }

            Mesh mesh;
            if (articulatedSource)
            {
                mesh = BuildArticulatedMesh(instance, hero.transform, fixedSurfaceUv);
                if (!mesh)
                {
                    DestroySource(instance);
                    Debug.LogError("Common body arm candidate is incomplete");
                    return null;
                }
            }
            else mesh = BuildLegacyMesh(filter, hero.transform);

            var bones = CharacterRigStandard.GetBones(hero);
            var root = new GameObject("Common plain-clothes body / animated");
            root.transform.SetParent(hero.transform, false);
            var skin = root.AddComponent<SkinnedMeshRenderer>();
            // The gameplay skeleton may already be aiming when Create is called.
            // Capture bind matrices in authored meter-space, converting each target
            // to the nested joint's local coordinates before restoring the live pose.
            var bindposes = new Matrix4x4[bones.Length];
            var oldPositions = new Vector3[bones.Length];
            var oldRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                oldPositions[i] = bones[i].localPosition;
                oldRotations[i] = bones[i].localRotation;
            }
            SetRestPose(bones, hero.transform);
            for (int i = 0; i < bones.Length; i++)
                bindposes[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = oldPositions[i];
                bones[i].localRotation = oldRotations[i];
            }
            mesh.bindposes = bindposes;

            var shader = Resources.Load<Shader>("GeneratedCommonBodyTexture");
            if (!shader) shader = Resources.Load<Shader>("GeneratedWardenTexture");
            if (!shader) shader = Resources.Load<Shader>("VaultSurface");
            if (!shader)
            {
                DestroySource(instance);
                Object.Destroy(root);
                Object.Destroy(mesh);
                return null;
            }
            var material = new Material(shader) { name = "Common body / two-view atlas", mainTexture = atlas };
            material.SetTexture("_MainTex", atlas);
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            skin.sharedMesh = mesh;
            skin.bones = bones;
            skin.rootBone = hero.chest;
            skin.sharedMaterials = materials;
            skin.updateWhenOffscreen = true;
            skin.localBounds = new Bounds(new Vector3(0, .95f, 0), new Vector3(2.2f, 2.4f, 1.4f));
            var owned = root.AddComponent<Owned>();
            owned.Init(mesh, material);
            // Keep the proven gameplay arms until the scan segments pass
            // separate grip and seam QA.
            owned.replacesLegacyArms = false;
            owned.SetLegacyArms(hero);
            if (!SplitByBodyRegion(skin, owned))
            {
                DestroySource(instance);
                Object.Destroy(root);
                Debug.LogError("Common body regions could not be separated");
                return null;
            }
            ReplaceHeadWithApproved(skin, hero, owned);
            owned.replacesLegacyArms = BuildArticulatedArms(instance, hero.transform, material, owned);
            if (owned.replacesLegacyArms)
                foreach (var renderer in owned.legacyArms) if (renderer) renderer.enabled = false;
            if (owned.regions.Length > 2 && owned.regions[2]) owned.regions[2].enabled = false;
            // Destroy is deferred until the end of the frame. Review cameras
            // may render immediately after Create, so hide every source part now.
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            DestroySource(instance);
            return skin;
        }

        static void DestroySource(GameObject instance)
        {
            if (!instance) return;
            // Deactivate before the runtime's deferred destroy: a later armor
            // refresh may re-enable cached renderers during this same frame.
            instance.SetActive(false);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            if (Application.isPlaying) Object.Destroy(instance);
            else Object.DestroyImmediate(instance);
        }

        // armorSlots uses ArmorSlot order: head, chest, arms, legs, feet.
        // Call after any code that re-enables the returned renderer.
        public static bool SetCoveredSlots(SkinnedMeshRenderer body, bool[] armorSlots)
        {
            if (!body || armorSlots == null || armorSlots.Length < 5) return false;
            var owned = body.GetComponent<Owned>();
            if (!owned || owned.regions == null) return false;
            owned.SetArmMaterialOverride(true);
            owned.headCovered = false;
            owned.chestLinerCovered = false;
            owned.hipsLinerCovered = false;
            if (owned.hipsLiner) owned.hipsLiner.enabled = owned.hipsLinerCovered;
            if (owned.chestLiner) owned.chestLiner.enabled = owned.chestLinerCovered;
            for (int slot = 0; slot < 5; slot++)
                if (owned.regions[slot]) owned.regions[slot].enabled = slot == 0 || !armorSlots[slot];
            if (owned.regions[2]) owned.regions[2].enabled = false;
            foreach (var renderer in owned.articulatedArms)
                if (renderer) renderer.enabled = owned.replacesLegacyArms && !armorSlots[owned.armSleeves.Contains(renderer) ? 1 : 2];
            foreach (var renderer in owned.legacyArms)
                if (renderer) renderer.enabled = !owned.replacesLegacyArms && (GeneratedWardenRig.IsBaseSleeve(renderer) ? !armorSlots[1]
                    : owned.IsLegacyHand(renderer) ? !armorSlots[2] : true);
            return true;
        }

        public static void SetVisible(SkinnedMeshRenderer body, bool visible)
        {
            if (!body) return;
            body.enabled = visible;
            var owned = body.GetComponent<Owned>();
            if (owned == null) return;
            owned.SetArmMaterialOverride(visible);
            if (owned.regions != null && owned.regions.Length > 0 && owned.regions[0])
                owned.regions[0].enabled = visible && !owned.headCovered;
            if (owned.chestLiner) owned.chestLiner.enabled = visible && owned.chestLinerCovered;
            if (owned.hipsLiner) owned.hipsLiner.enabled = visible && owned.hipsLinerCovered;
            if (owned.regions != null && owned.regions.Length > 2 && owned.regions[2])
                owned.regions[2].enabled = false;
            foreach (var renderer in owned.articulatedArms)
                if (renderer) renderer.enabled = visible && owned.replacesLegacyArms;
            foreach (var renderer in owned.legacyArms)
                if (renderer) renderer.enabled = visible && !owned.replacesLegacyArms;
        }

        public static bool IsLegacyArmRenderer(Renderer renderer, DetailedHero3D hero)
        {
            if (!renderer || !hero) return false;
            var t = renderer.transform;
            return t.IsChildOf(hero.leftUpperArm) || t.IsChildOf(hero.leftForearm) ||
                t.IsChildOf(hero.leftHand) || t.IsChildOf(hero.rightUpperArm) ||
                t.IsChildOf(hero.rightForearm) || t.IsChildOf(hero.rightHand);
        }

        public static bool ReplacesLegacyArms(SkinnedMeshRenderer body)
        {
            var owned = body ? body.GetComponent<Owned>() : null;
            return owned != null && owned.replacesLegacyArms;
        }

        static Mesh BuildLegacyMesh(MeshFilter filter, Transform hero)
        {
            var original = filter.sharedMesh;
            var input = original.vertices;
            var mesh = Object.Instantiate(original);
            mesh.name = "Common plain-clothes body / skinned";
            var toHero = hero.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var normalMatrix = toHero.inverse.transpose;
            var vertices = new Vector3[input.Length];
            var normals = original.normals;
            var weights = new BoneWeight[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                var sourcePoint = toHero.MultiplyPoint3x4(input[i]);
                weights[i] = Weight(sourcePoint);
                vertices[i] = RetargetArm(sourcePoint, weights[i]);
                if (normals.Length == input.Length)
                    normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            }
            mesh.vertices = vertices;
            if (normals.Length == input.Length) mesh.normals = normals;
            else mesh.RecalculateNormals();
            mesh.boneWeights = weights;
            return mesh;
        }

        // The candidate was cut along the shoulder, elbow and wrist seams in Blender.
        // Each arm piece has one bone, so no triangle can interpolate across joints.
        static Mesh BuildArticulatedMesh(GameObject instance, Transform hero, bool fixedSurfaceUv)
        {
            var names = new[] { "plain_body", "continuous_arm_bridge", "plain_left_upper", "plain_left_forearm",
                "plain_left_hand", "plain_right_upper", "plain_right_forearm", "plain_right_hand" };
            // FBX import mirrors X. The named Blender-left parts occupy Unity's
            // right side and must follow the right arm bones, and vice versa.
            var filters = instance.GetComponentsInChildren<MeshFilter>(true);
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var sideLeftUv = new List<Vector2>();
            var sideRightUv = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var triangles = new List<int>();
            // Only the torso, head and legs remain skinned. Arm sections are
            // separate rigid garment pieces parented to their matching joints.
            for (int part = 0; part < (filters.Length > 1 && Resources.Load<GameObject>(Root + "continuous-arm") ? 2 : 1); part++)
            {
                MeshFilter filter = fixedSurfaceUv && filters.Length == 1 ? filters[0] : null;
                if (!filter) foreach (var candidate in filters)
                    if (candidate.name == names[part] || part == 0 && candidate.name == "continuous_body") { filter = candidate; break; }
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) return null;
                var source = filter.sharedMesh;
                var points = source.vertices;
                var sourceNormals = source.normals;
                var sourceUv = source.uv;
                var attachment = source.colors;
                bool continuous = filter.name == "continuous_body" || filter.name == "continuous_arm_bridge";
                if (sourceUv.Length != points.Length) return null;
                var toHero = hero.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var normalMatrix = toHero.inverse.transpose;
                int start = positions.Count;
                for (int i = 0; i < points.Length; i++)
                {
                    var point = toHero.MultiplyPoint3x4(points[i]);
                    var normal = sourceNormals.Length == points.Length
                        ? normalMatrix.MultiplyVector(sourceNormals[i]).normalized : Vector3.up;
                    BoneWeight bodyWeight;
                    if (continuous && attachment.Length == points.Length && attachment[i].r > .001f)
                        point = FitContinuousSource(point, attachment[i].r, out bodyWeight);
                    else
                    {
                        normal = CharacterLowerBodyFit.Normal(point, normal);
                        point = CharacterLowerBodyFit.Position(point);
                        bodyWeight = BodyWeight(point);
                    }
                    weights.Add(bodyWeight);
                    positions.Add(point);
                    normals.Add(normal);
                    var frontBack = sourceUv[i];
                    uv.Add(fixedSurfaceUv ? frontBack : new Vector2(frontBack.x, .5f + frontBack.y * .5f));
                    // Unity uses Y for height and Z for front/back depth. The
                    // source atlas was authored in Blender's Z-up coordinates;
                    // using Z as height here turned the side photo across the
                    // body and produced duplicated limbs.
                    float height = Mathf.Clamp01(point.y / 1.82f) * .865f + .055f;
                    // A side photograph contains two nearly overlapping legs.
                    // Offset each physical leg a little in that projection so
                    // the near/far trouser and boot edges do not sample the
                    // exact same pixels during a stride.
                    float depth = Mathf.Clamp(point.z + (point.y < 1.02f
                        ? Mathf.Sign(point.x) * .020f : 0f), -.26f, .26f);
                    // Match the authored four-view atlas projection exactly.
                    // The previous .82 scale shifted/clamped the profile into
                    // a narrow strip and painted a second silhouette on limbs.
                    float leftTileX = .485f - depth * .40f;
                    float rightTileX = .485f + depth * .40f;
                    sideLeftUv.Add(new Vector2(leftTileX * .5f, height * .5f));
                    sideRightUv.Add(new Vector2(.5f + rightTileX * .5f, height * .5f));
                }
                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    var indices = source.GetTriangles(sub);
                    for (int i = 0; i + 2 < indices.Length; i += 3)
                    {
                        if (filter.name == "continuous_arm_bridge" && attachment.Length == points.Length
                            && (attachment[indices[i]].r + attachment[indices[i + 1]].r + attachment[indices[i + 2]].r) / 3f > .5f) continue;
                        var a = toHero.MultiplyPoint3x4(points[indices[i]]);
                        var b = toHero.MultiplyPoint3x4(points[indices[i + 1]]);
                        var c = toHero.MultiplyPoint3x4(points[indices[i + 2]]);
                        var centre = (a + b + c) / 3f;
                        triangles.Add(start + indices[i]);
                        triangles.Add(start + indices[i + 1]);
                        triangles.Add(start + indices[i + 2]);
                    }
                }
            }
            var mesh = new Mesh { name = "Common plain-clothes body / articulated",
                indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, sideLeftUv);
            mesh.SetUVs(2, sideRightUv);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool BuildArticulatedArms(GameObject instance, Transform hero, Material material, Owned owned)
        {
            bool continuous = instance.GetComponentsInChildren<MeshFilter>(true).Length > 1
                && Resources.Load<GameObject>(Root + "continuous-arm");
            GameObject sourceObject;
            Material armMaterial;
            if (continuous)
            {
                sourceObject = instance;
                armMaterial = material;
            }
            else
            {
                var sourcePrefab = Resources.Load<GameObject>(Root + Candidate);
                var armAtlas = Resources.Load<Texture2D>(Root + Candidate + "-atlas");
                if (!sourcePrefab || !armAtlas) return false;
                armMaterial = new Material(material) { name = "Common body / source arm atlas", mainTexture = armAtlas };
                armMaterial.SetTexture("_MainTex", armAtlas);
                owned.AddMaterial(armMaterial);
                sourceObject = Object.Instantiate(sourcePrefab, hero, false);
                sourceObject.SetActive(false);
            }
            var filters = sourceObject.GetComponentsInChildren<MeshFilter>(true);
            var bones = CharacterRigStandard.GetBones(hero.GetComponent<DetailedHero3D>());
            int count = 0;
            foreach (var filter in filters)
            {
                if ((!filter.name.StartsWith("plain_") && filter.name != "continuous_arm_bridge")
                    || filter.name == "plain_body" || filter.name == "continuous_body") continue;
                var source = filter.sharedMesh;
                if (!source || !source.isReadable) continue;
                var vertices = source.vertices;
                var normals = source.normals;
                var uv = source.uv;
                var attachment = source.colors;
                var weights = new BoneWeight[vertices.Length];
                var original = new Vector3[vertices.Length];
                var transform = hero.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var normalMatrix = transform.inverse.transpose;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = transform.MultiplyPoint3x4(vertices[i]); original[i] = point;
                    int side = point.x < 0f ? -1 : 1;
                    var rotation = Quaternion.identity;
                    if (continuous)
                        vertices[i] = FitContinuousSource(point, attachment.Length == vertices.Length ? attachment[i].r : 1f, out weights[i]);
                    else
                    {
                        vertices[i] = FitMeasuredBaseArm(point, side);
                        float arm = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.43f, 1.48f, point.y));
                        weights[i] = CharacterArmFit.Weight(vertices[i], side < 0 ? 3 : 6, arm);
                    }
                    if (point.y < .867f)
                        vertices[i] = CurlBaseFingers(vertices[i], out rotation);
                    if (normals.Length == vertices.Length) normals[i] = rotation * normalMatrix.MultiplyVector(normals[i]).normalized;
                    if (!continuous && uv.Length == vertices.Length) uv[i] = new Vector2(uv[i].x, .5f + .5f * uv[i].y);
                }
                for (int slot = 1; slot <= 2; slot++)
                {
                    var triangles = new List<int>();
                    for (int sub = 0; sub < source.subMeshCount; sub++)
                    {
                        var input = source.GetTriangles(sub);
                        for (int i = 0; i + 2 < input.Length; i += 3)
                        {
                            if (filter.name == "continuous_arm_bridge" && attachment.Length == vertices.Length
                                && (attachment[input[i]].r + attachment[input[i + 1]].r + attachment[input[i + 2]].r) / 3f <= .5f) continue;
                            float height = (original[input[i]].y + original[input[i + 1]].y + original[input[i + 2]].y) / 3f;
                            if ((height >= 1.0f ? 1 : 2) != slot) continue;
                            triangles.Add(input[i]); triangles.Add(input[i + 1]); triangles.Add(input[i + 2]);
                        }
                    }
                    if (triangles.Count == 0) continue;
                    var mesh = new Mesh { name = filter.name + " / shared joint skin" };
                    mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv;
                    mesh.boneWeights = weights;
                    mesh.bindposes = owned.regions[1].sharedMesh.bindposes;
                    mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
                    var piece = new GameObject(filter.name + (slot == 1 ? " / Chest sleeve" : " / Hands skin"));
                    piece.transform.SetParent(owned.transform, false);
                    var renderer = piece.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = mesh; renderer.sharedMaterial = armMaterial;
                    renderer.bones = bones; renderer.rootBone = bones[0];
                    renderer.updateWhenOffscreen = true; renderer.localBounds = owned.regions[1].localBounds;
                    owned.AddMesh(mesh); owned.AddArmRenderer(renderer);
                    if (slot == 1) owned.armSleeves.Add(renderer);
                    count++;
                }
            }
            if (!continuous) DestroySource(sourceObject);
            if (count < 6)
                foreach (var renderer in owned.articulatedArms) if (renderer) renderer.enabled = false;
            return count >= 6;
        }

        static Vector3 FitContinuousSource(Vector3 source, float attachment, out BoneWeight weight)
        {
            attachment = Mathf.Clamp01(attachment);
            if (attachment < .001f)
            {
                var lower = CharacterLowerBodyFit.Position(source);
                weight = BodyWeight(lower);
                return lower;
            }
            int side = source.x < 0f ? -1 : 1;
            var fitted = Vector3.Lerp(source, FitMeasuredBaseArm(source, side), attachment);
            weight = CharacterArmFit.Weight(fitted, side < 0 ? 3 : 6, attachment);
            return fitted;
        }

        static Vector3 CurlBaseFingers(Vector3 point, out Quaternion rotation)
        {
            // Leave wrist and palm fixed. Only the distal finger surface bends
            // around the common palm axis toward the existing grip sockets.
            const float knuckleHeight = .820f;
            float amount = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(knuckleHeight, .760f, point.y));
            rotation = Quaternion.AngleAxis(-105f * amount, Vector3.right);
            var pivot = new Vector3(point.x, knuckleHeight, .025f);
            return pivot + rotation * (point - pivot);
        }

        static Vector3 FitMeasuredBaseArm(Vector3 point, int side)
        {
            // Source cross-section bounds measured in Blender, converted to Unity:
            // shoulder (.249,1.427,-.049), elbow (.325,1.142,-.059),
            // wrist (.368,.940,+.073). The former -.035 wrist depth was on
            // the opposite side of the actual palm and displaced it by 10cm.
            float t; float x; float depth; float height;
            if (point.y >= 1.142f)
            {
                t = Mathf.InverseLerp(1.142f, 1.427f, point.y);
                x = Mathf.Lerp(.325f, .249f, t); depth = Mathf.Lerp(-.059f, -.049f, t);
                height = point.y;
            }
            else
            {
                t = (point.y - .940f) / (1.142f - .940f);
                x = Mathf.LerpUnclamped(.368f, .325f, t);
                depth = Mathf.LerpUnclamped(.073f, -.059f, t);
                height = Mathf.LerpUnclamped(CharacterArmFit.WristHeight, CharacterArmFit.ElbowHeight, t);
                if (point.y < .940f)
                {
                    // Keep the distal hand width and depth together as one palm.
                    x = .368f; depth = .073f;
                    height = CharacterArmFit.WristHeight + (point.y - .940f) * .78f;
                }
            }
            // Match the shared forearm/glove cross-section while keeping the
            // measured centreline, height and complete upper sleeve unchanged.
            float width = Mathf.Lerp(.72f, 1f, Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.0f, CharacterArmFit.ElbowHeight, point.y)));
            var result = new Vector3(side * CharacterArmFit.Axis + (point.x - side * x) * width,
                height, (point.z - depth) * width);
            float shoulder = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.36f, 1.47f, point.y));
            return Vector3.Lerp(result, point, shoulder);
        }

        static Transform ResolveArmBone(string name, int side, Transform[] bones)
        {
            int offset = side < 0 ? 3 : 6;
            int segment = name.Contains("upper") ? 0 : name.Contains("forearm") ? 1 : 2;
            return bones[offset + segment];
        }

        static void SetRestPose(Transform[] bones, Transform heroRoot)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (!bone) continue;
                bone.localRotation = Quaternion.identity;
                if (i >= CharacterRigStandard.RestPositions.Length) continue;
                Vector3 world = heroRoot.TransformPoint(CharacterRigStandard.RestPositions[i]);
                bone.localPosition = bone.parent ? bone.parent.InverseTransformPoint(world) : world;
            }
        }

        static BoneWeight ArmWeight(float sourceHeight, int side)
        {
            int upper = side < 0 ? 3 : 6;
            // The source is cut by face centre, so adjacent parts share some
            // vertices. Compute weights from the original coordinate on every
            // copy. The shoulder rim follows the trunk before the sleeve bends.
            if (sourceHeight > 1.36f)
                return Pair(0, upper, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(1.36f, 1.47f, sourceHeight)));
            if (sourceHeight > 1.08f)
                return Pair(upper, upper + 1, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(1.08f, 1.20f, sourceHeight)));
            if (sourceHeight > .84f)
                return Pair(upper + 1, upper + 2, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.84f, .94f, sourceHeight)));
            return Single(upper + 2);
        }

        static Vector3 ToArmRest(Vector3 point, int side, out Quaternion rotation)
        {
            // These centres are measured after FBX axis conversion.
            var sourceShoulder = new Vector3(side * .245f, 1.427f, -.055f);
            var sourceElbow = new Vector3(side * .345f, 1.142f, -.055f);
            var sourceWrist = new Vector3(side * .37f, .877f, -.035f);
            var targetShoulder = new Vector3(side * .18f, 1.427f, 0f);
            var targetElbow = new Vector3(side * .18f, 1.142f, 0f);
            var targetWrist = new Vector3(side * .18f, .877f, 0f);
            var upperRotation = Quaternion.FromToRotation(sourceElbow - sourceShoulder,
                targetElbow - targetShoulder);
            var lowerRotation = Quaternion.FromToRotation(sourceWrist - sourceElbow,
                targetWrist - targetElbow);
            float elbowBlend = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.20f, 1.08f, point.y));
            rotation = Quaternion.Slerp(upperRotation, lowerRotation, elbowBlend);
            // A continuous centreline and blended frame keep duplicated elbow
            // vertices together instead of producing two rotated cut edges.
            float upperT = Mathf.Clamp01((sourceShoulder.y - point.y) /
                (sourceShoulder.y - sourceElbow.y));
            float lowerT = Mathf.Clamp01((sourceElbow.y - point.y) /
                (sourceElbow.y - sourceWrist.y));
            var sourceCentre = point.y >= sourceElbow.y
                ? Vector3.LerpUnclamped(sourceShoulder, sourceElbow, upperT)
                : Vector3.LerpUnclamped(sourceElbow, sourceWrist, lowerT);
            var targetCentre = point.y >= sourceElbow.y
                ? Vector3.LerpUnclamped(targetShoulder, targetElbow, upperT)
                : Vector3.LerpUnclamped(targetElbow, targetWrist, lowerT);
            var result = targetCentre + rotation * (point - sourceCentre);
            float shoulder = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(1.35f, 1.47f, point.y));
            result = Vector3.Lerp(result, point, shoulder);
            // The candidate wrist and palm share one continuous piece. A palm
            // correction therefore starts at zero at the wrist, avoiding a gap.
            float palm = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.877f, .75f, point.y));
            result += new Vector3(0f, -.04f, -.11f) * palm;
            return result;
        }

        static bool SplitByBodyRegion(SkinnedMeshRenderer skin, Owned owned)
        {
            var source = skin.sharedMesh;
            var positions = source.vertices;
            var weights = source.boneWeights;
            if (positions.Length == 0 || weights.Length != positions.Length) return false;
            var normals = source.normals;
            var tangents = source.tangents;
            var colors = source.colors;
            var uv = new List<Vector4>[8];
            for (int channel = 0; channel < uv.Length; channel++)
            {
                uv[channel] = new List<Vector4>();
                source.GetUVs(channel, uv[channel]);
            }
            var regions = new SkinnedMeshRenderer[5];
            for (int slot = 0; slot < regions.Length; slot++)
            {
                var remap = new Dictionary<int, int>();
                var partPositions = new List<Vector3>();
                var partNormals = new List<Vector3>();
                var partTangents = new List<Vector4>();
                var partColors = new List<Color>();
                var partWeights = new List<BoneWeight>();
                var partUv = new List<Vector4>[8];
                for (int channel = 0; channel < partUv.Length; channel++) partUv[channel] = new List<Vector4>();
                var triangles = new List<int>[source.subMeshCount];
                int Map(int old)
                {
                    if (remap.TryGetValue(old, out int mapped)) return mapped;
                    mapped = partPositions.Count;
                    remap.Add(old, mapped);
                    partPositions.Add(positions[old]);
                    partWeights.Add(weights[old]);
                    if (normals.Length == positions.Length) partNormals.Add(normals[old]);
                    if (tangents.Length == positions.Length) partTangents.Add(tangents[old]);
                    if (colors.Length == positions.Length) partColors.Add(colors[old]);
                    for (int channel = 0; channel < partUv.Length; channel++)
                        if (uv[channel].Count == positions.Length) partUv[channel].Add(uv[channel][old]);
                    return mapped;
                }
                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    var destination = new List<int>();
                    triangles[sub] = destination;
                    var sourceTriangles = source.GetTriangles(sub);
                    for (int i = 0; i + 2 < sourceTriangles.Length; i += 3)
                    {
                        int a = sourceTriangles[i], b = sourceTriangles[i + 1], c = sourceTriangles[i + 2];
                        if (BodyRegion((positions[a] + positions[b] + positions[c]) / 3f,
                            weights[a], weights[b], weights[c], source.name.Contains("articulated")) != slot) continue;
                        destination.Add(Map(a)); destination.Add(Map(b)); destination.Add(Map(c));
                    }
                }
                if (partPositions.Count == 0) continue;
                var part = new Mesh { name = "Common body / " + (ArmorSlot)slot,
                    indexFormat = partPositions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                part.SetVertices(partPositions);
                if (partNormals.Count == partPositions.Count) part.SetNormals(partNormals);
                if (partTangents.Count == partPositions.Count) part.SetTangents(partTangents);
                if (partColors.Count == partPositions.Count) part.SetColors(partColors);
                for (int channel = 0; channel < partUv.Length; channel++)
                    if (partUv[channel].Count == partPositions.Count) part.SetUVs(channel, partUv[channel]);
                part.boneWeights = partWeights.ToArray();
                part.bindposes = source.bindposes;
                part.subMeshCount = triangles.Length;
                for (int sub = 0; sub < triangles.Length; sub++) part.SetTriangles(triangles[sub], sub, false);
                if (partNormals.Count != partPositions.Count) part.RecalculateNormals();
                part.RecalculateBounds();
                SkinnedMeshRenderer renderer;
                if (slot == 1) renderer = skin;
                else
                {
                    var partObject = new GameObject("Common body / " + (ArmorSlot)slot);
                    partObject.transform.SetParent(skin.transform, false);
                    renderer = partObject.AddComponent<SkinnedMeshRenderer>();
                }
                renderer.sharedMesh = part;
                renderer.bones = skin.bones;
                renderer.rootBone = skin.rootBone;
                renderer.sharedMaterials = skin.sharedMaterials;
                renderer.updateWhenOffscreen = skin.updateWhenOffscreen;
                renderer.localBounds = skin.localBounds;
                regions[slot] = renderer;
                owned.AddMesh(part);
            }
            owned.regions = regions;
            skin.enabled = regions[1] != null;
            return regions[1] != null;
        }

        static void ReplaceHeadWithApproved(SkinnedMeshRenderer body, DetailedHero3D hero, Owned owned)
        {
            if (!Resources.Load<GameObject>("Art/GeneratedWarden/head-rebuilt")) return;
            var source = GeneratedWardenPreviewAssets.CreateCharacterPart("body", hero);
            if (!source) return;
            var approved = GeneratedWardenRig.Bind(source, hero, true);
            if (!approved)
            {
                DestroySource(source);
                Debug.LogWarning("Common body could not bind the approved shared head");
                return;
            }
            approved.name = "Common body / approved shared head";
            approved.transform.SetParent(body.transform, false);
            approved.localBounds = body.localBounds;
            owned.AddMesh(approved.sharedMesh);
            foreach (var material in approved.sharedMaterials) owned.AddMaterial(material);
            var previous = owned.regions[0];
            owned.regions[0] = approved;
            // Keep the discarded projected face out of all later legacy-renderer
            // snapshots, including edit-mode preview refreshes.
            if (previous) DestroySource(previous.gameObject);
            DestroySource(source);
        }

        static void BuildChestSideLiner(SkinnedMeshRenderer chest, Owned owned)
        {
            var source = chest.sharedMesh;
            var vertices = source.vertices;
            var indices = new List<int>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var triangles = source.GetTriangles(sub);
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    var centre = (vertices[a] + vertices[b] + vertices[c]) / 3f;
                    if (centre.y < 1.10f || centre.y > 1.50f || Mathf.Abs(centre.x) < .13f) continue;
                    var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
                    if (Mathf.Abs(normal.x) < .55f || Mathf.Abs(normal.x) < Mathf.Abs(normal.z)) continue;
                    indices.Add(a); indices.Add(b); indices.Add(c);
                }
            }
            if (indices.Count == 0) return;
            var mesh = Object.Instantiate(source);
            mesh.name = "Common body / chest side lining";
            mesh.subMeshCount = 1;
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            var child = new GameObject("Common body / side lining under chest armor");
            child.transform.SetParent(chest.transform, false);
            var renderer = child.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = chest.bones;
            renderer.rootBone = chest.rootBone;
            renderer.sharedMaterial = chest.sharedMaterial;
            renderer.updateWhenOffscreen = chest.updateWhenOffscreen;
            renderer.localBounds = chest.localBounds;
            renderer.enabled = false;
            owned.chestLiner = renderer;
            owned.AddMesh(mesh);
        }

        static void BuildHipsLiner(SkinnedMeshRenderer body, Owned owned)
        {
            // Only retain waist triangles that belong to the hidden base legs.
            // The base chest remains visible in this state and already owns the
            // upper waistband, so copying it here would create coincident layers.
            var legs = owned.regions[3];
            if (!legs) return;
            var source = body.sharedMesh;
            var vertices = source.vertices;
            var indices = new List<int>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var triangles = source.GetTriangles(sub);
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    float height = (vertices[a].y + vertices[b].y + vertices[c].y) / 3f;
                    if (height <= .86f || height > 1.24f) continue;
                    indices.Add(a); indices.Add(b); indices.Add(c);
                }
            }
            if (indices.Count == 0) return;
            var mesh = Object.Instantiate(source);
            mesh.name = "Common body / retained waistband under partial armor";
            var inset=mesh.vertices;for(int j=0;j<inset.Length;j++){inset[j].x*=.995f;inset[j].z*=.995f;}mesh.vertices=inset;
            mesh.subMeshCount = 1;
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            var child = new GameObject("Common body / waistband lining");
            child.transform.SetParent(body.transform, false);
            var renderer = child.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = body.bones;
            renderer.rootBone = body.rootBone;
            renderer.sharedMaterial = body.sharedMaterial;
            renderer.updateWhenOffscreen = true;
            renderer.localBounds = body.localBounds;
            renderer.enabled = false;
            owned.hipsLiner = renderer;
            owned.AddMesh(mesh);
        }

        static float Influence(BoneWeight weight, int start, int end)
        {
            float amount = 0f;
            if (weight.boneIndex0 >= start && weight.boneIndex0 <= end) amount += weight.weight0;
            if (weight.boneIndex1 >= start && weight.boneIndex1 <= end) amount += weight.weight1;
            if (weight.boneIndex2 >= start && weight.boneIndex2 <= end) amount += weight.weight2;
            if (weight.boneIndex3 >= start && weight.boneIndex3 <= end) amount += weight.weight3;
            return amount;
        }

        static int BodyRegion(Vector3 centre, BoneWeight a, BoneWeight b, BoneWeight c, bool authoredBody)
        {
            float armBoundary = centre.y >= 1.40f ? .22f
                : centre.y >= 1.20f ? .20f
                : centre.y >= .95f ? .22f : .24f;
            if (!authoredBody && centre.y >= .76f && centre.y <= 1.57f && Mathf.Abs(centre.x) > armBoundary)
                return 2;
            float arms = (Influence(a, 3, 8) + Influence(b, 3, 8) + Influence(c, 3, 8)) / 3f;
            float legs = (Influence(a, 9, 14) + Influence(b, 9, 14) + Influence(c, 9, 14)) / 3f;
            float head = (Influence(a, 2, 2) + Influence(b, 2, 2) + Influence(c, 2, 2)) / 3f;
            if (authoredBody ? centre.y > 1.57f : head > .55f && centre.y > 1.43f) return 0;
            if (!authoredBody && arms > .45f) return 2;
            if (authoredBody) return centre.y < 1.04f
                ? (centre.y < CharacterLowerBodyFit.FeetSeamHeight ? 4 : 3) : 1;
            if (legs > .45f || centre.y < .90f) return centre.y < (authoredBody ? CharacterLowerBodyFit.FeetSeamHeight : .25f) ? 4 : 3;
            return 1;
        }

        static BoneWeight BodyWeight(Vector3 point)
        {
            if (point.y > 1.55f) return Single(2);
            if (point.y >= 1.02f)
            {
                if (point.y > 1.47f) return Pair(2, 0,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.47f, 1.58f, point.y)));
                return Pair(0, 1, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(1.03f, 1.30f, point.y)));
            }
            // Lower body keeps its existing leg weights, with no arm classification.
            return CharacterLowerBodyFit.Weight(point);
        }

        static BoneWeight Weight(Vector3 point)
        {
            float x = Mathf.Abs(point.x), y = point.y;
            if (y > 1.55f && x < .27f) return Single(2);
            // The hands reach below the belt. Test the lateral arm region before
            // the lower-body band, or wrists become weighted to the thighs.
            float armBoundary = y < 1.05f ? .25f
                : Mathf.Lerp(.21f, .17f, Mathf.InverseLerp(1.20f, 1.53f, y));
            if (y > .72f && x > armBoundary)
            {
                int upper = point.x < 0 ? 3 : 6;
                if (y < .99f) return Single(upper + 2);
                if (y < 1.19f) return Pair(upper + 1, upper + 2, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.08f, 1.22f, y)));
                return Pair(upper, upper + 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.29f, 1.44f, y)));
            }
            if (y < 1.02f)
            {
                int thigh = point.x < 0 ? 9 : 12;
                if (y < .14f) return Single(thigh + 2);
                if (y < .24f) return Pair(thigh + 2, thigh + 1,
                    1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.14f, .24f, y)));
                if (y < .52f) return Single(thigh + 1);
                if (y < .65f) return Pair(thigh, thigh + 1,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.52f, .65f, y)));
                if (y < .87f) return Single(thigh);
                // The scan's upper thigh approaches the pelvis at the hip seam.
                return Pair(thigh, 1, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.87f, 1.02f, y)));
            }
            if (y > 1.47f) return Pair(2, 0, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.47f, 1.58f, y)));
            return Pair(0, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.03f, 1.30f, y)));
        }

        static Vector3 RetargetArm(Vector3 source, BoneWeight weight)
        {
            float arm = 0f;
            if (weight.boneIndex0 >= 3 && weight.boneIndex0 <= 8) arm += weight.weight0;
            if (weight.boneIndex1 >= 3 && weight.boneIndex1 <= 8) arm += weight.weight1;
            if (weight.boneIndex2 >= 3 && weight.boneIndex2 <= 8) arm += weight.weight2;
            if (weight.boneIndex3 >= 3 && weight.boneIndex3 <= 8) arm += weight.weight3;
            if (arm <= 0f) return source;
            // Full correction at the glove and forearm, fading through the upper
            // sleeve to zero at the shoulder seam so it stays joined to the torso.
            float shoulder = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.08f, 1.43f, source.y));
            source.x -= Mathf.Sign(source.x) * .17f * (1f - shoulder) * arm;
            return source;
        }

        static BoneWeight Single(int bone) => new BoneWeight { boneIndex0 = bone, weight0 = 1f };
        static BoneWeight Pair(int a, int b, float aWeight) => new BoneWeight
        {
            boneIndex0 = a, weight0 = aWeight, boneIndex1 = b, weight1 = 1f - aWeight
        };
        sealed class Owned : MonoBehaviour
        {
            Mesh mesh;
            Material material;
            readonly List<Mesh> parts = new List<Mesh>();
            readonly HashSet<Material> additionalMaterials = new HashSet<Material>();
            public SkinnedMeshRenderer[] regions;
            public bool replacesLegacyArms;
            public SkinnedMeshRenderer chestLiner;
            public bool chestLinerCovered;
            public bool headCovered;
            public SkinnedMeshRenderer hipsLiner;
            public bool hipsLinerCovered;
            public readonly List<Renderer> articulatedArms = new List<Renderer>();
            public readonly HashSet<Renderer> armSleeves = new HashSet<Renderer>();
            public readonly List<Renderer> legacyArms = new List<Renderer>();
            readonly HashSet<Renderer> legacyHands = new HashSet<Renderer>();
            readonly Dictionary<Renderer, Material[]> originalArmMaterials = new Dictionary<Renderer, Material[]>();
            readonly Dictionary<Renderer, Material[]> candidateArmMaterials = new Dictionary<Renderer, Material[]>();
            readonly Dictionary<Material, Material> armMaterialClones = new Dictionary<Material, Material>();
            public void Init(Mesh ownedMesh, Material ownedMaterial)
            { mesh = ownedMesh; material = ownedMaterial; }
            public void SetLegacyArms(DetailedHero3D hero)
            {
                foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
                    if (IsLegacyArmRenderer(renderer, hero))
                    {
                        legacyArms.Add(renderer);
                        if (renderer.transform.IsChildOf(hero.leftHand) || renderer.transform.IsChildOf(hero.rightHand) ||
                            renderer.name.Contains("Exposed forearm tendons"))
                            legacyHands.Add(renderer);
                        var originals = renderer.sharedMaterials;
                        var replacements = (Material[])originals.Clone();
                        bool changed = false;
                        for (int i = 0; i < originals.Length; i++)
                        {
                            var source = originals[i];
                            if (!source) continue;
                            bool canvas = source.name.StartsWith("Faded olive woven canvas") ||
                                source == DetailedHeroMaterials3D.Get("Cloth");
                            bool stitch = source.name.StartsWith("Canvas folded seam") ||
                                source == DetailedHeroMaterials3D.Get("Stitch");
                            if (!canvas && !stitch) continue;
                            if (!armMaterialClones.TryGetValue(source, out var clone))
                            {
                                clone = new Material(source) { name = source.name + " / common body sleeve" };
                                var tint = canvas ? new Color(.39f, .34f, .22f) : new Color(.23f, .20f, .13f);
                                if (clone.HasProperty("_Color")) clone.SetColor("_Color", tint);
                                if (clone.HasProperty("_BaseColor")) clone.SetColor("_BaseColor", tint);
                                armMaterialClones.Add(source, clone);
                            }
                            replacements[i] = clone;
                            changed = true;
                        }
                        if (changed)
                        {
                            originalArmMaterials.Add(renderer, originals);
                            candidateArmMaterials.Add(renderer, replacements);
                            renderer.sharedMaterials = replacements;
                        }
                    }
            }
            public bool IsLegacyHand(Renderer renderer) => legacyHands.Contains(renderer);
            public void SetArmMaterialOverride(bool active)
            {
                var materials = active ? candidateArmMaterials : originalArmMaterials;
                foreach (var pair in materials) if (pair.Key) pair.Key.sharedMaterials = pair.Value;
            }
            public void AddMaterial(Material value) { if (value && value != material) additionalMaterials.Add(value); }
            public void AddMesh(Mesh part) { if (part) parts.Add(part); }
            public void AddArmRenderer(Renderer renderer) { if (renderer) articulatedArms.Add(renderer); }
            static void Release(Object value)
            {
                if (!value) return;
                if (Application.isPlaying) Object.Destroy(value);
                else Object.DestroyImmediate(value);
            }
            void OnDestroy()
            {
                SetArmMaterialOverride(false);
                foreach (var pair in armMaterialClones) Release(pair.Value);
                foreach (var value in additionalMaterials) Release(value);
                foreach (var part in parts) Release(part);
                Release(mesh);
                Release(material);
            }
        }
    }
}








