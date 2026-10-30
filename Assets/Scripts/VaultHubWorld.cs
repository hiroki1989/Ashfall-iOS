using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public float runDamage,runFire,runVitality,runDash,runChain,runHarvest;
        readonly Vector3 merchantPosition=new Vector3(-8,.65f,6),archivistPosition=new Vector3(8,.65f,5),portalPosition=new Vector3(0,.65f,12);
        public bool HasNearbyInteraction
        {
            get
            {
                if(player==null||worldTransitionPending)return false;
                if(screen==Screen.Hub)return Vector3.Distance(PlayerPosition,debtCollectorPosition)<3||Vector3.Distance(PlayerPosition,merchantPosition)<3||Vector3.Distance(PlayerPosition,portalPosition)<3||Vector3.Distance(PlayerPosition,archivistPosition)<3;
                if(!Active)return false;
                if(IsOverworld)return NearbyWorldCamp>=0||NearbyWorldEntrance>=0||NearbyOverworldActivity>=0;
                return Layout!=null&&ExpeditionExitOpen&&Vector3.Distance(PlayerPosition,ExitPosition)<3;
            }
        }
        string hubDialogue="";
        bool collectorIntroSeen,collectorOpen;int collectorClaims,collectorPage,collectorKnown;
        void OpenCollector()
        {
            if(!smoke)EquipmentDiscovery.RecordCurrent(Weapons,ArmorInventory,ArtifactInventory,MeleeWeaponInventory);
            collectorKnown=EquipmentDiscovery.CatalogCount();collectorPage=collectorIntroSeen?3:0;collectorOpen=true;
        }
        void DrawCollectorDialog()
        {
            if(!collectorOpen)return;
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.76f));Panel(new Rect(190,190,1060,520),Gold);
            Text(235,220,T("IONE / COLLECTOR","イオネ / コレクター"),heading,Gold,970);
            string[] en={"Hey, you. I collect gear. Anything with a story, a strange mechanism, or a good scar belongs in my catalog.","Find something new, and it'll be recorded. Come back and report when you've filled in more of the catalog. I want to hear about it.","For every 50 different items you discover, I'll give you one skill point. Don't bring me fifty copies of the same junk.","You've discovered "+collectorKnown+" kinds. Report every 50 and I'll pay you a skill point. So? Got something worth showing me?"};
            string[] ja={"おい、そこのあんた。私は装備を集めてる。妙な仕組みの銃も、傷だらけの防具も、気に入ったものは全部図鑑に載せるんだ。","初めて手に入れた装備は図鑑に記録される。図鑑が埋まったら、私に報告しに来な。どんな物か聞かせてもらうよ。","違う装備を50種類集めるたびに、スキルポイントを1つやる。同じガラクタを50個持って来ても駄目だからね。","今の図鑑は "+collectorKnown+"種類。50種類ごとに報告してくれりゃ、スキルポイントをやるよ。さあ、何か面白い物は見つかったかい？"};
            Text(235,305,(VaultLanguage.Japanese?ja:en)[Mathf.Clamp(collectorPage,0,3)],new GUIStyle(label){fontSize=29,wordWrap=true},UiPaper,970);
            if(collectorPage<3)
            {
                if(Button(new Rect(900,580,290,75),T("NEXT","次へ"))){collectorPage++;if(collectorPage==3){collectorIntroSeen=true;SaveProgression();}}
            }
            else
            {
                int available=Mathf.Max(0,collectorKnown/50-collectorClaims);
                Text(235,475,T("UNCLAIMED POINTS: ","受け取れるポイント：")+available,new GUIStyle(label){fontSize=25},Cyan,700);
                if(available>0&&Button(new Rect(235,580,360,75),T("REPORT / CLAIM","報告して受け取る"))){collectorClaims+=available;ExpeditionTokens+=available;SaveProgression();PlayUserSound("skill",.35f);Notify("COLLECTION / SKILL POINTS +"+available,"収集報告 / スキルポイント +"+available);}
                if(Button(new Rect(900,580,290,75),T("CLOSE","閉じる")))collectorOpen=false;
            }
        }
        Transform departingRaiderBoss; bool bossDeparting;
        public void BuildHub() {if(QueueWorldTransition(()=>BuildHubImmediate()))return;BuildHubImmediate();}
        void BuildHubImmediate()
        {
            IsOverworld=false;EnsureOverworldState();departingRaiderBoss=null;bossDeparting=false;
            if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            foreach(var material in materials)if(material!=null)Destroy(material);materials.Clear();Enemies.Clear();drops.Clear();meleeDrops.Clear();armorDrops.Clear();chests.Clear();supplyDrops.Clear();fieldAmmoCrates.Clear();ClearArtifactDrops();satellites.Clear();
            world=new GameObject("LAST LIGHT / survivor safehouse").transform;
            Layout=new SectorLayout(new System.Random(17017));System.Array.Clear(Layout.open,0,Layout.open.Length);
            for(int x=0;x<Layout.Columns;x++)for(int y=0;y<Layout.Rows;y++){var p=Layout.Center(new Vector2Int(x,y));if(new Vector2(p.x,p.z).magnitude<13.5f)Layout.open[x,y]=true;}
            Layout.walls.Clear();Layout.covers.Clear();halfWidth=17;halfDepth=17;
            art=new VaultArt(world,materials,new System.Random(17017));art.Floor(16,15,0);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;art.Column(new Vector3(Mathf.Sin(a)*12.5f,0,Mathf.Cos(a)*12.5f),2.5f);}
            art.Cover(merchantPosition+Vector3.back*2);art.Brazier(merchantPosition+Vector3.left*2);art.Cover(archivistPosition+Vector3.back*2);
            Transform HubNpc(Vector3 p,int variant,string name){var root=new GameObject(name).transform;root.SetParent(world,false);root.position=new Vector3(p.x,0,p.z);root.rotation=Quaternion.LookRotation(Vector3.back);var visual=HumanFactionVisual.Create(root,0,variant);visual.enabled=false;return root;}
            HubNpc(merchantPosition,0,"Mara / Salvage Merchant");
            HubNpc(archivistPosition,1,"Ione / Archivist");
            HubNpc(debtCollectorPosition,0,"Briggs / Debt Collector");
            art.Part("Merchant counter",PrimitiveType.Cube,merchantPosition+new Vector3(0,.25f,-1.4f),new Vector3(3,1.25f,.9f),new Color(.27f,.23f,.18f),world);
            art.Part("Merchant shelf",PrimitiveType.Cube,merchantPosition+new Vector3(0,1.2f,1.2f),new Vector3(3.4f,.18f,.7f),new Color(.25f,.23f,.19f),world);
            for(int n=0;n<4;n++)art.Part("Merchant stock",PrimitiveType.Cube,merchantPosition+new Vector3(-1.1f+n*.72f,1.45f,1.15f),new Vector3(.48f,.45f,.48f),new Color(.38f,.35f,.27f),world);
            art.Part("Debt office counter",PrimitiveType.Cube,debtCollectorPosition+new Vector3(0,.25f,-1.4f),new Vector3(2.8f,1.25f,.8f),new Color(.22f,.23f,.22f),world);
            art.Part("Archivist counter",PrimitiveType.Cube,archivistPosition+new Vector3(0,.25f,-1.4f),new Vector3(2.8f,1.25f,.8f),new Color(.26f,.25f,.21f),world);
            if(LastVisitedCamp==0&&!WorldStorySeen){var boss=ImportedEnemyVisual.TryCreateBossNPC(world,new Vector3(-3,0,7))??art.Character(new Vector3(-3,.65f,7),true,8);boss.rotation=Quaternion.Euler(0,180,0);boss.name="Malcolm Vale / explosive collar contract";departingRaiderBoss=boss;}
            ImportedWorldAssets.DressInterior(world,Layout,true);
            art.Part("Exit wall left",PrimitiveType.Cube,new Vector3(-7,2,12),new Vector3(10,4,.45f),new Color(.25f,.25f,.23f),world);
            art.Part("Exit wall right",PrimitiveType.Cube,new Vector3(7,2,12),new Vector3(10,4,.45f),new Color(.25f,.25f,.23f),world);
            art.Part("Exit doorway lintel",PrimitiveType.Cube,new Vector3(0,3.6f,12),new Vector3(4,.8f,.45f),new Color(.25f,.25f,.23f),world);
            // Restore the clear interaction marker at the doorway, without
            // changing the hub's established interior layout.
            art.Ring("Hub exit / yellow floor marker",new Vector3(0,.035f,10.9f),1.45f,.11f,VaultArt.Gold,true);
            player=art.Character(new Vector3(0,.65f,-3),true);barrel=art.Weapon(player,gun.color);VaultArt.ConfigureWeapon(barrel,gun.effect,gun.color,VaultLanguage.DisplayModel(gun));PlayerAppearance.ApplyArmor(EquippedArmor);
            foreach(var animation in world.GetComponentsInChildren<Animation>(true)){animation.Stop();animation.enabled=false;}foreach(var animator in world.GetComponentsInChildren<Animator>(true))if(!animator.GetComponentInParent<ImportedEnemyVisual>()){animator.Rebind();animator.Update(0);animator.enabled=false;}
            hp=maxHp;aim=Vector3.right;PlayerWeapon.Aim(aim);hubDialogue="";cam.orthographicSize=11/CameraZoom;UpdateCamera(true);
        }
        void UpdateHub()
        {
            toastTimer=Mathf.Max(0,toastTimer-Time.deltaTime);if(collectorOpen)return;
            if(bossDeparting&&departingRaiderBoss){var target=new Vector3(portalPosition.x,departingRaiderBoss.position.y,portalPosition.z+1);var delta=target-departingRaiderBoss.position;delta.y=0;if(delta.sqrMagnitude<.04f){Destroy(departingRaiderBoss.gameObject);departingRaiderBoss=null;bossDeparting=false;}else{departingRaiderBoss.rotation=Quaternion.LookRotation(delta);departingRaiderBoss.position=Vector3.MoveTowards(departingRaiderBoss.position,target,2.5f*Time.deltaTime);}}
            var move=ReadPlayerMovement();var previousPosition=player.position;player.position=MoveWithinRoom(player.position,move*EffectiveMoveSpeed*Time.deltaTime,.45f);TickFootsteps(previousPosition,player.position);
            if(!VaultMobileInputBridge.Enabled&&move.sqrMagnitude>.01f){aim=move.normalized;player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);}
            if(VaultMobileInputBridge.Enabled)ApplyMobileAim(VaultMobileInputBridge.Aim);
            if(VaultMobileInputBridge.Pressed(MobileAction.Interact,KeyCode.E)){
                if(Vector3.Distance(PlayerPosition,debtCollectorPosition)<3){debtPage=true;debtSkillTreePage=false;screen=Screen.Shop;}
                else if(Vector3.Distance(PlayerPosition,merchantPosition)<3){debtPage=false;debtSkillTreePage=false;shopSellingPage=false;pendingSaleKey=null;screen=Screen.Shop;}
                else if(Vector3.Distance(PlayerPosition,portalPosition)<3){ExitCampToOverworld();return;}
                else if(Vector3.Distance(PlayerPosition,archivistPosition)<3)OpenCollector();
            }
            if(Input.GetKeyDown(KeyCode.M)){OpenOverworldMap();return;}
            if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape)){RememberOverworldPosition(WorldGeography.Camps[LastVisitedCamp],false);screen=Screen.Menu;hubDialogue="";}
            UpdateCamera(false);
        }
        void DrawHubWorldLabels()
        {
            if(toastTimer>0){Panel(new Rect(270,130,880,90),Gold);Text(290,145,Toast,new GUIStyle(small){wordWrap=true},Gold,840);}
            string key=MobileTouchControls.Enabled?"":" [E]";
            DrawWorldLabel(merchantPosition+Vector3.up*2,T("MARA / MERCHANT","マラ / 装備商人")+key,Gold);
            DrawWorldLabel(archivistPosition+Vector3.up*2,T("IONE / COLLECTOR","イオネ / コレクター")+key,Cyan);
            DrawWorldLabel(debtCollectorPosition+Vector3.up*2,T("BRIGGS / DEBT COLLECTOR","ブリッグス / 借金取り")+key,new Color(1,.35f,.24f));
            DrawWorldLabel(portalPosition+Vector3.up*2,T("EXIT TO ASHFALL","アッシュフォールの街へ")+key,Gold);
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
            Text(42,272,T("Module barrier ","モジュールシールド ")+ArtifactShield.ToString("0")+T(" / Conditional ×"," / 条件火力 ×")+ArtifactDamageMultiplier.ToString("0.00"),tiny,Cyan,310);
        }
    }
}
