#if UNITY_EDITOR
using System;
using NeonVault;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Exercises chest and trouser transitions without depending on a saved game.
public static class KnoxSleevelessReview
{
    public static void Run()
    {
        var root=new GameObject("Knox sleeveless review");
        var hero=DetailedHero3D.Create(root.transform,DetailedHeroMaterials3D.Get);
        var knox=KnoxBodyVisual.Create(hero);
        if(!knox)throw new Exception("Knox model unavailable");
        var asm=typeof(KnoxBodyVisual).Assembly;
        var factory=asm.GetType("NeonVault.GeneratedWardenPreviewAssets");
        var rig=asm.GetType("NeonVault.GeneratedWardenRig");
        var source=(GameObject)factory.GetMethod("CreateCharacterPart").Invoke(null,new object[]{"armor",hero});
        var skin=(SkinnedMeshRenderer)rig.GetMethod("Bind").Invoke(null,new object[]{source,hero,false,true,true});
        var armor=(SkinnedMeshRenderer[])rig.GetMethod("SplitArmorBySlot").Invoke(null,new object[]{skin,root.transform});
        knox.FitArmorBoundary(armor);
        ArmorItem item=null;
        for(int i=0;i<200&&item==null;i++){
            var id="distinct.armor."+i.ToString("000");
            if(ArmorProductCatalog.Find(id)==null)continue;
            var candidate=Armory.CreateCatalog(id,Rarity.Common,1);
            if(SharedArmorSleeves.IsSleeveless(candidate))item=candidate;
        }
        if(item==null)throw new Exception("No sleeveless catalog item found");
        var gear=new ArmorItem[5];gear[1]=item;var sleeve=new SharedArmorSleeves();
        try {
            armor[1].enabled=true;
            knox.SetCovered(new[]{false,true,false,false,false});
            var partial=armor[1].sharedMesh;
            knox.PrepareSleevelessChest();sleeve.Apply(armor[1],null,null,gear);
            var open=armor[1].sharedMesh;
            if(open==partial)throw new Exception("Sleeveless mesh not applied");
            knox.SetCovered(new[]{false,true,false,false,false},true);
            if(armor[1].sharedMesh!=open)throw new Exception("Knox overwrote sleeveless mesh");
            knox.SetCovered(new[]{false,true,false,true,false},true);
            if(armor[1].sharedMesh!=open)throw new Exception("Adding trousers replaced sleeveless mesh");
            knox.SetCovered(new[]{false,true,false,false,false},false);
            if(armor[1].sharedMesh!=partial)throw new Exception("Normal partial chest not restored");
            Debug.Log("KNOX_SLEEVELESS_REVIEW_PASS "+item.catalogId);
        } finally { sleeve.Dispose();Object.DestroyImmediate(root); }
        EditorApplication.Exit(0);
    }
}
#endif
