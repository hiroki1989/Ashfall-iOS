using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonVault.Editor
{
    public static class RuinedCityPrefabReview
    {
        public static void Run()
        {
            const string output="D:/Unity/BuildTemp/RuinedCityPrefabReview";
            Directory.CreateDirectory(output);
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                RuinedCityBuildingImporter.EnsureAssets();
                var camera=new GameObject("Review camera").AddComponent<Camera>();
                camera.orthographic=true;camera.backgroundColor=new Color(.10f,.12f,.13f);camera.clearFlags=CameraClearFlags.SolidColor;
                camera.transform.rotation=Quaternion.Euler(32,42,0);
                var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(48,-35,0);
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.52f);
                var target=new RenderTexture(1100,800,24);camera.targetTexture=target;
                for(int i=0;i<6;i++)
                {
                    string path="Assets/Resources/ImportedWorld/RuinedCityBuildingsV2/city-"+i.ToString("00")+".prefab";
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(!source)throw new Exception("Missing generated building "+path);
                    var root=UnityEngine.Object.Instantiate(source);
                    var bounds=ImportedWorldBatch.SourceBounds(source);
                    if(bounds.size.x<.01f||bounds.size.y<.01f||bounds.size.z<.01f)throw new Exception("Invalid bounds "+path);
                    int boards=0,debris=0;
                    foreach(var child in root.GetComponentsInChildren<Transform>(true))
                    {if(child.name=="Ruin boarded frontage")boards++;if(child.name=="Ruin masonry debris")debris++;}
                    if(boards!=6||debris!=8)throw new Exception("Missing damage details "+path+" "+boards+"/"+debris);
                    camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)*.72f;
                    camera.transform.position=bounds.center-camera.transform.forward*camera.orthographicSize*3;
                    camera.nearClipPlane=.01f;camera.farClipPlane=camera.orthographicSize*8;
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                    File.WriteAllBytes(output+"/building-"+i.ToString("00")+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(root);
                }
                RenderTexture.active=null;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
                File.WriteAllText(output+"/result.txt","PASS: six generated buildings; bounds and 14 damage pieces each; renders captured");
                Debug.Log("RUINED_CITY_PREFAB_REVIEW_OK");
                EditorApplication.Exit(0);
            }
            catch(Exception e)
            {File.WriteAllText(output+"/result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
