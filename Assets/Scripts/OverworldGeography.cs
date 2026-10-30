using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public sealed partial class OverworldGeography
    {
        public const int Seed=741921,Columns=200,Rows=200;
        public const float HalfExtent=5000,ChunkSize=100;
        public SectorLayout Layout {get;private set;}
        public Vector3[] Camps {get;private set;}
        public Vector3[] Entrances {get;private set;}
        public Vector3[] Chests {get;private set;}
        // Compatibility: these contain no full-world prop catalogue. Query GetChunk instead.
        public readonly List<Rect> Buildings=new List<Rect>();
        public readonly List<Vector3> Trees=new List<Vector3>(),Cars=new List<Vector3>();
        readonly List<Vector3> protectedSites=new List<Vector3>();
        readonly Dictionary<Vector2Int,List<Vector3>> siteBuckets=new Dictionary<Vector2Int,List<Vector3>>();
        static int[] dungeonLevels;
        public static int DungeonLevel(int id)
        {
            if(dungeonLevels==null)new OverworldGeography();
            return dungeonLevels[Mathf.Clamp(id,0,dungeonLevels.Length-1)];
        }
        readonly Dictionary<Vector2Int,ChunkData> cache=new Dictionary<Vector2Int,ChunkData>();
        readonly Queue<Vector2Int> cacheOrder=new Queue<Vector2Int>();
        public sealed class ChunkData
        {
            public readonly List<Rect> Buildings=new List<Rect>();
            public readonly List<Vector3> Trees=new List<Vector3>(),Cars=new List<Vector3>();
        }
        public const int StreetSearchRadius=3;
        public const float BridgeHalfLength=32;
        public int District(Vector3 p)
        {
            if(Park(p))return 4;
            float belt=p.x+320*Mathf.Sin(p.z*.00065f);
            if(p.z<-2200)return 3;
            if(belt>-650&&belt<1250&&p.z>-1700&&p.z<1700)return 0;
            if(belt>1600&&p.z<2300)return 2;
            return belt<-700?3:1;
        }
        // These vectors are also consumed by the water shader: shore shading must
        // follow exactly the same centre line and varying width as navigation.
        public static readonly Vector4 RiverShapeParameters=new Vector4(420,430,140,110);
        public static readonly Vector4 RiverFrequencyParameters=new Vector4(.00067f,.00161f,0,.7f);
        public static readonly Vector4 RiverWidthParameters=new Vector4(110,15,5,0);
        public static readonly Vector4 RiverWidthFrequencyParameters=new Vector4(.00091f,.0023f,1.2f,0);
        public float RiverX(float z)=>RiverShapeParameters.x+RiverShapeParameters.y*Mathf.Sin(z*RiverFrequencyParameters.x+RiverFrequencyParameters.z)+RiverShapeParameters.z*Mathf.Sin(z*RiverFrequencyParameters.y+RiverFrequencyParameters.w);
        public float RiverWidth(float z)=>2*(RiverWidthParameters.x+RiverWidthParameters.y*Mathf.Sin(z*RiverWidthFrequencyParameters.x+RiverWidthFrequencyParameters.z)+RiverWidthParameters.z*Mathf.Sin(z*RiverWidthFrequencyParameters.y+RiverWidthFrequencyParameters.w));
        public bool Bridge(float z)=>Mathf.Abs(z-Mathf.Round(z/1000)*1000)<BridgeHalfLength&&Mathf.Abs(z)<4500;
        public bool River(Vector3 p)=>Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f;
        public bool PocketPark(Vector3 p)=>false;
        bool CityPark(Vector3 p,float x,float z,float rx,float rz,float phase)
        {
            float dx=(p.x-x)/rx,dz=(p.z-z)/rz;
            float angle=Mathf.Atan2(dz,dx);
            float edge=1+.12f*Mathf.Sin(angle*3+phase)+.06f*Mathf.Sin(angle*5-phase);
            return dx*dx+dz*dz<edge*edge;
        }
        // The outer green belts and two city parks occupy about 18% of the world.
        public bool Park(Vector3 p)=>p.x<-4000+330*Mathf.Sin(p.z*.0009f)
            ||p.z>4300+240*Mathf.Sin(p.x*.0008f)
            ||CityPark(p,-700,-500,600,350,.8f)
            ||CityPark(p,2900,2800,660,440,2.1f);
        // Legacy street APIs retained for compatibility only. Actual roads are the
        // finite bucketed graph in OverworldRoadNetwork, shared with streamed meshes.
        public float StreetX(int street,float z)=>street*100
            +74*Mathf.Sin(z*.0009f)+36*Mathf.Sin(z*.0021f+street*.27f)
            +13*Mathf.Sin(z*.006f+street*.72f);
        public float StreetZ(int street,float x)
        {
            float bend=70*Mathf.Sin(x*.00082f+.5f)+34*Mathf.Sin(x*.0019f-street*.24f)+12*Mathf.Sin(x*.0058f+street*.8f);
            if(street%10==0)bend*=Mathf.Clamp01((Mathf.Abs(x-RiverX(street*100))-130)/240);
            return street*100+bend;
        }
        public float StreetWidth(int street)=>street%10==0?25:street%5==0?15:7;
        public bool StreetSegmentOpen(int street,int segment,bool northSouth)
        {
            if(street%5==0)return true;
            var p=northSouth?new Vector3(StreetX(street,segment*100+50),0,segment*100+50):new Vector3(segment*100+50,0,StreetZ(street,segment*100+50));
            int district=District(p);
            int period=district==0?2:district==1?3:district==2?4:5;
            if(district==4)return false;
            // Omit entire local corridors coherently; district transitions join arterials.
            return ((street%period)+period)%period==1;
        }
        bool ParcelClear(Rect rect)
        {
            // Sample the whole footprint so curves and river banks cannot cut through a house.
            for(float x=rect.xMin;x<=rect.xMax+4;x+=4)for(float z=rect.yMin;z<=rect.yMax+4;z+=4){var p=new Vector3(Mathf.Min(x,rect.xMax),0,Mathf.Min(z,rect.yMax));if(River(p)||RoadClearance(p)<4.5f||SiteProtected(p,3)||Park(p))return false;}
            return true;
        }
        public OverworldGeography()
        {
            BuildRoadNetwork();
            var random=new System.Random(Seed);var map=new bool[Columns,Rows];
            for(int x=0;x<Columns;x++)for(int y=0;y<Rows;y++){var p=new Vector3((x+.5f)*50-HalfExtent,0,(y+.5f)*50-HalfExtent);map[x,y]=!River(p)||Bridge(p.z);}
            // Every bridge crossing occupies an entire coarse navigation row.
            for(int y=0;y<Rows;y++)if(Bridge((y+.5f)*50-HalfExtent))for(int x=0;x<Columns;x++)map[x,y]=true;
            Camps=new Vector3[14];for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7+.28f;Camps[i]=SafeSite(new Vector3(Mathf.Cos(a)*3600,.65f,Mathf.Sin(a)*3400));}
            Entrances=new Vector3[ExpeditionMap.StageCount];Entrances[0]=SafeSite(Camps[0]+new Vector3(-30,0,0));
            for(int i=1;i<100;i++)
            {Vector3 chosen=Vector3.zero;float best=-1;for(int trial=0;trial<40;trial++){var p=new Vector3(-4700+(float)random.NextDouble()*9400,.65f,-4700+(float)random.NextDouble()*9400);float near=float.MaxValue;foreach(var q in protectedSites)near=Mathf.Min(near,(p-q).sqrMagnitude);if(near>best){best=near;chosen=p;}}Entrances[i]=SafeSite(chosen);}
            Chests=new Vector3[500];for(int i=0;i<36;i++)Chests[i]=SafeSite(new Vector3(-4300+i%6*1700+(float)random.NextDouble()*250,.65f,-4300+i/6*1700+(float)random.NextDouble()*250));
            // Keep ID order stable; every cache reserves an accessible clearing before
            // any streamed parcel is generated, including caches in the widened floodplain.
            for(int i=36;i<180;i++)
            {
                int cell=i-36,cx=-45+cell%12*8,cz=-45+cell/12*8;
                Chests[i]=SafeSite(new Vector3(cx*100+25,.65f,cz*100+25));
            }
            // Append new IDs only after the legacy sites and random rolls are fixed.
            // A second, offset camp ring fills the interior gaps without renumbering saves.
            for(int i=7;i<Camps.Length;i++)
            {
                float a=(i-7)*Mathf.PI*2/7+.28f+Mathf.PI/7;
                Camps[i]=NewSite(new Vector3(Mathf.Cos(a)*2150,.65f,Mathf.Sin(a)*2050),true);
            }
            // 500 caches over the 100 km² world: five per km² on average.
            for(int i=180;i<Chests.Length;i++)
            {
                int slot=i-180,column=slot%20,row=slot/20;
                float jitterX=(float)random.NextDouble()*240-120,jitterZ=(float)random.NextDouble()*300-150;
                Chests[i]=NewSite(new Vector3(-4650+column*490+jitterX,.65f,-4650+row*620+jitterZ));
            }
            // Append only after every legacy random roll and site is fixed.
            // Three additional starter sites are constrained to the 1 km square
            // centred on the first camp; entrance zero is already within it.
            for(int i=100;i<103;i++)
                Entrances[i]=NewSite(Camps[0]+new Vector3((i-101)*180,0,220),false,Camps[0]);
            var extraRandom=new System.Random(Seed^0x5A17);
            for(int i=103;i<Entrances.Length;i++)
            {
                Vector3 chosen=Vector3.zero;float best=-1;
                for(int trial=0;trial<40;trial++)
                {
                    var p=new Vector3(-4700+(float)extraRandom.NextDouble()*9400,.65f,-4700+(float)extraRandom.NextDouble()*9400);
                    float near=float.MaxValue;foreach(var q in protectedSites)near=Mathf.Min(near,(p-q).sqrMagnitude);
                    if(near>best){best=near;chosen=p;}
                }
                Entrances[i]=NewSite(chosen);
            }
            AddResidentialRoads();
            var levels=new int[Entrances.Length];
            for(int i=0;i<levels.Length;i++)
            {
                float distance=Vector3.Distance(Camps[0],Entrances[i]);
                // Nearby expeditions stay gentle; farther rings vary by one level.
                int variation=i==0?0:(i*37%3)-1;
                levels[i]=i>=100&&i<103?1:Mathf.Clamp(1+Mathf.FloorToInt(distance/850)+variation,1,10);
            }
            dungeonLevels=levels;
            var entry=new Vector2Int(Mathf.FloorToInt((Camps[0].x+HalfExtent)/50),Mathf.FloorToInt((Camps[0].z+HalfExtent)/50));
            Layout=new SectorLayout(map,entry,new Vector2Int(10,10),50,Blocked);
        }
        Vector3 SafeSite(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,-4750,4750);p.z=Mathf.Clamp(p.z,-4750,4750);
            if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+35)p.x=RiverX(p.z)+RiverWidth(p.z)*.5f+45;
            protectedSites.Add(p);
            var key=new Vector2Int(Mathf.FloorToInt(p.x/100),Mathf.FloorToInt(p.z/100));
            if(!siteBuckets.TryGetValue(key,out var bucket)){bucket=new List<Vector3>();siteBuckets.Add(key,bucket);}bucket.Add(p);
            return p;
        }
        Vector3 NewSite(Vector3 desired,bool camp=false,Vector3? squareCenter=null)
        {
            // Bounded radial search reserves walkable land clear of roads and old entrances.
            for(int ring=0;ring<=60;ring++)for(int step=0;step<(ring==0?1:24);step++)
            {
                float angle=step*Mathf.PI*2/24;
                var p=desired+new Vector3(Mathf.Cos(angle)*ring*10,0,Mathf.Sin(angle)*ring*10);
                if(Mathf.Abs(p.x)>4750||Mathf.Abs(p.z)>4750||River(p)||RoadClearance(p)<8||SiteProtected(p,20))continue;
                if(squareCenter.HasValue&&(Mathf.Abs(p.x-squareCenter.Value.x)>500||Mathf.Abs(p.z-squareCenter.Value.z)>500))continue;
                if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+35)continue;
                if(camp&&!CampFootprintClear(p))continue;
                return SafeSite(p);
            }
            throw new InvalidOperationException("No clear overworld site near "+desired);
        }
        bool CampFootprintClear(Vector3 threshold)
        {
            // The facade sits north of the threshold. Reserve its complete shell,
            // canopy and a small shoulder rather than checking only the doorway.
            for(float x=-7;x<=7;x+=2)for(float z=0;z<=12;z+=2)
            {
                var p=threshold+new Vector3(x,0,z);
                if(River(p)||RoadClearance(p)<2)return false;
            }
            return true;
        }
        public bool SiteProtected(Vector3 p,float extra=0)
        {
            float radius=12+extra;int minX=Mathf.FloorToInt((p.x-radius)/100),maxX=Mathf.FloorToInt((p.x+radius)/100);
            int minZ=Mathf.FloorToInt((p.z-radius)/100),maxZ=Mathf.FloorToInt((p.z+radius)/100);
            for(int x=minX;x<=maxX;x++)for(int z=minZ;z<=maxZ;z++)
                if(siteBuckets.TryGetValue(new Vector2Int(x,z),out var bucket))foreach(var site in bucket)if((site-p).sqrMagnitude<radius*radius)return true;
            return false;
        }
        public ChunkData GetChunk(int x,int z)
        {
            var key=new Vector2Int(x,z);if(cache.TryGetValue(key,out var data))return data;
            data=new ChunkData();var rng=new System.Random(unchecked(Seed+x*73856093^z*19349663));
            float left=x*ChunkSize,bottom=z*ChunkSize;
            // Rejection packing makes buildings independent of the 100m streaming grid.
            // A two metre margin at chunk borders prevents neighbouring cache overlap.
            for(int attempt=0;attempt<1800&&data.Buildings.Count<48;attempt++)
            {
                var p=new Vector3(left+8+(float)rng.NextDouble()*84,0,bottom+8+(float)rng.NextDouble()*84);
                int district=District(p);if(district==4)continue;
                bool warehouse=district==2&&rng.NextDouble()<.09;
                float w=warehouse?24+(float)rng.NextDouble()*15:8+(float)rng.NextDouble()*14;
                float h=warehouse?19+(float)rng.NextDouble()*17:8+(float)rng.NextDouble()*14;
                var rect=new Rect(p.x-w*.5f,p.z-h*.5f,w,h);
                if(rect.xMin<left+2||rect.xMax>left+98||rect.yMin<bottom+2||rect.yMax>bottom+98||Mathf.Abs(p.x)>4960||Mathf.Abs(p.z)>4960)continue;
                // Keep coarse navigation cell centres free for the shared 50m graph.
                bool overlaps=false;
                foreach(var other in data.Buildings)if(new Rect(other.xMin-1.5f,other.yMin-1.5f,other.width+3,other.height+3).Overlaps(rect)){overlaps=true;break;}
                // Narrow pedestrian passages connect every 50m navigation centre.
                // Their clear width is independent of the vehicle-road layout.
                for(float spine=25;spine<100;spine+=50)
                    if((rect.xMin<left+spine+1.4f&&rect.xMax>left+spine-1.4f)||(rect.yMin<bottom+spine+1.4f&&rect.yMax>bottom+spine-1.4f))overlaps=true;
                if(!overlaps&&ParcelClear(rect))data.Buildings.Add(rect);
            }
            for(int i=0;i<140;i++){var p=new Vector3(left+10+(float)rng.NextDouble()*80,0,bottom+10+(float)rng.NextDouble()*80);if(RoadClearance(p)>1.5f&&RoadClearance(p)<4&&!River(p)&&!SiteProtected(p,5)&&Mathf.Abs(Mathf.Repeat(p.x,50)-25)>5&&Mathf.Abs(Mathf.Repeat(p.z,50)-25)>7){bool clear=true;foreach(var b in data.Buildings)if(new Rect(b.xMin-2,b.yMin-4,b.width+4,b.height+8).Contains(new Vector2(p.x,p.z))){clear=false;break;}if(clear){foreach(var car in data.Cars)if((car-p).sqrMagnitude<30){clear=false;break;}if(clear)data.Cars.Add(p);}}}
            // Separate RNG preserves existing buildings and cars; navigation spines stay clear.
            var forest=new System.Random(unchecked(Seed+x*92821+z*68917));
            for(int i=0;i<650;i++){
                var p=new Vector3(left+3+(float)forest.NextDouble()*94,0,bottom+3+(float)forest.NextDouble()*94);
                bool park=Park(p);if(!park&&i>=45)continue;
                if(River(p)||RoadClearance(p)<2.5f||SiteProtected(p,8)||Mathf.Abs(p.x)>4990||Mathf.Abs(p.z)>4990)continue;
                if(Mathf.Abs(Mathf.Repeat(p.x,50)-25)<4||Mathf.Abs(Mathf.Repeat(p.z,50)-25)<4)continue;
                bool occupied=false;foreach(var b in data.Buildings)if(new Rect(b.xMin-2,b.yMin-2,b.width+4,b.height+4).Contains(new Vector2(p.x,p.z))){occupied=true;break;}foreach(var car in data.Cars)if((car-p).sqrMagnitude<16)occupied=true;if(occupied)continue;
                bool near=false;foreach(var tree in data.Trees)if((tree-p).sqrMagnitude<12.25f){near=true;break;}
                if(!near)data.Trees.Add(p);
            }
            while(cache.Count>=49){var oldest=cacheOrder.Dequeue();cache.Remove(oldest);}cache.Add(key,data);cacheOrder.Enqueue(key);return data;
        }
        public int CachedChunkCount=>cache.Count;
        public bool Blocked(Vector3 p,float radius)
        {
            if(Mathf.Abs(p.x)+radius>=HalfExtent||Mathf.Abs(p.z)+radius>=HalfExtent)return true;
            if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+radius&&!Bridge(p.z))return true;
            int cx=Mathf.FloorToInt(p.x/100),cz=Mathf.FloorToInt(p.z/100);
            for(int x=Mathf.FloorToInt((p.x-radius-2)/100);x<=Mathf.FloorToInt((p.x+radius+2)/100);x++)for(int z=Mathf.FloorToInt((p.z-radius-3)/100);z<=Mathf.FloorToInt((p.z+radius+3)/100);z++)
            {
                var chunk=GetChunk(x,z);foreach(var b in chunk.Buildings)if(p.x+radius>b.xMin&&p.x-radius<b.xMax&&p.z+radius>b.yMin&&p.z-radius<b.yMax)return true;
                foreach(var tree in chunk.Trees){float dx=p.x-tree.x,dz=p.z-tree.z;if(dx*dx+dz*dz<(radius+.32f)*(radius+.32f))return true;}
                foreach(var car in chunk.Cars)if(Mathf.Abs(p.x-car.x)<radius+1&&Mathf.Abs(p.z-car.z)<radius+2.5f)return true;
            }
            return false;
        }
        public string RegionName(Vector3 p,bool ja)
        {if(!River(p)&&!Park(p)){int district=District(p);if(district==0)return ja?"崩壊した高層ビル街":"Fallen Downtown";if(district==1)return ja?"旧商店街":"Old Shopping Streets";if(district==2)return ja?"荒廃したモール地区":"Derelict Mall District";}if(River(p))return ja?"グレイウォーター河岸":"Graywater Riverfront";if(Park(p))return ja?"レッドウッド森林公園":"Redwood Forest Park";if(p.z<-1800)return ja?"南部工業地帯":"Southworks";if(p.x>1800)return ja?"旧市街・市庁舎地区":"Old Civic Quarter";return ja?"旧住宅地区":"Old Residential District";}
    }
}

