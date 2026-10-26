using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public sealed class OverworldGeography
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
        readonly Dictionary<Vector2Int,ChunkData> cache=new Dictionary<Vector2Int,ChunkData>();
        readonly Queue<Vector2Int> cacheOrder=new Queue<Vector2Int>();
        public sealed class ChunkData
        {
            public readonly List<Rect> Buildings=new List<Rect>();
            public readonly List<Vector3> Trees=new List<Vector3>(),Cars=new List<Vector3>();
        }
        public int District(Vector3 p)=>Park(p)?4:Mathf.Abs(Mathf.FloorToInt(p.x/250)+Mathf.FloorToInt(p.z/300)*3)%4;
        public float RiverX(float z)=>325+180*Mathf.Sin(z*.0008f)+65*Mathf.Sin(z*.0021f);
        public bool Bridge(float z){for(int i=-4;i<=4;i++)if(Mathf.Abs(z-i*1000)<45)return true;return false;}
        public bool River(Vector3 p)=>Mathf.Abs(p.x-RiverX(p.z))<30;
        public bool PocketPark(Vector3 p)=>Mathf.Abs(Mathf.FloorToInt(p.x/250)+Mathf.FloorToInt(p.z/250)*3)%7==0&&Mathf.Abs(Mathf.Repeat(p.x,250)-150)<13&&Mathf.Abs(Mathf.Repeat(p.z,250)-150)<13;
        public bool Park(Vector3 p)=>PocketPark(p)||p.x<-1700||p.z>3000||Mathf.Sin(p.x*.0011f)*Mathf.Cos(p.z*.0013f)>.61f;
        // Shared by rendering and placement: narrow winding local streets join wider arterials.
        // District-scale bends keep neighbouring streets coherent while each lane has
        // its own smaller meander. Rendering, map sampling and parcel clearance share these.
        public float StreetX(int street,float z)=>street*100
            +13*Mathf.Sin(z*.0019f+street*.17f)
            +7*Mathf.Sin(z*.0061f+street*.83f)
            +3*Mathf.Sin(z*.018f+street*1.37f);
        public float StreetZ(int street,float x)=>street*100
            +14*Mathf.Sin(x*.0017f-street*.19f)
            +6*Mathf.Sin(x*.0058f+street*.71f)
            +3*Mathf.Sin(x*.017f-street*1.11f);
        public float StreetWidth(int street)=>street%3==0?16:9;
        public bool StreetSegmentOpen(int street,int segment,bool northSouth)
        {
            if(street%3==0)return true;
            // Break up residential lanes into cul-de-sacs and ruined blocks while
            // continuous arterials retain a connected route through the city.
            int block=Mathf.FloorToInt(segment/2f);
            int hash=unchecked(street*73856093 ^ block*19349663 ^ (northSouth?83492791:297121507));
            return (hash&int.MaxValue)%7!=0;
        }
        public float RoadDistance(Vector3 p)
        {
            float distance=float.MaxValue;int cx=Mathf.RoundToInt(p.x/100),cz=Mathf.RoundToInt(p.z/100);
            for(int i=-1;i<=1;i++){if(StreetSegmentOpen(cx+i,Mathf.FloorToInt(p.z/100),true))distance=Mathf.Min(distance,Mathf.Abs(p.x-StreetX(cx+i,p.z)));if(StreetSegmentOpen(cz+i,Mathf.FloorToInt(p.x/100),false))distance=Mathf.Min(distance,Mathf.Abs(p.z-StreetZ(cz+i,p.x)));}
            return distance;
        }
        public float RoadClearance(Vector3 p)
        {
            float distance=float.MaxValue;int cx=Mathf.RoundToInt(p.x/100),cz=Mathf.RoundToInt(p.z/100);
            for(int i=-1;i<=1;i++){if(StreetSegmentOpen(cx+i,Mathf.FloorToInt(p.z/100),true))distance=Mathf.Min(distance,Mathf.Abs(p.x-StreetX(cx+i,p.z))-StreetWidth(cx+i)*.5f);if(StreetSegmentOpen(cz+i,Mathf.FloorToInt(p.x/100),false))distance=Mathf.Min(distance,Mathf.Abs(p.z-StreetZ(cz+i,p.x))-StreetWidth(cz+i)*.5f);}
            return distance;
        }
        bool ParcelClear(Rect rect)
        {
            // Sample the whole footprint so curves and river banks cannot cut through a house.
            for(float x=rect.xMin;x<=rect.xMax+2;x+=2)for(float z=rect.yMin;z<=rect.yMax+2;z+=2){var p=new Vector3(Mathf.Min(x,rect.xMax),0,Mathf.Min(z,rect.yMax));if(River(p)||RoadClearance(p)<1.8f||SiteProtected(p,3)||Park(p))return false;}
            return true;
        }
        public OverworldGeography()
        {
            var random=new System.Random(Seed);var map=new bool[Columns,Rows];
            for(int x=0;x<Columns;x++)for(int y=0;y<Rows;y++){var p=new Vector3((x+.5f)*50-HalfExtent,0,(y+.5f)*50-HalfExtent);map[x,y]=!River(p)||Bridge(p.z);}
            // Every bridge crossing occupies an entire coarse navigation row.
            for(int y=0;y<Rows;y++)if(Bridge((y+.5f)*50-HalfExtent))for(int x=0;x<Columns;x++)map[x,y]=true;
            Camps=new Vector3[7];for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7+.28f;Camps[i]=SafeSite(new Vector3(Mathf.Cos(a)*3600,.65f,Mathf.Sin(a)*3400));}
            Entrances=new Vector3[100];Entrances[0]=SafeSite(Camps[0]+new Vector3(-30,0,0));
            for(int i=1;i<100;i++)
            {Vector3 chosen=Vector3.zero;float best=-1;for(int trial=0;trial<40;trial++){var p=new Vector3(-4700+(float)random.NextDouble()*9400,.65f,-4700+(float)random.NextDouble()*9400);float near=float.MaxValue;foreach(var q in protectedSites)near=Mathf.Min(near,(p-q).sqrMagnitude);if(near>best){best=near;chosen=p;}}Entrances[i]=SafeSite(chosen);}
            Chests=new Vector3[180];for(int i=0;i<36;i++)Chests[i]=SafeSite(new Vector3(-4300+i%6*1700+(float)random.NextDouble()*250,.65f,-4300+i/6*1700+(float)random.NextDouble()*250));
            // Preserve the original chest IDs/cooldowns; add caches behind ruined lots,
            // in mall service alleys, and on park paths. Do not reserve new clearings:
            // these use existing 50m pedestrian spines, preserving every site and parcel.
            for(int i=36;i<Chests.Length;i++)
            {
                int cell=i-36,cx=-45+cell%12*8,cz=-45+cell/12*8;
                var chunk=GetChunk(cx,cz);
                Vector3 chosen=new Vector3(cx*100+25,.65f,cz*100+25);
                foreach(var building in chunk.Buildings)
                {
                    float localZ=Mathf.Repeat(building.center.y,50);
                    if(localZ<18||localZ>20)continue;
                    var candidate=new Vector3(building.center.x,.65f,cz*100+(building.center.y-cz*100<50?25:75));
                    if(Mathf.Abs(candidate.x-RiverX(candidate.z))<100)continue;
                    chosen=candidate;break;
                }
                if(Mathf.Abs(chosen.x-RiverX(chosen.z))<100)
                    chosen=new Vector3((cx+2)*100+25,.65f,cz*100+25);
                Chests[i]=chosen;
            }
            var entry=new Vector2Int(Mathf.FloorToInt((Camps[0].x+HalfExtent)/50),Mathf.FloorToInt((Camps[0].z+HalfExtent)/50));
            Layout=new SectorLayout(map,entry,new Vector2Int(10,10),50,Blocked);
        }
        Vector3 SafeSite(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,-4750,4750);p.z=Mathf.Clamp(p.z,-4750,4750);
            if(Mathf.Abs(p.x-RiverX(p.z))<100)p.x=RiverX(p.z)+110;
            protectedSites.Add(p);return p;
        }
        public bool SiteProtected(Vector3 p,float extra=0){foreach(var site in protectedSites)if((site-p).sqrMagnitude<(12+extra)*(12+extra))return true;return false;}
        public ChunkData GetChunk(int x,int z)
        {
            var key=new Vector2Int(x,z);if(cache.TryGetValue(key,out var data))return data;
            data=new ChunkData();var rng=new System.Random(unchecked(Seed+x*73856093^z*19349663));
            float left=x*ChunkSize,bottom=z*ChunkSize;
            for(int lotX=0;lotX<8;lotX++)for(int lotZ=0;lotZ<8;lotZ++)
            {
                var p=new Vector3(left+6.25f+lotX*12.5f,0,bottom+6.25f+lotZ*12.5f);
                if(Mathf.Abs(p.x)>4985||Mathf.Abs(p.z)>4985)continue;
                float w=10.5f+(float)rng.NextDouble()*.4f,h=10.5f+(float)rng.NextDouble()*.4f;
                // At least 2m between neighbouring parcels, including chunk seams. The
                // 50m navigation spines remain open even at the largest footprint.
                var rect=new Rect(p.x-w*.5f,p.z-h*.5f,w,h);
                if(ParcelClear(rect))data.Buildings.Add(rect);
            }
            for(int i=0;i<140;i++){var p=new Vector3(left+10+(float)rng.NextDouble()*80,0,bottom+10+(float)rng.NextDouble()*80);if(RoadClearance(p)>1.5f&&RoadClearance(p)<4&&!River(p)&&!SiteProtected(p,5)&&Mathf.Abs(Mathf.Repeat(p.x,50)-25)>5&&Mathf.Abs(Mathf.Repeat(p.z,50)-25)>7){bool clear=true;foreach(var b in data.Buildings)if(new Rect(b.xMin-2,b.yMin-4,b.width+4,b.height+8).Contains(new Vector2(p.x,p.z))){clear=false;break;}if(clear){foreach(var car in data.Cars)if((car-p).sqrMagnitude<30){clear=false;break;}if(clear)data.Cars.Add(p);}}}
            // Separate RNG preserves existing buildings and cars; navigation spines stay clear.
            var forest=new System.Random(unchecked(Seed+x*92821+z*68917));
            for(int i=0;i<650;i++){
                var p=new Vector3(left+3+(float)forest.NextDouble()*94,0,bottom+3+(float)forest.NextDouble()*94);
                bool park=Park(p);if(!park&&i>260)continue;
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
            if(Mathf.Abs(p.x-RiverX(p.z))<30+radius&&!Bridge(p.z))return true;
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
        {if(!River(p)&&!Park(p)){int district=District(p);if(district==0)return ja?"崩壊した高層ビル街":"Fallen Downtown";if(district==1)return ja?"旧商店街":"Old Shopping Streets";if(district==2)return ja?"荒廃したモール地区":"Derelict Mall District";}if(River(p))return ja?"グレイウォーター河岸":"Graywater Riverfront";if(Park(p))return ja?"レッドウッド森林公園":"Redwood Forest Park";if(p.z<-1800)return ja?"南部工業地帯":"Southworks";if(p.x>1800)return ja?"旧市街・市庁舎地区":"Old Civic Quarter";return ja?"アッシュフォール住宅地区":"Ashfall Neighborhoods";}
    }
}

