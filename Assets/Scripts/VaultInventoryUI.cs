using UnityEngine;
using System;
using System.Collections.Generic;
namespace NeonVault
{
    public partial class VaultGame
    {
        Vector2 weaponScroll,armorScroll,moduleDescriptionScroll;
        bool meleeInventoryPage; Vector2 meleeScroll; Texture2D meleeIconAtlas;
        readonly Dictionary<int,int> runUpgradeCounts=new Dictionary<int,int>();
        public void RecordRunUpgrade(int choice){if(choice>=0&&choice<13)runUpgradeCounts[choice]=runUpgradeCounts.TryGetValue(choice,out int count)?count+1:1;}
        void ClearRunUpgradeHistory()=>runUpgradeCounts.Clear();
        static List<TItem> EquippedFirst<TItem>(List<TItem> owned,Func<TItem,bool> isEquipped)
        {
            var ordered=new List<TItem>(owned.Count);
            foreach(var item in owned)if(isEquipped(item))ordered.Add(item);
            foreach(var item in owned)if(!isEquipped(item))ordered.Add(item);
            return ordered;
        }
        GUIStyle inventoryDetails;
        void InventoryCompare(Rect rect,int score,int reference)
        {
            Color value=reference<=0?Mist:score>=reference?Cyan:new Color(1,.55f,.38f);
            string comparison=reference<=0?T("NO EQUIPPED COMPARISON","比較対象なし"):T("COMPARE ","比較 ")+(score>=reference?"▲ ":"▼ ")+Mathf.Abs(score-reference);
            Text(rect.x,rect.y,comparison,tiny,value,rect.width);
        }
        int LowestEquippedGunScore()
        {
            int low=int.MaxValue;for(int i=0;i<Weapons.Count;i++)if(EquippedWeaponSlot(Weapons[i])>=0)low=Mathf.Min(low,GearScoring.Score(Weapons[i]));
            return low==int.MaxValue?0:low;
        }
        int EquippedArmorScore(ArmorSlot slot){if(EquippedArmor!=null)foreach(var a in EquippedArmor)if(a!=null&&a.slot==slot)return GearScoring.Score(a);return 0;}
        int EquippedModuleScore(ArtifactItem item){if(EquippedArtifacts!=null)foreach(var a in EquippedArtifacts)if(a!=null&&a.effect==item.effect)return GearScoring.Score(a);return 0;}
        string AffixCountLabel(GearAffix[] affixes,Rarity rarity)
        {
            int count=affixes==null?0:affixes.Length;
            return T("AFFIXES ","追加特性 ")+count+(rarity==Rarity.Legendary?T(" + UNIQUE"," + 固有効果"):"");
        }
        string GunNumbers(Gun item,bool equipped=false)
        {
            float rate=1/Mathf.Max(.001f,item.interval);
            float reloadTime=item.reloadSeconds;
            return T("SCORE ","評価 ")+GearScoring.Score(item)+T("  BASE · Lv ","  基礎値 · Lv ")+item.level+T("  DMG ","  威力 ")+GunDamageMin(item).ToString("0.0")+"–"+GunDamageMax(item).ToString("0.0")+T(" × "," × ")+item.pellets+
                T("  RATE ","  連射 ")+rate.ToString("0.0")+T("/s  MAG ","/秒  弾数 ")+item.magazine+T("  RELOAD ","  装填 ")+reloadTime.ToString("0.00")+T("s  SPEED ","秒  弾速 ")+item.speed.ToString("0.0")+T("  ACCURACY ","  命中精度 ")+Arsenal.Accuracy(item).ToString("0.0")+"%"+T("  CRIT ","  会心 ")+(GunCriticalChance(item)*100).ToString("0.#")+"% ×"+GunCriticalMultiplier(item).ToString("0.00")+"\n"+GearScoring.AffixSummary(item);
        }
        void InventoryCopy(float x,float y,string value,GUIStyle style,Color color,float width)
        {GUI.color=color;GUIStyle copy=value!=null&&value.Contains(LegendaryRedOpen)?new GUIStyle(style){richText=true}:style;GUI.Label(new Rect(x,y,width,copy.CalcHeight(new GUIContent(value),width)+4),value,copy);GUI.color=Color.white;}
        float ArmorRowHeight(ArmorItem item)=>Mathf.Max(132,74+inventoryDetails.CalcHeight(new GUIContent(Armory.Name(item)),370)+inventoryDetails.CalcHeight(new GUIContent(LegendaryDescription(item,Armory.Description(item))),370)+inventoryDetails.CalcHeight(new GUIContent(Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item)),370));
        float GunRowHeight(Gun item,bool equipped)=>Mathf.Max(164,94+inventoryDetails.CalcHeight(new GUIContent(VaultLanguage.GunName(item)),362)+inventoryDetails.CalcHeight(new GUIContent(LegendaryDescription(item,VaultLanguage.GunDescription(item))),362)+inventoryDetails.CalcHeight(new GUIContent(GunNumbers(item,equipped)),362));
        bool InventoryAction(Rect rect,string caption)
        {
            bool hover=rect.Contains(Event.current.mousePosition);
            Panel(rect,Cyan*(hover?1:.55f));
            if(hover)Box(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(.16f,.22f,.10f,.65f));
            GUI.color=hover?Color.white:Mist;
            bool clicked=GUI.Button(rect,caption,centered);GUI.color=Color.white;return clicked;
        }
        void EquippedTab(Rect rect)
        {
            Box(rect,new Color(.14f,.20f,.10f,1));
            Box(new Rect(rect.x,rect.y,3,rect.height),Cyan);
            Text(rect.x+10,rect.y+5,T("✓ EQUIPPED","✓ 装備中"),tiny,Cyan,rect.width-12);
        }
        void DrawArtifactInventoryScored()
        {
            Box(new Rect(0,0,1440,900),new Color(.027f,.028f,.023f,.97f));
            Text(48,32,T("FIELD MODULES / TWO SLOTS","拡張モジュール / 2枠"),heading,Gold,1050);
            Text(50,95,T("Permanent equipment. Trigger conditions and cooldowns reset each run.","恒久装備。発動条件と待機時間は周回開始でリセット。"),small,Mist,1100);
            DrawRunBuffLine(118);
            if(Button(new Rect(1190,80,225,48),T("CLOSE [I / ESC]","閉じる [I / ESC]")))ToggleInventory();
            if(Button(new Rect(48,145,340,45),T("WEAPONS / ARMOR","銃 / 防具"))){artifactPage=false;meleeInventoryPage=false;}
            if(Button(new Rect(405,145,225,45),T("MELEE KIT","近接武器"))){artifactPage=false;meleeInventoryPage=true;}
            artifactScroll=GUI.BeginScrollView(new Rect(48,220,1340,520),artifactScroll,new Rect(0,0,1300,Mathf.Max(520,ArtifactInventory.Count*188)));
            var ordered=EquippedFirst(ArtifactInventory,a=>Array.IndexOf(EquippedArtifacts,a)>=0);
            for(int i=0;i<ordered.Count;i++)
            {
                var item=ordered[i];float y=i*188;Panel(new Rect(0,y,1290,178),Arsenal.Colors[(int)item.rarity]);
                ManufacturerBranding.ModulePortrait(new Rect(12,y+10,84,84),item);Text(112,y+10,item.Name+" / Lv "+item.level+" / "+VaultLanguage.RarityName(item.rarity)+" · "+AffixCountLabel(item.affixes,item.rarity),new GUIStyle(small){wordWrap=true},Arsenal.Colors[(int)item.rarity],810);
                GUI.BeginGroup(new Rect(112,y+59,810,62));var moduleDescriptionStyle=new GUIStyle(small){wordWrap=true};moduleDescriptionStyle.normal.textColor=Mist;
                moduleDescriptionStyle.richText=true;string moduleDescription=LegendaryDescription(item,item.Description);
                float moduleDescriptionHeight=Mathf.Max(62,moduleDescriptionStyle.CalcHeight(new GUIContent(moduleDescription),790));
                moduleDescriptionScroll=GUI.BeginScrollView(new Rect(0,0,810,62),moduleDescriptionScroll,new Rect(0,0,790,moduleDescriptionHeight));
                GUI.Label(new Rect(0,0,790,moduleDescriptionHeight),moduleDescription,moduleDescriptionStyle);GUI.EndScrollView();GUI.EndGroup();
                Text(112,y+130,GearScoring.AffixSummary(item),tiny,Cyan,810);
                Text(112,y+153,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese)+" / "+T("SCORE ","評価 ")+GearScoring.Score(item),tiny,Gold,910);
                for(int slot=0;slot<2;slot++)if(Button(new Rect(955+slot*155,y+16,150,44),T("SLOT ","枠 ")+(slot+1)+(EquippedArtifacts[slot]==item?" ✓":"")))EquipArtifact(item,slot);
                if(Array.IndexOf(EquippedArtifacts,item)<0&&Button(new Rect(955,y+74,305,38),T("DISCARD","破棄"))){DiscardArtifact(item);break;}
                if(Array.IndexOf(EquippedArtifacts,item)<0)InventoryCompare(new Rect(955,y+119,300,25),GearScoring.Score(item),EquippedModuleScore(item));
            }
            GUI.EndScrollView();DrawAmmoInventoryStrip(755);Text(50,820,ArtifactInventory.Count+" / "+ArtifactCapacity+T(" owned · Effects require their listed trigger conditions"," 所持 · 記載の発動条件を満たす必要があります"),small,Mist,1200);
        }
        void DrawInventory()
        {
            if(artifactPage){DrawArtifactInventoryScored();return;}
            if(meleeInventoryPage){DrawMeleeInventory();return;}
            if(inventoryDetails==null)inventoryDetails=new GUIStyle(tiny){wordWrap=true,richText=true};
            Box(new Rect(0,0,1440,900),new Color(.027f,.028f,.023f,.97f));
            if(Button(new Rect(950,80,225,48),T("FIELD MODULES","拡張モジュール"))){artifactPage=true;meleeInventoryPage=false;}
            if(Button(new Rect(710,80,225,48),T("MELEE KIT","近接武器"))){artifactPage=false;meleeInventoryPage=true;}
            Text(48,32,T("ARSENAL / EQUIPMENT","武器庫 / 装備"),heading,Gold,1050);
            Text(50,90,T("Own up to 10 guns; equip 2. Choose a slot below. Q / wheel switches equipped guns only.","銃は10本所持・2本装備。下の枠を選んで装備。Q / ホイールは装備銃だけ切替。"),small,Mist,1100);
            DrawRunBuffLine(118);
            if(Button(new Rect(1190,80,225,48),T("CLOSE [I / ESC]","閉じる [I / ESC]")))ToggleInventory();
            Panel(new Rect(40,144,664,638),Gold);Panel(new Rect(726,144,674,638),Cyan);
            Text(60,158,T("WEAPONS  ","所持銃  ")+Weapons.Count+" / 10",label,Gold,620);
            var orderedWeapons=EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0);
            float weaponHeight=0;
            foreach(var item in orderedWeapons)weaponHeight+=GunRowHeight(item,EquippedWeaponSlot(item)>=0);
            weaponScroll=GUI.BeginScrollView(new Rect(52,200,640,564),weaponScroll,new Rect(0,0,610,Mathf.Max(560,weaponHeight)));
            float weaponY=0;
            for(int row=0;row<orderedWeapons.Count;row++)
            {
                Gun item=orderedWeapons[row];int i=Weapons.IndexOf(item),equippedSlot=EquippedWeaponSlot(item);float y=weaponY;bool chosen=equippedSlot>=0;
                float rowHeight=GunRowHeight(item,chosen);weaponY+=rowHeight;
                Panel(new Rect(0,y,606,rowHeight-8),Arsenal.Colors[(int)item.rarity]);
                ItemPortraits.Draw(new Rect(10,y+12,92,92),item);
                Text(10,y+108,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),new GUIStyle(tiny){wordWrap=true},Mist,92);
                float nameHeight=inventoryDetails.CalcHeight(new GUIContent(VaultLanguage.GunName(item)),362);
                Text(112,y+10,VaultLanguage.GunName(item),inventoryDetails,item.color,362);
                Text(112,y+13+nameHeight,VaultLanguage.RarityName(item.rarity)+" · "+AffixCountLabel(item.affixes,item.rarity),tiny,item.color,362);
                Text(112,y+32+nameHeight,T("TYPE  ","銃種  ")+VaultLanguage.GunClassName(item),tiny,Cyan,362);
                float descriptionY=y+52+nameHeight;
                float descriptionHeight=inventoryDetails.CalcHeight(new GUIContent(LegendaryDescription(item,VaultLanguage.GunDescription(item))),362);
                InventoryCopy(112,descriptionY,LegendaryDescription(item,VaultLanguage.GunDescription(item)),inventoryDetails,Mist,362);
                InventoryCopy(112,descriptionY+descriptionHeight+5,GunNumbers(item,chosen),inventoryDetails,chosen?Cyan:Mist,362);
                if(chosen){EquippedTab(new Rect(482,y+10,114,30));Text(486,y+43,T("SLOT ","枠 ")+(equippedSlot+1)+(item==gun?T(" · ACTIVE"," · 使用中"):""),tiny,Cyan,112);}
                else InventoryCompare(new Rect(482,y+43,122,20),GearScoring.Score(item),LowestEquippedGunScore());
                int slotCount=Mathf.Min(4,AvailableWeaponSlotCount);
                for(int slot=0;slot<slotCount;slot++)if(InventoryAction(new Rect(482+slot*28,y+64,26,34),(slot+1).ToString()))EquipWeapon(i,slot);
                if(Weapons.Count>1 && InventoryAction(new Rect(482,y+104,114,32),T("DISCARD","破棄"))){DiscardWeapon(i);break;}
            }
            GUI.EndScrollView();
            Text(746,158,T("ARMOR / FOUR SLOTS","防具 / 4部位"),label,Cyan,620);
            for(int i=1;i<5;i++)
            {
                ArmorItem equipped=EquippedArmor!=null&&i<EquippedArmor.Length?EquippedArmor[i]:null;
                float y=198+(i-1)*42;
                if(equipped!=null)ItemPortraits.Draw(new Rect(746,y,34,34),equipped);
                Text(792,y+6,Armory.SlotName((ArmorSlot)i),small,Mist,66);
                Text(861,y+6,equipped==null?T("— EMPTY —","— 未装備 —"):Armory.Name(equipped),small,equipped==null?Mist:Cyan,420);
                if(equipped!=null)Box(new Rect(1372,y+10,5,15),Cyan);
                if(equipped!=null&&InventoryAction(new Rect(1310,y+2,78,34),T("REMOVE","外す")))UnequipArmor(i);
            }
            Text(746,420,T("OWNED ARMOR  ","所持防具  ")+ActiveArmorCount+" / 50",small,Gold,610);
            float armorContentHeight=0;
            foreach(var item in ArmorInventory.FindAll(Armory.IsActiveArmor))armorContentHeight+=ArmorRowHeight(item);
            armorScroll=GUI.BeginScrollView(new Rect(738,451,650,313),armorScroll,new Rect(0,0,620,Mathf.Max(308,armorContentHeight)));
            float armorY=0;
            var orderedArmor=EquippedFirst(ArmorInventory.FindAll(Armory.IsActiveArmor),a=>Array.IndexOf(EquippedArmor,a)>=0);
            for(int i=0;i<orderedArmor.Count;i++)
            {
                ArmorItem item=orderedArmor[i];bool chosen=false;
                if(EquippedArmor!=null)foreach(var equipped in EquippedArmor)if(ReferenceEquals(equipped,item))chosen=true;
                float y=armorY,rowHeight=ArmorRowHeight(item);armorY+=rowHeight;
                Color rarityColor=Arsenal.Colors[(int)item.rarity];Panel(new Rect(0,y,616,rowHeight-8),rarityColor);
                ItemPortraits.Draw(new Rect(10,y+10,92,92),item);
                Text(10,y+107,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),new GUIStyle(tiny){wordWrap=true},Mist,92);
                float nameHeight=inventoryDetails.CalcHeight(new GUIContent(Armory.Name(item)),370);
                Text(112,y+8,Armory.Name(item),inventoryDetails,rarityColor,370);
                Text(112,y+11+nameHeight,Armory.SlotName(item.slot)+" / Lv "+item.level+" / "+VaultLanguage.RarityName(item.rarity)+" · "+AffixCountLabel(item.affixes,item.rarity),tiny,rarityColor,370);
                float descriptionY=y+32+nameHeight;
                float descriptionHeight=inventoryDetails.CalcHeight(new GUIContent(LegendaryDescription(item,Armory.Description(item))),370);
                InventoryCopy(112,descriptionY,LegendaryDescription(item,Armory.Description(item)),inventoryDetails,Mist,370);
                InventoryCopy(112,descriptionY+4+descriptionHeight,Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item),inventoryDetails,Cyan,370);
                if(!chosen)InventoryCompare(new Rect(492,y+97,114,20),GearScoring.Score(item),EquippedArmorScore(item.slot));
                if(chosen)EquippedTab(new Rect(492,y+8,114,30));
                else if(InventoryAction(new Rect(492,y+8,114,40),T("EQUIP","装備")))EquipArmor(item);
                if(!chosen && InventoryAction(new Rect(492,y+56,114,38),T("DISCARD","破棄"))){DiscardArmor(item);break;}
            }
            GUI.EndScrollView();
            DrawAmmoInventoryStrip(765);
            Panel(new Rect(40,800,1360,72),Gold);
            Text(60,812,T("TOTAL / ","総合 / ")+T("MOVE ","移動 ")+EffectiveMoveSpeed.ToString("0.0")+
                T("  DAMAGE REDUCTION ","  被ダメ軽減 ")+(DamageReduction*100).ToString("0")+T("%  REGEN ","%  HP回復 ")+HealthRegen.ToString("0.0")+T("/s","/秒"),small,Cyan,1300);
            if(gun!=null)Text(60,841,T("EQUIPPED BASE / ","装備銃の基礎値 / ")+GunNumbers(gun,true),small,Cyan,1300);
        }
        string RunBuffSummary()
        {
            if(screen!=Screen.Playing&&screen!=Screen.Pause&&screen!=Screen.Reward)return T("UPGRADES: none active","強化: 有効な効果なし");
            var parts=new List<string>();
            if(runDamage>.001f)parts.Add(T("DMG ","威力 ")+"+"+(runDamage*100).ToString("0")+"%");
            if(runFire>.001f)parts.Add(T("FIRE ","連射 ")+"+"+(runFire*100).ToString("0")+"%");
            if(runVitality>.01f)parts.Add(T("HP ","HP ")+"+"+runVitality.ToString("0"));
            if(runDash>.001f)parts.Add(T("DASH ","回避 ")+"+"+(runDash*100).ToString("0")+"%");
            if(runChain>.001f)parts.Add(T("CHAIN ","連鎖 ")+"+"+(runChain*100).ToString("0")+"%");
            if(runHarvest>.01f)parts.Add(T("KILL HEAL ","撃破回復 ")+"+"+runHarvest.ToString("0.#"));
            if(ArtifactShield>.01f)parts.Add(T("BARRIER ","障壁 ")+ArtifactShield.ToString("0.#"));
            if(ArtifactDamageMultiplier>1.001f)parts.Add(T("MOD DMG ","モジュール威力 ")+"×"+ArtifactDamageMultiplier.ToString("0.00"));
            if(reloadSurge>0)parts.Add(T("RELOAD ","装填強化 ")+reloadSurge.ToString("0.0")+T("s","秒"));
            string[] augments={T("OVERCHARGE","過負荷"),T("REINFORCED SHELL","強化装甲"),T("RAPID CYCLER","高速機構"),T("REPAIR PROTOCOL","修復手順"),T("SHOCK BOOSTER","衝撃増幅"),T("TESLA BRIDGE","電弧連鎖"),T("BIO RECOVERY","生体回収"),T("BALANCED FORCE","均衡火力"),T("FIELD MEDIC","野戦手当"),T("MOBILE RECOVERY","機動回復"),T("ARC CYCLER","連鎖機構"),T("STRIKE RUNNER","突撃機動"),T("FIELD RESUPPLY","現地補給")};
            foreach(var kv in runUpgradeCounts)if(kv.Key>=0&&kv.Key<augments.Length)parts.Add(augments[kv.Key]+(kv.Value>1?" ×"+kv.Value:""));
            return T("UPGRADES: ","強化: ")+(parts.Count==0?T("none active","有効な効果なし"):string.Join("  ·  ",parts));
        }
        void DrawRunBuffLine(float y)
        {
            string summary=RunBuffSummary();var style=new GUIStyle(tiny){fontSize=11,wordWrap=true};style.normal.textColor=Cyan;
            GUI.Label(new Rect(50,y-5,1330,30),summary,style);
        }
        string MeleeArtifactState(ArtifactItem item)
        {
            if(item.effect==ArtifactEffect.KillShield)return ArtifactShield>0?T("barrier active","障壁発動中"):T("on kill","敵撃破で発動");
            if(item.effect==ArtifactEffect.DashPulse)return T("on dash","ダッシュで発動");
            if(item.effect==ArtifactEffect.ReloadSurge)return ArtifactReloadSurgeRemaining(item)>0?T("surge active","強化発動中"):T("on reload","装填完了で発動");
            return ArtifactShield>0?T("conversion active","変換効果発動中"):T("requires barrier","障壁が必要");
        }
        void DrawAmmoInventoryStrip(float y)
        {
            Panel(new Rect(40,y,1360,30),Cyan);
            for(int i=0;i<ActiveAmmoTypeCount;i++){var type=ActiveAmmoTypeAt(i);Text(58+i*335,y+5,AmmoLabel(type)+"  "+AmmoReserve(type)+T(" in reserve"," 発"),tiny,Mist,325);}
        }
        internal void DrawMeleeIcon(Rect rect,MeleeWeaponFamily family)
        {
            if(meleeIconAtlas==null)meleeIconAtlas=Resources.Load<Texture2D>("Art/MeleeWeaponIcons");
            if(meleeIconAtlas!=null){float width=.2f;GUI.DrawTextureWithTexCoords(rect,meleeIconAtlas,new Rect((int)family*width,0,width,1),true);return;}
            Box(rect,new Color(.08f,.12f,.15f));Diamond(rect.center.x,rect.center.y,Mathf.Min(rect.width,rect.height)*.36f,Gold);
            Text(rect.center.x-11,rect.center.y-12,((int)family+1).ToString(),tiny,Color.white,26);
        }
        void DrawMeleeInventory()
        {
            Box(new Rect(0,0,1440,900),new Color(.027f,.028f,.023f,.97f));
            Text(48,32,T("MELEE KIT / FIELD BLADES","近接装備 / フィールドブレード"),heading,Gold,1050);
            Text(50,90,T("Carry a quiet backup for close quarters. Knives, shivs, cleavers, machetes and hatchets are tracked separately from firearms.","近距離用の予備武器。ナイフ、シヴ、クリーバー、マチェット、手斧を銃器とは別に管理。"),small,Mist,1100);
            DrawRunBuffLine(118);
            if(Button(new Rect(710,80,225,48),T("WEAPONS / ARMOR","銃 / 防具"))){meleeInventoryPage=false;artifactPage=false;}
            if(Button(new Rect(950,80,225,48),T("FIELD MODULES","拡張モジュール"))){artifactPage=true;meleeInventoryPage=false;}
            if(Button(new Rect(1190,80,225,48),T("CLOSE [I / ESC]","閉じる [I / ESC]")))ToggleInventory();
            Panel(new Rect(40,150,1360,600),Gold);Text(60,164,T("OWNED MELEE WEAPONS  ","所持近接武器  ")+MeleeWeaponInventory.Count+" / "+MeleeWeaponCapacity,label,Gold,700);
            meleeScroll=GUI.BeginScrollView(new Rect(60,205,1320,520),meleeScroll,new Rect(0,0,1290,Mathf.Max(510,MeleeWeaponInventory.Count*112)));
            float y=0;foreach(var item in MeleeWeaponInventory)
            {
                Color rarityColor=item.color;Panel(new Rect(0,y,1290,92),rarityColor);
                DrawMeleeIcon(new Rect(18,y+20,48,48),item.family);
                Text(88,y+7,MeleeWeaponCatalog.Name(item)+"  /  Lv "+item.level+" / "+VaultLanguage.RarityName(item.rarity),label,rarityColor,640);
                Text(88,y+42,T("SCORE ","評価 ")+MeleeWeaponCatalog.Score(item)+"  ·  "+T("DMG ","威力 ")+item.damage+"  ·  "+T("RATE ","間隔 ")+(1/Mathf.Max(.01f,item.interval)).ToString("0.0")+T("/s  ·  RANGE ","/秒  ·  射程 ")+item.range.ToString("0.0")+"m  ·  "+T("ARC ","攻撃角 ")+item.arcDegrees.ToString("0")+"°"+(item.uniqueEffect?LegendaryRedOpen+T("  ·  UNIQUE EFFECT","  ·  固有効果")+LegendaryRedClose:""),small,Mist,710);
                if(item==EquippedMeleeWeapon)EquippedTab(new Rect(890,y+8,150,28));
                else if(InventoryAction(new Rect(1070,y+8,125,36),T("EQUIP","装備")))EquipMeleeWeapon(item);
                if(item!=EquippedMeleeWeapon)InventoryCompare(new Rect(890,y+43,160,20),MeleeWeaponCatalog.Score(item),MeleeWeaponCatalog.Score(EquippedMeleeWeapon));
                if(item!=EquippedMeleeWeapon&&MeleeWeaponInventory.Count>1&&InventoryAction(new Rect(1210,y+52,125,34),T("DISCARD","破棄"))){MeleeWeaponInventory.Remove(item);SaveInventory();break;}
                y+=112;
            }
            GUI.EndScrollView();
            if(EquippedMeleeWeapon!=null)Text(60,770,T("EQUIPPED BACKUP / ","装備中の近接武器 / ")+MeleeWeaponCatalog.Name(EquippedMeleeWeapon)+" / "+VaultLanguage.RarityName(EquippedMeleeWeapon.rarity),small,EquippedMeleeWeapon.color,700);
            DrawAmmoInventoryStrip(812);
        }
    }
}

