using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public float runDamage,runFire,runVitality,runDash,runChain,runHarvest;
        readonly Vector3 merchantPosition=new Vector3(-7,.65f,2),archivistPosition=new Vector3(6,.65f,2),portalPosition=new Vector3(0,.65f,10);
        string hubDialogue="";
        public void BuildHub()
        {
            if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            foreach(var material in materials)if(material!=null)Destroy(material);materials.Clear();Enemies.Clear();drops.Clear();armorDrops.Clear();ClearArtifactDrops();satellites.Clear();
            world=new GameObject("LAST LIGHT / survivor safehouse").transform;
            Layout=new SectorLayout(new System.Random(17017));System.Array.Clear(Layout.open,0,Layout.open.Length);
            for(int x=0;x<Layout.Columns;x++)for(int y=0;y<Layout.Rows;y++){var p=Layout.Center(new Vector2Int(x,y));if(new Vector2(p.x,p.z).magnitude<13.5f)Layout.open[x,y]=true;}
            Layout.walls.Clear();Layout.covers.Clear();halfWidth=17;halfDepth=17;
            art=new VaultArt(world,materials,new System.Random(17017));art.Floor(16,15,0);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;art.Column(new Vector3(Mathf.Sin(a)*12.5f,0,Mathf.Cos(a)*12.5f),2.5f);}
            art.Cover(merchantPosition+Vector3.back*2);art.Brazier(merchantPosition+Vector3.left*2);art.Cover(archivistPosition+Vector3.back*2);
            var merchant=art.Character(merchantPosition,true,1);merchant.name="Mara / Salvage Merchant";
            var archivist=art.Character(archivistPosition,true,9);archivist.name="Ione / Archivist";
            var collector=art.Character(debtCollectorPosition,true,4);collector.name="Briggs / Debt Collector";
            art.Gate(portalPosition.z);art.Ring("Extraction pad",portalPosition-Vector3.up*.6f,1.8f,.12f,Cyan,true);
            player=art.Character(new Vector3(0,.65f,-3),true);barrel=art.Weapon(player,gun.color);VaultArt.ConfigureWeapon(barrel,gun.effect,gun.color,VaultLanguage.DisplayModel(gun));PlayerAppearance.ApplyArmor(EquippedArmor);
            aim=Vector3.right;PlayerWeapon.Aim(aim);hubDialogue="";cam.orthographicSize=11;UpdateCamera(true);
        }
        void UpdateHub()
        {
            var move=ReadPlayerMovement();player.position=MoveWithinRoom(player.position,move*EffectiveMoveSpeed*Time.deltaTime,.45f);
            if(!VaultMobileInputBridge.Enabled&&move.sqrMagnitude>.01f){aim=move.normalized;player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);}
            if(VaultMobileInputBridge.Enabled)ApplyMobileAim(VaultMobileInputBridge.Aim);
            if(VaultMobileInputBridge.Pressed(MobileAction.Interact,KeyCode.E)){
                if(Vector3.Distance(PlayerPosition,debtCollectorPosition)<3){debtPage=true;debtSkillTreePage=false;screen=Screen.Shop;}
                else if(Vector3.Distance(PlayerPosition,merchantPosition)<3){debtPage=false;debtSkillTreePage=false;shopSellingPage=false;pendingSaleKey=null;screen=Screen.Shop;}
                else if(Vector3.Distance(PlayerPosition,portalPosition)<3)screen=Screen.DungeonSelect;
                else if(Vector3.Distance(PlayerPosition,archivistPosition)<3)hubDialogue=T("IONE: Equipment survives each expedition; field upgrades do not. Flank plated infected. Dodge acid and a locked needle shot, then strike during recovery. Never let the shamblers surround you.","イオネ：持ち帰った装備は残りますが、現地改造は周回ごとに失われます。装甲屍は側面から、酸液と針撃ちは予告を避けてから。徘徊屍に囲まれないように。");
            }
            if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape)){screen=Screen.Menu;hubDialogue="";}
            UpdateCamera(false);
        }
        void DrawHubWorldLabels()
        {
            DrawWorldLabel(merchantPosition+Vector3.up*2,T("MARA / MERCHANT [E]","マラ / 装備商人 [E]"),Gold);
            DrawWorldLabel(archivistPosition+Vector3.up*2,T("IONE / ARCHIVIST [E]","イオネ / 記録官 [E]"),Cyan);
            DrawWorldLabel(debtCollectorPosition+Vector3.up*2,T("BRIGGS / DEBT COLLECTOR [E]","ブリッグス / 借金取り [E]"),new Color(1,.35f,.24f));
            DrawWorldLabel(portalPosition+Vector3.up*2,T("EXPEDITION GATE [E]","出撃ゲート [E]"),Gold);
            if(!string.IsNullOrEmpty(hubDialogue)){Panel(new Rect(250,595,940,112),Cyan);Text(270,613,hubDialogue,new GUIStyle(small){wordWrap=true},Mist,900);if(Button(new Rect(1050,658,116,35),T("CLOSE","閉じる")))hubDialogue="";}
        }
        void DrawWorldLabel(Vector3 position,string value,Color color)
        {
            var p=cam.WorldToScreenPoint(position);if(p.z<=0)return;float scale=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);
            float x=(p.x-(UnityEngine.Screen.width-1440*scale)/2)/scale,y=(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*scale)/2)/scale;
            Text(x-100,y,value,tiny,color,240);
        }
        public void RunHit(Foe struck,float damage,Vector3 position)
        {
            if(runChain<=0)return;Foe next=NearestEnemy(position,struck);if(next==null||Vector3.Distance(next.transform.position,position)>6)return;
            next.Hit(damage*runChain, next.transform.position+Vector3.up*.8f,Color.cyan);CombatVisuals.Burst(position,Color.cyan,4,world);
        }
        public void RunDashPulse(){if(runDash<=0)return;foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&Vector3.Distance(PlayerPosition,foe.transform.position)<3)foe.Hit(EffectiveDamage*.5f);}
        void DrawRunBonuses()
        {
            Panel(new Rect(24,155,338,152),Cyan*.6f);Text(42,165,T("FIELD UPGRADES / RESET ON RETURN","現地改造 / 帰還でリセット"),tiny,Cyan,310);
            Text(42,191,T("Damage +","威力 +")+(runDamage*100).ToString("0")+T("%   Fire +","%   連射 +")+(runFire*100).ToString("0")+"%",small,Mist,310);
            Text(42,218,T("Health +","HP +")+runVitality+T("   Dash +","   回避 +")+(runDash*100).ToString("0")+"%",small,Mist,310);
            Text(42,245,T("Chain ","連鎖 ")+(runChain*100).ToString("0")+T("% / Kill heal ","% / 撃破回復 ")+runHarvest,tiny,Mist,310);
            Text(42,272,T("Module barrier ","モジュール障壁 ")+ArtifactShield.ToString("0")+T(" / Conditional ×"," / 条件火力 ×")+ArtifactDamageMultiplier.ToString("0.00"),tiny,Cyan,310);
        }
    }
}
