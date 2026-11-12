#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class KnoxInventoryReview
{
    public static void Run()
    {
        const string pref="Vault.KnoxBody.Review";int previous=PlayerPrefs.GetInt(pref,0);
        var root=new GameObject("Knox inventory review");
        try{
            PlayerPrefs.SetInt(pref,1);
            var game=root.AddComponent<VaultGame>();game.gun=Arsenal.Model(12,Rarity.Common);
            var preview=root.AddComponent<EquipmentHeroPreview>();
            Directory.CreateDirectory("D:/Unity/AssetIntegration/KnoxCandidate");
            foreach(var mode in new[]{"base","chest","legs","equipped"}){
                game.EquippedArmor=new ArmorItem[5];
                if(mode=="chest"||mode=="equipped")game.EquippedArmor[(int)ArmorSlot.Chest]=Armory.TrailWarden(ArmorSlot.Chest);
                if(mode=="legs"||mode=="equipped")game.EquippedArmor[(int)ArmorSlot.Legs]=Armory.TrailWarden(ArmorSlot.Legs);
                if(mode=="equipped"){game.EquippedArmor[(int)ArmorSlot.Arms]=Armory.TrailWarden(ArmorSlot.Arms);game.EquippedArmor[(int)ArmorSlot.Feet]=Armory.TrailWarden(ArmorSlot.Feet);}
                typeof(EquipmentHeroPreview).GetField("lastRender",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(preview,-100f);
                var rt=preview.Render(game) as RenderTexture;if(!rt)throw new Exception("missing portrait texture");
                var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes("D:/Unity/AssetIntegration/KnoxCandidate/inventory-"+mode+".png",tex.EncodeToPNG());
                RenderTexture.active=old;Object.DestroyImmediate(tex);
            }
            Debug.Log("KNOX_INVENTORY_REVIEW_PASS");
        }finally{PlayerPrefs.SetInt(pref,previous);Object.DestroyImmediate(root);}
        EditorApplication.Exit(0);
    }
}
#endif
