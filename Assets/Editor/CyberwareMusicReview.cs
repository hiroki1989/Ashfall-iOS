using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class CyberwareMusicReview
{
 public static void Run(){try{string[] names={"Menu","Explore","Dungeon","Combat"};foreach(string n in names){string path="Assets/Resources/CyberwareBGM/"+n+".ogg";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);var importer=(AudioImporter)AssetImporter.GetAtPath(path);if(!clip||clip.length<60||importer.defaultSampleSettings.loadType!=AudioClipLoadType.Streaming)throw new Exception("Invalid clip or streaming: "+n);Debug.Log("BGM_CHECK "+n+" seconds="+clip.length+" load="+importer.defaultSampleSettings.loadType);}Debug.Log("CYBERWARE_BGM_REVIEW_OK");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
