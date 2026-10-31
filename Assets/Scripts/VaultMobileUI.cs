using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public bool MobileModalVisible=>InventoryOpen||manufacturerRegistry||collectorOpen||NpcConversationVisible||FactionBossConversationVisible||DebtFinalConversationVisible||DebtIntroVisible||WorldBossConversationVisible||actionSkillTutorialVisible||screen!=Screen.Playing&&screen!=Screen.Hub;
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
        bool MobileButton(Rect r,string caption,int fontSize=38){Panel(r,Gold);Box(new Rect(r.x+2,r.y+4,r.width-4,r.height-6),new Color(.15f,.24f,.30f));Box(new Rect(r.x+2,r.y+4,6,r.height-8),Gold);var style=new GUIStyle(centered){fontSize=fontSize,fontStyle=FontStyle.Normal,wordWrap=true,clipping=TextClipping.Clip};style.normal.textColor=UiPaper;style.hover.textColor=Gold;style.active.textColor=Gold;return GUI.Button(r,caption,style);}
        void DrawMobileMenu()
        {
            DrawTitleMenu(true);
        }
        void DrawMobileHub()
        {
            Panel(new Rect(24,22,620,116),Gold);Text(42,36,OverworldCampName(LastVisitedCamp,VaultLanguage.Japanese),new GUIStyle(label){fontSize=28},Gold,595);
            Text(42,84,T("LEVEL ","レベル ")+DebtLevel+T("  SCRAP ","  スクラップ ")+Credits.ToString("N0"),new GUIStyle(label){fontSize=25},Cyan,595);
            if(MobileButton(new Rect(690,30,220,72),T("CITY MAP","都市マップ"),28))OpenOverworldMap();
            if(MobileButton(new Rect(930,30,220,72),T("MAKERS","製品図鑑"),28)){manufacturerRegistry=true;manufacturerKind=-1;}
            string hint=Vector3.Distance(PlayerPosition,debtCollectorPosition)<3?T("USE: BRIGGS / DEBT + SKILLS","調べる：ブリッグス / 返済・スキル"):Vector3.Distance(PlayerPosition,merchantPosition)<3?T("USE: SERA / MERCHANT","調べる：セラ / 装備商人"):Vector3.Distance(PlayerPosition,archivistPosition)<3?T("USE: DR. GIDEON / COLLECTOR","調べる：ギデオン博士 / コレクター"):T("Move to an NPC and tap USE","NPCへ近づいて「調べる」");
            Text(380,740,string.IsNullOrEmpty(hubDialogue)?hint:hubDialogue,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,680);
            if(toastTimer>0){Panel(new Rect(30,170,620,145),Gold);Text(50,185,Toast,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,580);}
            DrawCollectorDialog();
        }
        void DrawMobileHUD()
        {
            if(gun==null)return;
            DrawCriticalHealthVignette();
            DrawHealthPanel(new Rect(20,20,420,125),true);
            Text(46,106,(room+1)+" / "+CurrentDungeon.AreaCount+"  "+(SelectedExpeditionStage==null?CurrentDungeon.Name:(SelectedStageId+1).ToString("00")+" / "+SelectedExpeditionStage.Name),new GUIStyle(label){fontSize=19,wordWrap=false,clipping=TextClipping.Clip},Mist,370);
            if(SelectedExpeditionStage!=null)
            {
                string objective=ExpeditionMap.MissionLabel(SelectedExpeditionStage,VaultLanguage.Japanese)+"  "+MissionProgress;
                Text(38,199,objective,new GUIStyle(label){fontSize=22,wordWrap=true},MissionCompleted?Cyan:Gold,600);
            }
            DrawHeldWeaponHUD(new Rect(350,797,280,80));
            int alive=0;foreach(var enemy in Enemies)if(enemy&&!enemy.dead)alive++;
            Text(38,154,ExpeditionExitOpen?T("EXIT OPEN / approach and tap USE","出口開放 / 近づいて「調べる」"):T("HOSTILES ","残敵 ")+alive,new GUIStyle(label){fontSize=25},ExpeditionExitOpen?Color.Lerp(Gold,Color.white,.5f+.5f*Mathf.Sin(Time.unscaledTime*5)):Mist,740);
            if(ExpeditionExitOpen)DrawDungeonExitDirection(true);
            if(toastTimer>0)Text(360,210,Toast,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,670);
            Rect mapRect=new Rect(1165,165,250,150);if(GUI.Button(mapRect,GUIContent.none,GUIStyle.none))OpenOverworldMap();Box(mapRect,new Color(.035f,.055f,.075f,.74f));
            if(Layout!=null)
            {
                float tile=Mathf.Min(220f/Layout.Columns,122f/Layout.Rows);Vector2 origin=new Vector2(1180,179);
                for(int x=0;x<Layout.Columns;x++)for(int z=0;z<Layout.Rows;z++)if(Layout.open[x,z]&&Layout.IsExplored(x,z))Box(new Rect(origin.x+x*tile,origin.y+(Layout.Rows-1-z)*tile,Mathf.Max(1,tile-.5f),Mathf.Max(1,tile-.5f)),Layout.IsVisible(x,z)?new Color(.28f,.44f,.46f):new Color(.13f,.2f,.22f));
                DrawTacticalMapMarkers(origin,tile,7);DrawMissionCacheGuidance(origin,tile,true);MapPoint(PlayerPosition,origin,tile,Cyan,5);var exit=Layout.ToCell(ExitPosition);if(Layout.IsExplored(exit.x,exit.y))MapPoint(ExitPosition,origin,tile,Gold,5);
            }
            if(Active&&!InventoryOpen)
            {
                Vector3 p=cam.WorldToScreenPoint(AimReticleWorldPosition);
                Vector2 reticle=MobileCanvasPoint(p);DrawCrosshair(reticle.x,reticle.y);
                DrawWorldLootPreview();
                if(NearbySupplyChest)WorldLabel(new Rect(25,430,390,55),T("USE: OPEN CACHE","調べる：物資箱を開ける"),Gold,20);
                foreach(var foe in Enemies)if(foe&&!foe.dead&&(foe.elite||foe.kind==3||foe.hp<foe.maxHp)&&Layout.IsVisible(foe.transform.position))
                {var screen=cam.WorldToScreenPoint(foe.transform.position+Vector3.up*2.5f);if(screen.z<=0)continue;Vector2 at=MobileCanvasPoint(screen);WorldLabel(new Rect(at.x-82,at.y-31,164,25),EnemyHealthLabel(foe),UiPaper,17);Box(new Rect(at.x-30,at.y,60,5),PanelInk);Box(new Rect(at.x-30,at.y,60*Mathf.Clamp01(foe.hp/foe.maxHp),5),foe.HealthBarColor);}
            }
        }
        void DrawMobilePause()
        {
            Box(new Rect(0,0,1440,900),new Color(.01f,.02f,.025f,.88f));Text(300,190,T("EXPEDITION PAUSED","探索一時停止"),heading,Gold,900);
            DrawCameraZoom(300,225,400,true);DrawMasterVolume(740,225,400,true);
            if(MobileButton(new Rect(300,320,840,120),T("RESUME","探索を再開"))){screen=Screen.Playing;Time.timeScale=1;}
            if(MobileButton(new Rect(300,470,840,120),T("EQUIPMENT","装備")))ToggleInventory();
            if(MobileButton(new Rect(300,755,840,100),T("WORLD MAP","広域地図")))OpenOverworldMap();
            if(MobileButton(new Rect(300,610,405,120),T("SAVE & MENU","保存してメニューへ")))SaveRunAndReturnToMenu();
            if(MobileButton(new Rect(735,610,405,120),IsOverworld?T("CAMP MAP","拠点マップ"):T("WITHDRAW TO ENTRANCE","入口へ退却")))WithdrawFromExploration();
        }
        void DrawMobileOverlay()
        {
            Box(new Rect(0,0,1440,900),PanelInk);
            if(screen==Screen.Reward)
            {
                MobileCopyAt(50,70,T("CHOOSE YOUR AUGMENT","強化を選択"),Gold);
                string[] names={T("OVERCHARGE / Damage +25%","威力 +25%"),T("REINFORCED / HP +30, heal 35","最大HP +30 / HP35回復"),T("RAPID CYCLER / Fire rate +22%","連射速度 +22%"),T("REPAIR / Restore all HP","HPを全回復"),T("SHOCK / Dash cooldown +35%","回避クールダウン短縮 +35% / 衝撃波"),T("TESLA / 30% chain damage","威力30%の連鎖攻撃"),T("BIO / Heal 3 HP per kill","撃破ごとにHP3回復"),T("BALANCED / Damage +12%, fire +10%","威力 +12% / 連射 +10%"),T("MEDIC / HP +20, heal 45","最大HP +20 / HP45回復"),T("MOBILE / Dash +20%, heal on kill","回避短縮 +20% / 撃破時HP1回復"),T("ARC / Chain +20%, fire +10%","連鎖 +20% / 連射 +10%"),T("STRIKE / Damage +15%, dash +15%","威力 +15% / 回避短縮 +15%"),T("RESUPPLY / Heal 50%, ammunition","HP50%回復 / 全弾薬補給")};
                for(int i=0;i<3;i++)if(MobileButton(new Rect(50,220+i*195,1340,155),names[choices[i]])){int c=choices[i];RecordRunUpgrade(c);ChooseReward(i);}
                return;
            }
            MobileCopyAt(260,220,screen==Screen.Victory?T("DUNGEON CLEARED","ダンジョンクリア"):T("SIGNAL LOST","生存信号を喪失"),Gold);
            if(screen==Screen.Victory&&SelectedExpeditionStage!=null)MobileCopyAt(260,295,ExpeditionMap.MissionLabel(SelectedExpeditionStage,VaultLanguage.Japanese),Mist,940);
            if(screen==Screen.Victory&&(completedRewardGun!=null||completedRewardArmor!=null))DrawCompletedGearReward(new Rect(260,365,920,150));else MobileCopyAt(260,380,Toast,Mist,940);
            if(screen==Screen.Victory)MobileCopyAt(260,525,CompletedPayoutLabel(),Gold,940);
            if(MobileButton(new Rect(260,590,920,140),screen==Screen.Victory?T("CONFIRM","確認"):T("RETURN TO SAFEHOUSE","拠点へ帰還"))){if(screen==Screen.Victory&&PendingWorldStage>=0)ReturnFromWorldDungeon(true);else EnterHub();}
        }    }
}
