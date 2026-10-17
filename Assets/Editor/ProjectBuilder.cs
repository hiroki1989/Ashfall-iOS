using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonVault.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Ashfall/Open startup scene")]
        public static void OpenStartup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/NeonVault.unity",OpenSceneMode.Single);
            Debug.Log("ASHFALL_PLAYER_PROJECT_READY");
        }
        [MenuItem("Ashfall/Prepare startup scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/NeonVault.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/NeonVault.unity",true)};
            // Keep the storage identity so existing PlayerPrefs survive the visual/world migration.
            PlayerSettings.companyName="NeonVault"; PlayerSettings.productName="Neon Vault";
            PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"}))
            {
                string file=AssetDatabase.GUIDToAssetPath(path);var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.isReadable=file.Contains("/LayeredPlayer/")||file.Contains("/LayeredEnemies/")||file.Contains("Directions")||file.Contains("Props")||file.Contains("WeaponAtlas")||file.Contains("32Walk")||file.Contains("ItemPortrait")||file.Contains("KeyEquipment")||file.Contains("Yaw")||file.Contains("Shooting32");importer.alphaIsTransparency=true;
                bool tiled=file.Contains("Floor")||file.Contains("ArmorSurface")||file.Contains("WastelandSurface")||file.Contains("WastelandLeather")||file.Contains("WastelandCloth");
                importer.wrapMode=tiled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.mipmapEnabled=tiled;
                if(tiled){importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;}
                if(file.Contains("/SurvivorMaterials/")||file.Contains("/Environment/")){importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=false;}
                if(file.Contains("/LayeredPlayer/")||file.Contains("/LayeredEnemies/")){importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;}
                importer.SaveAndReimport();
            }
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler"); if(input!=null){input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Ashfall/Build Windows prototype")]
        public static void Build()
        {
            Prepare(); Directory.CreateDirectory("Builds/Windows");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/NeonVault.unity"},locationPathName="Builds/Windows/NeonVault.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+result.summary.result);
            Debug.Log("NEON_VAULT_BUILD_OK");
        }
        public static void BuildHeroReview(){var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/NeonVault.unity"},locationPathName="Builds/Windows/NeonVault.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Hero build failed");Debug.Log("HERO_BUILD_OK");}
        public static void BuildLayeredReview(){Build();LayeredPlayerBaker.Bake();}
        public static void BuildEnemyReview(){Build();EnemyVisualBaker.Bake();}
        public static void BuildQuarterStrideReview(){Build();LayeredPlayerBaker.Bake();CharacterQualityBaker.Bake();}
        public static void BuildExpandedReview(){Build();LayeredPlayerBaker.Bake();EnemyVisualBaker.Bake();PopItemBaker.Bake();}
        public static void BuildHandgunAccuracyReview(){Build();LayeredPlayerBaker.Bake();PopItemBaker.Bake();}
        public static void BuildCombat2DReview(){Build();LayeredPlayerBaker.Bake();EnemyVisualBaker.Bake();PopItemBaker.Bake();}
    }
}
