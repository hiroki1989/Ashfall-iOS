#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeonVault
{
    public static class Downloads2CharacterSetup
    {
        [MenuItem("NEON VAULT/Assets/Configure Downloads2 Characters")]
        public static void RunAll()
        {
            ConfigureTextures();
            ConfigureStaticNpcFbx("Iona", "iona_meshy");
            ConfigureStaticNpcFbx("Briggs", "briggs_meshy");
            ConfigureStaticNpcFbx("Sera", "sera_meshy");
            ConfigureRiggedFbx("Raven", "Raven_MeshyPBR", "raven_meshy", null);
            ConfigureRiggedFbx("Vex", "Vex_MeshyPBR", "vex_meshy", null);
            ConfigureRiggedFbx("ValeBoss", "Veil_MeshyPBR", "veil_meshy", SetupVeilClips);
            BakeVeilPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DOWNLOADS2_CHARACTER_SETUP_COMPLETE");
        }

        public static void RunVexSera()
        {
            ConfigureTextures();
            ConfigureStaticNpcFbx("Sera", "sera_meshy");
            ConfigureRiggedFbx("Vex", "Vex_MeshyPBR", "vex_meshy", null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VEX_SERA_SETUP_COMPLETE");
        }

        static void ConfigureTextures()
        {
            string[] dirs = {
                "Assets/Resources/Characters/Iona/Textures",
                "Assets/Resources/Characters/Briggs/Textures",
                "Assets/Resources/Characters/Raven/Textures",
                "Assets/Resources/Characters/Vex/Textures",
                "Assets/Resources/Characters/Sera/Textures",
                "Assets/Resources/Characters/ValeBoss/Textures",
            };
            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*_meshy_*.png"))
                {
                    string path = file.Replace('\\', '/');
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) continue;
                    bool dirty = false;
                    if (path.EndsWith("_n.png"))
                    {
                        if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
                    }
                    else if (path.EndsWith("_m.png"))
                    {
                        if (imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
                        if (imp.alphaSource != TextureImporterAlphaSource.FromInput) { imp.alphaSource = TextureImporterAlphaSource.FromInput; dirty = true; }
                    }
                    else
                    {
                        if (!imp.sRGBTexture) { imp.sRGBTexture = true; dirty = true; }
                    }
                    if (imp.maxTextureSize != 2048) { imp.maxTextureSize = 2048; dirty = true; }
                    if (imp.filterMode != FilterMode.Trilinear) { imp.filterMode = FilterMode.Trilinear; dirty = true; }
                    if (imp.anisoLevel != 8) { imp.anisoLevel = 8; dirty = true; }
                    if (imp.textureCompression != TextureImporterCompression.CompressedHQ) { imp.textureCompression = TextureImporterCompression.CompressedHQ; dirty = true; }
                    if (dirty) imp.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();
        }

        static Material EnsurePbrMaterial(string texDir, string matName, string texPrefix)
        {
            string matPath = texDir + "/" + matName + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var std = AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");
            if (!mat)
            {
                mat = new Material(std) { name = matName };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != std)
            {
                mat.shader = std;
            }

            var alb = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/" + texPrefix + "_a.png");
            var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/" + texPrefix + "_n.png");
            var met = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/" + texPrefix + "_m.png");

            mat.SetColor("_Color", Color.white);
            if (alb) mat.SetTexture("_MainTex", alb);
            if (nrm)
            {
                mat.SetTexture("_BumpMap", nrm);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 0.95f);
            }
            if (met)
            {
                mat.SetTexture("_MetallicGlossMap", met);
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_GlossMapScale", 1.0f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ConfigureStaticNpcFbx(string charName, string texPrefix)
        {
            string fbxPath = "Assets/Resources/Characters/" + charName + "/" + charName + ".fbx";
            string texDir = "Assets/Resources/Characters/" + charName + "/Textures";
            var mat = EnsurePbrMaterial(texDir, charName + "_MeshyPBR", texPrefix);

            var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) throw new Exception("Missing FBX: " + fbxPath);
            imp.isReadable = true;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.optimizeBones = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.External;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), charName + "_MeshyPBR"), mat);
            imp.SaveAndReimport();
        }

        static void ConfigureRiggedFbx(string charName, string matName, string texPrefix, Action<ModelImporter> customClips)
        {
            string fbxPath = "Assets/Resources/Characters/" + charName + "/" + charName + ".fbx";
            string texDir = "Assets/Resources/Characters/" + charName + "/Textures";
            var mat = EnsurePbrMaterial(texDir, matName, texPrefix);

            var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) throw new Exception("Missing FBX: " + fbxPath);
            imp.isReadable = true;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.optimizeBones = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.External;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), mat);
            customClips?.Invoke(imp);
            imp.SaveAndReimport();
        }

        static void SetupVeilClips(ModelImporter imp)
        {
            var defClips = imp.defaultClipAnimations;
            if (defClips == null || defClips.Length == 0) return;
            foreach (var c in defClips)
            {
                string n = c.name.ToLowerInvariant();
                c.loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run");
            }
            imp.clipAnimations = defClips;
        }

        static void BakeVeilPrefabs()
        {
            string fbxPath = "Assets/Resources/Characters/ValeBoss/ValeBoss.fbx";
            var srcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (!srcPrefab) throw new Exception("Missing ValeBoss.fbx prefab at " + fbxPath);

            var allClips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview"))
                .ToArray();

            AnimationClip FindClip(params string[] tokens)
            {
                foreach (var t in tokens)
                {
                    var m = allClips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(t));
                    if (m) return m;
                }
                return null;
            }

            var idleClip = FindClip("veil_idle", "idle");
            var walkClip = FindClip("veil_walk", "walk");
            var runClip = FindClip("veil_run", "run");
            var atkClip = FindClip("veil_attack", "attack");
            var dthClip = FindClip("veil_death", "death");
            Debug.Log($"Veil clips loaded: count={allClips.Length}, idle={idleClip?.name}, walk={walkClip?.name}, run={runClip?.name}, atk={atkClip?.name}, death={dthClip?.name}");

            foreach (var spec in new[] {
                ("Enemy_11", 2.35f, false),
                ("RaiderBossNPC", 2.35f, true),
            })
            {
                string outName = spec.Item1;
                float height = spec.Item2;
                bool suppressRifle = spec.Item3;
                var root = new GameObject(outName);
                try
                {
                    var clone = UnityEngine.Object.Instantiate(srcPrefab, root.transform, false);
                    clone.name = "ValeBossModel";
                    foreach (var tr in clone.GetComponentsInChildren<Transform>(true))
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
                    foreach (var anim in clone.GetComponentsInChildren<Animator>(true))
                    {
                        anim.applyRootMotion = false;
                        anim.runtimeAnimatorController = null;
                        anim.enabled = false;
                    }

                    // Normalize initial REST scale to roughly targetHeight (ImportedEnemyVisual.Bind refines feet/height cleanly)
                    float scale = height / 1.70f;
                    clone.transform.localScale = Vector3.one * scale;
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localRotation = Quaternion.identity;

                    var driver = root.AddComponent<ImportedEnemyVisual>();
                    driver.model = clone.transform;
                    driver.useNamedBones = true;
                    driver.suppressMask = true;
                    driver.suppressBossRifle = suppressRifle;
                    driver.targetHeight = height;
                    driver.idleClip = idleClip;
                    driver.walkClip = walkClip;
                    driver.runClip = runClip;
                    driver.attackClip = atkClip;
                    driver.deathClip = dthClip;

                    string prefabPath = "Assets/Resources/ImportedEnemies/" + outName + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log("Baked Veil prefab: " + prefabPath + " targetHeight=" + height);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }
    }
}
#endif
