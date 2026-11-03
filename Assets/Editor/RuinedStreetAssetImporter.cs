using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor
{
    // Preserve the downloaded source assets; create small, instanced, weathered
    // variants only for street furniture used by the outdoor chunk streamer.
    [InitializeOnLoad]
    public sealed class RuinedStreetAssetImporter : IPreprocessBuildWithReport
    {
        const string Output = "Assets/Resources/ImportedWorld/NewStreet";
        static readonly string[] Sources = {
            "Assets/LowpolyStreetPack/Prefabs/StreetProps/NewsBoard/NewsBoard.prefab",
            "Assets/LowpolyStreetPack/Prefabs/StreetProps/MailBox/MailBox.prefab",
            "Assets/LowpolyStreetPack/Prefabs/StreetProps/Phone/Phone.prefab",
            "Assets/LowpolyStreetPack/Prefabs/StreetProps/RoadBlocks/RoadBlock_A.prefab",
            "Assets/LowpolyStreetPack/Prefabs/StreetProps/Signs/StreetSigns/StreetSign_A.prefab",
            "Assets/ModularLowpolyStreetsFree/Prefabs/Other/Trash_can_1.prefab",
            "Assets/ModularLowpolyStreetsFree/Prefabs/Other/Hydrant.prefab",
            "Assets/ModularLowpolyStreetsFree/Prefabs/Other/Traffic_light.prefab",
            "Assets/transformer_box/prefab/transformer_box.prefab",
            "Assets/FreeOpenBuilding_1.1.2/FOB_Props/Prefabs/WoodPart.prefab"
        };
        static RuinedStreetAssetImporter(){EditorApplication.delayCall+=EnsureAssets;}
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report){EnsureAssets();}
        public static void EnsureAssets()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureAssets;return;}
            if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld","NewStreet");
            for(int i=0;i<Sources.Length;i++)
            {
                string output=Output+"/street-"+i.ToString("00")+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(output))continue;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]);
                if(!source){Debug.LogWarning("Street source missing: "+Sources[i]);continue;}
                var root=PrefabUtility.InstantiatePrefab(source) as GameObject;if(!root)continue;
                int count=0;
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mesh=renderer.GetComponent<MeshFilter>();if(!mesh||!mesh.sharedMesh)continue;
                    var original=renderer.sharedMaterials;var worn=new Material[original.Length];
                    for(int slot=0;slot<original.Length;slot++)
                    {
                        if(!original[slot])continue;
                        string path=Output+"/worn-"+i.ToString("00")+"-"+count+"-"+slot+".mat";
                        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(!mat)
                        {
                            var texture=original[slot].HasProperty("_MainTex")?original[slot].GetTexture("_MainTex"):null;
                            if(!texture&&original[slot].HasProperty("_BaseMap"))texture=original[slot].GetTexture("_BaseMap");
                            var shader=Shader.Find("Standard");if(!shader)continue;
                            mat=new Material(shader){name="Weathered street material",mainTexture=texture,enableInstancing=true};
                            Color baseColor=original[slot].HasProperty("_Color")?original[slot].color:Color.white;
                            mat.color=new Color(baseColor.r*.62f,baseColor.g*.58f,baseColor.b*.52f,1);
                            mat.SetFloat("_Glossiness",.06f);mat.SetFloat("_Metallic",0);
                            AssetDatabase.CreateAsset(mat,path);
                        }
                        worn[slot]=mat;
                    }
                    renderer.sharedMaterials=worn;count++;
                }
                if(count>0)PrefabUtility.SaveAsPrefabAsset(root,output);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
