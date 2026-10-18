using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
namespace NeonVault.Editor {
 public static class EquipmentArtEditorCheck {

 [MenuItem("Neon Vault/Diagnostics/Check Equipment Illustrations")]
 public static void Run(){
 var b=new System.Text.StringBuilder();b.AppendLine("Project="+Application.dataPath);b.AppendLine("Playing="+EditorApplication.isPlaying+" Compiling="+EditorApplication.isCompiling);
 var type=typeof(DistinctEquipmentPortraits);var dict=type.GetField("sheets",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as IDictionary;
 b.AppendLine("Cached sheets="+dict.Count);foreach(DictionaryEntry e in dict){var t=(Texture2D)e.Value.GetType().GetField("texture",BindingFlags.Public|BindingFlags.Instance).GetValue(e.Value);b.AppendLine("CACHE "+e.Key+" live="+(bool)t);}
 int checks=0,failures=0;
 foreach(string maker in new[]{"kestrel","ironwake","voltarc","mireline","cinder","aegis"})foreach(string kind in new[]{"gun-parts","armor-parts","module-parts","armor-accessories"}){
 var path="Art/DistinctEquipment/"+maker+"-"+kind+"-v1";var t=Resources.Load<Texture2D>(path);var j=Resources.Load<TextAsset>(path+"-bounds");var table=j?JsonUtility.FromJson<DistinctEquipmentPortraits.BoundsTable>(j.text):null;
 int expected=kind=="gun-parts"?18:kind=="armor-parts"?36:kind=="module-parts"?12:30;var parsed=j?DistinctEquipmentPortraits.ParseBounds(j.text,expected):null;checks++;if(!t||parsed==null)failures++;
 b.AppendLine(path+" texture="+(t?t.width+"x"+t.height:"MISSING")+" metadata="+(table?.cells?.Length??0)+" first="+(table?.cells?.Length>0?table.cells[0].Rect.ToString():"NONE"));}
 checks++;if(DistinctEquipmentPortraits.ParseBounds("{\"cells\":[{\"x\":0,\"y\":0,\"width\":0,\"height\":0}]}",1)!=null)failures++;b.AppendLine("Checks="+checks+" Failures="+failures);
 Directory.CreateDirectory("D:/Unity/QA-editor-item-art");File.WriteAllText("D:/Unity/QA-editor-item-art/editor-art-check.txt",b.ToString());
 }
 }
}
