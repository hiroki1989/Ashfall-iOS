using UnityEngine;

namespace NeonVault
{
    public enum LootPresentationKind { Weapon, Armor, Module, Scrap, Ammo, AmmoCrate, Chest, Melee, HealthPack }

    public partial class VaultGame
    {
        void ApplyProductDropModel(GameObject root,object item)
        {
            if(!root)return;
            foreach(Transform child in root.transform)if(!child.name.StartsWith("Loot rarity ")){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            Transform model=null;
            if(item is Gun gun)model=ManufacturerEquipment3D.BuildWeapon(root.transform,gun).root;
            else if(item is ArmorItem armor)model=BespokeLegendaryDesigns.Index(armor)>=0?BespokeLegendaryModels3D.BuildArmor(root.transform,armor,BespokeLegendaryDesigns.Index(armor)):DistinctEquipmentModels3D.BuildArmor(root.transform,armor,DistinctEquipmentDesigns.For(armor));
            else if(item is ArtifactItem module)model=DistinctEquipmentModels3D.BuildModule(root.transform,module);
            if(!model)return;
            model.localPosition=Vector3.zero;model.localRotation=Quaternion.Euler(0,35,-12);
            var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=.82f/Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            Vector3 offset=model.position-bounds.center;model.localScale*=scale;model.position=root.transform.position+offset*scale+Vector3.up*.08f;
        }

        /// <summary>Lower-frequency salvage rewards plus ammo drops with visible, testable outcomes.</summary>
        public bool TryRollScrapDrop(bool boss, out int amount)
        {
            amount=0;
            // Modestly reduce both the old 50%/15–40k common reward and 95%/650–1,000k boss reward.
            float chance=VaultBalanceProfile.Current!=null?(boss?VaultBalanceProfile.Current.scrapDropChanceBoss:VaultBalanceProfile.Current.scrapDropChance):(boss?.80f:.45f);
            if(rng==null||rng.NextDouble()>=Mathf.Clamp01(chance))return false;
            amount=ScaleScrap(boss?rng.Next(550000,900001):rng.Next(12000+DungeonIndex*3000,32001+DungeonIndex*3000));
            return true;
        }

        public bool TryRollAmmoDrop(bool boss,AmmoType type,out int amount)
        {
            amount=0;
            float chance=VaultBalanceProfile.Current!=null?(boss?VaultBalanceProfile.Current.ammoDropChanceBoss:VaultBalanceProfile.Current.ammoDropChance):(boss?1f:.64f);
            float quantity=VaultBalanceProfile.Current!=null?VaultBalanceProfile.Current.ammoQuantityMultiplier:1f;
            if(rng==null||AmmoPackSize(type)<=0||rng.NextDouble()>=Mathf.Clamp01(chance))return false;
            amount=Mathf.Max(1,Mathf.RoundToInt(AmmoPackSize(type)*(boss?.80f:.60f)*Mathf.Max(.01f,quantity)));
            return true;
        }

        /// <summary>Localized, self-identifying label for a field ammunition pickup.</summary>
        public string AmmoPickupName(AmmoType type, int amount)
        {
            return VaultLanguage.T(AmmoPickupEnglish(type,amount),AmmoPickupJapanese(type,amount));
        }

        public string AmmoPickupEnglish(AmmoType type,int amount)=>
            (type==AmmoType.Handgun?"Handgun rounds":type==AmmoType.Shotgun?"Shotgun shells":type==AmmoType.SniperRifle?"Sniper rifle rounds":type==AmmoType.AssaultRifle?"Assault rifle rounds":"Heavy machine gun rounds")+" ×"+amount;
        public string AmmoPickupJapanese(AmmoType type,int amount)=>
            (type==AmmoType.Handgun?"ハンドガン弾":type==AmmoType.Shotgun?"ショットガン弾":type==AmmoType.SniperRifle?"スナイパーライフル弾":type==AmmoType.AssaultRifle?"アサルトライフル弾":"ヘビーマシンガン弾")+" ×"+amount;

        /// <summary>Creates a distinct, readable 3D field prop without baking UI text into art.</summary>
        public GameObject CreateLootPresentation(string displayName, Vector3 position, Vector3 size, Color accent,
            LootPresentationKind kind, AmmoType ammoType = AmmoType.AssaultRifle, int amount = 0,
            Rarity rarity = Rarity.Common)
        {
            var root = new GameObject("Loot / " + displayName);
            root.transform.SetParent(world, false);
            root.transform.position = position;
            float sx = Mathf.Max(.25f, size.x), sy = Mathf.Max(.12f, size.y), sz = Mathf.Max(.25f, size.z);
            Color steel = new Color(.20f, .23f, .23f), dark = new Color(.075f, .09f, .09f);

            if(kind==LootPresentationKind.Chest)
            {
                EquipmentChestVisual.Create(root.transform);
                return root;
            }

            if (kind == LootPresentationKind.HealthPack)
            {
                var canvas=new Color(.76f,.75f,.67f);
                art.Part("Health pack / canvas pouch",PrimitiveType.Cube,Vector3.zero,new Vector3(sx,sy,sz),canvas,root.transform);
                art.Part("Health pack / red cross vertical",PrimitiveType.Cube,new Vector3(0,sy*.52f,0),new Vector3(sx*.13f,.025f,sz*.53f),accent,root.transform,true);
                art.Part("Health pack / red cross horizontal",PrimitiveType.Cube,new Vector3(0,sy*.54f,0),new Vector3(sx*.53f,.025f,sz*.13f),accent,root.transform,true);
                for(int side=-1;side<=1;side+=2)
                    art.Part("Health pack / closure",PrimitiveType.Cube,new Vector3(side*sx*.42f,sy*.1f,0),new Vector3(.055f,sy*.7f,sz*.82f),new Color(.17f,.24f,.21f),root.transform);
            }
            else if (kind == LootPresentationKind.Ammo || kind == LootPresentationKind.AmmoCrate)
            {
                // Distinct compact field box with exposed brass casings; caliber is color-coded.
                Color brass = ammoType == AmmoType.Shotgun ? new Color(1f,.47f,.13f) :
                    ammoType == AmmoType.SniperRifle ? new Color(.45f,.77f,.95f) :
                    ammoType == AmmoType.Handgun ? new Color(.84f,.72f,.38f) : new Color(.28f,.83f,.75f);
                art.Part("AMMO CRATE / high-visibility shell", PrimitiveType.Cube, Vector3.zero, new Vector3(sx,sy,sz), new Color(.17f,.25f,.22f), root.transform);
                art.Part("AMMO CRATE / amber lid panel", PrimitiveType.Cube, new Vector3(0,sy*.31f,0), new Vector3(sx*1.03f,.055f,sz*1.03f), new Color(1f,.43f,.08f), root.transform,true);
                for(int side=-1;side<=1;side+=2)art.Part("AMMO CRATE / cyan end stripe",PrimitiveType.Cube,new Vector3(side*sx*.42f,sy*.08f,0),new Vector3(.07f,sy*.72f,sz*1.01f),new Color(.29f,.91f,.82f),root.transform,true);
                art.Part("Ammo box / brass latch", PrimitiveType.Cube, new Vector3(sx*.51f,0,0), new Vector3(.045f,sy*.54f,.13f), brass, root.transform, true);
                int casings = ammoType == AmmoType.Shotgun ? 3 : 5;
                for(int i=0;i<casings;i++)
                {
                    float x=(i-(casings-1)*.5f)*Mathf.Min(.105f,sx*.18f);
                    art.Part("Ammo cartridge / brass case", PrimitiveType.Cylinder, new Vector3(x,sy*.50f,sz*.28f), new Vector3(.065f,.10f,.065f), brass, root.transform);
                    art.Part("Ammo cartridge / dark primer", PrimitiveType.Cylinder, new Vector3(x,sy*.56f,sz*.28f), new Vector3(.035f,.018f,.035f), dark, root.transform);
                }
                art.Part("Ammo box / caliber band", PrimitiveType.Cube, new Vector3(0,sy*.48f,-sz*.12f), new Vector3(sx*.70f,.035f,.04f), brass, root.transform, true);
            }
            else
            {
                art.Part("Salvage crate / reinforced steel", PrimitiveType.Cube, Vector3.zero, new Vector3(sx,sy,sz), steel, root.transform);
                art.Part("Salvage crate / lid seam", PrimitiveType.Cube, new Vector3(0,sy*.34f,0), new Vector3(sx*1.02f,.035f,sz*1.02f), dark, root.transform);
                for(int side=-1;side<=1;side+=2)
                    art.Part("Salvage crate / retaining strap", PrimitiveType.Cube, new Vector3(0,sy*.53f,side*sz*.28f), new Vector3(sx*1.04f,.025f,.055f), new Color(.45f,.25f,.12f), root.transform);

                switch(kind)
                {
                    case LootPresentationKind.Weapon:
                        art.Part("Weapon cache / receiver", PrimitiveType.Cube,new Vector3(0,sy*.70f,0),new Vector3(sx*.72f,.12f,sz*.52f),dark,root.transform);
                        art.Part("Weapon cache / barrel", PrimitiveType.Cylinder,new Vector3(0,sy*.73f,-sz*.34f),new Vector3(.055f,sz*.40f,.055f),accent,root.transform).localRotation=Quaternion.Euler(90,0,0);
                        art.Part("Weapon cache / magazine", PrimitiveType.Cube,new Vector3(0,sy*.50f,sz*.02f),new Vector3(sx*.14f,.15f,.10f),accent,root.transform);
                        break;
                    case LootPresentationKind.Melee:
                        var grip=art.Part("Melee / wrapped grip",PrimitiveType.Cylinder,new Vector3(-sx*.24f,sy*.48f,0),new Vector3(.09f,sy*.92f,.09f),new Color(.30f,.20f,.12f),root.transform);
                        grip.localRotation=Quaternion.Euler(0,0,-34);
                        art.Part("Melee / cross guard",PrimitiveType.Cube,new Vector3(-sx*.02f,sy*.60f,0),new Vector3(sx*.48f,.055f,.11f),accent,root.transform);
                        var blade=art.Part("Melee / forged blade",PrimitiveType.Cube,new Vector3(sx*.22f,sy*.62f,0),new Vector3(sx*.57f,.08f,.15f),steel,root.transform);
                        blade.localRotation=Quaternion.Euler(0,0,-34);
                        var edge=art.Part("Melee / bright cutting edge",PrimitiveType.Cube,new Vector3(sx*.26f,sy*.68f,-.01f),new Vector3(sx*.48f,.025f,.16f),accent,root.transform,true);
                        edge.localRotation=Quaternion.Euler(0,0,-34);
                        art.Part("Melee / pommel",PrimitiveType.Sphere,new Vector3(-sx*.43f,sy*.35f,0),new Vector3(.13f,.10f,.13f),accent,root.transform,true);
                        break;
                    case LootPresentationKind.Armor:
                        art.Part("Armor cache / chest plate", PrimitiveType.Capsule,new Vector3(0,sy*.69f,0),new Vector3(sx*.55f,.12f,sz*.48f),new Color(.36f,.42f,.40f),root.transform);
                        art.Part("Armor cache / riveted badge", PrimitiveType.Cube,new Vector3(0,sy*.78f,sz*.08f),new Vector3(.13f,.04f,.10f),accent,root.transform,true);
                        for(int side=-1;side<=1;side+=2)art.Part("Armor cache / shoulder plate",PrimitiveType.Cube,new Vector3(side*sx*.30f,sy*.70f,0),new Vector3(.11f,.07f,sz*.34f),accent,root.transform);
                        break;
                    case LootPresentationKind.Module:
                        art.Part("Module cache / insulated cradle",PrimitiveType.Cube,new Vector3(0,sy*.67f,0),new Vector3(sx*.58f,.09f,sz*.52f),dark,root.transform);
                        art.Part("Module cache / faceted core",PrimitiveType.Cube,new Vector3(0,sy*.93f,0),new Vector3(.21f,.20f,.21f),accent,root.transform,true).localRotation=Quaternion.Euler(0,45,45);
                        art.Part("Module cache / retaining fork",PrimitiveType.Cube,new Vector3(sx*.22f,sy*.91f,0),new Vector3(.045f,.18f,.28f),steel,root.transform);
                        break;
                    case LootPresentationKind.Scrap:
                        for(int i=0;i<3;i++)
                        {
                            var ingot=art.Part("Scrap / salvaged ingot",PrimitiveType.Cube,new Vector3((i-1)*sx*.19f,sy*.70f,(i%2==0?1:-1)*sz*.12f),new Vector3(sx*.25f,.09f,sz*.28f),i==1?accent:new Color(.55f,.47f,.31f),root.transform);
                            ingot.localRotation=Quaternion.Euler(0,17*i,0);
                        }
                        art.Part("Scrap / stamped copper token",PrimitiveType.Cylinder,new Vector3(0,sy*.82f,0),new Vector3(.16f,.04f,.16f),accent,root.transform,true);
                        break;
                    default:
                        art.Part("Cache / center marker",PrimitiveType.Cube,new Vector3(0,sy*.73f,0),new Vector3(.16f,.06f,.16f),accent,root.transform,true);
                        break;
                }
            }

            if(kind==LootPresentationKind.Weapon||kind==LootPresentationKind.Armor||
               kind==LootPresentationKind.Module||kind==LootPresentationKind.Melee)
                LootRarityBeacon.Add(root.transform,rarity);
            return root;
        }
    }
}
