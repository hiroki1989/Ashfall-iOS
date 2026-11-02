using System;
using System.IO;
using System.Text;
using NeonVault;
using UnityEngine;

// Ephemeral editor audit: never opens a scene or touches saves.
public static class V40CombatReview
{
    public static void Run()
    {
        var lines=new StringBuilder();int checks=0, failures=0;
        void Check(bool pass,string name){checks++;lines.AppendLine((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
        var root=new GameObject("V40 combat review");root.SetActive(false);
        try
        {
            var game=root.AddComponent<VaultGame>();
            var shot=new GameObject("shot").AddComponent<Shot>();shot.transform.SetParent(root.transform);shot.game=game;shot.damage=100;shot.element=GunElement.Poison;shot.elementPercent=.055f;shot.elementBonus=1;shot.elementDuration=3;
            for(int kind=0;kind<=10;kind++)
            {
                var obj=new GameObject("foe-"+kind);obj.transform.SetParent(root.transform);var foe=obj.AddComponent<Foe>();foe.kind=kind;foe.game=game;foe.hp=foe.maxHp=10000;
                GunElements.Apply(shot,foe,Vector3.zero);
                Check((obj.GetComponent<GunPoison>()!=null)==(kind==10),"poison application kind="+kind);
            }
            var bossObj=new GameObject("human-boss");bossObj.transform.SetParent(root.transform);var boss=bossObj.AddComponent<Foe>();boss.kind=3;boss.SetRaiderBoss(true);boss.game=game;boss.hp=boss.maxHp=10000;GunElements.Apply(shot,boss,Vector3.zero);
            Check(bossObj.GetComponent<GunPoison>()!=null,"human boss susceptible");
            foreach(float roll in new[]{.055f,.0725f,.09f,.19f,.22f,.25f}) Check(GunElements.PoisonPercent(roll)*3f>.55f,"poison total exceeds strongest fire for roll "+roll);
            var gun=new Gun("review","",Rarity.Common,GunEffect.Normal,100,.5f,20,20,1){element=GunElement.Poison,elementPercent=.055f,elementDuration=3};
            int oldScore=GearScoring.Score(gun);gun.elementPercent=.19f;Check(oldScore==GearScoring.Score(gun),"legacy poison score equals migrated potency");
            Check(GunElements.Description(gun,true).Contains("19%"),"description displays damage applied");
            shot.element=GunElement.None;shot.legendaryEffect=false;shot.critical=false;shot.moduleAmplified=false;Check(shot.DamageNumberColor==Color.white,"normal damage white");
        }
        catch(Exception ex){failures++;lines.AppendLine("EXCEPTION "+ex);}
        finally{UnityEngine.Object.DestroyImmediate(root);}
        lines.AppendLine("checks="+checks+" failures="+failures);
        const string path="D:/Unity/BuildTemp/V40CombatReview/result.txt";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,lines.ToString());
        Debug.Log(lines.ToString());
        if(failures>0)throw new Exception("V40CombatReview failed; see "+path);
    }
}
