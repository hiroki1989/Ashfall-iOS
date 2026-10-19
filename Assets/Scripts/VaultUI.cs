using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        static string T(string english,string japanese)=>VaultLanguage.T(english,japanese);
        readonly Color Mist=new Color(.76f,.82f,.87f), PanelInk=new Color(.025f,.04f,.057f,.96f);
        GUIStyle heading,centered,tiny,number;
        Vector2 expeditionMapScroll;
        int expeditionMapSelection;
        internal Vector2? AimUiReviewPoint;
        void Styles()
        {
            if(label!=null)return;
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=false};label.normal.textColor=Color.white;
            small=new GUIStyle(label){fontSize=14};tiny=new GUIStyle(label){fontSize=12};
            title=new GUIStyle(label){fontSize=68,fontStyle=FontStyle.Bold};
            heading=new GUIStyle(label){fontSize=38,fontStyle=FontStyle.Bold};
            number=new GUIStyle(label){fontSize=42,fontStyle=FontStyle.Bold};
            centered=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(label){fontSize=20,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(26,12,8,8)};
            button.normal.background=null;button.hover.background=null;button.active.background=null;
        }
        void Box(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        void Text(float x,float y,string value,GUIStyle style=null,Color? color=null,float width=900)
        {GUI.color=color??Color.white;GUI.Label(new Rect(x,y,width,160),value,style??label);GUI.color=Color.white;}
        void Line(Vector2 a,Vector2 b,Color color,float thickness=1)
        {
            Matrix4x4 previous=GUI.matrix;Vector2 delta=b-a;GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            Box(new Rect(a.x,a.y,delta.magnitude,thickness),color);GUI.matrix=previous;
        }
        void Diamond(float x,float y,float size,Color color)
        {Line(new Vector2(x,y-size),new Vector2(x+size,y),color);Line(new Vector2(x+size,y),new Vector2(x,y+size),color);Line(new Vector2(x,y+size),new Vector2(x-size,y),color);Line(new Vector2(x-size,y),new Vector2(x,y-size),color);}
        void Panel(Rect r,Color accent)
        {
            Box(new Rect(r.x+4,r.y+5,r.width,r.height),new Color(0,0,0,.44f));
            Box(r,PanelInk);Box(new Rect(r.x+2,r.y+2,r.width-4,24),new Color(.10f,.15f,.20f,.44f));
            Box(new Rect(r.x,r.y,r.width,2),new Color(accent.r,accent.g,accent.b,.65f));
            Box(new Rect(r.x,r.yMax-1,r.width,1),new Color(accent.r,accent.g,accent.b,.2f));
            foreach(float x in new[]{r.x,r.xMax-16}){Box(new Rect(x,r.y,16,2),accent);Box(new Rect(x,r.yMax-2,16,2),accent);}
            Box(new Rect(r.x,r.y,2,14),accent);Box(new Rect(r.xMax-2,r.yMax-14,2,14),accent);
        }
        bool Button(Rect r,string value,bool primary=false)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            Panel(r,primary||hover?Gold:Cyan*.65f);
            if(hover||primary)Box(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),new Color(.12f,.32f,.42f,hover?.45f:.25f));
            Diamond(r.x+12,r.center.y,3,primary?Gold:Cyan);
            bool clicked=GUI.Button(r,value,button);Text(r.xMax-35,r.center.y-14,"›",label,hover?Gold:Cyan,28);return clicked;
        }
        void LanguageSelector()
        {
            Panel(new Rect(1190,22,226,40),Gold*.6f);
            for(int i=0;i<2;i++)
            {
                bool chosen=i==0?VaultLanguage.Japanese:!VaultLanguage.Japanese;Rect r=new Rect(1198+i*106,27,104,30);
                if(chosen)Box(r,new Color(.3f,.23f,.11f,.7f));
                GUI.color=chosen?Gold:Mist;if(GUI.Button(r,i==0?"日本語":"ENGLISH",centered))VaultLanguage.Set(i==0,!smoke);GUI.color=Color.white;
            }
        }
        void OnGUI()
        {
            Styles();float scale=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);
            GUI.matrix=MobileTouchControls.Enabled?MobileCanvasMatrix:Matrix4x4.TRS(new Vector3((UnityEngine.Screen.width-1440*scale)/2,(UnityEngine.Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            if(screen!=Screen.Shop)debtPage=false;
            if(MobileTouchControls.Enabled)
            {
                if(DebtIntroVisible){DrawMobileDebtIntro();return;}
                if(manufacturerRegistry)DrawMobileManufacturer();
                else if(InventoryOpen)DrawMobileInventory();
                else if(screen==Screen.Menu)DrawMobileMenu();
                else if(screen==Screen.Hub)DrawMobileHub();
                else if(screen==Screen.Shop)DrawMobileShop();
                else if(screen==Screen.DungeonSelect)DrawMobileCityMap();
                else if(screen==Screen.Playing)DrawMobileHUD();
                else if(screen==Screen.Pause){DrawMobileHUD();DrawMobilePause();}
                else DrawMobileOverlay();
                if(screen==Screen.Menu&&!InventoryOpen&&!manufacturerRegistry)DrawMobileLanguageButton();return;
            }
            if(DebtIntroVisible){DrawDebtIntro();return;}
            if(manufacturerRegistry)DrawManufacturerRegistry();
            else if(InventoryOpen)DrawInventory();
            else if(screen==Screen.Menu)DrawMenu();
            else if(screen==Screen.Hub){DrawHub();DrawHubWorldLabels();if(Button(new Rect(1060,735,340,50),T("MANUFACTURERS","メーカー製品図鑑"))){manufacturerRegistry=true;manufacturerKind=-1;}}
            else if(screen==Screen.Shop)DrawShop();
            else if(screen==Screen.DungeonSelect)DrawExpeditionWorldMap();
            else
            {
                DrawHUD();if(screen!=Screen.Playing)DrawOverlay();
            }
            if(screen==Screen.Menu&&!InventoryOpen&&!manufacturerRegistry)LanguageSelector();
        }
        void DrawMobileLanguageButton()
        { if(MobileButton(new Rect(1190,12,220,94),VaultLanguage.Japanese?"ENGLISH":"日本語"))VaultLanguage.Set(!VaultLanguage.Japanese,!smoke); }
        void DrawMobileDebtIntro()
        {
            Box(new Rect(0,0,1440,900),new Color(.012f,.018f,.024f,.985f));Panel(new Rect(55,125,1330,660),new Color(1,.27f,.19f));
            Text(95,160,T("COLLAR DEBT // 1,000,000,000 SCRAP","首輪の負債 // 10億スクラップ"),new GUIStyle(heading){fontSize=42,wordWrap=true},new Color(1,.39f,.28f),1230);
            Text(95,245,T("You wake to a live explosive collar and a contract stamped with one billion scrap. The Raider Boss owns the detonator; his collector, Briggs, owns your earnings. Run the contaminated zones, bring back anything valuable, and pay until the collar comes off. Then settle the score with the boss.","目を覚ますと、首には作動中の爆弾首輪。契約書には10億スクラップの借金。起爆装置を握るのはレイダーボス、稼ぎを取り立てるのはブリッグスだ。汚染区域へ出撃し、戦利品を持ち帰って返済しろ。首輪を外したら、ボスに借りを返す。"),new GUIStyle(label){fontSize=31,wordWrap=true},Mist,1220);
            Text(95,480,T("Repayment earns levels and skill points. Choose a weapon path, grow stronger, and survive long enough to clear the debt.","返済でレベルとスキルポイントを獲得する。武器系統を選んで強くなり、借金を返しきるまで生き残れ。"),new GUIStyle(label){fontSize=29,wordWrap=true},Cyan,1220);
            if(MobileButton(new Rect(300,640,840,120),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃")))MarkDebtIntroSeen();
        }
        void DrawDebtIntro()
        {
            Box(new Rect(0,0,1440,900),new Color(.012f,.018f,.024f,.985f));
            Panel(new Rect(205,150,1030,600),new Color(1,.27f,.19f));
            Text(250,190,T("COLLAR DEBT // 1,000,000,000 SCRAP","首輪の負債 // 10億スクラップ"),heading,new Color(1,.39f,.28f),930);
            Text(255,280,T("You wake to a live explosive collar and a contract stamped with one billion scrap. The Raider Boss owns the detonator; his collector, Briggs, owns your earnings. Run the contaminated zones, bring back anything valuable, and pay until the collar comes off. Then settle the score with the boss.","目を覚ますと、首には作動中の爆弾首輪。契約書には10億スクラップの借金。起爆装置を握るのはレイダーボス、稼ぎを取り立てるのはブリッグスだ。汚染区域へ出撃し、戦利品を持ち帰って返済しろ。首輪を外したら、ボスに借りを返す。"),new GUIStyle(label){fontSize=23,wordWrap=true},Mist,920);
            Text(255,490,T("Every 25,000,000 paid grants a level and a skill point. Find Briggs at the safehouse to make payments and build your weapon path.","返済累計2,500万ごとにレベルとスキルポイントを獲得。拠点のブリッグスに話しかけ、返済と武器系統の強化を進めよう。"),small,Cyan,900);
            if(MobileTouchControls.Enabled?MobileButton(new Rect(350,620,740,115),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃")):Button(new Rect(505,640,430,64),T("I UNDERSTAND / BEGIN","契約を受け入れて出撃"),true))MarkDebtIntroSeen();
        }
        void DrawMenu()
        {
            if(menuArt!=null)GUI.DrawTexture(new Rect(0,0,1440,900),menuArt,ScaleMode.ScaleAndCrop);
            for(int i=0;i<40;i++)Box(new Rect(i*24,0,25,900),new Color(.008f,.016f,.022f,.96f*Mathf.Pow(1-i/40f,.65f)));
            Box(new Rect(0,0,1440,78),new Color(.012f,.02f,.025f,.65f));
            Text(48,27,"A F   /   THE WORLD AFTER THE FLASH",small,Gold);
            Line(new Vector2(64,139),new Vector2(505,139),Gold*.6f);Diamond(64,139,6,Gold);
            Text(64,164,T("SURVIVAL PROTOCOL // 07","生存プロトコル // 07"),small,Cyan);
            Text(60,204,"ASHFALL",title,Gold);Text(66,300,T("AFTER THE LAST LIGHT","最後の灯が消えた世界で"),heading,Color.white,800);
            Text(66,375,T("SCAVENGE. SURVIVE. RETURN.","拾え。生き延びろ。帰ってこい。"),label);
            Text(66,423,T("Outfit your survivor. Salvage experimental weapons.\nFace infected hordes across three contaminated zones.","避難拠点で装備を整え、実験兵器を回収。\n3つの汚染区域で、感染者の群れと変異体を突破せよ。"),small,Mist,560);
            if(Button(new Rect(66,483,440,60),T("LAST LIGHT / HOME BASE","灯火基地 / 拠点へ"),true))EnterHub();
            if(Button(new Rect(66,552,440,54),T("ARSENAL / EQUIPMENT","武器庫 / 装備")))ToggleInventory();
            if(HasSavedRun&&Button(new Rect(66,616,440,54),T("CONTINUE SAVED EXPEDITION","保存した探索を再開"),true))ResumeSavedRun();
            Line(new Vector2(67,686),new Vector2(506,686),new Color(.4f,.5f,.5f,.5f));
            Text(67,700,MobileTouchControls.Enabled?T("LEFT STICK MOVE / RIGHT STICK AIM / HOLD FIRE","左スティック移動 / 右スティック照準 / 射撃ボタンを押す"):T("WASD  MOVE  /  MOUSE  AIM + FIRE  /  SPACE  DASH","WASD 移動  /  マウス 照準・射撃  /  SPACE ダッシュ"),small,Mist);
            Text(67,727,MobileTouchControls.Enabled?T("RELOAD / MELEE / TAKE / USE / GEAR / PAUSE BUTTONS","装填 / 近接 / 拾得 / 調べる / 装備 / 停止ボタン"):T("R RELOAD / RMB MELEE / F PICKUP / E EXIT / I EQUIPMENT / ESC PAUSE","R 装填 / 右クリック近接 / F 拾得 / E 出口 / I 装備 / ESC 停止"),small,Mist);
            Panel(new Rect(960,644,408,162),Gold);
            Text(982,660,T("THE CONTAMINATED FRONTIER","汚染されたフロンティア"),small,Gold);
            Text(982,696,T("SAFEHOUSE / 3 HOT ZONES","避難拠点 / 3つの汚染区域"),label);
            Text(982,738,T("Infected hordes. Radiation mutants.\nTwelve retro-future weapon designs.","感染者の群れ。放射能変異体。\n12系統のレトロフューチャー兵器。"),small,Mist,365);
            Box(new Rect(0,834,1440,66),new Color(.012f,.022f,.027f,.94f));
            Text(48,852,toastTimer>0?Toast:T("QUARTER VIEW / WASTELAND SURVIVAL","クオータービュー / 荒野のサバイバル"),small,toastTimer>0?Gold:Mist);
            Text(1200,852,"ASHFALL  /  0.7",tiny,Gold,220);toastTimer-=Time.unscaledDeltaTime;
        }
        void DrawOverlay()
        {
            Box(new Rect(0,0,1440,900),new Color(.008f,.015f,.022f,.88f));
            if(screen==Screen.Reward)
            {
                Text(162,195,T("CHOOSE YOUR AUGMENT","強化を選択"),heading,Gold);
                Text(165,260,T("Take one field upgrade into zone "+(room+2)+".","現地改造をひとつ選び、区域 "+(room+2)+" へ。"),label,Mist);
                string[] names={T("OVERCHARGE","オーバーチャージ"),T("REINFORCED SHELL","強化装甲"),T("RAPID CYCLER","高速機構"),T("REPAIR PROTOCOL","修復プロトコル"),T("SHOCK BOOSTER","衝撃ブースター"),T("TESLA BRIDGE","テスラ導線"),T("BIO RECOVERY","生体回収")};
                string[] desc={T("Run damage +25%","周回の威力 +25%"),T("Run max HP +30 / heal 35","周回HP +30 / HP35回復"),T("Run fire rate +22%","周回の連射 +22%"),T("Restore all HP","HPを全回復"),T("Dash cooldown bonus +35% / dash pulse","回避待機短縮 +35% / ダッシュ波動"),T("Hits chain 30% damage to a nearby target","命中で近くの敵へ威力30%の連鎖"),T("Heal 3 HP per kill this run","周回中の撃破ごとにHP3回復")};
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
            {Text(390,365,T("Take a breath. Your arsenal is waiting.","ひと息つこう。戦いは、ここから続く。"),label,Mist);if(Button(new Rect(390,443,660,68),T("RESUME EXPEDITION","探索を再開"),true)){screen=Screen.Playing;Time.timeScale=1;}}
            else
            {Text(390,372,Toast,label,Gold);if(Button(new Rect(390,443,660,68),T("RETURN TO LAST LIGHT","灯火基地へ帰還"),true))EnterHub();}
            if(Button(new Rect(390,529,660,62),T("RETURN TO LAST LIGHT","灯火基地へ帰還")))EnterHub();
            if(screen==Screen.Pause && Button(new Rect(390,606,660,42),T("ARSENAL / EQUIPMENT [I]","武器庫 / 装備 [I]")))ToggleInventory();
            if(screen==Screen.Pause && Button(new Rect(390,654,660,48),T("SAVE EXPEDITION & RETURN TO MENU","探索を保存してメニューへ")))SaveRunAndReturnToMenu();
        }
        void DrawHUD()
        {
            int alive=0;foreach(var f in Enemies)if(f!=null&&!f.dead)alive++;
            Panel(new Rect(24,22,338,88),Gold*.7f);
            Text(42,32,T("ZONE","探索区域"),tiny,Mist);Text(42,56,(room+1).ToString("00")+" / "+CurrentDungeon.AreaCount.ToString("00"),label,Gold);
            for(int i=0;i<CurrentDungeon.AreaCount;i++)Box(new Rect(159+i*25,68,18,4),i<=room?Gold:new Color(.2f,.27f,.28f));
            Text(440,28,room==CurrentDungeon.AreaCount-1?T("WASTELAND COLOSSUS","荒野の巨骸"):SelectedExpeditionStage==null?CurrentDungeon.Name:(SelectedStageId+1).ToString("00")+" / "+SelectedExpeditionStage.Name,label,Gold,650);
            Text(440,61,cleared?T("EXIT UNSEALED / Follow the gold marker","出口が開放 / 金色のマーカーへ"):T("HOSTILES  "+alive+"   /   EXPLORE THE CHAMBERS","残敵 "+alive+"   /   広間を探索せよ"),small,Mist,720);
            if(SelectedExpeditionStage!=null){var stage=SelectedExpeditionStage;string mission=ExpeditionMap.MissionLabel(stage.mission,VaultLanguage.Japanese);if(stage.mission==ExpeditionMission.HuntTarget||stage.mission==ExpeditionMission.DestroyNest)mission+=" / "+stage.Target;mission+="  ·  "+MissionProgress;Text(42,117,mission,new GUIStyle(tiny){wordWrap=true},MissionCompleted?Cyan:Gold,350);}else Text(42,121,T("SEED "+seed+"  /  "+Mathf.FloorToInt(runTime)+"s","シード "+seed+"  /  "+Mathf.FloorToInt(runTime)+"秒"),tiny,Mist,350);
            if(toastTimer>0){Panel(new Rect(405,111,720,44),Gold*.6f);Text(421,123,Toast,small,Gold,695);}
            Panel(new Rect(24,754,375,122),Cyan);
            Text(43,765,T("VITALS","生存状態"),tiny,Mist);
            Text(42,785,Mathf.CeilToInt(hp).ToString(),number,Color.white,110);Text(138,805,"/ "+maxHp+" HP",small,Mist,160);
            float value=Mathf.Clamp01(hp/maxHp);
            for(int i=0;i<15;i++){Rect r=new Rect(44+i*22,841,18,12);Box(r,new Color(.15f,.23f,.24f));Box(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value*15-i),r.height),hp<maxHp*.25f?new Color(1,.27f,.3f):Cyan);}
            Panel(new Rect(414,798,197,78),dodgeCooldown<=0?Cyan:Gold*.4f);
            Diamond(436,836,7,dodgeCooldown<=0?Cyan:Mist);
            Text(455,809,T("SPACE / DASH","SPACE / 回避"),tiny,Mist,150);Text(455,836,dodgeCooldown<=0?T("READY","使用可能"):dodgeCooldown.ToString("0.0")+T("s","秒"),small,Cyan,150);
            Panel(new Rect(981,676,435,200),gun.color);
            Text(1001,685,(ActiveWeaponSlot+1)+" / "+WeaponSlotCount+T("  WHEEL / SWITCH  ·  I / EQUIPMENT","  ホイール / 銃切替  ·  I / 装備"),tiny,Mist,395);
            Text(1001,710,GunNumbers(gun,false).Replace(T("  RATE ","  連射 "),T("\nRATE ","\n連射 ")).Replace(T("  RELOAD ","  装填 "),T("\nRELOAD ","\n装填 ")),tiny,Gold,395);
            Text(1081,766,VaultLanguage.RarityName(gun.rarity),tiny,gun.color,320);
            ItemPortraits.Draw(new Rect(1001,775,68,68),gun);Text(1081,788,VaultLanguage.GunName(gun),small,Color.white,320);
            Text(1081,811,VaultLanguage.GunDescription(gun),new GUIStyle(tiny){wordWrap=true},Mist,320);
            Text(1001,846,reload>0?T("RELOADING…","リロード中…"):T("[R] RELOAD","[R] リロード"),small,Gold,230);
            AmmoType activeAmmo=VaultGame.AmmoForGun(gun);
            Text(1260,833,reload>0?T("RELOADING","装填中"):ammo.ToString("00")+" / "+EffectiveMagazine+"  ·  "+AmmoReserve(activeAmmo)+" "+T("reserve","予備"),tiny,gun.color,145);
            if(reload>0)Box(new Rect(982,873,433*Mathf.Clamp01(1-reload/Mathf.Max(.01f,EffectiveReload)),3),Gold);
            DrawMap();
            if(screen!=Screen.Playing || InventoryOpen)return;
            float s=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);
            float mx=(Input.mousePosition.x-(UnityEngine.Screen.width-1440*s)/2)/s,my=(UnityEngine.Screen.height-Input.mousePosition.y-(UnityEngine.Screen.height-900*s)/2)/s;if(MobileTouchControls.Enabled){Vector3 mobileAim=VaultMobileInputBridge.ToWorldMovement(MobileTouchControls.Aim).normalized;Vector3 screenAim=cam.WorldToScreenPoint(PlayerPosition+mobileAim*6+Vector3.up*.9f);Vector2 canvasAim=MobileCanvasPoint(new Vector2(screenAim.x,screenAim.y));mx=canvasAim.x;my=canvasAim.y;}
            if(smoke&&AimUiReviewPoint.HasValue){mx=AimUiReviewPoint.Value.x;my=AimUiReviewPoint.Value.y;}
            Drop drop=NearDrop();
            ArmorDrop armorDrop=NearArmor();MeleeDrop meleeDrop=NearMeleeDrop();ArtifactDrop moduleDrop=NearArtifact();
            float gunD=drop==null?float.MaxValue:Vector3.Distance(PlayerPosition,drop.position),armorD=armorDrop==null?float.MaxValue:Vector3.Distance(PlayerPosition,armorDrop.position),meleeD=meleeDrop==null?float.MaxValue:Vector3.Distance(PlayerPosition,meleeDrop.position),moduleD=moduleDrop==null?float.MaxValue:Vector3.Distance(PlayerPosition,moduleDrop.position);
            bool showGun=drop!=null&&gunD<=armorD&&gunD<=meleeD&&gunD<=moduleD;
            bool showArmor=armorDrop!=null&&armorD<gunD&&armorD<=meleeD&&armorD<=moduleD;
            bool showMelee=meleeDrop!=null&&meleeD<gunD&&meleeD<armorD&&meleeD<=moduleD;
            bool showModule=moduleDrop!=null&&moduleD<gunD&&moduleD<armorD&&moduleD<meleeD;
            if(showGun)
            {
                Panel(new Rect(410,628,620,141),drop.gun.color);ItemPortraits.Draw(new Rect(422,647,76,76),drop.gun);Text(510,640,T(MobileTouchControls.Enabled?"TAP TAKE WEAPON / ":"[F] PICK UP WEAPON / ",MobileTouchControls.Enabled?"タップで回収 / ":"[F] 銃を拾う / ")+VaultLanguage.RarityName(drop.gun.rarity),tiny,drop.gun.color);
                Text(510,665,VaultLanguage.GunName(drop.gun),label,Color.white,500);Text(510,701,ManufacturerCatalog.Name(ManufacturerBranding.Maker(drop.gun),VaultLanguage.Japanese),tiny,Mist,500);
                Text(430,736,GunNumbers(drop.gun)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(drop.gun),new GUIStyle(tiny){wordWrap=true},Gold,575);
            }
            if(showArmor)
            {
                ArmorItem item=armorDrop.item;Color rarityColor=Arsenal.Colors[(int)item.rarity];Panel(new Rect(410,610,620,159),rarityColor);
                Text(430,622,T(MobileTouchControls.Enabled?"TAP TAKE ARMOR / ":"[F] PICK UP ARMOR / ",MobileTouchControls.Enabled?"タップで回収 / ":"[F] 防具を拾う / ")+Armory.SlotName(item.slot)+" / Lv "+item.level,tiny,rarityColor,575);
                ItemPortraits.Draw(new Rect(422,654,76,76),item);Text(510,647,Armory.Name(item),label,rarityColor,500);
                Text(510,677,Armory.Description(item),new GUIStyle(small){wordWrap=true},Mist,500);
                GUIStyle statsStyle=new GUIStyle(tiny){wordWrap=true};Text(430,714,Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item),statsStyle,Gold,575);
            }
            if(showModule)
            {ArtifactItem relic=moduleDrop.item;Color rarityColor=Arsenal.Colors[(int)relic.rarity];Panel(new Rect(410,610,620,159),rarityColor);ManufacturerBranding.ModulePortrait(new Rect(422,654,76,76),relic);Text(510,622,T(MobileTouchControls.Enabled?"TAP TAKE MODULE / ":"[F] FIELD MODULE / ",MobileTouchControls.Enabled?"タップで回収 / ":"[F] 拡張モジュール / ")+VaultLanguage.RarityName(relic.rarity)+" / Lv "+relic.level,tiny,rarityColor,500);Text(510,647,relic.Name+"  ·  "+T("SCORE ","評価 ")+GearScoring.Score(relic),label,Color.white,500);Text(510,681,relic.Description,new GUIStyle(small){wordWrap=true},Mist,500);}
            if(showMelee)
            {MeleeWeaponItem meleeLoot=meleeDrop.item;Color rarityColor=meleeLoot.color;Panel(new Rect(410,610,620,159),rarityColor);DrawMeleeIcon(new Rect(422,654,64,64),meleeLoot.family);Text(510,622,T(MobileTouchControls.Enabled?"TAP TAKE MELEE / ":"[F] PICK UP MELEE / ",MobileTouchControls.Enabled?"タップで回収 / ":"[F] 近接武器を拾う / ")+VaultLanguage.RarityName(meleeLoot.rarity)+" / Lv "+meleeLoot.level,tiny,rarityColor,500);Text(510,647,MeleeWeaponCatalog.Name(meleeLoot)+"  ·  "+T("SCORE ","評価 ")+MeleeWeaponCatalog.Score(meleeLoot),label,Color.white,500);Text(510,681,T("DMG ","威力 ")+meleeLoot.damage+"  ·  "+T("RATE ","速度 ")+(1/Mathf.Max(.01f,meleeLoot.interval)).ToString("0.0")+T("/s  · RANGE ","/秒  · 射程 ")+meleeLoot.range.ToString("0.0")+"m",small,Mist,500);Text(510,714,T("ARC ","攻撃角 ")+meleeLoot.arcDegrees.ToString("0")+"°  ·  "+(meleeLoot.uniqueEffect?T("LEGENDARY UNIQUE EFFECT","レジェンダリー固有効果"):T("Store and equip in [I]","[I]で所持品に保存・装備")),tiny,Cyan,500);}
            if(NearbyAmmoCrate||NearbySupplyChest)
            {bool crate=NearbyAmmoCrate;Panel(new Rect(410,474,620,58),crate?new Color(.92f,.45f,.18f):Gold);Text(428,482,crate?T(MobileTouchControls.Enabled?"TAP OPEN AMMO CRATE":"[F] OPEN AMMO CRATE",MobileTouchControls.Enabled?"タップで弾薬箱を開ける":"[F] 弾薬箱を開ける"):T(MobileTouchControls.Enabled?"TAP OPEN SUPPLY CACHE":"[F] OPEN SUPPLY CACHE",MobileTouchControls.Enabled?"タップで物資箱を開ける":"[F] 物資箱を開ける"),small,Color.white,580);Text(428,509,crate?AmmoLabel(NearbyAmmoCrateType)+T(" · FULL PACK"," · 1パック"):T("Contains salvage, ammunition, or equipment","回収品・弾薬・装備のいずれか"),tiny,Mist,580);}
            if(cleared)
            {Vector3 p=cam.WorldToScreenPoint(ExitPosition+Vector3.up*2);if(p.z>0){float x=(p.x-(UnityEngine.Screen.width-1440*s)/2)/s,y=(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*s)/2)/s;Text(Mathf.Clamp(x-80,20,1260),Mathf.Clamp(y,170,680),T("[E] DESCEND","[E] 次のエリアへ"),small,Gold,190);}}
            foreach(var f in Enemies)if(f!=null&&!f.dead&&(f.kind==3||f.hp<f.maxHp-.01f)&&Layout.IsVisible(f.transform.position))
            {
                Vector3 p=cam.WorldToScreenPoint(f.transform.position+Vector3.up*(f.kind==3?4.6f:2.4f));if(p.z<=0)continue;
                float x=(p.x-(UnityEngine.Screen.width-1440*s)/2)/s,y=(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*s)/2)/s;
                Box(new Rect(x-28,y,56,4),PanelInk);Box(new Rect(x-28,y,56*Mathf.Clamp01(f.hp/f.maxHp),4),f.kind==3?Gold:Cyan);
                Text(x-60,y-18,Foe.Name(f.kind),tiny,f.kind==8?Cyan:Mist,160);
            }
            DrawCrosshair(mx,my);
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
            ExpeditionStage stage=ExpeditionMap.Get(Mathf.Clamp(expeditionMapSelection,0,ExpeditionMap.StageCount-1));bool unlocked=stage.level<=DebtLevel;bool cleared=clearedExpeditionStages.Contains(stage.id);
            Text(1042,174,T("FIELD DOSSIER / ","現地記録 / ")+stage.Name,label,Gold,330);
            Text(1042,219,stage.Location,new GUIStyle(small){wordWrap=true},stage.regionColor,330);
            Line(new Vector2(1042,270),new Vector2(1374,270),new Color(.55f,.72f,.72f,.38f));
            Text(1042,286,T("THREAT CLEARANCE","危険度許可"),tiny,Mist,320);
            Text(1042,311,T("LEVEL ","レベル ")+stage.level+"   /   "+T("YOUR LEVEL ","現在 ")+DebtLevel,label,unlocked?Cyan:new Color(1,.40f,.31f),330);
            Text(1042,356,T("BIOME","地形"),tiny,Mist,320);Text(1042,378,stage.Biome,small,Color.white,330);
            Text(1042,422,T("CONTRACT","任務"),tiny,Mist,320);
            Text(1042,445,ExpeditionMap.MissionLabel(stage.mission,VaultLanguage.Japanese),label,Gold,330);
            Text(1042,481,T("MARKED TARGET","標的"),tiny,Mist,320);
            Text(1042,503,stage.Target,small,new Color(1,.51f,.37f),330);
            Text(1042,548,T("FIRST CLEAR CACHE","初回クリア報酬"),tiny,Mist,320);
            Text(1042,572,ExpeditionRewardDescription(stage.id),small,Cyan,330);
            Text(1042,619,cleared?T("SITE CLEARED","制圧済み"):unlocked?T("CONTRACT AVAILABLE","出撃可能"):T("CLEARANCE TOO LOW","許可レベル不足"),tiny,cleared?Cyan:unlocked?Gold:new Color(1,.40f,.31f),330);
            if(Button(new Rect(1040,680,335,62),unlocked?T("DEPLOY TO SITE","地点へ出撃"):T("LOCKED / REPAY TO LEVEL UP","ロック中 / 返済で成長"),unlocked)&&unlocked)EnterExpeditionStage(stage.id);
            Text(1042,757,T("REWARD IS FIXED PER SITE; CLAIMED ONCE.","報酬は地点ごとに固定・初回のみ。"),tiny,Mist,330);
        }
        void DrawMap()
        {
            Layout.Discover(PlayerPosition);
            Panel(new Rect(1190,80,226,271),Cyan*.7f);Text(1206,90,T("HOT ZONE / TACTICAL","汚染区域 / 戦術マップ"),tiny,Gold,210);
            float tile=Mathf.Min(190f/Layout.Columns,195f/Layout.Rows);Vector2 origin=new Vector2(1214,117);
            for(int x=0;x<Layout.Columns;x++)for(int z=0;z<Layout.Rows;z++)if(Layout.open[x,z]&&Layout.IsExplored(x,z))Box(new Rect(origin.x+x*tile,origin.y+(Layout.Rows-1-z)*tile,tile-.7f,tile-.7f),Layout.IsVisible(x,z)?new Color(.28f,.44f,.46f):new Color(.13f,.20f,.22f));
            DrawTacticalMapMarkers(origin,tile,4);
            var exitCell=Layout.ToCell(ExitPosition);if(Layout.IsExplored(exitCell.x,exitCell.y))MapPoint(ExitPosition,origin,tile,cleared?Gold:new Color(.48f,.42f,.28f),4);MapPoint(PlayerPosition,origin,tile,Cyan,4);
            Text(1206,278,T("SECTORS","周回ルート"),tiny,Mist,190);
            for(int i=0;i<route.Count;i++){float x=1210+i*27;Box(new Rect(x,309,16,16),i==room?Cyan:i<room?Gold*.65f:new Color(.22f,.3f,.31f));if(i<6)Box(new Rect(x+16,316,11,1),Gold*.5f);}
            Text(1206,332,T("YOU / HOSTILES / EXIT","自分 / 敵 / 出口"),tiny,Mist,205);
        }
        void MapPoint(Vector3 p,Vector2 origin,float tile,Color color,float size)
        {Vector2Int c=Layout.ToCell(p);float x=origin.x+(c.x+.5f)*tile,y=origin.y+(Layout.Rows-.5f-c.y)*tile;Diamond(x,y,size,color);Box(new Rect(x-1,y-1,2,2),color);}
    }
}





