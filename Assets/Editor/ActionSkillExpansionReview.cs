using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Does not read, overwrite, or clear player save data.
public static class ActionSkillExpansionReview
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly List<string> failures=new List<string>();
    static int checks;
    static FieldInfo F(string name)=>typeof(VaultGame).GetField(name,Instance)??throw new MissingFieldException(name);
    static MethodInfo M(string name)=>typeof(VaultGame).GetMethod(name,Instance)??throw new MissingMethodException(name);
    static void Set(VaultGame g,string name,object v)=>F(name).SetValue(g,v);
    static object Get(VaultGame g,string name)=>F(name).GetValue(g);
    static void Check(bool ok,string name){checks++;if(!ok)failures.Add(name);}
    static bool Near(float a,float b)=>Mathf.Abs(a-b)<.0001f;
    static void Unlock(VaultGame game,ActionSkillKind kind,int branch,int tier,int slot)
    {
        int index=(((int)kind*3+branch)*5+tier)*4+slot;
        ((int[])Get(game,"actionSkillExtraRanks"))[index]=1;
        Set(game,"actionSkillExtraValuesDirty",true);
    }
    static object Capture(VaultGame g,bool enabled)=>M("CaptureActionElement").Invoke(g,new object[]{enabled});
    static T Part<T>(object value,string name)=>(T)value.GetType().GetField(name).GetValue(value);
    static void ReviewElements(VaultGame g)
    {
        g.gun=new Gun("review","",Rarity.Rare,GunEffect.Normal,10,.5f,20,20,1);
        foreach(GunElement kind in new[]{GunElement.Fire,GunElement.Electric,GunElement.Poison,GunElement.Explosive})
        {
            g.gun.element=kind;g.gun.elementPercent=.25f;g.gun.elementRadius=2.5f;g.gun.elementDuration=4;g.gun.elementChains=3;
            var inactive=Capture(g,false);var active=Capture(g,true);
            Check(Part<GunElement>(inactive,"element")==GunElement.None,kind+" unavailable without infusion");
            Check(Part<GunElement>(active,"element")==kind&&Near(Part<float>(active,"percent"),.25f)&&Near(Part<float>(active,"radius"),2.5f)&&Near(Part<float>(active,"duration"),4)&&Part<int>(active,"chains")==3,kind+" snapshot preserves parameters");
            g.gun.element=GunElement.None;g.gun.elementPercent=.9f;
            Check(Part<GunElement>(active,"element")==kind&&Near(Part<float>(active,"percent"),.25f),kind+" snapshot unaffected by weapon mutation");
        }
        Check(g.ActionSkillExtraValue("turret_element")==0&&g.ActionSkillExtraValue("grenade_element")==0,"both infusions default off");
        Unlock(g,ActionSkillKind.Mechanic,2,2,3);Check(g.ActionSkillExtraValue("turret_element")>0&&g.ActionSkillExtraValue("grenade_element")==0,"turret infusion independent");
        Unlock(g,ActionSkillKind.Bomber,2,2,3);Check(g.ActionSkillExtraValue("grenade_element")>0,"grenade infusion unlocked independently");
    }
    static void ReviewSaveFormat(VaultGame game)
    {
        Type save=typeof(VaultGame).GetNestedType("ActionSkillSave",BindingFlags.NonPublic);
        var old=JsonUtility.FromJson("{\"version\":1,\"selected\":1,\"ranks\":[2,3]}",save);
        Check((int)save.GetField("version").GetValue(old)==1&&(int)save.GetField("selected").GetValue(old)==1&&((int[])save.GetField("ranks").GetValue(old)).Length==2&&save.GetField("extraRanks").GetValue(old)==null,"legacy save decodes without extra ranks");
        int[] extra=new int[180];extra[123]=1;save.GetField("extraRanks").SetValue(old,extra);
        var copy=JsonUtility.FromJson(JsonUtility.ToJson(old),save);
        Check(((int[])save.GetField("extraRanks").GetValue(copy))[123]==1,"expanded ranks serialize and restore");
    }
    static void ReviewNodes(VaultGame g)
    {
        Unlock(g,ActionSkillKind.Mechanic,0,0,0);
        Check(g.ActionSkillExtraValue("turret_damage")>0,"turret damage node has effect");
        Unlock(g,ActionSkillKind.Mechanic,0,2,3);Check(g.ActionSkillExtraValue("turret_focus")>0,"turret focus node");
        Unlock(g,ActionSkillKind.Mechanic,1,2,3);Check(g.ActionSkillExtraValue("turret_follow")>0,"mid-tier turret follow");
        Unlock(g,ActionSkillKind.Mechanic,1,4,3);Check(g.ActionSkillExtraValue("turret_double")>0,"second turret node");
        Unlock(g,ActionSkillKind.Bomber,0,2,3);Unlock(g,ActionSkillKind.Bomber,1,2,3);
        Check(g.ActionSkillExtraValue("grenade_heal")>0&&g.ActionSkillExtraValue("grenade_suppress")>0,"grenade tactical nodes");
        Unlock(g,ActionSkillKind.Medic,0,2,3);Unlock(g,ActionSkillKind.Medic,1,2,3);Unlock(g,ActionSkillKind.Medic,2,1,3);Unlock(g,ActionSkillKind.Medic,2,2,3);
        Check(g.ActionSkillExtraValue("medic_guard")>0&&g.ActionSkillExtraValue("medic_damage")>0&&g.ActionSkillExtraValue("medic_follow")>0&&g.ActionSkillExtraValue("medic_debuff")>0,"medic tactical nodes");
        g.ResetActionSkills();
        Check(g.ActionSkillSpentPoints==0&&g.ActionSkillExtraValue("turret_element")==0&&g.ActionSkillExtraValue("grenade_element")==0&&g.ActionSkillExtraValue("turret_double")==0,"reset clears extra ranks and cached values");
    }
    static void ClearExtra(VaultGame game)
    {
        Array.Clear((int[])Get(game,"actionSkillExtraRanks"),0,180);
        Set(game,"actionSkillExtraValuesDirty",true);
    }
    static void UnlockEffect(VaultGame game,string id)
    {
        var method=typeof(VaultGame).GetMethod("ExtraEffect",Static);
        for(int k=0;k<3;k++)for(int b=0;b<3;b++)for(int t=0;t<5;t++)for(int slot=0;slot<4;slot++)
            if((string)method.Invoke(null,new object[]{(ActionSkillKind)k,b,t,slot})==id)
            {Unlock(game,(ActionSkillKind)k,b,t,slot);return;}
        throw new Exception("Missing test node: "+id);
    }
    static void UnlockTactical(VaultGame game,string id)
    {
        var method=typeof(VaultGame).GetMethod("ExtraTactical",Static);
        for(int k=0;k<3;k++)for(int b=0;b<3;b++)for(int t=0;t<5;t++)for(int slot=0;slot<4;slot++)
            if((string)method.Invoke(null,new object[]{(ActionSkillKind)k,b,t,slot})==id)
            {Unlock(game,(ActionSkillKind)k,b,t,slot);return;}
        throw new Exception("Missing tactical test node: "+id);
    }
    static bool Property(VaultGame g,string name)=>(bool)typeof(VaultGame).GetProperty(name,Instance).GetValue(g);
    static void ReviewTacticalBoundaries(VaultGame game)
    {
        ClearExtra(game);
        string[] ids={"turret_near_damage","turret_near_reload","turret_near_critical","turret_near_guard","turret_hit_charge","turret_near_element","grenade_hit_heal","grenade_hit_charge","grenade_slow","medic_field_damage","medic_field_reload","medic_field_charge"};
        float[] values={.1f,.15f,.05f,.06f,.1f,.12f,.005f,.15f,.5f,.1f,.15f,.12f};
        for(int i=0;i<ids.Length;i++){Check(Near(game.ActionSkillExtraValue(ids[i]),0),ids[i]+" defaults to zero");UnlockTactical(game,ids[i]);Check(Near(game.ActionSkillExtraValue(ids[i]),values[i]),ids[i]+" exact value");}
        var world=(Transform)Get(game,"world");var player=(Transform)Get(game,"player");
        var turret=new GameObject("tactical review turret").transform;turret.SetParent(world,false);
        var head=new GameObject("tactical review head").transform;head.SetParent(turret,false);
        Set(game,"actionSkillSelected",ActionSkillKind.Mechanic);Set(game,"actionSkillObject",turret);Set(game,"actionSkillHead",head);Set(game,"actionSkillDuration",10f);
        game.gun=new Gun("tactical-review","",Rarity.Rare,GunEffect.Normal,100,.5f,20,20,1){modelVariant=0,accuracy=60,element=GunElement.Fire};
        turret.position=player.position+Vector3.right*8.01f;
        float farDamage=game.EffectiveDamage,farReload=game.EffectiveReload,farCritical=game.EffectiveCriticalChance,farElement=game.ActionSkillElementBonus(GunElement.Fire);
        Check(!Property(game,"PlayerNearActionTurret"),"8.01m outside turret bonus");
        turret.position=player.position+Vector3.right*8f;
        Check(Property(game,"PlayerNearActionTurret"),"8m boundary inside turret bonus");
        Check(game.EffectiveDamage>farDamage&&game.EffectiveReload<farReload&&game.EffectiveCriticalChance>farCritical&&Near(game.ActionSkillElementBonus(GunElement.Fire)-farElement,.12f),"turret positioning changes actual gun stats and element bonus");
        var source=player.GetComponent<AudioSource>()??player.gameObject.AddComponent<AudioSource>();Set(game,"audioSource",source);
        var hitClip=AudioClip.Create("tactical guard test",128,1,8000,false);Set(game,"hitSound",hitClip);
        game.maxHp=1000;game.hp=1000;Set(game,"invincible",0f);game.Hurt(100);float nearHurt=1000-game.hp;
        turret.position=player.position+Vector3.right*8.01f;game.hp=1000;Set(game,"invincible",0f);game.Hurt(100);float farHurt=1000-game.hp;
        Check(Near(nearHurt,farHurt*.94f),"turret guard reduces incoming damage by 6% only within range");
        var second=new GameObject("second tactical turret").transform;second.SetParent(world,false);second.position=player.position+Vector3.left*7.99f;turret.position=player.position+Vector3.right*9f;Set(game,"extraTurretObject",second);
        Check(Property(game,"PlayerNearActionTurret"),"second turret grants nearby bonus independently");
        Set(game,"actionSkillDuration",0f);Check(!Property(game,"PlayerNearActionTurret"),"expired turret grants no nearby bonus");
        Set(game,"extraTurretObject",null);UnityEngine.Object.DestroyImmediate(second.gameObject);
        Set(game,"actionSkillSelected",ActionSkillKind.Medic);Set(game,"actionSkillHead",null);Set(game,"actionSkillDuration",10f);Set(game,"actionSkillRadius",3f);
        turret.position=player.position+Vector3.right*3.01f;float fieldFarDamage=game.EffectiveDamage,fieldFarReload=game.EffectiveReload;
        Check(!Property(game,"PlayerInActionHealingField"),"outside healing field grants no bonus");
        turret.position=player.position+Vector3.right*3f;
        Check(Property(game,"PlayerInActionHealingField")&&game.EffectiveDamage>fieldFarDamage&&game.EffectiveReload<fieldFarReload,"healing field boundary grants gun and reload bonuses");
        Set(game,"actionSkillDuration",0f);Check(!Property(game,"PlayerInActionHealingField"),"expired healing field grants no bonus");
        UnityEngine.Object.DestroyImmediate(hitClip);
        UnityEngine.Object.DestroyImmediate(turret.gameObject);Set(game,"actionSkillObject",null);Set(game,"actionSkillHead",null);
        ClearExtra(game);foreach(string id in ids)Check(Near(game.ActionSkillExtraValue(id),0),id+" removed after reset");
        foreach(string id in ids)UnlockTactical(game,id);
        for(int i=0;i<ids.Length;i++)Check(Near(game.ActionSkillExtraValue(ids[i]),values[i]),ids[i]+" reacquires same value");
    }
    static void ReviewGrenadeBalance(VaultGame game)
    {
        ClearExtra(game);UnlockTactical(game,"grenade_hit_charge");UnlockTactical(game,"grenade_hit_heal");UnlockTactical(game,"grenade_slow");
        var world=(Transform)Get(game,"world");var player=(Transform)Get(game,"player");
        var source=player.GetComponent<AudioSource>()??player.gameObject.AddComponent<AudioSource>();Set(game,"audioSource",source);
        var targetObject=new GameObject("grenade balance target");targetObject.transform.SetParent(world,false);targetObject.transform.position=player.position;
        var target=targetObject.AddComponent<Foe>();target.game=game;target.hp=target.maxHp=10000;game.Enemies.Add(target);
        var type=typeof(VaultGame).GetNestedType("ActionGrenade",BindingFlags.NonPublic);var grenade=Activator.CreateInstance(type,true);
        type.GetField("end").SetValue(grenade,target.transform.position);type.GetField("damage").SetValue(grenade,100f);type.GetField("radius").SetValue(grenade,3f);
        Set(game,"<ActionSkillPoints>k__BackingField",0f);Set(game,"grenadeChargeAwarded",false);((System.Collections.IDictionary)Get(game,"actionGrenadeHits")).Clear();game.maxHp=1000;game.hp=500;
        M("ExplodeActionGrenade").Invoke(game,new[]{grenade});float first=10000-target.hp,charge=game.ActionSkillPoints;
        Check(Near(charge,game.ActionSkillChargeMax*.15f),"first salvo hit refunds exactly 15% action gauge");
        Check(Near(game.hp,505),"grenade hit restores 0.5% maximum HP per target");
        float afterFirst=target.hp;M("ExplodeActionGrenade").Invoke(game,new[]{grenade});float second=afterFirst-target.hp;
        Check(Near(game.ActionSkillPoints,charge),"subsequent explosions in same salvo do not refund again");
        // Foe HP is 10,000, so two float subtractions quantize the measured
        // damage to roughly 0.001. Compare with a small relative tolerance.
        Check(first>second&&Mathf.Abs(second-first/1.4f)<=Mathf.Max(.005f,first*.0001f),"same enemy takes diminishing damage from repeated blasts: first="+first+" second="+second);
        Check(first>0&&first<=100,"blast damage has bounded first-hit damage");
        var extraTargets=new List<Foe>();
        for(int i=0;i<5;i++){var extra=new GameObject("grenade heal cap target");extra.transform.SetParent(world,false);extra.transform.position=target.transform.position;var foe=extra.AddComponent<Foe>();foe.game=game;foe.hp=foe.maxHp=10000;game.Enemies.Add(foe);extraTargets.Add(foe);}
        game.hp=500;M("ExplodeActionGrenade").Invoke(game,new[]{grenade});
        Check(Near(game.hp,525),"grenade healing counts at most five targets even when six are hit");
        foreach(var foe in extraTargets){game.Enemies.Remove(foe);UnityEngine.Object.DestroyImmediate(foe.gameObject);}
        Set(game,"<ActionSkillPoints>k__BackingField",game.ActionSkillChargeMax-.1f);Set(game,"grenadeChargeAwarded",false);M("ExplodeActionGrenade").Invoke(game,new[]{grenade});
        Check(Near(game.ActionSkillPoints,game.ActionSkillChargeMax),"grenade charge refund is capped at gauge maximum");
        game.Enemies.Remove(target);UnityEngine.Object.DestroyImmediate(targetObject);ClearExtra(game);
    }
    static void ReviewSkillShotLegendarySuppression(VaultGame game)
    {
        var world=(Transform)Get(game,"world");var player=(Transform)Get(game,"player");
        var targetObject=new GameObject("skill projectile legendary target");targetObject.transform.SetParent(world,false);targetObject.transform.position=player.position;
        var target=targetObject.AddComponent<Foe>();target.game=game;target.hp=target.maxHp=10000;game.Enemies.Add(target);
        game.gun=new Gun("legendary skill projectile review","",Rarity.Legendary,GunEffect.Normal,10,.5f,20,20,1){modelVariant=0,legendaryKey="gun-0-1"};
        var ready=(System.Collections.IDictionary)Get(game,"legendaryReady");ready.Clear();
        var shotObject=new GameObject("skill projectile");shotObject.transform.SetParent(world,false);shotObject.transform.position=target.transform.position;
        var shot=shotObject.AddComponent<Shot>();shot.game=game;shot.friendly=true;shot.skillProjectile=true;shot.damage=1;shot.hitAccuracy=100;shot.life=5;
        typeof(Shot).GetMethod("Update",Instance).Invoke(shot,null);
        Check(target.hp<10000&&ready.Count==0,"turret shot damages but does not trigger equipped legendary on-hit");
        var normalObject=new GameObject("normal projectile");normalObject.transform.SetParent(world,false);normalObject.transform.position=target.transform.position;
        var normal=normalObject.AddComponent<Shot>();normal.game=game;normal.friendly=true;normal.damage=1;normal.hitAccuracy=100;normal.life=5;
        typeof(Shot).GetMethod("Update",Instance).Invoke(normal,null);
        Check(ready.Contains("gun-0-1"),"normal shot still triggers equipped legendary on-hit");
        game.Enemies.Remove(target);UnityEngine.Object.DestroyImmediate(targetObject);if(shotObject)UnityEngine.Object.DestroyImmediate(shotObject);if(normalObject)UnityEngine.Object.DestroyImmediate(normalObject);
    }
    static void ReviewFullyUpgradedSkills(VaultGame game)
    {
        var ranks=(int[])Get(game,"actionSkillRanks");var extra=(int[])Get(game,"actionSkillExtraRanks");
        for(int i=0;i<ranks.Length;i++)ranks[i]=i%5==4?1:6;
        for(int i=0;i<extra.Length;i++)extra[i]=1;Set(game,"actionSkillExtraValuesDirty",true);
        var world=(Transform)Get(game,"world");var player=(Transform)Get(game,"player");
        game.gun=new Gun("fully-upgraded-skill-review","",Rarity.Rare,GunEffect.Normal,100,.13f,20,20,1){modelVariant=0,accuracy=60};
        Set(game,"actionSkillSelected",ActionSkillKind.Mechanic);M("DeployActionTurret").Invoke(game,null);
        var turret=(Transform)Get(game,"actionSkillObject");var head=(Transform)Get(game,"actionSkillHead");var second=(Transform)Get(game,"extraTurretObject");
        Check(turret&&head&&second,"full mechanic deploys two functioning turrets");
        Check((float)Get(game,"actionSkillDuration")<=22f&&(float)Get(game,"actionSkillRadius")<=35f,"full turret duration and range are bounded");
        var targetObject=new GameObject("fully upgraded skill target");targetObject.transform.SetParent(world,false);targetObject.transform.position=head.position+Vector3.forward*2;
        var target=targetObject.AddComponent<Foe>();target.game=game;target.hp=target.maxHp=100000;game.Enemies.Add(target);
        object[] tick={.1f,head,0f};M("TickActionTurret").Invoke(game,tick);
        Check((float)Get(game,"actionSkillPower")<=game.EffectiveDamage*.9501f,"full primary turret per-projectile damage respects 95% weapon cap");
        Check((float)tick[2]>=.04f&&(float)tick[2]>=1f/(game.EffectiveFireRate*1.3501f),"full turret firing rate respects the 35% bonus and interval floor");
        game.Enemies.Remove(target);UnityEngine.Object.DestroyImmediate(targetObject);
        Set(game,"actionSkillSelected",ActionSkillKind.Bomber);M("ThrowActionGrenades").Invoke(game,null);
        var grenades=(System.Collections.IList)Get(game,"actionGrenades");bool bounded=grenades.Count>0&&grenades.Count<=12;
        float weaponDps=game.EffectiveDamage*Mathf.Max(1,game.gun.pellets)*game.EffectiveFireRate;
        foreach(var grenade in grenades){var type=grenade.GetType();float radius=(float)type.GetField("radius").GetValue(grenade),damage=(float)type.GetField("damage").GetValue(grenade);bounded&=radius<=7.0001f&&damage>0&&damage/Mathf.Max(1,weaponDps)<8f;}
        Check(bounded,"full bomber caps grenade count, radius, and positive finite DPS-relative damage");
        foreach(var grenade in grenades){var visual=(Transform)grenade.GetType().GetField("visual").GetValue(grenade);if(visual)UnityEngine.Object.DestroyImmediate(visual.gameObject);}grenades.Clear();
        // Avoid calling Destroy on runtime objects in EditMode.
        if(turret)UnityEngine.Object.DestroyImmediate(turret.gameObject);if(second)UnityEngine.Object.DestroyImmediate(second.gameObject);
        Set(game,"actionSkillObject",null);Set(game,"actionSkillHead",null);Set(game,"extraTurretObject",null);Set(game,"extraTurretHead",null);
        Set(game,"actionSkillSelected",ActionSkillKind.Medic);M("DeployActionHealingField").Invoke(game,null);
        Check((float)Get(game,"actionSkillDuration")<=24f&&(float)Get(game,"actionSkillPower")<=.09001f,"full medic duration and HP-per-second healing respect caps");
        Check((float)Get(game,"actionSkillRadius")>3f&&(float)Get(game,"actionSkillRadius")<=15f,"full medic healing radius remains within tested balance bound");
        var field=(Transform)Get(game,"actionSkillObject");if(field)UnityEngine.Object.DestroyImmediate(field.gameObject);
        var material=(Material)Get(game,"actionSkillRingMaterial");if(material)UnityEngine.Object.DestroyImmediate(material);
        Set(game,"actionSkillObject",null);Set(game,"actionSkillRingMaterial",null);Array.Clear(ranks,0,ranks.Length);ClearExtra(game);
    }
    static void ReviewLevelBonusAndRespec(VaultGame game)
    {
        game.ResetActionSkills();Set(game,"debtPaid",0);
        Check(game.DebtLevel==1&&game.ActionSkillLevelBonusPoints==0&&game.ActionSkillAvailablePoints==game.ExpeditionTokens,"level one has no level bonus");
        var legacy=(int[])Get(game,"actionSkillRanks");legacy[0]=4;legacy[1]=2;
        int legacySpent=game.ActionSkillSpentPoints;
        Set(game,"debtPaid",1000000000);
        Check(game.DebtLevel==123&&game.ActionSkillLevelBonusPoints==96,"full debt repayment yields level 123 and 96 bonus points");
        Check(legacy[0]==4&&legacy[1]==2&&game.ActionSkillSpentPoints==legacySpent,"new points do not change legacy invested ranks");
        Unlock(game,ActionSkillKind.Mechanic,0,0,0);
        int spent=game.ActionSkillSpentPoints,available=game.ActionSkillAvailablePoints;
        Check(available==122+96+game.ExpeditionTokens-spent,"level points, bonus, tokens and all spent ranks combine");
        game.ResetActionSkills();
        Check(game.ActionSkillSpentPoints==0&&game.ActionSkillAvailablePoints==available+spent&&game.ActionSkillExtraValue("turret_damage")==0,"respec returns both old and expanded invested points");
        Set(game,"debtPaid",0);
    }
    static void ReviewRunBonusesPreserved(VaultGame game)
    {
        // This test exercises actual upgrade paths; smoke mode must also keep
        // tutorial hooks from writing the user's live PlayerPrefs.
        game.ResetActionSkills();Set(game,"debtPaid",1000000000);
        game.runVitality=50f;game.runDamage=.37f;game.runFire=.29f;
        Set(game,"baseMaxHp",150f);game.damageBonus=game.runDamage;game.fireBonus=game.runFire;
        game.hp=100f;M("RecalculateArmor").Invoke(game,null);
        float originalMax=game.maxHp,originalBase=(float)Get(game,"baseMaxHp");
        bool oldBought=game.TryUpgradeActionSkill(ActionSkillKind.Mechanic,0,0);
        Check(oldBought,"base skill can be bought during a run");
        Check(Near(game.maxHp,originalMax)&&Near((float)Get(game,"baseMaxHp"),originalBase)&&Near(game.damageBonus,.37f)&&Near(game.fireBonus,.29f),"base skill purchase preserves run HP, damage and fire rate bonuses");
        bool extraBought=game.TryUpgradeActionSkillExtra(ActionSkillKind.Mechanic,0,0,0);
        Check(extraBought,"expanded skill can be bought during a run");
        Check(Near(game.maxHp,originalMax)&&Near((float)Get(game,"baseMaxHp"),originalBase)&&Near(game.damageBonus,.37f)&&Near(game.fireBonus,.29f),"expanded skill purchase preserves run HP, damage and fire rate bonuses");
        game.ResetActionSkills();
        Check(Near(game.maxHp,originalMax)&&Near((float)Get(game,"baseMaxHp"),originalBase)&&Near(game.damageBonus,.37f)&&Near(game.fireBonus,.29f),"skill reset preserves run HP, damage and fire rate bonuses");
        game.runVitality=game.runDamage=game.runFire=0f;game.damageBonus=game.fireBonus=0f;Set(game,"baseMaxHp",100f);Set(game,"debtPaid",0);
    }
    static void ReviewPassiveStats(VaultGame game)
    {
        ClearExtra(game);
        foreach(var entry in new[]{new KeyValuePair<string,int>("handgun_damage",14),new KeyValuePair<string,int>("shotgun_damage",3),new KeyValuePair<string,int>("assault_damage",0),new KeyValuePair<string,int>("heavy_damage",1),new KeyValuePair<string,int>("sniper_damage",2)})
        {
            ClearExtra(game);game.gun=new Gun("qa-"+entry.Key,"",Rarity.Rare,GunEffect.Normal,50,.5f,20,20,1){modelVariant=entry.Value,accuracy=60};
            float before=game.EffectiveDamage;UnlockEffect(game,entry.Key);
            Check(game.EffectiveDamage>before,entry.Key+" changes damage for matching gun");
        }
        ClearExtra(game);game.gun=new Gun("qa-rifle","",Rarity.Rare,GunEffect.Normal,50,.5f,20,20,1){modelVariant=0,accuracy=60};
        float accuracy=game.EffectiveAccuracy,fireRate=game.EffectiveFireRate,reload=game.EffectiveReload,move=game.EffectiveMoveSpeed,critical=game.EffectiveCriticalChance,critDamage=game.EffectiveCriticalMultiplier,guard=game.DamageReduction;
        M("RecalculateArmor").Invoke(game,null);float maximum=game.maxHp;
        foreach(string id in new[]{"accuracy","fire_rate","reload_speed","move_speed","critical_chance","critical_damage","damage_reduction","max_health","action_charge"})UnlockEffect(game,id);
        M("RecalculateArmor").Invoke(game,null);
        Check(game.EffectiveAccuracy>accuracy,"accuracy node changes actual accuracy");
        Check(game.EffectiveFireRate>fireRate,"fire rate node changes actual fire rate");
        Check(game.EffectiveReload<reload,"reload node reduces actual reload time");
        Check(game.EffectiveMoveSpeed>move,"move node changes actual movement speed");
        Check(game.EffectiveCriticalChance>critical&&game.EffectiveCriticalMultiplier>critDamage,"critical nodes change chance and multiplier");
        Check(game.DamageReduction>guard,"damage reduction node changes actual mitigation");
        Check(game.maxHp>maximum,"max health node changes max HP on recalculation");
        Check(game.ActionSkillExtraValue("action_charge")>0,"action charge node grants a positive charge multiplier");
    }
    static void ReviewMedicGuard(VaultGame game)
    {
        ClearExtra(game);UnlockEffect(game,"medic_guard");
        Set(game,"actionSkillSelected",ActionSkillKind.Medic);Set(game,"actionSkillDuration",10f);Set(game,"actionSkillRadius",3f);
        var field=(Transform)Get(game,"actionSkillObject");var player=(Transform)Get(game,"player");field.position=player.position;
        var source=player.gameObject.AddComponent<AudioSource>();Set(game,"audioSource",source);
        var clip=AudioClip.Create("review hit",128,1,8000,false);Set(game,"hitSound",clip);
        game.maxHp=1000;game.hp=1000;Set(game,"invincible",0f);game.Hurt(100);float inside=1000-game.hp;
        game.hp=1000;Set(game,"invincible",0f);field.position=player.position+Vector3.right*10;game.Hurt(100);float outside=1000-game.hp;
        Check(inside<outside&&Near(inside,outside*.95f),"medic guard reduces damage only inside the field");
        UnityEngine.Object.DestroyImmediate(clip);
    }
    static void ReviewGeneratedSkillElements(VaultGame game)
    {
        var grenades=(System.Collections.IList)Get(game,"actionGrenades");
        foreach(GunElement kind in new[]{GunElement.Fire,GunElement.Electric,GunElement.Poison,GunElement.Explosive})
        for(int unlocked=0;unlocked<2;unlocked++)
        {
            ClearExtra(game);
            if(unlocked!=0){Unlock(game,ActionSkillKind.Mechanic,2,2,3);Unlock(game,ActionSkillKind.Bomber,2,2,3);}
            game.gun=new Gun("generated-element-review","",Rarity.Rare,GunEffect.Normal,20,.5f,20,20,1){modelVariant=0,element=kind,elementPercent=.3f,elementRadius=2.5f,elementDuration=4,elementChains=3};
            var previous=(Transform)Get(game,"actionSkillObject");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var extra=(Transform)Get(game,"extraTurretObject");if(extra)UnityEngine.Object.DestroyImmediate(extra.gameObject);
            M("DeployActionTurret").Invoke(game,null);
            var head=(Transform)Get(game,"actionSkillHead");var turretSnapshot=Get(game,"turretElement");
            var expected=unlocked==0?GunElement.None:kind;
            Check(head!=null&&Part<GunElement>(turretSnapshot,"element")==expected,kind+" generated turret "+(unlocked==0?"without":"with")+" infusion");
            var world=(Transform)Get(game,"world");
            foreach(var oldShot in world.GetComponentsInChildren<Shot>(true))UnityEngine.Object.DestroyImmediate(oldShot.gameObject);
            var targetObj=new GameObject("turret target");targetObj.transform.SetParent(world,false);targetObj.transform.position=head.position+Vector3.forward*2;
            var target=targetObj.AddComponent<Foe>();target.game=game;target.hp=target.maxHp=1000;game.Enemies.Add(target);
            M("TickActionTurret").Invoke(game,new object[]{.1f,head,0f});
            var fired=world.GetComponentsInChildren<Shot>(true);
            bool shotCorrect=fired.Length>0;
            foreach(var shot in fired){shotCorrect&=shot.element==expected;if(unlocked!=0)shotCorrect&=Near(shot.elementPercent,.3f)&&Near(shot.elementRadius,2.5f)&&Near(shot.elementDuration,4)&&shot.elementChains==3;}
            Check(shotCorrect,kind+" emitted turret projectile "+(unlocked==0?"without":"with")+" infusion");
            game.Enemies.Remove(target);UnityEngine.Object.DestroyImmediate(targetObj);
            foreach(var shot in fired)UnityEngine.Object.DestroyImmediate(shot.gameObject);
            foreach(object old in grenades){var visual=(Transform)old.GetType().GetField("visual").GetValue(old);if(visual)UnityEngine.Object.DestroyImmediate(visual.gameObject);}grenades.Clear();
            M("ThrowActionGrenades").Invoke(game,null);
            bool correct=grenades.Count>0;
            foreach(object grenade in grenades)
            {
                var type=grenade.GetType();var visual=(Transform)type.GetField("visual").GetValue(grenade);var snapshot=type.GetField("element").GetValue(grenade);
                correct&=visual!=null&&Part<GunElement>(snapshot,"element")==expected;
                if(unlocked!=0)correct&=Near(Part<float>(snapshot,"percent"),.3f)&&Near(Part<float>(snapshot,"radius"),2.5f)&&Near(Part<float>(snapshot,"duration"),4)&&Part<int>(snapshot,"chains")==3;
            }
            Check(correct,kind+" generated grenades "+(unlocked==0?"without":"with")+" infusion and captured parameters");
        }
        foreach(object grenade in grenades){var visual=(Transform)grenade.GetType().GetField("visual").GetValue(grenade);if(visual)UnityEngine.Object.DestroyImmediate(visual.gameObject);}grenades.Clear();
        ClearExtra(game);
    }
    static void ReviewChargeShots(VaultGame game)
    {
        ClearExtra(game);
        var player=(Transform)Get(game,"player");var world=(Transform)Get(game,"world");
        var barrel=new GameObject("charge review barrel");barrel.transform.SetParent(player,false);barrel.AddComponent<AimedWeapon>();Set(game,"barrel",barrel.transform);
        if(Get(game,"audioSource")==null)Set(game,"audioSource",player.gameObject.AddComponent<AudioSource>());
        game.gun=new Gun("qa-charge","",Rarity.Rare,GunEffect.Normal,50,.5f,20,20,1){modelVariant=0,accuracy=60};game.ammo=10;Set(game,"reload",0f);
        game.Fire();var shots=world.GetComponentsInChildren<Shot>(true);Check(shots.Length>0,"baseline shot created");
        if(shots.Length==0)return;float before=shots[shots.Length-1].actionCharge;
        UnlockEffect(game,"action_charge");game.Fire();shots=world.GetComponentsInChildren<Shot>(true);
        Check(shots.Length>1&&shots[shots.Length-1].actionCharge>before,"action charge node increases fired shot charge");
    }
    static void ReviewDoubleTurret(VaultGame game,Transform root)
    {
        var map=new bool[16,16];for(int x=0;x<16;x++)for(int y=0;y<16;y++)map[x,y]=true;
        var layout=new SectorLayout(map,new Vector2Int(2,2),new Vector2Int(13,13));
        Set(game,"<Layout>k__BackingField",layout);
        var world=new GameObject("skill review world").transform;world.SetParent(root,false);
        var player=new GameObject("skill review player").transform;player.SetParent(world,false);player.position=layout.Center(new Vector2Int(5,5));
        Set(game,"world",world);Set(game,"player",player);Set(game,"art",new VaultArt(world,new List<Material>(),new System.Random(2)));Set(game,"aim",Vector3.forward);
        game.screen=VaultGame.Screen.Playing;
        M("DeployActionTurret").Invoke(game,null);
        Check((Transform)Get(game,"actionSkillHead")!=null&&(Transform)Get(game,"extraTurretHead")==null,"default deployment creates one turret");
        // Avoid Destroy in EditMode; dispose the first temporary world explicitly.
        UnityEngine.Object.DestroyImmediate(world.gameObject);world=new GameObject("two turret world").transform;world.SetParent(root,false);
        player=new GameObject("player").transform;player.SetParent(world,false);player.position=layout.Center(new Vector2Int(5,5));
        Set(game,"world",world);Set(game,"player",player);Set(game,"art",new VaultArt(world,new List<Material>(),new System.Random(3)));
        Unlock(game,ActionSkillKind.Mechanic,1,4,3);M("DeployActionTurret").Invoke(game,null);
        Check((Transform)Get(game,"actionSkillHead")!=null&&(Transform)Get(game,"extraTurretHead")!=null,"two turret node creates two aiming heads");
    }
    public static void Run()
    {
        checks=0;failures.Clear();GameObject root=null;
        try
        {
            root=new GameObject("ActionSkillExpansionReview");root.SetActive(false);
            var game=root.AddComponent<VaultGame>();Set(game,"smoke",true);Set(game,"actionSkillLoaded",true);
            ReviewSaveFormat(game);ReviewElements(game);ReviewNodes(game);ReviewLevelBonusAndRespec(game);ReviewRunBonusesPreserved(game);ReviewPassiveStats(game);ReviewDoubleTurret(game,root.transform);ReviewGeneratedSkillElements(game);ReviewChargeShots(game);ReviewMedicGuard(game);ReviewTacticalBoundaries(game);ReviewGrenadeBalance(game);ReviewSkillShotLegendarySuppression(game);ReviewFullyUpgradedSkills(game);
        }
        catch(Exception e){failures.Add(e.ToString());}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        string result="checks="+checks+" failures="+failures.Count+"\n"+string.Join("\n",failures);
        const string output="D:/Unity/BuildTemp/ActionSkillExpansionReview/result.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,result);Debug.Log("ACTION_SKILL_EXPANSION_REVIEW "+result);
        if(Application.isBatchMode)EditorApplication.Exit(failures.Count==0?0:1);
    }
}

