using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public bool MobileModalVisible=>InventoryOpen||manufacturerRegistry||DebtIntroVisible||WorldBossConversationVisible||screen!=Screen.Playing&&screen!=Screen.Hub;
        public static Matrix4x4 MobileCanvasMatrix
        {
            get
            {
                Rect s=MobileTouchControls.SafeRect;if(s.width<1)s=new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height);
                // Fit the mobile canvas to the complete safe area. The old uniform
                // scale letterboxed landscape phones and made every label tiny.
                return Matrix4x4.TRS(new Vector3(s.x,s.y,0),Quaternion.identity,new Vector3(s.width/1440f,s.height/900f,1));
            }
        }
        Vector2 MobileCanvasPoint(Vector2 bottomScreen)=>MobileCanvasMatrix.inverse.MultiplyPoint3x4(new Vector3(bottomScreen.x,UnityEngine.Screen.height-bottomScreen.y,0));
        bool MobileButton(Rect r,string caption){Panel(r,Gold);Box(new Rect(r.x+2,r.y+4,r.width-4,r.height-6),new Color(.15f,.24f,.30f));Box(new Rect(r.x+2,r.y+4,6,r.height-8),Gold);var style=new GUIStyle(centered){fontSize=38,fontStyle=FontStyle.Bold,wordWrap=true,clipping=TextClipping.Clip};style.normal.textColor=UiPaper;style.hover.textColor=Gold;style.active.textColor=Gold;return GUI.Button(r,caption,style);}
        void DrawMobileMenu()
        {
            if(menuArt)GUI.DrawTexture(new Rect(0,0,1440,900),menuArt,ScaleMode.ScaleAndCrop);
            for(int i=0;i<40;i++)Box(new Rect(i*25,0,26,900),new Color(.035f,.055f,.065f,.97f*Mathf.Pow(1-i/40f,.4f)));
            Text(60,90,T("LAST LIGHT  /  SURVIVOR OPERATIONS","最後の灯  /  生存者の記録"),new GUIStyle(label){fontSize=22,fontStyle=FontStyle.Bold},Gold,680);
            Text(54,146,"ASHFALL",new GUIStyle(title){fontSize=104},UiPaper,850);
            Box(new Rect(60,300,72,4),Gold);
            Text(60,330,T("SCAVENGE. SURVIVE. RETURN.","拾え。生き延びろ。帰ってこい。"),new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Bold},UiPaper,720);
            Text(60,390,T("Move left. Aim right. Hold the gold trigger.","左で移動。右で照準。金色の射撃ボタンを長押し。"),new GUIStyle(label){fontSize=22,wordWrap=true},Mist,700);
            if(MobileButton(new Rect(58,470,650,112),T("CONTINUE / ASHFALL","アッシュフォールへ")))ContinueOverworldGame();
            if(MobileButton(new Rect(58,605,650,112),T("EQUIPMENT","装備を確認")))ToggleInventory();
            if(HasSavedRun&&MobileButton(new Rect(58,740,650,100),T("CONTINUE SAVED EXPEDITION","保存した探索を再開")))ResumeSavedRun();
            Text(60,855,T("Landscape / iPhone + iPad","横画面 / iPhone・iPad"),small,Mist,650);
        }
        void DrawMobileHub()
        {
            Panel(new Rect(24,22,620,116),Gold);Text(42,36,OverworldCampName(LastVisitedCamp,VaultLanguage.Japanese),new GUIStyle(label){fontSize=28},Gold,595);
            Text(42,84,T("LEVEL ","レベル ")+DebtLevel+T("  SCRAP ","  スクラップ ")+Credits.ToString("N0"),new GUIStyle(label){fontSize=25},Cyan,595);
            if(MobileButton(new Rect(690,30,270,110),T("CITY MAP","都市マップ")))OpenOverworldMap();
            if(MobileButton(new Rect(995,30,410,110),T("EXIT TO CITY","街へ出る")))ExitCampToOverworld();
            if(MobileButton(new Rect(690,157,270,110),T("MERCHANT","装備商人"))){debtPage=false;debtSkillTreePage=false;shopSellingPage=false;screen=Screen.Shop;}
            if(MobileButton(new Rect(690,284,270,110),T("MAKERS","製品図鑑"))){manufacturerRegistry=true;manufacturerKind=-1;}
            string hint=Vector3.Distance(PlayerPosition,debtCollectorPosition)<3?T("USE: BRIGGS / DEBT + SKILLS","調べる：ブリッグス / 返済・スキル"):Vector3.Distance(PlayerPosition,merchantPosition)<3?T("USE: MARA / MERCHANT","調べる：マラ / 装備商人"):Vector3.Distance(PlayerPosition,archivistPosition)<3?T("USE: IONE / ADVICE","調べる：イオネ / 探索の助言"):T("Move to an NPC and tap USE","NPCへ近づいて「調べる」");
            Text(380,740,string.IsNullOrEmpty(hubDialogue)?hint:hubDialogue,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,680);
            if(toastTimer>0){Panel(new Rect(30,170,620,145),Gold);Text(50,185,Toast,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,580);}
        }
        void DrawMobileHUD()
        {
            if(gun==null)return;
            DrawCriticalHealthVignette();
            DrawHealthPanel(new Rect(20,20,420,125),true);
            Text(46,106,(room+1)+" / "+CurrentDungeon.AreaCount+"  "+(SelectedExpeditionStage==null?CurrentDungeon.Name:(SelectedStageId+1).ToString("00")+" / "+SelectedExpeditionStage.Name),new GUIStyle(label){fontSize=19,wordWrap=false,clipping=TextClipping.Clip},Mist,370);
            DrawHeldWeaponHUD(new Rect(456,20,370,104));
            int alive=0;foreach(var enemy in Enemies)if(enemy&&!enemy.dead)alive++;
            Text(38,154,ExpeditionExitOpen?T("EXIT OPEN / approach and tap USE","出口開放 / 近づいて「調べる」"):T("HOSTILES ","残敵 ")+alive,new GUIStyle(label){fontSize=25},ExpeditionExitOpen?Gold:Mist,740);
            if(toastTimer>0)Text(360,210,Toast,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,670);
            Rect mapRect=new Rect(1165,165,250,150);if(GUI.Button(mapRect,GUIContent.none,GUIStyle.none))OpenOverworldMap();Box(mapRect,new Color(.035f,.055f,.075f,.74f));
            if(Layout!=null)
            {
                float tile=Mathf.Min(220f/Layout.Columns,122f/Layout.Rows);Vector2 origin=new Vector2(1180,179);
                for(int x=0;x<Layout.Columns;x++)for(int z=0;z<Layout.Rows;z++)if(Layout.open[x,z]&&Layout.IsExplored(x,z))Box(new Rect(origin.x+x*tile,origin.y+(Layout.Rows-1-z)*tile,Mathf.Max(1,tile-.5f),Mathf.Max(1,tile-.5f)),Layout.IsVisible(x,z)?new Color(.28f,.44f,.46f):new Color(.13f,.2f,.22f));
                DrawTacticalMapMarkers(origin,tile,7);MapPoint(PlayerPosition,origin,tile,Cyan,5);var exit=Layout.ToCell(ExitPosition);if(Layout.IsExplored(exit.x,exit.y))MapPoint(ExitPosition,origin,tile,Gold,5);
            }
            if(Active&&!InventoryOpen)
            {
                Vector3 worldAim=VaultMobileInputBridge.ToWorldMovement(MobileTouchControls.Aim).normalized;
                Vector3 p=cam.WorldToScreenPoint(PlayerPosition+worldAim*6+Vector3.up*.9f);
                Vector2 reticle=MobileCanvasPoint(p);DrawCrosshair(reticle.x,reticle.y);
                var drop=NearDrop();var armor=NearArmor();var module=NearArtifact();var melee=NearMeleeDrop();
                bool equipmentNearby=drop!=null||armor!=null||module!=null||melee!=null;
                if(equipmentNearby||NearbyAmmoCrate||NearbySupplyChest)
                {
                    Rarity lootRarity=drop!=null?drop.gun.rarity:armor!=null?armor.item.rarity:module!=null?module.item.rarity:melee!=null?melee.item.rarity:Rarity.Common;
                    Color lootColor=equipmentNearby?Arsenal.Colors[Mathf.Clamp((int)lootRarity,0,Arsenal.Colors.Length-1)]:Gold;
                    Panel(new Rect(365,480,710,160),lootColor);
                    if(drop!=null)ItemPortraits.Draw(new Rect(382,495,128,128),drop.gun);
                    else if(armor!=null)ItemPortraits.Draw(new Rect(382,495,128,128),armor.item);
                    else if(module!=null)ManufacturerBranding.ModulePortrait(new Rect(382,495,128,128),module.item);
                    else if(melee!=null)DrawMeleeIcon(new Rect(382,495,128,128),melee.item.family);
                    string rarity=drop!=null?VaultLanguage.RarityName(drop.gun.rarity):armor!=null?VaultLanguage.RarityName(armor.item.rarity):module!=null?VaultLanguage.RarityName(module.item.rarity):melee!=null?VaultLanguage.RarityName(melee.item.rarity):NearbyAmmoCrate?T("AMMO CRATE","弾薬箱"):T("SUPPLY CHEST","物資チェスト");
                    int score=drop!=null?GearScoring.Score(drop.gun):armor!=null?GearScoring.Score(armor.item):module!=null?GearScoring.Score(module.item):melee!=null?MeleeWeaponCatalog.Score(melee.item):0;
                    int level=drop!=null?drop.gun.level:armor!=null?armor.item.level:module!=null?module.item.level:melee!=null?melee.item.level:0;
                    Text(528,492,rarity,new GUIStyle(label){fontSize=34},lootColor,520);
                    if(equipmentNearby)
                    {
                        Text(528,540,T("LEVEL ","レベル ")+level,new GUIStyle(label){fontSize=29},Mist,520);
                        Text(528,586,T("SCORE ","評価 ")+score,new GUIStyle(label){fontSize=32},Color.white,520);
                    }
                    else Text(528,560,T("Tap TAKE to open","「拾得」で開封"),new GUIStyle(label){fontSize=29},Cyan,520);
                }
                foreach(var foe in Enemies)if(foe&&!foe.dead&&(foe.elite||foe.kind==3||foe.hp<foe.maxHp)&&Layout.IsVisible(foe.transform.position))
                {var screen=cam.WorldToScreenPoint(foe.transform.position+Vector3.up*2.5f);if(screen.z<=0)continue;Vector2 at=MobileCanvasPoint(screen);Box(new Rect(at.x-30,at.y,60,5),PanelInk);Box(new Rect(at.x-30,at.y,60*Mathf.Clamp01(foe.hp/foe.maxHp),5),foe.HealthBarColor);}
            }
        }
        void DrawMobilePause()
        {
            Box(new Rect(0,0,1440,900),new Color(.01f,.02f,.025f,.88f));Text(300,190,T("EXPEDITION PAUSED","探索一時停止"),heading,Gold,900);
            DrawCameraZoom(300,225,840,true);
            if(MobileButton(new Rect(300,320,840,120),T("RESUME","探索を再開"))){screen=Screen.Playing;Time.timeScale=1;}
            if(MobileButton(new Rect(300,470,840,120),T("EQUIPMENT","装備")))ToggleInventory();
            if(MobileButton(new Rect(300,755,840,100),T("WORLD MAP","全体地図")))OpenOverworldMap();
            if(MobileButton(new Rect(300,610,405,120),T("SAVE & MENU","保存してメニューへ")))SaveRunAndReturnToMenu();
            if(MobileButton(new Rect(735,610,405,120),IsOverworld?T("CAMP MAP","拠点マップ"):T("WITHDRAW TO ENTRANCE","入口へ退却")))WithdrawFromExploration();
        }
        void DrawMobileOverlay()
        {
            Box(new Rect(0,0,1440,900),PanelInk);
            if(screen==Screen.Reward)
            {
                MobileCopyAt(50,70,T("CHOOSE YOUR AUGMENT","強化を選択"),Gold);
                string[] names={T("OVERCHARGE / Damage +25%","威力 +25%"),T("REINFORCED / HP +30, heal 35","最大HP +30 / HP35回復"),T("RAPID CYCLER / Fire rate +22%","連射速度 +22%"),T("REPAIR / Restore all HP","HPを全回復"),T("SHOCK / Dash cooldown +35%","回避待機短縮 +35% / 衝撃波"),T("TESLA / 30% chain damage","威力30%の連鎖攻撃"),T("BIO / Heal 3 HP per kill","撃破ごとにHP3回復")};
                for(int i=0;i<3;i++)if(MobileButton(new Rect(50,220+i*195,1340,155),names[choices[i]])){int c=choices[i];RecordRunUpgrade(c);ChooseReward(i);}
                return;
            }
            MobileCopyAt(260,220,screen==Screen.Victory?T("ZONE SECURED","汚染区域を制圧"):T("SIGNAL LOST","生存信号を喪失"),Gold);
            if(screen==Screen.Victory&&(completedRewardGun!=null||completedRewardArmor!=null))DrawCompletedGearReward(new Rect(260,335,920,150));else MobileCopyAt(260,340,Toast,Mist,940);
            if(MobileButton(new Rect(260,550,920,140),T("RETURN TO SAFEHOUSE","拠点へ帰還")))EnterHub();
        }    }
}
