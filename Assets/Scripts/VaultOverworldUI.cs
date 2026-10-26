using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        Texture2D overworldAtlasTexture;
        SectorLayout overworldAtlasSource;
        Texture2D worldLocalTexture;
        Vector2 worldLocalOrigin=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
        float worldLocalRefresh;
        readonly Color[] worldLocalPixels=new Color[50*38];
        int overworldSelectedCamp=-1;
        public static string OverworldCampName(int index,bool japanese)
        {
            string[] en={"Boss Hideout","Iron Rest","Canal Refuge","Green Shelter","Ash Post","North Watch","Wayfarer Camp"};
            string[] ja={"ボスのアジト","鉄の休息所","運河の避難所","緑陰のシェルター","灰の前哨地","北の監視所","旅人の野営地"};
            return (japanese?ja:en)[Mathf.Clamp(index,0,6)];
        }
        GUIStyle WorldCopy(int size=22)=>new GUIStyle(label){fontSize=size,wordWrap=true,clipping=TextClipping.Clip};
        void WorldLabel(Rect bounds,string value,Color color,int size=22)
        {var style=WorldCopy(size);style.normal.textColor=color;GUI.Label(bounds,value,style);}
        // Filled scanlines stay legible under the mobile canvas's nonuniform scale.
        void WorldMarker(float x,float y,float radius,Color color)
        {for(int row=-Mathf.CeilToInt(radius);row<=Mathf.CeilToInt(radius);row++){float half=Mathf.Max(1,radius-Mathf.Abs(row));Box(new Rect(x-half,y+row,half*2,1.2f),color);}}
        bool WorldButton(Rect bounds,string value,bool enabled=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;Panel(bounds,enabled?Cyan:Mist);if(enabled&&bounds.Contains(Event.current.mousePosition))Box(new Rect(bounds.x+4,bounds.y+3,bounds.width-8,bounds.height-6),new Color(.17f,.24f,.28f,.7f));
            FieldUIArt.Plate(bounds,true);var style=new GUIStyle(centered){fontSize=MobileTouchControls.Enabled?24:20,wordWrap=true};
            style.normal.textColor=enabled?UiPaper:Mist;bool result=GUI.Button(bounds,value,style);GUI.enabled=previous;return result;
        }
        void EnsureOverworldAtlas()
        {
            var geo=WorldGeography;if(geo==null||geo.Layout==null)return;
            var layout=geo.Layout;if(overworldAtlasTexture!=null&&overworldAtlasSource==layout)return;
            if(overworldAtlasTexture!=null)Destroy(overworldAtlasTexture);
            overworldAtlasSource=layout;const int resolution=1024;
            overworldAtlasTexture=new Texture2D(resolution,resolution,TextureFormat.RGBA32,false){name="Ashfall surveyed city atlas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[resolution*resolution];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++){
                var p=new Vector3((x+.5f-resolution*.5f)*10000/resolution,0,(y+.5f-resolution*.5f)*10000/resolution);
                bool river=geo.River(p)&&!geo.Bridge(p.z);float road=geo.RoadClearance(p);bool park=geo.Park(p);
                int sx=Mathf.RoundToInt(p.x/100),sz=Mathf.RoundToInt(p.z/100);bool arterial=(sx%3==0&&Mathf.Abs(p.x-geo.StreetX(sx,p.z))<9)||(sz%3==0&&Mathf.Abs(p.z-geo.StreetZ(sz,p.x))<9);
                Color color=river?new Color(.07f,.26f,.33f):arterial?new Color(.41f,.43f,.41f):road<0?new Color(.14f,.19f,.21f):road<2?new Color(.14f,.19f,.21f):park?new Color(.10f,.21f,.18f):new Color(.12f,.16f,.19f);
                if(!river&&road>3&&!park){float parcelX=Mathf.Repeat(p.x,12.5f),parcelZ=Mathf.Repeat(p.z,12.5f);if(parcelX>1&&parcelX<11.5f&&parcelZ>1&&parcelZ<11.5f)color=Color.Lerp(color,new Color(.16f,.20f,.22f),.15f);}
                float noise=Mathf.PerlinNoise(p.x*.003f+100,p.z*.003f+100);color*=.88f+.18f*noise;
                if(x%102==0||y%102==0)color=Color.Lerp(color,new Color(.4f,.52f,.56f),.16f);
                colors[y*resolution+x]=color;
            }
            overworldAtlasTexture.SetPixels(colors);overworldAtlasTexture.Apply(false,true);
        }
        Vector2 WorldAtlasPoint(Rect map,Vector3 position)
        {
            var l=WorldGeography.Layout;
            return new Vector2(map.x+(position.x/(l.Columns*l.CellSize)+.5f)*map.width,map.y+(.5f-position.z/(l.Rows*l.CellSize))*map.height);
        }
        public void DrawOverworldHUD()
        {
            bool mobile=MobileTouchControls.Enabled;DrawCriticalHealthVignette();
            DrawHealthPanel(new Rect(20,20,400,125),mobile);
            Panel(new Rect(440,20,590,125),Cyan);
            WorldLabel(new Rect(460,30,550,40),WorldGeography==null?T("WASTELAND","荒野"):WorldGeography.RegionName(PlayerPosition,VaultLanguage.Japanese),Gold,26);
            WorldLabel(new Rect(460,78,550,52),T("LEVEL ","レベル ")+DebtLevel+"   /   "+Credits.ToString("N0")+T(" SCRAP"," スクラップ"),UiPaper);
            if(WorldButton(mobile?new Rect(440,255,180,64):new Rect(1050,20,175,74),T("MAP","全体地図")))OpenOverworldMap();
            if(!mobile&&WorldButton(new Rect(1240,20,180,74),T("EQUIPMENT","装備")))ToggleInventory();
            DrawWorldLocalMap(new Rect(1165,165,250,180));
            if(gun!=null)
            {
                Rect r=mobile?new Rect(440,150,590,94):new Rect(1000,760,415,115);Panel(r,gun.color);
                WorldLabel(new Rect(r.x+16,r.y+8,r.width-32,40),VaultLanguage.GunName(gun),gun.color,mobile?23:20);
                WorldLabel(new Rect(r.x+16,r.y+48,r.width-32,48),reload>0?T("RELOADING…","装填中…"):ammo+" / "+EffectiveMagazine+T("   RESERVE ","   予備 ")+AmmoReserve(AmmoForGun(gun)),UiPaper,26);
            }
            DrawWorldLootPreview();
            DrawWorldCombatIndicators();
            int entrance=NearbyWorldEntrance,camp=NearbyWorldCamp;
            if(entrance>=0)
            {
                var stage=ExpeditionMap.Get(entrance);bool unlocked=stage.level<=DebtLevel;
                if(WorldButton(mobile?new Rect(440,325,570,60):new Rect(940,625,475,86),stage.Name+" / Lv."+stage.level+"\n"+(unlocked?T("ENTER / E","入場 / E"):T("LEVEL TOO LOW","レベル不足")),unlocked))EnterNearbyWorldDungeon(entrance);
            }
            else if(camp>=0)
            {
                if(WorldButton(mobile?new Rect(440,325,570,60):new Rect(940,625,475,86),OverworldCampName(camp,VaultLanguage.Japanese)+"\n"+T("ENTER CAMP / E","拠点へ入る / E")))EnterNearbyWorldCamp(camp);
            }
            else if(NearbyAmmoCrate||NearbySupplyChest)WorldLabel(new Rect(940,625,475,74),T("USE / F: OPEN CACHE","調べる / F：物資箱を開ける"),Gold,26);
            if(toastTimer>0){Rect notice=mobile?new Rect(25,150,390,100):new Rect(435,160,570,72);Panel(notice,Gold);WorldLabel(new Rect(notice.x+12,notice.y+10,notice.width-24,notice.height-15),Toast,Gold,mobile?20:21);}
        }
        void DrawWorldCombatIndicators()
        {
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&Layout.IsVisible(foe.transform.position))
            {
                var p=cam.WorldToScreenPoint(foe.transform.position+Vector3.up*2.6f);if(p.z<=0)continue;
                Vector2 at=MobileTouchControls.Enabled?MobileCanvasPoint(p):new Vector2((p.x-(UnityEngine.Screen.width-1440*Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f))*.5f)/Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f),(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f))*.5f)/Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f));
                WorldLabel(new Rect(at.x-35,at.y-27,100,26),"Lv."+GetOverworldEnemyLevel(foe),UiPaper,17);
                Box(new Rect(at.x-28,at.y,56,5),Ink);Box(new Rect(at.x-28,at.y,56*Mathf.Clamp01(foe.hp/foe.maxHp),5),foe.HealthBarColor);
            }
            if(!Active)return;
            Vector3 target=PlayerPosition+aim*5+Vector3.up*.9f;var point=cam.WorldToScreenPoint(target);
            if(MobileTouchControls.Enabled){Vector2 at=MobileCanvasPoint(point);DrawCrosshair(at.x,at.y);}
            else {float scale=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);DrawCrosshair((Input.mousePosition.x-(UnityEngine.Screen.width-1440*scale)*.5f)/scale,(UnityEngine.Screen.height-Input.mousePosition.y-(UnityEngine.Screen.height-900*scale)*.5f)/scale);}
        }
        void DrawWorldLootPreview()
        {
            var drop=NearDrop();var armor=NearArmor();var module=NearArtifact();var melee=NearMeleeDrop();
            float best=float.MaxValue;int kind=-1;
            void Candidate(int index,Vector3 position){float d=(position-PlayerPosition).sqrMagnitude;if(d<best){best=d;kind=index;}}
            if(drop!=null)Candidate(0,drop.position);if(armor!=null)Candidate(1,armor.position);if(module!=null)Candidate(2,module.position);if(melee!=null)Candidate(3,melee.position);
            if(kind<0)return;
            Rarity rarity=kind==0?drop.gun.rarity:kind==1?armor.item.rarity:kind==2?module.item.rarity:melee.item.rarity;
            int level=kind==0?drop.gun.level:kind==1?armor.item.level:kind==2?module.item.level:melee.item.level;
            int score=kind==0?GearScoring.Score(drop.gun):kind==1?GearScoring.Score(armor.item):kind==2?GearScoring.Score(module.item):MeleeWeaponCatalog.Score(melee.item);
            var color=Arsenal.Colors[Mathf.Clamp((int)rarity,0,Arsenal.Colors.Length-1)];
            bool mobile=MobileTouchControls.Enabled;Rect card=mobile?new Rect(25,265,390,155):new Rect(435,570,570,155);Panel(card,color);
            Rect picture=new Rect(card.x+12,card.y+15,100,118);
            if(kind==0)ItemPortraits.Draw(picture,drop.gun);else if(kind==1)ItemPortraits.Draw(picture,armor.item);else if(kind==2)ManufacturerBranding.ModulePortrait(picture,module.item);else DrawMeleeIcon(picture,melee.item.family);
            WorldLabel(new Rect(card.x+130,card.y+12,card.width-140,40),VaultLanguage.RarityName(rarity),color,mobile?22:27);
            WorldLabel(new Rect(card.x+130,card.y+54,card.width-140,42),T("LEVEL ","レベル ")+level+T("   SCORE ","   評価 ")+score,UiPaper,mobile?21:25);
            WorldLabel(new Rect(card.x+130,card.y+100,card.width-140,46),T("TAKE / F","拾得 / F"),Gold,22);
        }
        void DrawWorldLocalMap(Rect map)
        {
            EnsureOverworldAtlas();if(overworldAtlasTexture==null)return;
            Panel(map,Cyan);Rect inside=new Rect(map.x+10,map.y+30,map.width-20,map.height-40);
            var l=WorldGeography.Layout;float u=PlayerPosition.x/(l.Columns*l.CellSize)+.5f,v=PlayerPosition.z/(l.Rows*l.CellSize)+.5f;
            float uw=Mathf.Min(1,200f/(l.CellSize*l.Columns)),vh=Mathf.Min(1,150f/(l.CellSize*l.Rows));float left=Mathf.Clamp(u-uw*.5f,0,1-uw),bottom=Mathf.Clamp(v-vh*.5f,0,1-vh);
            Vector2 localOrigin=new Vector2(left*l.Columns*l.CellSize-l.Columns*l.CellSize*.5f,bottom*l.Rows*l.CellSize-l.Rows*l.CellSize*.5f);
            UpdateWorldLocalTexture(localOrigin);GUI.DrawTexture(inside,worldLocalTexture);
            WorldLabel(new Rect(map.x+10,map.y+3,map.width-20,27),T("LOCAL / 200 m / NORTH ↑","周辺 / 200m / 北 ↑"),Mist,17);
            Vector2 Local(Vector3 p)=>new Vector2(inside.x+(p.x/(l.Columns*l.CellSize)+.5f-left)/uw*inside.width,inside.yMax-(p.z/(l.Rows*l.CellSize)+.5f-bottom)/vh*inside.height);
            foreach(var enemy in Enemies)if(enemy!=null&&!enemy.dead){var p=Local(enemy.transform.position);if(inside.Contains(p))Box(new Rect(p.x-2,p.y-2,4,4),UiDanger);}
            for(int i=0;i<WorldGeography.Entrances.Length;i++){var p=Local(WorldGeography.Entrances[i]);if(inside.Contains(p))WorldMarker(p.x,p.y,3,Gold);}
            void LootDot(Vector3 position)
            {
                var p=Local(position);if(!inside.Contains(p))return;
                if(legendaryMapIcon==null)legendaryMapIcon=CreateLegendaryMapIcon();
                GUI.DrawTexture(new Rect(p.x-7,p.y-7,14,14),legendaryMapIcon);
            }
            foreach(var d in drops)if(d.gun.rarity==Rarity.Legendary)LootDot(d.position);
            foreach(var d in armorDrops)if(d.item.rarity==Rarity.Legendary)LootDot(d.position);
            foreach(var d in artifactDrops)if(d.item.rarity==Rarity.Legendary)LootDot(d.position);
            foreach(var d in meleeDrops)if(d.item.rarity==Rarity.Legendary)LootDot(d.position);
            var player=Local(PlayerPosition);WorldMarker(player.x,player.y,5,Cyan);
        }
        void UpdateWorldLocalTexture(Vector2 origin)
        {
            if(worldLocalTexture==null)worldLocalTexture=new Texture2D(50,38,TextureFormat.RGBA32,false){name="200m local geography",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(Event.current.type!=EventType.Repaint)return;if((origin-worldLocalOrigin).sqrMagnitude<16)return;if(Time.unscaledTime<worldLocalRefresh)return;
            worldLocalRefresh=Time.unscaledTime+.75f;worldLocalOrigin=origin;var geo=WorldGeography;
            for(int z=0;z<38;z++)for(int x=0;x<50;x++)
            {
                var p=new Vector3(origin.x+x*4+2,0,origin.y+z*4+2);
                worldLocalPixels[z*50+x]=geo.River(p)&&!geo.Bridge(p.z)?new Color(.12f,.32f,.42f):LocalMapBuilding(geo,p)?new Color(.09f,.13f,.16f):geo.RoadDistance(p)<7?new Color(.51f,.46f,.35f):geo.Park(p)?new Color(.21f,.34f,.25f):new Color(.32f,.33f,.29f);
            }
            worldLocalTexture.SetPixels(worldLocalPixels);worldLocalTexture.Apply(false,false);
        }
        bool LocalMapBuilding(OverworldGeography geo,Vector3 p){var chunk=geo.GetChunk(Mathf.FloorToInt(p.x/100),Mathf.FloorToInt(p.z/100));foreach(var b in chunk.Buildings)if(b.Contains(new Vector2(p.x,p.z)))return true;return false;}
        public void DrawOverworldAtlas()
        {
            EnsureOverworldAtlas();Box(new Rect(0,0,1440,900),new Color(.025f,.045f,.06f,.98f));
            WorldLabel(new Rect(30,20,1090,55),T("THE WASTELAND / FIELD ATLAS","荒野 / 広域地図"),Gold,36);
            if(WorldButton(new Rect(1195,16,215,66),T("BACK","戻る")))CloseOverworldMap();
            if(overworldAtlasTexture==null)return;
            Rect map=new Rect(30,100,900,650);GUI.DrawTexture(map,overworldAtlasTexture);
            var paper=Resources.Load<Texture2D>("Art/UI/FieldKit/map-paper-v1");if(paper){var old=GUI.color;GUI.color=new Color(1,1,1,.10f);GUI.DrawTexture(map,paper);GUI.color=old;}
            WorldLabel(new Rect(50,110,650,35),T("ASHFALL  /  SURVEYED CITY • 10 × 10 km","アッシュフォール / 都市調査図 • 10 × 10 km"),UiPaper,20);
            WorldLabel(new Rect(50,158,300,30),T("WESTERN WOODLAND","西部森林帯"),new Color(.65f,.8f,.68f),16);
            WorldLabel(new Rect(550,420,300,30),T("OLD CIVIC QUARTER","旧市街・商業区"),Mist,16);
            WorldLabel(new Rect(400,675,300,30),T("SOUTHWORKS INDUSTRIAL","南部工業地帯"),Mist,16);
            DrawWorldMapScale(map);
            for(int i=0;i<WorldGeography.Entrances.Length;i++)
            {
                Vector2 p=WorldAtlasPoint(map,WorldGeography.Entrances[i]);var stage=ExpeditionMap.Get(i);bool selected=overworldSelectedCamp<0&&expeditionMapSelection==i;
                Color color=clearedExpeditionStages.Contains(i)?Cyan:stage.level<=DebtLevel?Gold:Mist;
                if(selected)Box(new Rect(p.x-12,p.y-12,24,24),new Color(.02f,.04f,.06f,.9f));FieldUIArt.Icon(new Rect(p.x-(selected?16:11),p.y-(selected?16:11),selected?32:22,selected?32:22),7,color);
                if(GUI.Button(new Rect(p.x-13,p.y-13,26,26),GUIContent.none,GUIStyle.none)){expeditionMapSelection=i;overworldSelectedCamp=-1;}
            }
            for(int i=0;i<WorldGeography.Camps.Length;i++)
            {
                Vector2 p=WorldAtlasPoint(map,WorldGeography.Camps[i]);FieldUIArt.Icon(new Rect(p.x-18,p.y-18,36,36),6,IsCampVisited(i)?Color.white:Color.gray);
                if(GUI.Button(new Rect(p.x-16,p.y-16,32,32),(i+1).ToString(),centered))overworldSelectedCamp=i;
            }
            if(IsOverworld){var p=WorldAtlasPoint(map,PlayerPosition);WorldMarker(p.x,p.y,12,Color.white);Box(new Rect(p.x-2,p.y-2,4,4),Cyan);}
            WorldLabel(new Rect(35,756,890,42),T("◇ Dungeon   ■ Camp (cyan: visited)   White: you   ↑ North","◇ ダンジョン　■ 拠点（水色：訪問済み）　白：現在地　↑ 北"),Mist,19);
            Panel(new Rect(950,100,460,650),Gold);
            if(overworldSelectedCamp>=0)
            {
                int camp=overworldSelectedCamp;bool visited=IsCampVisited(camp);bool travel=visited&&CanWorldFastTravel;
                WorldLabel(new Rect(975,125,410,100),OverworldCampName(camp,VaultLanguage.Japanese),Gold,32);
                WorldLabel(new Rect(975,250,410,210),visited?T("Discovered refuge. Travel here to rest and trade. Fast travel is unavailable inside dungeons.","発見済みの避難所。休息と取引のために移動できます。ダンジョン内からは移動できません。"):T("Undiscovered. Reach this camp on foot to unlock fast travel.","未訪問。歩いて到達するとファストトラベルが解放されます。"),Mist,26);
                if(WorldButton(new Rect(975,620,410,100),T("FAST TRAVEL","ファストトラベル"),travel))FastTravelToCamp(camp);
            }
            else DrawWorldStageBriefing();
            for(int i=0;i<7;i++)if(WorldButton(new Rect(30+i*198,812,188,70),(i+1)+" / "+OverworldCampName(i,VaultLanguage.Japanese),IsCampVisited(i)))overworldSelectedCamp=i;
        }

        string WorldDistanceLabel(Vector3 destination)
        {
            Vector3 origin=IsOverworld?PlayerPosition:WorldGeography.Camps[LastVisitedCamp];
            float distance=Vector2.Distance(new Vector2(origin.x,origin.z),new Vector2(destination.x,destination.z));
            return distance>=1000?(distance/1000f).ToString("0.00")+" km":Mathf.RoundToInt(distance)+" m";
        }
        void DrawWorldMapScale(Rect map)
        {
            float width=map.width*1000f/(WorldGeography.Layout.Columns*WorldGeography.Layout.CellSize);
            Rect backdrop=new Rect(map.x+12,map.yMax-48,width+140,38);Box(backdrop,new Color(.02f,.035f,.04f,.94f));
            float x=backdrop.x+8,y=backdrop.y+22;Box(new Rect(x,y,width,3),UiPaper);Box(new Rect(x,y-5,2,10),UiPaper);Box(new Rect(x+width-2,y-5,2,10),UiPaper);
            WorldLabel(new Rect(x+width+12,backdrop.y+5,115,30),"1 km / 10 km",UiPaper,17);
        }
        void DrawWorldStageBriefing()
        {
            var stage=ExpeditionMap.Get(Mathf.Clamp(expeditionMapSelection,0,99));bool eligible=stage.level<=DebtLevel;
            WorldLabel(new Rect(975,120,410,95),(stage.id+1).ToString("00")+" / "+stage.Name,Gold,29);
            WorldLabel(new Rect(975,220,410,75),stage.Location+" / "+stage.Biome,Mist,23);
            WorldLabel(new Rect(975,305,410,70),T("REQUIRED LEVEL ","必要レベル ")+stage.level+T(" / YOURS "," / 現在 ")+DebtLevel,eligible?Cyan:UiDanger,25);
            WorldLabel(new Rect(975,375,410,105),ExpeditionMap.MissionLabel(stage.mission,VaultLanguage.Japanese),UiPaper,26);
            WorldLabel(new Rect(975,490,410,80),ExpeditionRewardDescription(stage.id),Gold,22);
            WorldLabel(new Rect(975,575,410,40),T("DISTANCE ","入口まで ")+WorldDistanceLabel(WorldGeography.Entrances[stage.id]),Mist,21);
            bool nearby=IsOverworld&&NearbyWorldEntrance==stage.id&&eligible;
            if(WorldButton(new Rect(975,620,410,100),nearby?T("ENTER DUNGEON","ダンジョンに入る"):eligible?T("WALK TO THIS ENTRANCE","この入口まで歩いて移動"):T("LEVEL TOO LOW","レベル不足"),nearby))EnterNearbyWorldDungeon(stage.id);
        }
        public void DrawWorldBossConversation()
        {
            Panel(new Rect(220,240,1000,610),Gold);
            WorldLabel(new Rect(260,268,920,60),T("THE BOSS / YOUR CONTRACT","ボス / お前の契約"),Gold,38);
            WorldLabel(new Rect(260,350,920,340),T("You owe me 1,000,000,000 scrap. That bomb collar stays until you repay it all.\n\nWalk out into Ashfall. Reach dungeon entrances on foot, complete missions, and bring scrap back to Briggs.\n\nEach camp you visit unlocks fast travel. Use it outdoors or between camps. It never works inside a dungeon.","お前の借金は10億スクラップだ。全額返すまで、その爆弾首輪は外れない。\n\n拠点を出て荒野を歩け。ダンジョンの入口は各地にある。歩いて入口に到達してから入るんだ。装備とスクラップを回収し、ここで返済しろ。\n\n訪れた拠点ごとにファストトラベルが解放される。訪問済み拠点へは荒野か拠点から移動できる。ダンジョン内からは使えない。"),UiPaper,26);
            if(WorldButton(new Rect(340,735,760,86),T("UNDERSTOOD / START EXPLORING","わかった / 探索を始める")))CompleteWorldBossConversation();
        }
    }
}




