using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Headless, deterministic integration review. It creates only transient objects and
// does not write PlayerPrefs or touch player save data.
public static class ElementCombatReview
{
    static readonly List<string> failures = new List<string>();
    static int checks;
    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) failures.Add(message);
    }
    static bool Near(float a, float b, float tolerance = .0001f)
        => Math.Abs(a - b) <= tolerance * Math.Max(1f, Math.Max(Math.Abs(a), Math.Abs(b)));
    static Gun Weapon(int model, string id, int pellets = 1, float damage = 10f)
    {
        return new Gun(id, "", Rarity.Rare, GunEffect.Normal, damage, .5f, 20f, 20, pellets)
        { modelVariant = model, catalogId = id, manufacturerId = ManufacturerCatalog.Id(0), accuracy = 90f };
    }
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static FieldInfo Field(Type type,string name)
        => type.GetField(name,PrivateInstance) ?? throw new MissingFieldException(type.FullName,name);
    static MethodInfo Method(Type type,string name)
        => type.GetMethod(name,PrivateInstance) ?? throw new MissingMethodException(type.FullName,name);
    static void ReviewStash(VaultGame game)
    {
        var gameType=typeof(VaultGame);
        game.screen=VaultGame.Screen.Hub;
        var stash=Field(gameType,"stash").GetValue(game);
        var stored=(List<Gun>)stash.GetType().GetField("guns").GetValue(stash);
        var old=JsonUtility.FromJson<Gun>("{\"id\":\"review-old-handgun\",\"name\":\"old handgun\",\"damage\":100,\"interval\":0.5,\"modelVariant\":14,\"pellets\":1}");
        stored.Add(old);
        var journalType=gameType.GetNestedType("StashMove",BindingFlags.NonPublic);
        var journal=JsonUtility.FromJson("{\"deposit\":false,\"category\":0,\"gun\":"+JsonUtility.ToJson(old)+"}",journalType);
        var replay=Method(gameType,"ApplyStashMove");
        replay.Invoke(game,new[]{journal});
        Check(stored.Count==0&&game.Weapons.Count==1&&Near(game.Weapons[0].damage,115f),"old stash journal recovery applies weapon migration");
        replay.Invoke(game,new[]{journal});
        Check(stored.Count==0&&game.Weapons.Count==1&&Near(game.Weapons[0].damage,115f),"replayed stash journal neither duplicates nor rescales gun");
        game.Weapons.Clear();
        old=JsonUtility.FromJson<Gun>("{\"id\":\"review-old-shotgun\",\"name\":\"old shotgun\",\"damage\":12,\"interval\":0.5,\"modelVariant\":13,\"pellets\":7}");
        stored.Add(old);
        bool moved=(bool)Method(gameType,"MoveStash").Invoke(game,new object[]{old,false});
        Check(moved&&stored.Count==0&&game.Weapons.Count==1&&game.Weapons[0].shotgunProfileVersion==1&&game.Weapons[0].pellets>=1&&game.Weapons[0].pellets<=12,"withdraw old shotgun migrates before inventory use");
        Check(Near(game.Weapons[0].damage*game.Weapons[0].pellets,84f,.071f),"withdraw old shotgun retains approximate full-hit damage");
        game.Weapons.Clear(); stored.Clear();
    }
    static Foe CreateFoe(VaultGame game,Transform parent,Vector3 position)
    {
        var obj=new GameObject("review foe");obj.transform.SetParent(parent,false);obj.transform.position=position;
        var foe=obj.AddComponent<Foe>();foe.game=game;foe.kind=0;foe.maxHp=foe.hp=1000;game.Enemies.Add(foe);return foe;
    }
    static void ReviewHits(VaultGame game,Transform parent)
    {
        var map=new bool[16,7];for(int x=0;x<16;x++)for(int z=0;z<7;z++)map[x,z]=true;
        var layout=new SectorLayout(map,new Vector2Int(2,3),new Vector2Int(13,3));
        Field(typeof(VaultGame),"<Layout>k__BackingField").SetValue(game,layout);
        game.screen=VaultGame.Screen.Playing;
        var foes=new List<Foe>();for(int i=0;i<5;i++)foes.Add(CreateFoe(game,parent,layout.Center(new Vector2Int(3+i,3))));
        var obj=new GameObject("review projectile");obj.transform.SetParent(parent,false);var shot=obj.AddComponent<Shot>();
        shot.game=game;shot.damage=100;shot.elementPercent=.2f;shot.elementBonus=1f;shot.elementRadius=2.3f;shot.elementDuration=3;shot.elementChains=2;shot.velocity=Vector3.forward;
        var point=foes[0].transform.position+Vector3.up*.5f;
        shot.element=GunElement.Fire;GunElements.Apply(shot,foes[0],point);
        Check(Near(foes[0].hp,980f),"fire hit applies additional damage to primary target");
        foes[0].hp=1000;shot.elementBonus=1.5f;GunElements.Apply(shot,foes[0],point);
        Check(Near(foes[0].hp,970f),"elemental bonus increases only the additional elemental damage");
        shot.elementBonus=1f;
        foreach(var foe in foes)foe.hp=1000;
        shot.element=GunElement.Electric;GunElements.Apply(shot,foes[0],point);
        Check(Near(foes[0].hp,980f)&&Near(foes[1].hp,980f)&&Near(foes[2].hp,980f)&&Near(foes[3].hp,1000f),"electricity propagates through two hops only");
        foreach(var foe in foes)foe.hp=1000;
        shot.element=GunElement.Explosive;GunElements.Apply(shot,foes[0],point);
        Check(Near(foes[0].hp,980f)&&Near(foes[1].hp,980f)&&Near(foes[2].hp,1000f),"explosion hits nearby enemy without chaining");
        foreach(var foe in foes)foe.hp=1000;
        shot.element=GunElement.Poison;GunElements.Apply(shot,foes[0],point);GunElements.Apply(shot,foes[0],point);
        Check(Near(foes[0].hp,1000f),"poison has no instantaneous additional damage");
        var poison=foes[0].GetComponent<GunPoison>();
        Check(poison!=null,"poison component exists after hits");
        if(poison!=null)
        {
            var stacks=(IList)Field(typeof(GunPoison),"stacks").GetValue(poison);
            Check(stacks.Count==2,"two pellets create two poison stacks");
            foreach(var stack in stacks)stack.GetType().GetField("timer").SetValue(stack,1f);
            Method(typeof(GunPoison),"Update").Invoke(poison,null);
            Check(Near(foes[0].hp,960f),"two poison pellets both contribute to a tick");
        }
        game.Enemies.Clear();
    }
    static void ReviewFiring(VaultGame game,Transform parent)
    {
        var barrelParent=new GameObject("review weapon mount");barrelParent.transform.SetParent(parent,false);
        var barrel=new GameObject("review barrel");barrel.transform.SetParent(barrelParent.transform,false);
        barrel.AddComponent<AimedWeapon>();
        Field(typeof(VaultGame),"barrel").SetValue(game,barrel.transform);
        Field(typeof(VaultGame),"aim").SetValue(game,Vector3.right);
        game.screen=VaultGame.Screen.Playing;
        game.EquippedArtifacts[0]=new ArtifactItem { affixes=new[] { new GearAffix { type="elementDamage", value=.35f } } };
        foreach(int pellets in new[]{1,3,12})
        {
            var world=new GameObject("review shot world "+pellets);world.transform.SetParent(parent,false);
            Field(typeof(VaultGame),"world").SetValue(game,world.transform);
            var gun=Weapon(13,"review.fire."+pellets,pellets,10f);
            gun.effect=GunEffect.Scatter;gun.accuracy=100f;gun.shotgunProfileVersion=1;gun.weaponDamageRevision=1;
            gun.shotgunSpreadDegrees=20f;gun.element=GunElement.Electric;gun.elementPercent=.13f;
            gun.elementRadius=3.1f;gun.elementChains=4;gun.elementDuration=2.5f;
            game.gun=gun;game.ammo=10;game.reload=0;
            float expectedHalfAngle=Mathf.Max(0,Arsenal.SpreadHalfAngle(gun)+(Arsenal.Accuracy(gun)-game.EffectiveAccuracy)*.06f);
            game.Fire();
            var shots=world.GetComponentsInChildren<Shot>(true);
            Check(shots.Length==pellets,"Fire emits configured shotgun pellet count / "+pellets);
            Check(game.ammo==9,"one shotgun trigger consumes exactly one round / "+pellets);
            for(int i=0;i<shots.Length;i++)
            {
                var shot=shots[i];
                Check(shot.friendly&&shot.game==game&&shot.element==GunElement.Electric&&Near(shot.elementPercent,.13f)&&Near(shot.elementRadius,3.1f)&&shot.elementChains==4&&Near(shot.elementDuration,2.5f)&&Near(shot.elementBonus,1.35f),"Fire snapshots element and module bonus / "+pellets+" / "+i);
                float angle=Vector3.SignedAngle(Vector3.right,shot.velocity,Vector3.up);
                if(pellets==1)Check(Math.Abs(angle)<=expectedHalfAngle+.01f,"single pellet is inside shotgun cone");
                else Check(Math.Abs(angle-Mathf.Lerp(-expectedHalfAngle,expectedHalfAngle,i/(pellets-1f)))<.02f,"pellet emitted at configured angle / "+pellets+" / "+i);
            }
            gun.element=GunElement.Fire;gun.elementPercent=.5f;
            foreach(var shot in shots)Check(shot.element==GunElement.Electric&&Near(shot.elementPercent,.13f),"shot snapshot survives weapon change / "+pellets);
        }
        game.EquippedArtifacts[0]=null;
    }
    static void ReviewDamageColors(Transform parent)
    {
        var cameraObject=new GameObject("review damage camera");cameraObject.transform.SetParent(parent,false);
        var camera=cameraObject.AddComponent<Camera>();
        var shotObject=new GameObject("review damage color categories");shotObject.transform.SetParent(parent,false);
        var shot=shotObject.AddComponent<Shot>();
        shot.critical=true;Color critical=shot.DamageNumberColor;
        shot.critical=false;shot.moduleAmplified=true;Color module=shot.DamageNumberColor;
        shot.legendaryEffect=true;Color legendary=shot.DamageNumberColor;
        Check(legendary==Shot.LegendaryDamageColor,"legendary number color takes priority");
        var colors=new[]{GunElements.ColorFor(GunElement.Fire),GunElements.ColorFor(GunElement.Electric),
            GunElements.ColorFor(GunElement.Poison),GunElements.ColorFor(GunElement.Explosive),critical,module,legendary};
        var labels=new[]{"fire","electric","poison","explosive","critical","module","legendary"};
        for(int i=0;i<colors.Length;i++)
        {
            for(int j=0;j<i;j++)
            {
                Color a=colors[i],b=colors[j];
                float distance=Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
                Check(distance>.15f,"distinct damage colors / "+labels[i]+" / "+labels[j]);
            }
            int oldCount=parent.GetComponentsInChildren<TextMesh>(true).Length;
            CombatVisuals.DamageNumber(Vector3.zero,100f,colors[i],camera,parent);
            var texts=parent.GetComponentsInChildren<TextMesh>(true);
            Check(texts.Length==oldCount+1,"damage number created / "+labels[i]);
            if(texts.Length!=oldCount+1)continue;
            var text=texts[texts.Length-1];
            Check(Near(text.color.r,colors[i].r,.004f)&&Near(text.color.g,colors[i].g,.004f)&&Near(text.color.b,colors[i].b,.004f),"text preserves category RGB within Color32 precision / "+labels[i]);
            var floating=text.GetComponent<DamageFloat>();
            Check(floating!=null,"damage float component present / "+labels[i]);
            if(floating!=null)
            {
                Method(typeof(DamageFloat),"LateUpdate").Invoke(floating,null);
                Check(Near(text.color.r,colors[i].r,.004f)&&Near(text.color.g,colors[i].g,.004f)&&Near(text.color.b,colors[i].b,.004f),"animation preserves category RGB within Color32 precision / "+labels[i]);
            }
        }
    }
    public static void Run()
    {
        const string output = "D:/Unity/BuildTemp/ElementCombatReview/result.txt";
        failures.Clear(); checks = 0;
        try
        {
            var counts = new HashSet<int>();
            for (int i = 0; i < 500; i++)
            {
                var gun = Weapon(13, "review.shotgun." + i, 7, 12f);
                float initialDamage = gun.damage * gun.pellets;
                Arsenal.EnsureShotgunProfile(gun);
                counts.Add(gun.pellets);
                Check(gun.pellets >= 1 && gun.pellets <= 12, "shotgun pellet count / " + i);
                Check(gun.shotgunSpreadDegrees >= 5f && gun.shotgunSpreadDegrees < 31f, "shotgun angle / " + i);
                Check(gun.damage * gun.pellets >= initialDamage * .929f && gun.damage * gun.pellets <= initialDamage * 1.071f, "full-hit damage retained / " + i);
                string before = JsonUtility.ToJson(gun);
                Arsenal.EnsureShotgunProfile(gun);
                Check(before == JsonUtility.ToJson(gun), "shotgun migration idempotent / " + i);
                var copy = JsonUtility.FromJson<Gun>(before);
                Check(copy.pellets == gun.pellets && Near(copy.shotgunSpreadDegrees, gun.shotgunSpreadDegrees) && Near(copy.damage, gun.damage), "shotgun save roundtrip / " + i);
                var same = Weapon(13, "review.shotgun." + i, 7, 12f);
                Arsenal.EnsureShotgunProfile(same);
                Check(same.pellets == gun.pellets && Near(same.shotgunSpreadDegrees, gun.shotgunSpreadDegrees), "same design remains stable / " + i);
            }
            Check(counts.Count == 12, "all twelve pellet counts occur across 500 identities");
            foreach (int model in new[] { 12, 13, 14, 15, 16, 17, 2, 7, 8 })
            {
                var gun = Weapon(model, "review.balance." + model, model == 13 ? 7 : 1, 100f);
                Arsenal.EnsureShotgunProfile(gun);
                float initial = gun.damage;
                Arsenal.EnsureWeaponDamageBalance(gun);
                var category = Arsenal.Category(model);
                float expected = category == WeaponClass.Handgun || category == WeaponClass.SniperRifle ? 1.15f : 1f;
                Check(Near(gun.damage, initial * expected), "weapon-class boost / " + model);
                float once = gun.damage;
                Arsenal.EnsureWeaponDamageBalance(gun);
                var loaded = JsonUtility.FromJson<Gun>(JsonUtility.ToJson(gun));
                Arsenal.EnsureWeaponDamageBalance(loaded);
                Check(Near(gun.damage, once) && Near(loaded.damage, once), "weapon-class boost once after save / " + model);
            }

            var rng = new System.Random(10731);
            int[,] observed = new int[6, 5];
            int[] chains = new int[6];
            for (int maker = 0; maker < 6; maker++)
            for (int i = 0; i < 4000; i++)
            {
                var gun = Weapon(12, "review.element");
                gun.manufacturerId = ManufacturerCatalog.Id(maker);
                GunElements.Roll(gun, rng);
                Check((int)gun.element >= 0 && (int)gun.element <= 4, "one valid element / " + maker + "/" + i);
                observed[maker, (int)gun.element]++;
                if (gun.element == GunElement.Electric)
                {
                    Check(gun.elementChains >= 1 && gun.elementChains <= 5, "electric chain range");
                    if (gun.elementChains >= 1 && gun.elementChains <= 5) chains[gun.elementChains]++;
                }
                if (i < 10)
                {
                    var loaded = JsonUtility.FromJson<Gun>(JsonUtility.ToJson(gun));
                    Check(loaded.element == gun.element && loaded.elementVersion == gun.elementVersion && loaded.elementChains == gun.elementChains && Near(loaded.elementPercent, gun.elementPercent) && Near(loaded.elementRadius, gun.elementRadius) && Near(loaded.elementDuration, gun.elementDuration), "element save roundtrip / " + maker + "/" + i);
                    string saved = JsonUtility.ToJson(loaded); GunElements.Roll(loaded, rng);
                    Check(saved == JsonUtility.ToJson(loaded), "saved element does not reroll / " + maker + "/" + i);
                }
            }
            Check(observed[0, 0] > observed[2, 0], "Kestrel has fewer elemental weapons than Voltarc");
            Check(observed[2, 2] > observed[2, 1] && observed[2, 2] > observed[2, 3] && observed[2, 2] > observed[2, 4], "Voltarc electric affinity");
            Check(observed[3, 3] > observed[3, 1] && observed[3, 3] > observed[3, 2] && observed[3, 3] > observed[3, 4], "Mireline poison affinity");
            Check(observed[4, 1] > observed[4, 2] && observed[4, 1] > observed[4, 3] && observed[4, 1] > observed[4, 4], "Cinder fire affinity");
            Check(observed[1, 4] > observed[1, 1] && observed[1, 4] > observed[1, 2] && observed[1, 4] > observed[1, 3], "Ironwake explosive affinity");
            Check(chains[1] > chains[2] && chains[2] > chains[3] && chains[3] > chains[4] && chains[4] > chains[5] && chains[5] > 0, "five-target chains are rare, decreasing by chain count");
            foreach (GunElement element in new[] { GunElement.Fire, GunElement.Electric, GunElement.Poison, GunElement.Explosive })
            {
                var plain = Weapon(12, "review.score");
                var augmented = JsonUtility.FromJson<Gun>(JsonUtility.ToJson(plain));
                augmented.element = element; augmented.elementPercent = .2f;
                augmented.elementDuration = 4; augmented.elementChains = 3;
                Check(GearScoring.Score(augmented) > GearScoring.Score(plain), "element raises score / " + element);
                Check(!string.IsNullOrEmpty(GunElements.Description(augmented, true)) && !string.IsNullOrEmpty(GunElements.Description(augmented, false)), "element description in both languages / " + element);
            }
            var red=GunElements.ColorFor(GunElement.Fire);var yellow=GunElements.ColorFor(GunElement.Electric);
            var purple=GunElements.ColorFor(GunElement.Poison);var orange=GunElements.ColorFor(GunElement.Explosive);
            Check(red.r>red.g*2&&red.r>red.b*2,"fire damage color is red");
            Check(yellow.r>.8f&&yellow.g>.7f&&yellow.b<.3f,"electric damage color is yellow");
            Check(purple.r>purple.g*2&&purple.b>purple.g*2,"poison damage color is purple");
            Check(orange.r>.7f&&orange.g>.15f&&orange.g<.5f&&orange.b<.15f,"explosive damage color is dark orange");
            var legacy = JsonUtility.FromJson<Gun>("{\"name\":\"legacy\",\"damage\":100,\"modelVariant\":14,\"pellets\":1}");
            GearBalance.Migrate(legacy);
            Check(legacy.element == GunElement.None && Near(legacy.damage, 115f), "legacy save keeps no element and gets one-time handgun boost");
            GearBalance.Migrate(legacy);
            Check(Near(legacy.damage, 115f), "legacy save migration is idempotent");

            var affixes = new HashSet<string>();
            string productId = ModuleProductCatalog.Get(0, 0).id;
            for (int i = 0; i < 3000; i++)
            {
                var module = ArtifactCatalog.CreateCatalog(productId, Rarity.Rare, 1, rng);
                foreach (var affix in module.affixes)
                {
                    if (affix == null) continue;
                    affixes.Add(affix.type);
                    if (affix.type.EndsWith("gunDamage") || affix.type == "shotgunDamage" || affix.type == "rifleDamage" || affix.type == "sniperDamage" || affix.type == "heavyDamage")
                        Check(affix.value / module.power >= .0339f, "class-specific bonus exceeds normal base range");
                }
            }
            foreach (string name in new[] { "elementDamage", "handgunDamage", "shotgunDamage", "rifleDamage", "sniperDamage", "heavyDamage" })
                Check(affixes.Contains(name), "module can roll " + name);

            // Inactive host avoids VaultGame.Awake, which reads/writes real save
            // state. smoke suppresses persistence inside storage journal paths.
            var root=new GameObject("element review isolated root");root.SetActive(false);
            try
            {
                var game=root.AddComponent<VaultGame>();
                Field(typeof(VaultGame),"smoke").SetValue(game,true);
                Field(typeof(VaultGame),"world").SetValue(game,root.transform);
                ReviewStash(game);
                ReviewHits(game,root.transform);
                ReviewFiring(game,root.transform);
                ReviewDamageColors(root.transform);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        catch (Exception ex) { failures.Add(ex.ToString()); }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string result = "checks=" + checks + " failures=" + failures.Count + "\n" + string.Join("\n", failures);
        File.WriteAllText(output, result);
        Debug.Log("ELEMENT_COMBAT_REVIEW " + result);
        if (Application.isBatchMode) EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }
}
