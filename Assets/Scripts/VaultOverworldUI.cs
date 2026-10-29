using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        Texture2D overworldAtlasTexture;
        bool aerialAtlas;
        readonly Texture2D[] aerialDetailTiles=new Texture2D[4];
        bool aerialTilesLoaded;
        void DrawAerialSurvey(Rect bounds,Rect uv)
        {
            GUI.DrawTextureWithTexCoords(bounds,overworldAtlasTexture,uv);
            if(!aerialAtlas)return;
            if(!aerialTilesLoaded)
            {
                aerialTilesLoaded=true;
                for(int i=0;i<4;i++)aerialDetailTiles[i]=Resources.Load<Texture2D>("Art/UI/FieldKit/aerial-city-detail-"+i);
            }
            for(int i=0;i<4;i++)
            {
                if(!aerialDetailTiles[i])continue;
                var tile=new Rect(i%2*.5f,i/2*.5f,.5f,.5f);
                float x0=Mathf.Max(uv.xMin,tile.xMin),x1=Mathf.Min(uv.xMax,tile.xMax);
                float y0=Mathf.Max(uv.yMin,tile.yMin),y1=Mathf.Min(uv.yMax,tile.yMax);
                if(x1<=x0||y1<=y0)continue;
                var target=new Rect(bounds.x+(x0-uv.x)*bounds.width/uv.width,
                    bounds.y+(uv.yMax-y1)*bounds.height/uv.height,
                    (x1-x0)*bounds.width/uv.width,(y1-y0)*bounds.height/uv.height);
                GUI.DrawTextureWithTexCoords(target,aerialDetailTiles[i],new Rect((x0-tile.x)*2,(y0-tile.y)*2,(x1-x0)*2,(y1-y0)*2));
            }
        }
        Texture2D worldBuildingAtlas;
        Color32[] worldBuildingPixels;
        readonly bool[] worldBuildingChunksReady=new bool[10000];
        readonly System.Collections.Generic.List<int> worldBuildingQueue=new System.Collections.Generic.List<int>(10000);
        int worldBuildingQueueIndex;
        RectInt worldBuildingVisibleChunks;
        bool worldBuildingViewReady,worldBuildingDirty;
        readonly bool[] worldBuildingDirtyTiles=new bool[32*32];
        float worldBuildingNextUpload;
        readonly Color32[] worldBuildingUploadPixels=new Color32[64*64];
        OverworldGeography buildingAtlasGeography;
        void DrawWorldBuildingAtlas(Rect bounds,Rect uv)
        {
            if(aerialAtlas)return;
            const int size=2048;
            if(buildingAtlasGeography==null)buildingAtlasGeography=new OverworldGeography();
            if(worldBuildingAtlas==null)
            {
                worldBuildingAtlas=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Surveyed building footprints",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                worldBuildingPixels=new Color32[size*size];worldBuildingAtlas.SetPixels32(worldBuildingPixels);worldBuildingAtlas.Apply(false,false);
            }
            if(Event.current.type==EventType.Repaint)
            {
                int x0=Mathf.Clamp(Mathf.FloorToInt(uv.xMin*100),0,99),x1=Mathf.Clamp(Mathf.CeilToInt(uv.xMax*100)-1,0,99);
                int z0=Mathf.Clamp(Mathf.FloorToInt(uv.yMin*100),0,99),z1=Mathf.Clamp(Mathf.CeilToInt(uv.yMax*100)-1,0,99);
                var visible=new RectInt(x0,z0,x1-x0+1,z1-z0+1);
                if(!worldBuildingViewReady||visible!=worldBuildingVisibleChunks)
                {
                    worldBuildingVisibleChunks=visible;worldBuildingViewReady=true;
                    worldBuildingQueue.Clear();worldBuildingQueueIndex=0;
                    int cx=(x0+x1)/2,cz=(z0+z1)/2;
                    int radius=Mathf.Max(Mathf.Max(cx-x0,x1-cx),Mathf.Max(cz-z0,z1-cz));
                    // Center-out rings make the current view readable first. Never survey outside it.
                    void QueueChunk(int x,int z)
                    {
                        if(x<x0||x>x1||z<z0||z>z1)return;
                        int index=z*100+x;if(!worldBuildingChunksReady[index])worldBuildingQueue.Add(index);
                    }
                    QueueChunk(cx,cz);
                    for(int r=1;r<=radius;r++)
                    {
                        for(int x=cx-r;x<=cx+r;x++){QueueChunk(x,cz-r);QueueChunk(x,cz+r);}
                        for(int z=cz-r+1;z<cz+r;z++){QueueChunk(cx-r,z);QueueChunk(cx+r,z);}
                    }
                }
                var timer=System.Diagnostics.Stopwatch.StartNew();int count=0;
                // Chunk work is atomic; cap both elapsed generation time and chunks per repaint.
                while(worldBuildingQueueIndex<worldBuildingQueue.Count&&count<16&&timer.Elapsed.TotalMilliseconds<2)
                {
                    int k=worldBuildingQueue[worldBuildingQueueIndex++];
                    if(worldBuildingChunksReady[k])continue;
                    int x=k%100-50,z=k/100-50;count++;
                    foreach(var building in buildingAtlasGeography.GetChunk(x,z).Buildings)
                    {
                        int left=Mathf.Clamp(Mathf.CeilToInt((building.xMin+5001)*size/10000),0,size-1),right=Mathf.Clamp(Mathf.FloorToInt((building.xMax+4999)*size/10000),0,size-1);
                        int bottom=Mathf.Clamp(Mathf.CeilToInt((building.yMin+5001)*size/10000),0,size-1),top=Mathf.Clamp(Mathf.FloorToInt((building.yMax+4999)*size/10000),0,size-1);
                                                if(right<left||top<bottom)continue;
                        int tone=54+Mathf.Abs((left*73+bottom*31)%19);
                        for(int py=bottom;py<=top;py++)for(int px=left;px<=right;px++)
                        {
                            bool rim=px==left||py==top;
                            int shade=rim?tone+16:tone;
                            worldBuildingPixels[py*size+px]=new Color32((byte)(shade+7),(byte)(shade+6),(byte)shade,220);
                        }
                        worldBuildingDirty=true;
                        for(int tileZ=bottom/64;tileZ<=top/64;tileZ++)for(int tileX=left/64;tileX<=right/64;tileX++)worldBuildingDirtyTiles[tileZ*32+tileX]=true;
                    }
                    worldBuildingChunksReady[k]=true;
                }
                if(worldBuildingDirty&&Time.unscaledTime>=worldBuildingNextUpload)
                {
                    // Reuse one small tile buffer: no full-atlas CPU copy or growing rectangle allocations.
                    for(int tile=0;tile<worldBuildingDirtyTiles.Length;tile++)
                    {
                        if(!worldBuildingDirtyTiles[tile])continue;
                        int left=(tile%32)*64,bottom=(tile/32)*64;
                        for(int row=0;row<64;row++)System.Array.Copy(worldBuildingPixels,(bottom+row)*size+left,worldBuildingUploadPixels,row*64,64);
                        worldBuildingAtlas.SetPixels32(left,bottom,64,64,worldBuildingUploadPixels);
                        worldBuildingDirtyTiles[tile]=false;
                    }
                    // Apply may still transfer the full texture on some backends, so throttle it.
                    worldBuildingAtlas.Apply(false,false);worldBuildingDirty=false;worldBuildingNextUpload=Time.unscaledTime+.2f;
                }
            }
            var atlasTint=GUI.color;GUI.color=new Color(1,1,1,Mathf.InverseLerp(1,3,worldMapZoom)*.85f);
            GUI.DrawTextureWithTexCoords(bounds,worldBuildingAtlas,uv);GUI.color=atlasTint;
        }
        SectorLayout overworldAtlasSource;
        Texture2D worldLocalTexture;
        Vector2 worldLocalOrigin=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
        float worldLocalRefresh;
        readonly Color[] worldLocalPixels=new Color[200*150];
        int overworldSelectedCamp=-1;
        float worldMapZoom=2.4f;
        Vector2 worldMapCenter=new Vector2(.5f,.5f);
        bool worldMapDragging,worldMapInitialized;
        int worldMapTouchId=-1;
        float worldMapVerticalScale=1;
        Vector2 worldMapLastPointer;
        float worldMapPinchDistance;
        public static string OverworldCampName(int index,bool japanese)
        {
            string[] en={"Boss Hideout","Iron Rest","Canal Refuge","Green Shelter","Ash Post","North Watch","Wayfarer Camp","Rail Refuge","Foundry Rest","Lake Watch","Pine Shelter","Old Mill","South Haven","Ridge Camp"};
            string[] ja={"ボスのアジト","鉄の休息所","運河の避難所","緑陰のシェルター","灰の前哨地","北の監視所","旅人の野営地","鉄路の避難所","鋳造所の休息所","湖の監視所","松林のシェルター","古い製粉所","南の安息所","尾根の野営地"};
            return (japanese?ja:en)[Mathf.Clamp(index,0,13)];
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
            FieldUIArt.Plate(bounds,false);var style=new GUIStyle(centered){fontSize=bounds.height<50?16:MobileTouchControls.Enabled?24:20,wordWrap=bounds.height>=50};
            style.normal.textColor=enabled?UiPaper:Mist;bool result=GUI.Button(bounds,value,style);GUI.enabled=previous;return result;
        }
        Color WorldActivityColor(int type)=>type==0?Gold:type==1?Cyan:new Color(.73f,.55f,.92f);
        void DrawOverworldActivityHUD(bool interactionAvailable)
        {
            if(OverworldActivityChoiceOpen)
            {
                Rect card=new Rect(365,185,710,520);Panel(card,Cyan);
                WorldLabel(new Rect(390,205,570,65),OverworldActivityTitle,Gold,32);
                if(WorldButton(new Rect(973,202,76,60),"×"))OpenNearbyOverworldActivity();
                WorldLabel(new Rect(390,285,650,120),OverworldActivityDescription,UiPaper,25);
                for(int choice=0;choice<OverworldActivityChoiceCount;choice++)
                {
                    int selected=choice;
                    if(WorldButton(new Rect(390,425+choice*112,650,92),OverworldActivityChoiceLabel(choice)))ChooseOverworldActivity(selected);
                }
                return;
            }
            if(!interactionAvailable||NearbyOverworldActivity<0)return;
            bool mobile=MobileTouchControls.Enabled;
            Rect prompt=mobile?new Rect(440,325,570,60):new Rect(940,625,475,86);
            if(WorldButton(prompt,OverworldActivityTitle+"\n"+T(mobile?"INVESTIGATE":"INVESTIGATE / E",mobile?"調べる":"調べる / E")))OpenNearbyOverworldActivity();
        }
        void EnsureOverworldAtlas()
        {
            var geo=WorldGeography;if(geo==null||geo.Layout==null)return;
            var layout=geo.Layout;if(overworldAtlasTexture!=null&&overworldAtlasSource==layout)return;
            if(overworldAtlasTexture!=null&&!aerialAtlas)Destroy(overworldAtlasTexture);
            var aerial=Resources.Load<Texture2D>("Art/UI/FieldKit/aerial-city-v1");
            if(aerial){overworldAtlasTexture=aerial;overworldAtlasSource=layout;aerialAtlas=true;return;}
            aerialAtlas=false;
            overworldAtlasSource=layout;const int resolution=1024;
            overworldAtlasTexture=new Texture2D(resolution,resolution,TextureFormat.RGBA32,false){name="Ashfall surveyed city atlas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[resolution*resolution];
            for(int y=0;y<resolution;y++)for(int x=0;x<resolution;x++){
                var p=new Vector3((x+.5f-resolution*.5f)*10000/resolution,0,(y+.5f-resolution*.5f)*10000/resolution);
                bool river=geo.River(p)&&!geo.Bridge(p.z);float road=geo.RoadClearance(p);bool park=geo.Park(p);
                int sx=Mathf.RoundToInt(p.x/100),sz=Mathf.RoundToInt(p.z/100);bool arterial=road<0&&((sx%3==0&&Mathf.Abs(p.x-geo.StreetX(sx,p.z))<9)||(sz%3==0&&Mathf.Abs(p.z-geo.StreetZ(sz,p.x))<9));
                Color color=river?new Color(.10f,.23f,.27f):arterial?new Color(.52f,.49f,.40f):road<0?new Color(.31f,.32f,.29f):road<2?new Color(.28f,.34f,.33f):park?new Color(.18f,.27f,.21f):new Color(.12f,.16f,.17f);
                if(!river&&road>3&&!park){float district=Mathf.PerlinNoise(p.x*.0018f+11,p.z*.0018f+7);color=Color.Lerp(color,new Color(.28f,.30f,.29f),district*.3f);}
                float noise=Mathf.PerlinNoise(p.x*.003f+100,p.z*.003f+100);color*=.88f+.18f*noise;
                colors[y*resolution+x]=color;
            }
            overworldAtlasTexture.SetPixels(colors);overworldAtlasTexture.Apply(false,true);
        }
        Vector2 WorldAtlasPoint(Rect map,Vector3 position)
        {
            var l=WorldGeography.Layout;
            float u=position.x/(l.Columns*l.CellSize)+.5f,v=position.z/(l.Rows*l.CellSize)+.5f;
            return new Vector2(map.center.x+(u-worldMapCenter.x)*map.width*worldMapZoom,map.center.y-(v-worldMapCenter.y)*map.width*worldMapZoom*worldMapVerticalScale);
        }
        public void DrawOverworldHUD()
        {
            bool mobile=MobileTouchControls.Enabled;DrawCriticalHealthVignette();
            DrawHealthPanel(new Rect(20,20,400,125),mobile);
            Box(new Rect(440,20,590,63),new Color(.025f,.04f,.05f,.55f));
            Box(new Rect(440,20,2,63),new Color(Cyan.r,Cyan.g,Cyan.b,.65f));
            WorldLabel(new Rect(456,24,550,28),WorldGeography==null?T("WASTELAND","荒野"):WorldGeography.RegionName(PlayerPosition,VaultLanguage.Japanese),Gold,21);
            WorldLabel(new Rect(456,51,550,27),T("LEVEL ","レベル ")+DebtLevel+"   /   "+Credits.ToString("N0")+T(" SCRAP"," スクラップ"),UiPaper,19);
            if(!mobile&&WorldButton(new Rect(1240,20,180,74),T("EQUIPMENT","装備")))ToggleInventory();
            DrawWorldLocalMap(new Rect(1165,165,250,180));
            if(gun!=null)
            {
                DrawHeldWeaponHUD(mobile?new Rect(350,797,280,80):new Rect(1190,812,226,64));
            }
            if(!DrawActivityRewardPreview())DrawWorldLootPreview();
            DrawWorldCombatIndicators();
            DrawOverworldRushHUD();
            int entrance=NearbyWorldEntrance,camp=NearbyWorldCamp;
            if(entrance>=0)
            {
                var stage=ExpeditionMap.Get(entrance,DebtLevel);
                if(WorldButton(mobile?new Rect(440,325,570,60):new Rect(940,625,475,86),stage.Name+" / Lv."+stage.level+"\n"+T(mobile?"ENTER":"ENTER / E",mobile?"入場":"入場 / E")))EnterNearbyWorldDungeon(entrance);
            }
            else if(camp>=0)
            {
                if(WorldButton(mobile?new Rect(440,325,570,60):new Rect(940,625,475,86),OverworldCampName(camp,VaultLanguage.Japanese)+"\n"+T(mobile?"ENTER CAMP":"ENTER CAMP / E",mobile?"拠点へ入る":"拠点へ入る / E")))EnterNearbyWorldCamp(camp);
            }
            else if(NearbyAmmoCrate||NearbySupplyChest)WorldLabel(new Rect(940,625,475,74),T(mobile?"USE: OPEN CACHE":"USE / F: OPEN CACHE",mobile?"調べる：物資箱を開ける":"調べる / F：物資箱を開ける"),Gold,26);
            DrawOverworldActivityHUD(entrance<0&&camp<0&&!NearbyAmmoCrate&&!NearbySupplyChest);
            if(toastTimer>0){Rect notice=mobile?new Rect(25,150,390,100):new Rect(435,160,570,72);Panel(notice,Gold);WorldLabel(new Rect(notice.x+12,notice.y+10,notice.width-24,notice.height-15),Toast,Gold,mobile?20:21);}
        }
        void DrawWorldCombatIndicators()
        {
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&Layout.IsVisible(foe.transform.position))
            {
                var p=cam.WorldToScreenPoint(foe.transform.position+Vector3.up*2.6f);if(p.z<=0)continue;
                Vector2 at=MobileTouchControls.Enabled?MobileCanvasPoint(p):new Vector2((p.x-(UnityEngine.Screen.width-1440*Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f))*.5f)/Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f),(UnityEngine.Screen.height-p.y-(UnityEngine.Screen.height-900*Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f))*.5f)/Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f));
                WorldLabel(new Rect(at.x-82,at.y-31,164,25),EnemyHealthLabel(foe),UiPaper,17);
                Box(new Rect(at.x-28,at.y,56,5),Ink);Box(new Rect(at.x-28,at.y,56*Mathf.Clamp01(foe.hp/foe.maxHp),5),foe.HealthBarColor);
            }
            if(!Active)return;
            var point=cam.WorldToScreenPoint(AimReticleWorldPosition);
            if(MobileTouchControls.Enabled){Vector2 at=MobileCanvasPoint(point);DrawCrosshair(at.x,at.y);}
            else {float scale=Mathf.Min(UnityEngine.Screen.width/1440f,UnityEngine.Screen.height/900f);Vector3 display=HasAimTarget?point:Input.mousePosition;DrawCrosshair((display.x-(UnityEngine.Screen.width-1440*scale)*.5f)/scale,(UnityEngine.Screen.height-display.y-(UnityEngine.Screen.height-900*scale)*.5f)/scale);}
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
            bool mobile=MobileTouchControls.Enabled;Rect card=mobile?new Rect(25,265,390,155):new Rect(25,190,390,155);Panel(card,color);
            Rect picture=new Rect(card.x+12,card.y+15,100,118);
            if(kind==0)ItemPortraits.Draw(picture,drop.gun);else if(kind==1)ItemPortraits.Draw(picture,armor.item);else if(kind==2)ManufacturerBranding.ModulePortrait(picture,module.item);else ManufacturerPortraits.Melee(picture,melee.item);
            WorldLabel(new Rect(card.x+130,card.y+12,card.width-140,40),VaultLanguage.RarityName(rarity),color,mobile?22:27);
            WorldLabel(new Rect(card.x+130,card.y+54,card.width-140,42),T("LEVEL ","レベル ")+level+T("   SCORE ","   評価 ")+score,UiPaper,mobile?21:25);
            WorldLabel(new Rect(card.x+130,card.y+100,card.width-140,46),T(MobileTouchControls.Enabled?"TAKE":"TAKE / F",MobileTouchControls.Enabled?"拾う":"拾う / F"),Gold,22);
        }
        bool DrawActivityRewardPreview()
        {
            if(Time.unscaledTime>=activityRewardUntil)return false;
            bool mobile=MobileTouchControls.Enabled;Rect card=mobile?new Rect(25,265,390,155):new Rect(25,190,390,155);
            object item=(object)LastActivityRewardGun??LastActivityRewardArmor??(object)LastActivityRewardModule;
            if(item==null&&LastActivityRewardScrap<=0)return false;
            Rarity rarity=item is Gun g?g.rarity:item is ArmorItem a?a.rarity:item is ArtifactItem m?m.rarity:Rarity.Legendary;
            Color color=Arsenal.Colors[(int)rarity];Panel(card,color);
            if(item==null){WorldLabel(new Rect(card.x+15,card.y+15,card.width-30,50),T("SCRAP RECEIVED","スクラップ獲得"),Gold,23);WorldLabel(new Rect(card.x+15,card.y+72,card.width-30,60),LastActivityRewardScrap.ToString("N0"),UiPaper,30);return true;}
            Rect picture=new Rect(card.x+10,card.y+16,100,120);string itemName;int level,score;
            if(item is Gun gun){ItemPortraits.Draw(picture,gun);itemName=VaultLanguage.GunName(gun);level=gun.level;score=GearScoring.Score(gun);}
            else if(item is ArmorItem armor){ItemPortraits.Draw(picture,armor);itemName=Armory.Name(armor);level=armor.level;score=GearScoring.Score(armor);}
            else {var module=(ArtifactItem)item;ManufacturerBranding.ModulePortrait(picture,module);itemName=ArtifactCatalog.DisplayName(module);level=module.level;score=GearScoring.Score(module);}
            WorldLabel(new Rect(card.x+120,card.y+8,card.width-130,37),itemName,UiPaper,20);
            WorldLabel(new Rect(card.x+120,card.y+44,card.width-130,32),VaultLanguage.RarityName(rarity),color,20);
            WorldLabel(new Rect(card.x+120,card.y+77,card.width-130,30),T("LEVEL ","レベル ")+level+T("  SCORE ","  評価 ")+score,UiPaper,18);
            WorldLabel(new Rect(card.x+120,card.y+109,card.width-130,38),LastActivityRewardOnGround?T("ON GROUND / TAKE","足元に配置 / 拾う"):T("RECEIVED","獲得"),Gold,18);return true;
        }
        void DrawWorldLocalMap(Rect map)
        {
            if(GUI.Button(map,GUIContent.none,GUIStyle.none))OpenOverworldMap();
            EnsureOverworldAtlas();if(overworldAtlasTexture==null)return;
            Panel(map,Cyan);Rect inside=new Rect(map.x+10,map.y+30,map.width-20,map.height-40);
            var l=WorldGeography.Layout;float u=PlayerPosition.x/(l.Columns*l.CellSize)+.5f,v=PlayerPosition.z/(l.Rows*l.CellSize)+.5f;
            float uw=Mathf.Min(1,200f/(l.CellSize*l.Columns)),vh=Mathf.Min(1,150f/(l.CellSize*l.Rows));float left=Mathf.Clamp(u-uw*.5f,0,1-uw),bottom=Mathf.Clamp(v-vh*.5f,0,1-vh);
            Vector2 localOrigin=new Vector2(left*l.Columns*l.CellSize-l.Columns*l.CellSize*.5f,bottom*l.Rows*l.CellSize-l.Rows*l.CellSize*.5f);
            if(aerialAtlas)DrawAerialSurvey(inside,new Rect(left,bottom,uw,vh));
            else {UpdateWorldLocalTexture(localOrigin);GUI.DrawTexture(inside,worldLocalTexture);}
            WorldLabel(new Rect(map.x+10,map.y+3,map.width-20,27),T("LOCAL / 200 m / NORTH ↑","周辺 / 200m / 北 ↑"),Mist,17);
            Vector2 Local(Vector3 p)=>new Vector2(inside.x+(p.x/(l.Columns*l.CellSize)+.5f-left)/uw*inside.width,inside.yMax-(p.z/(l.Rows*l.CellSize)+.5f-bottom)/vh*inside.height);
            foreach(var enemy in Enemies)if(enemy!=null&&!enemy.dead){var p=Local(enemy.transform.position);if(inside.Contains(p))Box(new Rect(p.x-2,p.y-2,4,4),UiDanger);}
            for(int i=0;i<WorldGeography.Entrances.Length;i++){var p=Local(WorldGeography.Entrances[i]);if(inside.Contains(p))DrawMapArt(new Rect(p.x-10,p.y-10,20,20),1,Color.white);}
            for(int i=0;i<WorldGeography.Camps.Length;i++)
            {
                var p=Local(WorldGeography.Camps[i]);if(!inside.Contains(p))continue;
                Box(new Rect(p.x-6,p.y-6,12,12),new Color(.02f,.04f,.05f,.9f));
                DrawMapArt(new Rect(p.x-10,p.y-10,20,20),0,IsCampVisited(i)?Color.white:Mist);
            }
            foreach(var site in NearbyOverworldActivitySites)
            {
                if(site.completed)continue;
                var p=Local(site.position);
                if(inside.Contains(p))DrawMapArt(new Rect(p.x-10,p.y-10,20,20),site.type==1?2:site.type==2?4:3,Color.white);
            }
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
            bool tutorialTarget=TryFirstStepsWaypoint(out Vector3 firstStepsTarget);
            if(tutorialTarget||OverworldWaypointKind!=0)
            {
                Vector3 target=tutorialTarget?firstStepsTarget:OverworldWaypointPosition;
                Vector2 targetOnMap=Local(target), direction=targetOnMap-player;
                Rect safe=new Rect(inside.x+15,inside.y+15,inside.width-30,inside.height-30);
                if(inside.Contains(targetOnMap))
                {
                    // The point of the pin marks the real destination, even when close to the player.
                    DrawWaypointPin(targetOnMap,tutorialTarget);
                }
                else if(direction.sqrMagnitude>0.001f)
                {
                    Vector2 origin=new Vector2(Mathf.Clamp(player.x,safe.xMin,safe.xMax),Mathf.Clamp(player.y,safe.yMin,safe.yMax));
                    float fraction=float.MaxValue;
                    if(direction.x>0)fraction=Mathf.Min(fraction,(safe.xMax-origin.x)/direction.x);
                    if(direction.x<0)fraction=Mathf.Min(fraction,(safe.xMin-origin.x)/direction.x);
                    if(direction.y>0)fraction=Mathf.Min(fraction,(safe.yMax-origin.y)/direction.y);
                    if(direction.y<0)fraction=Mathf.Min(fraction,(safe.yMin-origin.y)/direction.y);
                    DrawWaypointArrow(origin+direction*Mathf.Max(0,fraction),direction.normalized,tutorialTarget);
                }
            }
        }
        Texture2D waypointArt,mapIconsArt;
        void DrawMapArt(Rect rect,int icon,Color tint)
        {
            if(!mapIconsArt)mapIconsArt=Resources.Load<Texture2D>("Art/UI/map-icons-v1");
            if(!mapIconsArt){FieldUIArt.Icon(rect,icon==0?6:icon==1?7:9,tint);return;}
            Color old=GUI.color;GUI.color=tint;
            GUI.DrawTextureWithTexCoords(rect,mapIconsArt,new Rect(icon*.2f,.21f,.2f,.58f),true);GUI.color=old;
        }
        void DrawWaypointPin(Vector2 point,bool tutorial=false)
        {
            if(!waypointArt)waypointArt=Resources.Load<Texture2D>("Art/UI/map-waypoint-v1");
            if(waypointArt)GUI.DrawTexture(new Rect(point.x-22,point.y-38,44,42),waypointArt,ScaleMode.ScaleToFit,true);
            if(tutorial)WorldLabel(new Rect(point.x-44,point.y-48,88,24),T("OBJECTIVE","目的地"),Color.white,18);
        }
        void DrawWaypointArrow(Vector2 center,Vector2 direction,bool tutorial=false)
        {
            if(!waypointArt)waypointArt=Resources.Load<Texture2D>("Art/UI/map-waypoint-v1");
            if(waypointArt){var previous=GUI.matrix;float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;GUI.matrix=previous*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(new Vector3(-center.x,-center.y,0));float size=tutorial?46:34;GUI.DrawTexture(new Rect(center.x-size*.5f,center.y-size*.5f,size,size),waypointArt,ScaleMode.ScaleToFit,true);GUI.matrix=previous;}
        }
        void UpdateWorldLocalTexture(Vector2 origin)
        {
            if(worldLocalTexture==null)worldLocalTexture=new Texture2D(200,150,TextureFormat.RGBA32,false){name="200m local geography",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(Event.current.type!=EventType.Repaint)return;if((origin-worldLocalOrigin).sqrMagnitude<16)return;if(Time.unscaledTime<worldLocalRefresh)return;
            worldLocalRefresh=Time.unscaledTime+.75f;worldLocalOrigin=origin;var geo=WorldGeography;
            for(int z=0;z<150;z++)for(int x=0;x<200;x++)
            {
                var p=new Vector3(origin.x+x+.5f,0,origin.y+z+.5f);
                worldLocalPixels[z*200+x]=geo.River(p)&&!geo.Bridge(p.z)?new Color(.12f,.23f,.26f):LocalMapBuilding(geo,p)?new Color(.38f,.36f,.31f):geo.RoadClearance(p)<0?new Color(.62f,.58f,.48f):geo.Park(p)?new Color(.22f,.29f,.23f):new Color(.16f,.19f,.19f);
            }
            worldLocalTexture.SetPixels(worldLocalPixels);worldLocalTexture.Apply(false,false);
        }
        bool LocalMapBuilding(OverworldGeography geo,Vector3 p){var chunk=geo.GetChunk(Mathf.FloorToInt(p.x/100),Mathf.FloorToInt(p.z/100));foreach(var b in chunk.Buildings)if(b.Contains(new Vector2(p.x,p.z)))return true;return false;}
        void SetWorldMapZoom(float zoom,Vector2 anchor,Rect map)
        {
            float next=Mathf.Clamp(zoom,1f,8f);if(Mathf.Approximately(next,worldMapZoom))return;
            float u=worldMapCenter.x+(anchor.x-map.center.x)/(map.width*worldMapZoom);
            float v=worldMapCenter.y-(anchor.y-map.center.y)/(map.width*worldMapZoom*worldMapVerticalScale);
            worldMapCenter.x=u-(anchor.x-map.center.x)/(map.width*next);
            worldMapCenter.y=v+(anchor.y-map.center.y)/(map.width*next*worldMapVerticalScale);
            worldMapZoom=next;ClampWorldMapCenter();
        }
        void ClampWorldMapCenter()
        {
            float margin=.5f/worldMapZoom;
            worldMapCenter.x=Mathf.Clamp(worldMapCenter.x,margin,1-margin);
            float verticalMargin=margin*650f/(900f*worldMapVerticalScale);worldMapCenter.y=Mathf.Clamp(worldMapCenter.y,verticalMargin,1-verticalMargin);
        }
        void PanWorldMap(Vector2 delta,Rect map)
        {
            worldMapCenter.x-=delta.x/(map.width*worldMapZoom);
            worldMapCenter.y+=delta.y/(map.width*worldMapZoom*worldMapVerticalScale);
            ClampWorldMapCenter();
        }
        void HandleWorldMapInput(Rect map)
        {
            var evt=Event.current;
            if(MobileTouchControls.Enabled && Input.touchCount>0)
            {
                if(evt.type==EventType.Repaint)
                {
                    if(Input.touchCount==2)
                    {
                        Vector2 a=MobileCanvasPoint(Input.GetTouch(0).position),b=MobileCanvasPoint(Input.GetTouch(1).position);
                        float distance=Vector2.Distance(a,b);Vector2 midpoint=(a+b)*.5f;
                        if(worldMapPinchDistance>0 && map.Contains(midpoint))
                        {
                            PanWorldMap(midpoint-worldMapLastPointer,map);
                            SetWorldMapZoom(worldMapZoom*distance/worldMapPinchDistance,midpoint,map);
                        }
                        worldMapPinchDistance=distance;worldMapLastPointer=midpoint;worldMapTouchId=-1;worldMapDragging=false;
                    }
                    else if(Input.touchCount==1)
                    {
                        Touch touch=Input.GetTouch(0);Vector2 pointer=MobileCanvasPoint(touch.position);
                        if(worldMapTouchId==touch.fingerId && worldMapPinchDistance==0)PanWorldMap(pointer-worldMapLastPointer,map);
                        else if(map.Contains(pointer))worldMapTouchId=touch.fingerId;
                        worldMapLastPointer=pointer;worldMapPinchDistance=0;
                        if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)worldMapTouchId=-1;
                    }
                }
                return;
            }
            worldMapPinchDistance=0;worldMapTouchId=-1;
            if(evt.type==EventType.ScrollWheel&&map.Contains(evt.mousePosition))
            {SetWorldMapZoom(worldMapZoom*Mathf.Pow(1.15f,-evt.delta.y),evt.mousePosition,map);evt.Use();}
            if(evt.type==EventType.MouseDown&&evt.button==0&&map.Contains(evt.mousePosition))
            {worldMapDragging=true;worldMapLastPointer=evt.mousePosition;}
            if(evt.type==EventType.MouseDrag&&worldMapDragging)
            {PanWorldMap(evt.mousePosition-worldMapLastPointer,map);worldMapLastPointer=evt.mousePosition;evt.Use();}
            if(evt.type==EventType.MouseUp)worldMapDragging=false;
        }
        public void DrawOverworldAtlas()
        {
            EnsureOverworldAtlas();Box(new Rect(0,0,1440,900),new Color(.025f,.045f,.06f,.98f));
            WorldLabel(new Rect(30,20,1090,55),T("THE WASTELAND / FIELD ATLAS","荒野 / 広域地図"),UiPaper,32);
            if(WorldButton(new Rect(1195,16,215,66),T("BACK","戻る")))CloseOverworldMap();
            if(overworldAtlasTexture==null)return;
            Rect map=new Rect(30,100,900,650);
            // The mobile canvas fills a device's safe area nonuniformly. Compensate
            // here so the surveyed city keeps equal physical scale on both axes.
            worldMapVerticalScale=Mathf.Abs(GUI.matrix.m00/Mathf.Max(.001f,GUI.matrix.m11));
            if(!worldMapInitialized)
            {
                Vector3 origin=IsOverworld?PlayerPosition:WorldGeography.Camps[LastVisitedCamp];
                worldMapCenter=new Vector2(origin.x/10000f+.5f,origin.z/10000f+.5f);
                ClampWorldMapCenter();worldMapInitialized=true;
            }
            HandleWorldMapInput(map);
            float visible=1f/worldMapZoom;
            float verticalVisible=visible*map.height/(map.width*worldMapVerticalScale);
            Rect uv=new Rect(worldMapCenter.x-visible*.5f,worldMapCenter.y-verticalVisible*.5f,visible,verticalVisible);
            DrawAerialSurvey(map,uv);
            DrawWorldBuildingAtlas(map,uv);
            if(!aerialAtlas)FieldUIArt.MapSurface(map);
            DrawAtlasDistrictLabels(map);
            WorldLabel(new Rect(50,110,650,35),T("ASHFALL  /  CITY SURVEY","アッシュフォール / 都市調査図"),UiPaper,20);
            DrawWorldMapScale(map);
            FieldUIArt.Icon(new Rect(map.xMax-61,map.yMax-54,42,34),5,UiPaper);
            if(GUI.Button(new Rect(map.xMax-66,map.yMax-58,52,44),GUIContent.none,GUIStyle.none)){var pos=IsOverworld?PlayerPosition:WorldGeography.Camps[LastVisitedCamp];worldMapCenter=new Vector2(pos.x/10000f+.5f,pos.z/10000f+.5f);ClampWorldMapCenter();}
            for(int i=0;i<WorldGeography.Entrances.Length;i++)
            {
                Vector2 p=WorldAtlasPoint(map,WorldGeography.Entrances[i]);if(!map.Contains(p))continue;var stage=ExpeditionMap.Get(i,DebtLevel);bool selected=overworldSelectedCamp<0&&expeditionMapSelection==i;
                Color color=OverworldWaypointKind==2&&OverworldWaypointId==i?Gold:selected?new Color(.95f,.55f,.28f):clearedExpeditionStages.Contains(i)?new Color(.63f,.79f,.72f):IsOverworldDungeonDiscovered(i)?UiPaper:Mist;
                if(selected)Box(new Rect(p.x-12,p.y-12,24,24),new Color(.02f,.04f,.06f,.9f));DrawMapArt(new Rect(p.x-(selected?18:13),p.y-(selected?18:13),selected?36:26,selected?36:26),1,color);
                if(OverworldWaypointKind==2&&OverworldWaypointId==i)WorldLabel(new Rect(p.x+12,p.y-22,26,28),"▲",Gold,19);
                if(GUI.Button(new Rect(p.x-13,p.y-13,26,26),GUIContent.none,GUIStyle.none)){expeditionMapSelection=i;overworldSelectedCamp=-1;}
            }
            for(int i=0;i<WorldGeography.Camps.Length;i++)
            {
                Vector2 p=WorldAtlasPoint(map,WorldGeography.Camps[i]);if(!map.Contains(p))continue;DrawMapArt(new Rect(p.x-18,p.y-18,36,36),0,IsCampVisited(i)?Color.white:Color.gray);
                if(OverworldWaypointKind==1&&OverworldWaypointId==i)WorldLabel(new Rect(p.x+15,p.y-24,28,28),"▲",Gold,20);
                if(GUI.Button(new Rect(p.x-18,p.y-18,36,36),GUIContent.none,GUIStyle.none))overworldSelectedCamp=i;
            }
            if(IsOverworld)foreach(var site in NearbyOverworldActivitySites)
            {
                if(site.completed)continue;
                Vector2 p=WorldAtlasPoint(map,site.position);
                if(map.Contains(p))DrawMapArt(new Rect(p.x-14,p.y-14,28,28),site.type==1?2:site.type==2?4:3,Color.white);
            }
            if(IsOverworld){var p=WorldAtlasPoint(map,PlayerPosition);if(map.Contains(p)){WorldMarker(p.x,p.y,12,Color.white);Box(new Rect(p.x-2,p.y-2,4,4),Cyan);}}
            if(WorldButton(new Rect(744,112,78,55),"−"))SetWorldMapZoom(worldMapZoom/1.5f,map.center,map);
            if(WorldButton(new Rect(830,112,78,55),"+"))SetWorldMapZoom(worldMapZoom*1.5f,map.center,map);
            Panel(new Rect(950,100,460,650),Gold);
            if(overworldSelectedCamp>=0)
            {
                int camp=overworldSelectedCamp;bool visited=IsCampVisited(camp);bool travel=visited&&CanWorldFastTravel;
                WorldLabel(new Rect(975,125,410,100),OverworldCampName(camp,VaultLanguage.Japanese),Gold,32);
                WorldLabel(new Rect(975,250,410,210),visited?T("Discovered refuge. Travel here to rest and trade. Fast travel is unavailable inside dungeons.","発見済みの避難所。休息と取引のために移動できます。ダンジョン内からは移動できません。"):T("Undiscovered. Reach this camp on foot to unlock fast travel.","未訪問。歩いて到達するとファストトラベルが解放されます。"),Mist,26);
                bool tracking=OverworldWaypointKind==1&&OverworldWaypointId==camp;
                if(WorldButton(new Rect(975,500,410,75),tracking?T("CLEAR DESTINATION","目的地指定を解除"):T("SET DESTINATION","目的地に指定")))SetOverworldWaypoint(tracking?0:1,camp);
                if(WorldButton(new Rect(975,620,410,100),T("FAST TRAVEL","ファストトラベル"),travel))FastTravelToCamp(camp);
            }
            else DrawWorldStageBriefing();
            for(int i=0;i<WorldGeography.Camps.Length;i++)if(WorldButton(new Rect(30+(i%7)*198,784+(i/7)*52,188,46),(i+1)+" / "+OverworldCampName(i,VaultLanguage.Japanese),IsCampVisited(i)))
            {
                overworldSelectedCamp=i;
                Vector3 camp=WorldGeography.Camps[i];worldMapCenter=new Vector2(camp.x/10000f+.5f,camp.z/10000f+.5f);ClampWorldMapCenter();
            }
        }

        void DrawAerialMapLegend(Rect map)
        {
            Rect legend=new Rect(map.xMax-172,map.y+88,156,152);
            FieldUIArt.Plate(legend,false);
            int[] icons={6,7,11};
            string[] names={T("Camp","拠点"),T("Dungeon","ダンジョン"),T("Discovery","発見")};
            for(int i=0;i<3;i++)
            {
                float y=legend.y+15+i*43;
                DrawMapArt(new Rect(legend.x+12,y,28,28),i==0?0:i==1?1:4,Color.white);
                WorldLabel(new Rect(legend.x+47,y+3,104,32),names[i],UiPaper,18);
            }
        }
        string WorldDistanceLabel(Vector3 destination)
        {
            Vector3 origin=IsOverworld?PlayerPosition:WorldGeography.Camps[LastVisitedCamp];
            float distance=Vector2.Distance(new Vector2(origin.x,origin.z),new Vector2(destination.x,destination.z));
            return distance>=1000?(distance/1000f).ToString("0.00")+" km":Mathf.RoundToInt(distance)+" m";
        }
        void DrawAtlasDistrictLabels(Rect map)
        {
            Vector2[] sites={new Vector2(-1350,-2100),new Vector2(-1700,-650),new Vector2(-700,-500),new Vector2(700,400),new Vector2(3650,650),new Vector2(2900,2800)};
            string[] en={"SOUTH MARKET","OLD CITY","RIVERFRONT PARK","CIVIC QUARTER","EAST MARKET","NORTH COMMON"};
            string[] ja={"南部市場","旧市街","河岸公園","市庁舎地区","東部市場","北の公園"};
            for(int i=0;i<sites.Length;i++)
            {
                Vector2 p=WorldAtlasPoint(map,new Vector3(sites[i].x,0,sites[i].y));
                if(p.x<map.x+16||p.x>map.xMax-170||p.y<map.y+50||p.y>map.yMax-30)continue;
                var title=VaultLanguage.Japanese?ja[i]:en[i];
                Box(new Rect(p.x-5,p.y+5,4,4),new Color(.72f,.76f,.71f,.85f));
                WorldLabel(new Rect(p.x+5,p.y-5,170,24),title,new Color(.79f,.83f,.79f,.88f),worldMapZoom>4?18:15);
            }
        }
        void DrawWorldMapScale(Rect map)
        {
            float width=map.width*1000f/(WorldGeography.Layout.Columns*WorldGeography.Layout.CellSize)*worldMapZoom;
            Rect backdrop=new Rect(map.x+12,map.yMax-48,width+140,38);Box(backdrop,new Color(.02f,.035f,.04f,.94f));
            float x=backdrop.x+8,y=backdrop.y+22;Box(new Rect(x,y,width,3),UiPaper);Box(new Rect(x,y-5,2,10),UiPaper);Box(new Rect(x+width-2,y-5,2,10),UiPaper);
            WorldLabel(new Rect(x+width+12,backdrop.y+5,115,30),"1 km",UiPaper,17);
        }
        void DrawWorldStageBriefing()
        {
            var stage=ExpeditionMap.Get(Mathf.Clamp(expeditionMapSelection,0,ExpeditionMap.StageCount-1),DebtLevel);bool discovered=IsOverworldDungeonDiscovered(stage.id)||clearedExpeditionStages.Contains(stage.id);
            WorldLabel(new Rect(975,120,410,95),(stage.id+1).ToString("00")+" / "+(discovered?stage.Name:"?"),Gold,29);
            WorldLabel(new Rect(975,220,410,75),discovered?stage.Location+" / "+stage.Biome:"?",Mist,23);
            WorldLabel(new Rect(975,305,410,70),T("LEVEL ","レベル ")+(discovered?stage.level.ToString():"?"),Cyan,25);
            WorldLabel(new Rect(975,375,410,105),discovered?ExpeditionMap.MissionLabel(stage.mission,VaultLanguage.Japanese):"?",UiPaper,26);
            WorldLabel(new Rect(975,490,410,80),discovered?ExpeditionRewardDescription(stage.id):"?",Gold,22);
            WorldLabel(new Rect(975,575,410,40),T("DISTANCE ","入口まで ")+WorldDistanceLabel(WorldGeography.Entrances[stage.id]),Mist,21);
            bool tracking=OverworldWaypointKind==2&&OverworldWaypointId==stage.id;
            if(WorldButton(new Rect(975,615,410,60),tracking?T("CLEAR DESTINATION","目的地指定を解除"):T("SET DESTINATION","目的地に指定")))SetOverworldWaypoint(tracking?0:2,stage.id);
            bool nearby=IsOverworld&&NearbyWorldEntrance==stage.id;
            if(WorldButton(new Rect(975,685,410,55),nearby?T("ENTER DUNGEON","ダンジョンに入る"):T("WALK TO THIS ENTRANCE","この入口まで歩いて移動"),nearby))EnterNearbyWorldDungeon(stage.id);
        }
        public void DrawWorldBossConversation()
        {
            DrawCinematicBossIntro();
        }
    }
}




