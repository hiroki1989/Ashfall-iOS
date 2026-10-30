using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        static string T(string english,string japanese)=>VaultLanguage.T(english,japanese);
        readonly Color Mist=new Color(.69f,.76f,.80f), PanelInk=new Color(.065f,.10f,.13f,.97f);
        static readonly Color UiPaper=new Color(.96f,.94f,.87f);
        static readonly Color UiDanger=new Color(1f,113/255f,139/255f);
        const string LegendaryRedOpen="<color=#FF4545>";
        const string LegendaryRedClose="</color>";
        string LegendaryDescription(object item,string description)
        {
            if(string.IsNullOrEmpty(description))return description;
            if(item is ArmorItem legacyArmor&&string.IsNullOrEmpty(legacyArmor.legendaryKey)&&legacyArmor.uniqueEffect>0)
            {
                string prefix=ArmorProductCatalog.Description(legacyArmor,VaultLanguage.Japanese);
                return description.StartsWith(prefix)?prefix+LegendaryRedOpen+description.Substring(prefix.Length)+LegendaryRedClose:LegendaryRedOpen+description+LegendaryRedClose;
            }
            bool legendary=item is Gun gun&&!string.IsNullOrEmpty(gun.legendaryKey)
                ||item is ArmorItem armor&&!string.IsNullOrEmpty(armor.legendaryKey)
                ||item is ArtifactItem module&&(module.rarity==Rarity.Legendary||module.uniqueEffect>0);
            if(!legendary)return description;
            int split=item is ArtifactItem?description.LastIndexOf('\n'):description.IndexOf('\n');
            if(split<0)return LegendaryRedOpen+description+LegendaryRedClose;
            return item is ArtifactItem
                ?description.Substring(0,split+1)+LegendaryRedOpen+description.Substring(split+1)+LegendaryRedClose
                :LegendaryRedOpen+description.Substring(0,split)+LegendaryRedClose+description.Substring(split);
        }
        GUIStyle heading,centered,tiny,number;
        Vector2 expeditionMapScroll;
        int expeditionMapSelection;
        internal Vector2? AimUiReviewPoint;
        void Styles()
        {
            if(label!=null)
            {
                Font displayFont=englishHeadingFont==null?font:englishHeadingFont;
                title.font=displayFont;heading.font=displayFont;return;
            }
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=false};label.normal.textColor=Color.white;
            small=new GUIStyle(label){fontSize=14};tiny=new GUIStyle(label){fontSize=12};
            Font headingFont=englishHeadingFont==null?font:englishHeadingFont;
            title=new GUIStyle(label){font=headingFont,fontSize=68,fontStyle=FontStyle.Normal};
            heading=new GUIStyle(label){font=headingFont,fontSize=38,fontStyle=FontStyle.Normal};
            number=new GUIStyle(label){fontSize=42,fontStyle=FontStyle.Normal};
            centered=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(label){fontSize=20,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(26,12,8,8)};
            button.normal.background=null;button.hover.background=null;button.active.background=null;
            button.normal.textColor=Ink;button.hover.textColor=Ink;button.active.textColor=Ink;
        }
        void Box(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        void Text(float x,float y,string value,GUIStyle style=null,Color? color=null,float width=900)
        {GUI.color=color??Color.white;GUIStyle copy=style??label;if(value!=null&&value.Contains(LegendaryRedOpen)&&!copy.richText)copy=new GUIStyle(copy){richText=true};GUI.Label(new Rect(x,y,width,160),value,copy);GUI.color=Color.white;}
        void Line(Vector2 a,Vector2 b,Color color,float thickness=1)
        {
            Matrix4x4 previous=GUI.matrix;Vector2 delta=b-a;
            GUI.matrix=previous*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one)*Matrix4x4.Translate(new Vector3(-a.x,-a.y,0));
            Box(new Rect(a.x,a.y,delta.magnitude,thickness),color);GUI.matrix=previous;
        }
        void Diamond(float x,float y,float size,Color color)
        {Line(new Vector2(x,y-size),new Vector2(x+size,y),color);Line(new Vector2(x+size,y),new Vector2(x,y+size),color);Line(new Vector2(x,y+size),new Vector2(x-size,y),color);Line(new Vector2(x-size,y),new Vector2(x,y-size),color);}
        void Panel(Rect r,Color accent)
        {
            if(FieldUIArt.Plate(r,false))return;
            Box(new Rect(r.x,r.y+3,r.width,r.height),new Color(0,0,0,.14f));
            Box(r,new Color(.055f,.075f,.095f,.94f));
            Box(new Rect(r.x,r.y,3,r.height),new Color(accent.r,accent.g,accent.b,.65f));
            Box(new Rect(r.x+16,r.yMax-1,r.width-32,1),new Color(.45f,.52f,.55f,.18f));
        }
        bool Button(Rect r,string value,bool primary=false)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            Color accent=primary?Gold:Cyan;
            FieldUIArt.Plate(r,primary||hover);
            var style=new GUIStyle(button){fontStyle=FontStyle.Normal};
            style.normal.textColor=primary?Ink:UiPaper;style.hover.textColor=primary?Ink:UiPaper;style.active.textColor=primary?Ink:UiPaper;
            style.normal.textColor=UiPaper;style.hover.textColor=Color.white;style.active.textColor=Color.white;
            bool clicked=GUI.Button(r,value,style);
            if(clicked)PlayUserSound("skill",.09f);
            Text(r.xMax-31,r.center.y-15,"›",new GUIStyle(label){fontSize=25},primary?Ink:accent,26);
            return clicked;
        }
        void LanguageSelector()
        {
            Panel(new Rect(1190,838,226,40),Gold*.6f);
            for(int i=0;i<2;i++)
            {
                bool chosen=i==0?VaultLanguage.Japanese:!VaultLanguage.Japanese;Rect r=new Rect(1198+i*106,843,104,30);
                if(chosen)Box(r,new Color(.3f,.23f,.11f,.7f));
                GUI.color=chosen?Gold:Mist;if(GUI.Button(r,i==0?"日本語":"ENGLISH",centered))VaultLanguage.Set(i==0,!smoke);GUI.color=Color.white;
            }
        }
        void OnGUI()
        {
            Styles();float scale=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);
            if(MobileTouchControls.Enabled && screen==Screen.Menu && !InventoryOpen && !manufacturerRegistry)
            {
                // Cover the physical display, including the unsafe margins around the
                // camera cutout; interactive controls remain inside the safe area.
                GUI.matrix=Matrix4x4.identity;
                if(menuArt)GUI.DrawTexture(new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height),menuArt,ScaleMode.ScaleAndCrop);
                else GUI.DrawTexture(new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height),Texture2D.blackTexture);
            }
            GUI.matrix=MobileTouchControls.Enabled?MobileCanvasMatrix:Matrix4x4.TRS(new Vector3((UnityEngine.Screen.width-1440*scale)/2,(UnityEngine.Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(screen!=Screen.Shop)debtPage=false;
            if(worldTransitionPending){DrawWorldLoading();return;}
            if(DebtFinalConversationVisible){DrawDebtFinalConversation();return;}
            if(NpcConversationVisible){DrawNpcConversation();return;}
            if(screen==Screen.Dead){DrawDeathScreen();return;}
            if(VaultStorageVisible){DrawVaultStorage();return;}
            if(MobileTouchControls.Enabled)
            {
                if(WorldBossConversationVisible){DrawWorldBossConversation();return;}
                if(DebtIntroVisible){DrawMobileDebtIntro();return;}
                if(manufacturerRegistry)DrawMobileManufacturer();
                else if(InventoryOpen){if(actionSkillInventoryOpen)DrawActionSkillInventory();else DrawEquipmentScreen(false);}
                else if(screen==Screen.Menu)DrawMobileMenu();
                else if(screen==Screen.Hub)DrawMobileHub();
                else if(screen==Screen.Shop){if(!DrawEquipmentScreen(true))DrawMobileShop();}
                else if(screen==Screen.DungeonSelect)DrawOverworldAtlas();
                else if(screen==Screen.Playing){if(IsOverworld)DrawOverworldHUD();else DrawMobileHUD();DrawActionSkillHUD();}
                else if(screen==Screen.Pause){if(IsOverworld)DrawOverworldHUD();else DrawMobileHUD();DrawMobilePause();}
                else DrawMobileOverlay();
                DrawFirstStepsTutorial();DrawActionSkillTutorial();
                if(screen==Screen.Menu&&!InventoryOpen&&!manufacturerRegistry)DrawMobileLanguageButton();return;
            }
            if(WorldBossConversationVisible){DrawWorldBossConversation();return;}
            if(DebtIntroVisible){DrawDebtIntro();return;}
            if(manufacturerRegistry)DrawManufacturerRegistry();
            else if(InventoryOpen){if(actionSkillInventoryOpen)DrawActionSkillInventory();else DrawEquipmentScreen(false);}
            else if(screen==Screen.Menu)DrawMenu();
            else if(screen==Screen.Hub){DrawHub();DrawHubWorldLabels();if(Button(new Rect(1060,735,260,42),T("MANUFACTURERS","メーカー製品図鑑"))){manufacturerRegistry=true;manufacturerKind=-1;}DrawCollectorDialog();}
            else if(screen==Screen.Shop){if(!DrawEquipmentScreen(true))DrawShop();}
            else if(screen==Screen.DungeonSelect)DrawOverworldAtlas();
            else
            {
                if(IsOverworld)DrawOverworldHUD();else DrawHUD();if(screen==Screen.Playing)DrawActionSkillHUD();else DrawOverlay();
            }
            DrawFirstStepsTutorial();DrawActionSkillTutorial();
            if(screen==Screen.Menu&&!InventoryOpen&&!manufacturerRegistry)LanguageSelector();
        }
        void DrawDeathScreen()
        {
            float elapsed=DeathSequenceDuration-Mathf.Max(0,deathReturnAt-Time.unscaledTime);
            float backdrop=Mathf.SmoothStep(0,.94f,Mathf.Clamp01(elapsed/1.4f));
            float titleAlpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.65f)/1.1f));
            titleAlpha*=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-3.25f)/1.05f));
            Box(new Rect(0,0,1440,900),new Color(.008f,.008f,.01f,backdrop));
            var title=new GUIStyle(heading){fontSize=80,alignment=TextAnchor.MiddleCenter};
            Text(170,330,"YOU DIED",title,new Color(.78f,.16f,.14f,titleAlpha),1100);
            float blackout=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-3.65f)/.75f));
            if(blackout>0)Box(new Rect(0,0,1440,900),new Color(0,0,0,blackout));
        }
        void DrawMobileLanguageButton()
        { if(MobileButton(new Rect(1190,794,220,94),VaultLanguage.Japanese?"ENGLISH":"日本語"))VaultLanguage.Set(!VaultLanguage.Japanese,!smoke); }
        void DrawMobileDebtIntro()
        {
            Box(new Rect(0,0,1440,900),new Color(.012f,.018f,.024f,.985f));Panel(new Rect(55,125,1330,660),new Color(1,.27f,.19f));
            Text(95,160,T("COLLAR DEBT // 1,000,000,000 SCRAP","首輪の負債 // 10億スクラップ"),new GUIStyle(heading){fontSize=42,wordWrap=true},new Color(1,.39f,.28f),1230);
            Text(95,245,T("You wake to a live explosive collar and a contract stamped with one billion scrap. The Malcolm Vale owns the detonator; his collector, Briggs, owns your earnings. Run the contaminated zones, bring back anything valuable, and pay until the collar comes off. Then settle the score with the boss.","目を覚ますと、首には作動中の爆弾首輪。契約書には10億スクラップの借金。起爆装置を握るのはマルコム・ヴェイル、稼ぎを取り立てるのはブリッグスだ。汚染区域へ出撃し、戦利品を持ち帰って返済しろ。首輪を外したら、ボスに借りを返す。"),new GUIStyle(label){fontSize=31,wordWrap=true},Mist,1220);
            Text(95,480,T("Repayment earns levels and skill points. Choose an action skill and upgrade its branches.","返済でレベルとスキルポイントを獲得する。アクションスキルと強化の方向を選ぼう。"),new GUIStyle(label){fontSize=29,wordWrap=true},Cyan,1220);
            if(MobileButton(new Rect(300,640,840,120),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃")))MarkDebtIntroSeen();
        }
        void DrawDebtIntro()
        {
            Box(new Rect(0,0,1440,900),new Color(.012f,.018f,.024f,.985f));
            Panel(new Rect(205,150,1030,600),new Color(1,.27f,.19f));
            Text(250,190,T("COLLAR DEBT // 1,000,000,000 SCRAP","首輪の負債 // 10億スクラップ"),heading,new Color(1,.39f,.28f),930);
            Text(255,280,T("You wake to a live explosive collar and a contract stamped with one billion scrap. The Malcolm Vale owns the detonator; his collector, Briggs, owns your earnings. Run the contaminated zones, bring back anything valuable, and pay until the collar comes off. Then settle the score with the boss.","目を覚ますと、首には作動中の爆弾首輪。契約書には10億スクラップの借金。起爆装置を握るのはマルコム・ヴェイル、稼ぎを取り立てるのはブリッグスだ。汚染区域へ出撃し、戦利品を持ち帰って返済しろ。首輪を外したら、ボスに借りを返す。"),new GUIStyle(label){fontSize=23,wordWrap=true},Mist,920);
            Text(255,490,T("Repayment grants levels and skill points. The first level costs 1,000,000 scrap. Visit Briggs to repay and upgrade action skills.","返済でレベルとスキルポイントを獲得。最初のレベルは100万スクラップ。拠点のブリッグスに話しかけて返済と強化を進めよう。"),small,Cyan,900);
            if(MobileTouchControls.Enabled?MobileButton(new Rect(350,620,740,115),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃")):Button(new Rect(505,640,430,64),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃"),true))MarkDebtIntroSeen();
        }
        void DrawMenu()
        {
            DrawTitleMenu(false);
            toastTimer-=Time.unscaledDeltaTime;
        }
        void DrawOverlay()
        {
            Box(new Rect(0,0,1440,900),new Color(.008f,.015f,.022f,.88f));
            if(screen==Screen.Reward)
            {
                Text(162,195,T("CHOOSE YOUR AUGMENT","強化を選択"),heading,Gold);
                Text(165,260,T("Take one field upgrade into zone "+(room+2)+".","現地改造をひとつ選び、区域 "+(room+2)+" へ。"),label,Mist);
                string[] names={T("OVERCHARGE","オーバーチャージ"),T("REINFORCED SHELL","強化装甲"),T("RAPID CYCLER","高速機構"),T("REPAIR PROTOCOL","修復プロトコル"),T("SHOCK BOOSTER","衝撃ブースター"),T("TESLA BRIDGE","テスラ導線"),T("BIO RECOVERY","生体回収"),T("BALANCED FORCE","均衡火力"),T("FIELD MEDIC","野戦手当"),T("MOBILE RECOVERY","機動回復"),T("ARC CYCLER","連鎖機構"),T("STRIKE RUNNER","突撃機動"),T("FIELD RESUPPLY","現地補給")};
                string[] desc={T("Run damage +25%","周回の威力 +25%"),T("Run max HP +30 / heal 35","周回HP +30 / HP35回復"),T("Run fire rate +22%","周回の連射 +22%"),T("Restore all HP","HPを全回復"),T("Dash cooldown bonus +35% / dash pulse","回避クールダウン短縮 +35% / ダッシュ波動"),T("Hits chain 30% damage to a nearby target","命中で近くの敵へ威力30%の連鎖"),T("Heal 3 HP per kill this run","撃破ごとにHP3回復"),T("Damage +12% / fire rate +10%","威力 +12% / 連射 +10%"),T("Max HP +20 / heal 45","最大HP +20 / HP45回復"),T("Dash cooldown +20% / heal 1 per kill","回避短縮 +20% / 撃破時HP1回復"),T("Chain damage +20% / fire rate +10%","連鎖 +20% / 連射 +10%"),T("Damage +15% / dash cooldown +15%","威力 +15% / 回避短縮 +15%"),T("Heal 50% and replenish ammunition","HP50%回復 / 全弾薬を補給")};
                for(int i=0;i<3;i++)
                {
                    int c=choices[i];Rect r=new Rect(165+i*375,345,350,306);Panel(r,c==1?Cyan:Gold);
                    Diamond(r.center.x,r.y+70,30,c==1?Cyan:Gold);Text(r.x+22,r.y+128,"0"+(i+1)+" / "+names[c],label,Color.white,315);
                    Text(r.x+22,r.y+174,desc[c],small,Mist,315);
                    if(Button(new Rect(r.x+20,r.y+224,310,57),T("ACCEPT AUGMENT","この強化を選ぶ"),true)){RecordRunUpgrade(c);ChooseReward(i);}
                }
                return;
            }
            string text=screen==Screen.Pause?T("EXPEDITION PAUSED","探索一時停止"):screen==Screen.Victory?T("ZONE SECURED","汚染区域を制圧"):T("SIGNAL LOST","生存信号を喪失");
            Panel(new Rect(350,171,740,554),screen==Screen.Victory?Gold:Cyan);
            Diamond(720,205,12,Gold);Text(390,249,text,heading,screen==Screen.Victory?Gold:Color.white,680);
            Text(390,320,CurrentDungeon.Name+" / "+(room+1)+" / "+CurrentDungeon.AreaCount+T("  ·  Scrap ","  ·  スクラップ ")+Credits,small,Mist,660);
            if(screen==Screen.Pause)
            {DrawCameraZoom(390,365,310,false);DrawMasterVolume(740,365,310,false);if(Button(new Rect(390,443,660,68),T("RESUME EXPEDITION","探索を再開"),true)){screen=Screen.Playing;Time.timeScale=1;}}
            else
            {if(screen==Screen.Victory&&(completedRewardGun!=null||completedRewardArmor!=null))DrawCompletedGearReward(new Rect(390,343,660,93));else Text(390,372,Toast,label,Gold,660);if(Button(new Rect(390,443,660,68),T("RETURN TO LAST LIGHT","灯火基地へ帰還"),true))EnterHub();}
            if(Button(new Rect(390,529,660,62),IsOverworld?T("CAMP MAP","拠点マップ"):T("WITHDRAW TO ENTRANCE","入口へ退却")))WithdrawFromExploration();
            if(screen==Screen.Pause&&Button(new Rect(390,700,660,42),T("WORLD MAP","広域地図")))OpenOverworldMap();
            if(screen==Screen.Pause && Button(new Rect(390,606,660,42),T("ARSENAL / EQUIPMENT [I]","インベントリ [I]")))ToggleInventory();
            if(screen==Screen.Pause && Button(new Rect(390,654,660,48),T("SAVE EXPEDITION & RETURN TO MENU","探索を保存してメニューへ")))SaveRunAndReturnToMenu();
        }
        void DrawHeldWeaponHUD(Rect r)
        {
            if(gun==null)return;Box(r,new Color(.025f,.04f,.05f,.55f));
            Box(new Rect(r.x,r.y,2,r.height),new Color(.62f,.72f,.72f,.55f));
            FieldUIArt.GunClassIcon(new Rect(r.x+7,r.y+5,58,r.height-10),gun,UiPaper);
            var ammoStyle=new GUIStyle(number){fontSize=25,alignment=TextAnchor.MiddleCenter};
            GUI.color=UiPaper;GUI.Label(new Rect(r.x+70,r.y+8,r.width-80,r.height-16),ammo+" / "+AmmoReserve(AmmoForGun(gun)),ammoStyle);GUI.color=Color.white;
            if(reload>0)Box(new Rect(r.x+8,r.yMax-7,(r.width-16)*Mathf.Clamp01(1-reload/Mathf.Max(.01f,EffectiveReload)),2),Gold);
        }
        void DrawHUD()
        {
            DrawCriticalHealthVignette();
            int alive=0;foreach(var f in Enemies)if(f!=null&&!f.dead)alive++;
            Panel(new Rect(24,22,338,88),Gold*.7f);
            Text(42,32,T("ZONE","探索区域"),tiny,Mist);Text(42,56,(room+1).ToString("00")+" / "+CurrentDungeon.AreaCount.ToString("00"),label,Gold);
            for(int i=0;i<CurrentDungeon.AreaCount;i++)Box(new Rect(159+i*25,68,18,4),i<=room?Gold:new Color(.2f,.27f,.28f));
            Text(440,28,room==CurrentDungeon.AreaCount-1?T("WASTELAND COLOSSUS","荒野の巨骸"):SelectedExpeditionStage==null?CurrentDungeon.Name:(SelectedStageId+1).ToString("00")+" / "+SelectedExpeditionStage.Name,label,Gold,650);
            Text(440,61,ExpeditionExitOpen?T("EXIT UNSEALED / Follow the gold marker","出口が開放 / 金色のマーカーへ"):T("HOSTILES  "+alive+"   /   EXPLORE THE CHAMBERS","残敵 "+alive+"   /   広間を探索せよ"),small,ExpeditionExitOpen?Color.Lerp(Gold,Color.white,.5f+.5f*Mathf.Sin(Time.unscaledTime*5)):Mist,720);
            if(ExpeditionExitOpen)DrawDungeonExitDirection(false);
            if(SelectedExpeditionStage!=null){var stage=SelectedExpeditionStage;string mission=ExpeditionMap.MissionLabel(stage,VaultLanguage.Japanese);if(stage.mission==ExpeditionMission.HuntTarget||stage.mission==ExpeditionMission.DestroyNest)mission+=" / "+stage.Target;mission+="  ·  "+MissionProgress;if(stage.mission==ExpeditionMission.RecoverSupplies||stage.mission==ExpeditionMission.SurveySectors||stage.mission==ExpeditionMission.SalvageSweep)mission+=T(" / floors "," / フロア ")+SurveyedFloorCount;Text(42,117,mission,new GUIStyle(tiny){wordWrap=true},MissionCompleted?Cyan:Gold,350);}else Text(42,121,T("SEED "+seed+"  /  "+Mathf.FloorToInt(runTime)+"s","シード "+seed+"  /  "+Mathf.FloorToInt(runTime)+"秒"),tiny,Mist,350);
            if(toastTimer>0){Panel(new Rect(405,111,720,44),Gold*.6f);Text(421,123,Toast,small,Gold,695);}
            DrawHealthPanel(new Rect(24,754,375,122),false);
            Panel(new Rect(414,798,197,78),dodgeCooldown<=0?Cyan:Gold*.4f);
            Diamond(436,836,7,dodgeCooldown<=0?Cyan:Mist);
            Text(455,809,T("SPACE / DASH","SPACE / 回避"),tiny,Mist,150);Text(455,836,dodgeCooldown<=0?T("READY","使用可能"):dodgeCooldown.ToString("0.0")+T("s","秒"),small,Cyan,150);
            DrawHeldWeaponHUD(new Rect(1190,812,226,64));
            DrawMap();
            if(screen!=Screen.Playing || InventoryOpen)return;
            float s=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);
            float mx=(Input.mousePosition.x-(UnityEngine.Screen.width-1440*s)/2)/s,my=(UnityEngine.Screen.height-Input.mousePosition.y-(UnityEngine.Screen.height-900*s)/2)/s;if(MobileTouchControls.Enabled||HasAimTarget){Vector3 screenAim=cam.WorldToScreenPoint(AimReticleWorldPosition);if(MobileTouchControls.Enabled){Vector2 canvasAim=MobileCanvasPoint(new Vector2(screenAim.x,screenAim.y));mx=canvasAim.x;my=canvasAim.y;}else{mx=(screenAim.x-(UnityEngine.Screen.width-1440*s)/2)/s;my=(UnityEngine.Screen.height-screenAim.y-(UnityEngine.Screen.height-900*s)/2)/s;}}
            if(smoke&&AimUiReviewPoint.HasValue){mx=AimUiReviewPoint.Value.x;my=AimUiReviewPoint.Value.y;}
            DrawWorldLootPreview();
            if(NearbySupplyChest)WorldLabel(new Rect(25,355,390,55),T("USE / F: OPEN CACHE","調べる / F：物資箱を開ける"),Gold,20);
            if(ExpeditionExitOpen)
            {Vector3 p=cam.WorldToScreenPoint(ExitPosition+Vector3.up*2);if(p.z>0){float x=(p.x-(UnityEngine.Screen.width-1440*s)/2)/s,y=(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*s)/2)/s;Text(Mathf.Clamp(x-80,20,1260),Mathf.Clamp(y,170,680),T("[E] DESCEND","[E] 次のエリアへ"),small,Gold,190);}}
            foreach(var f in Enemies)if(f!=null&&!f.dead&&(f.kind==3||f.elite||f.hp<f.maxHp-.01f)&&Layout.IsVisible(f.transform.position))
            {
                Vector3 p=cam.WorldToScreenPoint(f.transform.position+Vector3.up*(f.kind==3?4.6f:2.4f));if(p.z<=0)continue;
                float x=(p.x-(UnityEngine.Screen.width-1440*s)/2)/s,y=(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*s)/2)/s;
                Box(new Rect(x-28,y,56,4),PanelInk);Box(new Rect(x-28,y,56*Mathf.Clamp01(f.hp/f.maxHp),4),f.HealthBarColor);
                WorldLabel(new Rect(x-82,y-27,164,25),EnemyHealthLabel(f),f.kind==8?Cyan:UiPaper,17);
            }
            DrawCrosshair(mx,my);
        }
        string EnemyHealthLabel(Foe foe)
        {
            int level=IsOverworld?GetOverworldEnemyLevel(foe):SelectedExpeditionStage!=null?SelectedExpeditionStage.level:CurrentDungeon.minLevel;
            var human=foe.GetComponentInChildren<HumanFactionVisual>();
            string name=human&&human.Faction==1?T("RED CLAW","レッドクロウ"):human&&human.Faction==2?T("COYOTES","コヨーテ"):Foe.Name(foe.kind);
            return "Lv."+Mathf.Max(1,level)+" "+name;
        }
        void DrawCompactLootCard(object item,Rarity rarity,int level,int score)
        {
            Color accent=Arsenal.Colors[Mathf.Clamp((int)rarity,0,Arsenal.Colors.Length-1)];
            Panel(new Rect(410,628,620,141),accent);
            Rect portrait=new Rect(426,645,106,106);
            if(item is Gun gun)ItemPortraits.Draw(portrait,gun);
            else if(item is ArmorItem armor)ItemPortraits.Draw(portrait,armor);
            else if(item is ArtifactItem module)ManufacturerBranding.ModulePortrait(portrait,module);
            else if(item is MeleeWeaponItem melee)DrawMeleeIcon(portrait,melee.family);
            Text(550,640,VaultLanguage.RarityName(rarity),label,accent,440);
            Text(550,680,T("LEVEL ","レベル ")+level,small,Mist,440);
            Text(550,716,T("SCORE ","評価 ")+score,label,Color.white,440);
        }
        void DrawCrosshair(float mx,float my)
        {
            float finalAccuracy=EffectiveAccuracy;
            float reticleGap=2+(100-finalAccuracy)*.16f+Mathf.Clamp01(fireTimer/Mathf.Max(.01f,gun.interval))*2;
            Color reticle=reload>0?Gold:new Color(.72f,1,.96f,1);
            Box(new Rect(mx-3,my-3,6,6),new Color(.008f,.012f,.018f,.95f));Box(new Rect(mx-1,my-1,2,2),Color.white);
            for(int axis=0;axis<4;axis++)
            {
                Rect bar=axis==0?new Rect(mx+reticleGap,my-1,6,2):axis==1?new Rect(mx-1,my-reticleGap-6,2,6):axis==2?new Rect(mx-reticleGap-6,my-1,6,2):new Rect(mx-1,my+reticleGap,2,6);
                Box(new Rect(bar.x-1,bar.y-1,bar.width+2,bar.height+2),new Color(.008f,.012f,.018f,.95f));Box(bar,reticle);
            }
        }
        void DrawExpeditionWorldMap(){DrawExpeditionCityMap();}
        void FillExpeditionPolygon(Vector2[] points,Color color)
        {
            float minY=points[0].y,maxY=minY;foreach(var p in points){minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            for(float y=minY;y<=maxY;y+=5){float left=float.MaxValue,right=float.MinValue;for(int i=0,j=points.Length-1;i<points.Length;j=i++){Vector2 a=points[i],b=points[j];if((a.y>y)!=(b.y>y)){float x=a.x+(y-a.y)*(b.x-a.x)/(b.y-a.y);left=Mathf.Min(left,x);right=Mathf.Max(right,x);}}if(right>left)Box(new Rect(left,y,right-left,5),color);}
        }
        void DrawExpeditionBriefing()
        {
            ExpeditionStage stage=ExpeditionMap.Get(Mathf.Clamp(expeditionMapSelection,0,ExpeditionMap.StageCount-1),DebtLevel);bool unlocked=true;bool cleared=clearedExpeditionStages.Contains(stage.id);
            Text(1042,174,T("FIELD DOSSIER / ","現地記録 / ")+stage.Name,label,Gold,330);
            Text(1042,219,stage.Location,new GUIStyle(small){wordWrap=true},stage.regionColor,330);
            Line(new Vector2(1042,270),new Vector2(1374,270),new Color(.55f,.72f,.72f,.38f));
            Text(1042,286,T("THREAT CLEARANCE","危険度許可"),tiny,Mist,320);
            Text(1042,311,T("LEVEL ","レベル ")+stage.level+"   /   "+T("YOUR LEVEL ","現在 ")+DebtLevel,label,unlocked?Cyan:new Color(1,.40f,.31f),330);
            Text(1042,356,T("BIOME","地形"),tiny,Mist,320);Text(1042,378,stage.Biome,small,Color.white,330);
            Text(1042,422,T("CONTRACT","任務"),tiny,Mist,320);
            Text(1042,445,ExpeditionMap.MissionLabel(stage,VaultLanguage.Japanese),label,Gold,330);
            Text(1042,481,T("MARKED TARGET","標的"),tiny,Mist,320);
            Text(1042,503,stage.Target,small,new Color(1,.51f,.37f),330);
            Text(1042,548,cleared?T("REPEAT CLEAR REWARD","再攻略報酬"):T("FIRST CLEAR CACHE","初回クリア報酬"),tiny,Mist,320);
            Text(1042,572,ExpeditionRewardDescription(stage.id),small,Cyan,330);
            Text(1042,609,cleared?T("MISSION PAYOUT CLAIMED","任務報酬は受取済み"):T("MISSION PAYOUT / ","任務達成報酬 / ")+MissionRewardDescription(stage.id),tiny,Gold,330);
            Text(1042,640,cleared?T("SITE CLEARED","制圧済み"):unlocked?T("CONTRACT AVAILABLE","出撃可能"):T("CLEARANCE TOO LOW","許可レベル不足"),tiny,cleared?Cyan:unlocked?Gold:new Color(1,.40f,.31f),330);
            if(Button(new Rect(1040,680,335,62),unlocked?T("DEPLOY TO SITE","地点へ出撃"):T("LOCKED / REPAY TO LEVEL UP","ロック中 / 返済で成長"),unlocked)&&unlocked)EnterExpeditionStage(stage.id);
            Text(1042,757,T("SKILL POINTS: FIRST CLEAR ONLY. REPLAYS GIVE GEAR OR SCRAP.","スキルポイントは初回のみ。再攻略は装備かスクラップ。"),tiny,Mist,330);
        }
        void DrawMap()
        {
            Layout.Discover(PlayerPosition);
            if(GUI.Button(new Rect(1190,80,226,271),GUIContent.none,GUIStyle.none))OpenOverworldMap();
            Panel(new Rect(1190,80,226,271),Cyan*.7f);Text(1206,90,T("HOT ZONE / TACTICAL","汚染区域 / 戦術マップ"),tiny,Gold,210);
            float tile=Mathf.Min(190f/Layout.Columns,195f/Layout.Rows);Vector2 origin=new Vector2(1214,117);
            for(int x=0;x<Layout.Columns;x++)for(int z=0;z<Layout.Rows;z++)if(Layout.open[x,z]&&Layout.IsExplored(x,z))Box(new Rect(origin.x+x*tile,origin.y+(Layout.Rows-1-z)*tile,tile-.7f,tile-.7f),Layout.IsVisible(x,z)?new Color(.28f,.44f,.46f):new Color(.13f,.20f,.22f));
            DrawTacticalMapMarkers(origin,tile,4);DrawMissionCacheGuidance(origin,tile,false);
            var exitCell=Layout.ToCell(ExitPosition);if(Layout.IsExplored(exitCell.x,exitCell.y))MapPoint(ExitPosition,origin,tile,ExpeditionExitOpen?Gold:new Color(.48f,.42f,.28f),4);MapPoint(PlayerPosition,origin,tile,Cyan,4);
            Text(1206,278,T("SECTORS","周回ルート"),tiny,Mist,190);
            for(int i=0;i<route.Count;i++){float x=1210+i*27;Box(new Rect(x,309,16,16),i==room?Cyan:i<room?Gold*.65f:new Color(.22f,.3f,.31f));if(i<6)Box(new Rect(x+16,316,11,1),Gold*.5f);}
            Text(1206,332,T("YOU / HOSTILES / EXIT","自分 / 敵 / 出口"),tiny,Mist,205);
        }
        void MapPoint(Vector3 p,Vector2 origin,float tile,Color color,float size)
        {Vector2Int c=Layout.ToCell(p);float x=origin.x+(c.x+.5f)*tile,y=origin.y+(Layout.Rows-.5f-c.y)*tile;Diamond(x,y,size,color);Box(new Rect(x-1,y-1,2,2),color);}
    }
}





