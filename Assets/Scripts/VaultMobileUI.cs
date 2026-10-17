using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public bool MobileModalVisible=>InventoryOpen||manufacturerRegistry||DebtIntroVisible||screen!=Screen.Playing&&screen!=Screen.Hub;
        public static Matrix4x4 MobileCanvasMatrix
        {
            get
            {
                Rect s=MobileTouchControls.SafeRect;if(s.width<1)s=new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height);
                float scale=Mathf.Min(s.width/1440f,s.height/900f);
                return Matrix4x4.TRS(new Vector3(s.x+(s.width-1440*scale)*.5f,s.y+(s.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            }
        }
        Vector2 MobileCanvasPoint(Vector2 bottomScreen)=>MobileCanvasMatrix.inverse.MultiplyPoint3x4(new Vector3(bottomScreen.x,UnityEngine.Screen.height-bottomScreen.y,0));
        bool MobileButton(Rect r,string caption){Panel(r,Cyan);return GUI.Button(r,caption,new GUIStyle(centered){fontSize=29,wordWrap=true});}
        void DrawMobileMenu()
        {
            if(menuArt)GUI.DrawTexture(new Rect(0,0,1440,900),menuArt,ScaleMode.ScaleAndCrop);
            Box(new Rect(0,0,770,900),new Color(.018f,.026f,.03f,.9f));
            Text(55,118,"ASHFALL",title,Gold,660);Text(60,222,T("MOBILE / LAST LIGHT","モバイル / 最後の灯"),heading,Cyan,670);
            Text(60,310,T("Left stick: move\nRight stick: drag to aim, hold to fire\nRelease to stop shooting","左スティック：移動\n右スティック：照準・押し続けて射撃\n指を離すと射撃停止"),new GUIStyle(label){fontSize=28,wordWrap=true},Mist,650);
            if(MobileButton(new Rect(58,510,650,112),T("ENTER SAFEHOUSE","拠点へ")))EnterHub();
            if(MobileButton(new Rect(58,645,650,112),T("EQUIPMENT","装備を確認")))ToggleInventory();
            Text(60,805,T("Landscape / iPhone + iPad","横画面 / iPhone・iPad"),label,Mist,650);
        }
        void DrawMobileHub()
        {
            Panel(new Rect(24,22,620,116),Gold);Text(42,36,T("LAST LIGHT / SAFEHOUSE","灯火基地 / 避難拠点"),new GUIStyle(label){fontSize=28},Gold,595);
            Text(42,84,T("LEVEL ","レベル ")+DebtLevel+T("  SCRAP ","  スクラップ ")+Credits.ToString("N0"),new GUIStyle(label){fontSize=25},Cyan,595);
            if(MobileButton(new Rect(690,30,270,110),T("CITY MAP","都市マップ")))screen=Screen.DungeonSelect;
            if(MobileButton(new Rect(690,157,270,110),T("MERCHANT","装備商人"))){debtPage=false;debtSkillTreePage=false;shopSellingPage=false;screen=Screen.Shop;}
            if(MobileButton(new Rect(690,284,270,110),T("MAKERS","製品図鑑"))){manufacturerRegistry=true;manufacturerKind=-1;}
            string hint=Vector3.Distance(PlayerPosition,debtCollectorPosition)<3?T("USE: BRIGGS / DEBT + SKILLS","調べる：ブリッグス / 返済・スキル"):Vector3.Distance(PlayerPosition,merchantPosition)<3?T("USE: MARA / MERCHANT","調べる：マラ / 装備商人"):Vector3.Distance(PlayerPosition,archivistPosition)<3?T("USE: IONE / ADVICE","調べる：イオネ / 探索の助言"):T("Move to an NPC and tap USE","NPCへ近づいて「調べる」");
            Text(380,740,string.IsNullOrEmpty(hubDialogue)?hint:hubDialogue,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,680);
        }
        void DrawMobileHUD()
        {
            if(gun==null)return;
            Panel(new Rect(20,20,590,125),Cyan);
            Text(38,28,Mathf.CeilToInt(hp)+" / "+maxHp+" HP",new GUIStyle(number){fontSize=36},hp<maxHp*.25f?Gold:Color.white,570);
            Text(38,81,(room+1)+" / "+CurrentDungeon.AreaCount+"  "+CurrentDungeon.Name,new GUIStyle(label){fontSize=25},Gold,570);
            Box(new Rect(36,132,556,6),new Color(.12f,.2f,.22f));Box(new Rect(36,132,556*Mathf.Clamp01(hp/maxHp),6),Cyan);
            Panel(new Rect(630,20,400,125),gun.color);
            Text(646,28,(ActiveWeaponSlot+1)+" / "+WeaponSlotCount+"  "+VaultLanguage.GunName(gun),new GUIStyle(label){fontSize=24},gun.color,370);
            Text(646,83,reload>0?T("RELOADING ","装填中 ")+Mathf.CeilToInt(reload*10)/10f:ammo+" / "+EffectiveMagazine+T("  RESERVE ","  予備 ")+AmmoReserve(AmmoForGun(gun)),new GUIStyle(label){fontSize=27},Color.white,370);
            int alive=0;foreach(var enemy in Enemies)if(enemy&&!enemy.dead)alive++;
            Text(38,154,cleared?T("EXIT OPEN / approach and tap USE","出口開放 / 近づいて「調べる」"):T("HOSTILES ","残敵 ")+alive,new GUIStyle(label){fontSize=25},cleared?Gold:Mist,740);
            if(toastTimer>0)Text(360,210,Toast,new GUIStyle(label){fontSize=25,wordWrap=true},Gold,670);
            Rect mapRect=new Rect(1070,170,345,205);Panel(mapRect,Cyan);
            if(Layout!=null)
            {
                float tile=Mathf.Min(310f/Layout.Columns,176f/Layout.Rows);Vector2 origin=new Vector2(1085,184);
                for(int x=0;x<Layout.Columns;x++)for(int z=0;z<Layout.Rows;z++)if(Layout.open[x,z]&&Layout.IsExplored(x,z))Box(new Rect(origin.x+x*tile,origin.y+(Layout.Rows-1-z)*tile,Mathf.Max(1,tile-.5f),Mathf.Max(1,tile-.5f)),Layout.IsVisible(x,z)?new Color(.28f,.44f,.46f):new Color(.13f,.2f,.22f));
                MapPoint(PlayerPosition,origin,tile,Cyan,5);var exit=Layout.ToCell(ExitPosition);if(Layout.IsExplored(exit.x,exit.y))MapPoint(ExitPosition,origin,tile,Gold,5);
            }
            if(Active&&!InventoryOpen)
            {
                Vector3 worldAim=VaultMobileInputBridge.ToWorldMovement(MobileTouchControls.Aim).normalized;
                Vector3 p=cam.WorldToScreenPoint(PlayerPosition+worldAim*6+Vector3.up*.9f);
                Vector2 reticle=MobileCanvasPoint(p);DrawCrosshair(reticle.x,reticle.y);
                var drop=NearDrop();var armor=NearArmor();var module=NearArtifact();var melee=NearMeleeDrop();
                string loot=drop!=null?VaultLanguage.GunName(drop.gun):armor!=null?Armory.Name(armor.item):module!=null?module.item.Name:melee!=null?MeleeWeaponCatalog.Name(melee.item):null;
                if(loot!=null||NearbyAmmoCrate||NearbySupplyChest)
                {
                    Panel(new Rect(420,490,600,120),Gold);
                    Text(440,510,loot??(NearbyAmmoCrate?T("AMMO CRATE","弾薬箱"):T("SUPPLY CHEST","物資チェスト")),new GUIStyle(label){fontSize=26,wordWrap=true},Gold,560);
                    Text(440,566,T("Tap TAKE to collect / open","「拾得」で回収・開封"),new GUIStyle(label){fontSize=25},Mist,560);
                }
                foreach(var foe in Enemies)if(foe&&!foe.dead&&foe.hp<foe.maxHp&&Layout.IsVisible(foe.transform.position))
                {var screen=cam.WorldToScreenPoint(foe.transform.position+Vector3.up*2.5f);if(screen.z<=0)continue;Vector2 at=MobileCanvasPoint(screen);Box(new Rect(at.x-30,at.y,60,5),PanelInk);Box(new Rect(at.x-30,at.y,60*Mathf.Clamp01(foe.hp/foe.maxHp),5),Gold);}
            }
        }
        void DrawMobilePause()
        {
            Box(new Rect(0,0,1440,900),new Color(.01f,.02f,.025f,.88f));Text(300,190,T("EXPEDITION PAUSED","探索一時停止"),heading,Gold,900);
            if(MobileButton(new Rect(300,320,840,120),T("RESUME","探索を再開"))){screen=Screen.Playing;Time.timeScale=1;}
            if(MobileButton(new Rect(300,470,840,120),T("EQUIPMENT","装備")))ToggleInventory();
            if(MobileButton(new Rect(300,620,840,120),T("RETURN TO SAFEHOUSE","拠点へ帰還"))){Time.timeScale=1;EnterHub();}
        }
    }
}
