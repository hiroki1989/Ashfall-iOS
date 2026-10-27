using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public sealed partial class OverworldGeography
    {
        public readonly struct RoadSegment
        {
            public readonly Vector3 A,B;
            public readonly float Width;
            public readonly bool BridgeDeck;
            public RoadSegment(Vector3 a,Vector3 b,float width,bool bridge=false){A=a;B=b;Width=width;BridgeDeck=bridge;}
        }
        readonly Dictionary<Vector2Int,List<RoadSegment>> roadBuckets=new Dictionary<Vector2Int,List<RoadSegment>>();
        static readonly RoadSegment[] noRoads=new RoadSegment[0];
        public int RoadSegmentCount {get;private set;}
        public IReadOnlyList<RoadSegment> GetRoadSegmentsForChunk(int x,int z)
            =>roadBuckets.TryGetValue(new Vector2Int(x,z),out var list)?(IReadOnlyList<RoadSegment>)list:noRoads;
        public float RoadDistance(Vector3 p)=>RoadMeasure(p,false);
        public float RoadClearance(Vector3 p)=>RoadMeasure(p,true);
        float RoadMeasure(Vector3 p,bool edge)
        {
            // Buckets include the complete road shoulder. A 100m neighbouring ring
            // supplies useful verge distance without any full-world graph traversal.
            float best=200;int cx=Mathf.FloorToInt(p.x/100),cz=Mathf.FloorToInt(p.z/100);
            for(int x=cx-1;x<=cx+1;x++)for(int z=cz-1;z<=cz+1;z++)
            {
                if(!roadBuckets.TryGetValue(new Vector2Int(x,z),out var roads))continue;
                foreach(var road in roads)
                {
                    float dx=road.B.x-road.A.x,dz=road.B.z-road.A.z;
                    float t=Mathf.Clamp01(((p.x-road.A.x)*dx+(p.z-road.A.z)*dz)/(dx*dx+dz*dz));
                    float ox=p.x-road.A.x-t*dx,oz=p.z-road.A.z-t*dz;
                    best=Mathf.Min(best,Mathf.Sqrt(ox*ox+oz*oz)-(edge?road.Width*.5f:0));
                }
            }
            return best;
        }
        void RegisterRoad(Vector3 a,Vector3 b,float width,bool bridge=false)
        {
            if((b-a).sqrMagnitude<.01f)return;
            var road=new RoadSegment(a,b,width,bridge);float margin=width*.5f+5;
            int left=Mathf.FloorToInt((Mathf.Min(a.x,b.x)-margin)/100),right=Mathf.FloorToInt((Mathf.Max(a.x,b.x)+margin)/100);
            int bottom=Mathf.FloorToInt((Mathf.Min(a.z,b.z)-margin)/100),top=Mathf.FloorToInt((Mathf.Max(a.z,b.z)+margin)/100);
            for(int x=left;x<=right;x++)for(int z=bottom;z<=top;z++)
            {
                var key=new Vector2Int(x,z);if(!roadBuckets.TryGetValue(key,out var roads)){roads=new List<RoadSegment>();roadBuckets.Add(key,roads);}roads.Add(road);
            }
            RoadSegmentCount++;
        }
        Vector3 BankPoint(float z,int side)=>new Vector3(RiverX(z)+side*(RiverWidth(z)*.5f+120+25*Mathf.Sin(z*.0013f)),0,z);
        void JoinBank(Vector3 end,float width)
        {
            int side=end.x<RiverX(end.z)?-1:1;
            // Move directly away from water, then terminate exactly on the shared
            // riverbank polyline (20m steps), rather than merely near its curve.
            float t=Mathf.Clamp((end.z+4900)/20,0,489.999f);int step=Mathf.FloorToInt(t);
            var bank=Vector3.Lerp(BankPoint(-4900+step*20,side),BankPoint(-4900+(step+1)*20,side),t-step);
            float length=Vector3.Distance(end,bank);int count=Mathf.Max(1,Mathf.CeilToInt(length/20));
            for(int i=0;i<count;i++)RegisterRoad(Vector3.Lerp(end,bank,i/(float)count),Vector3.Lerp(end,bank,(i+1)/(float)count),width);
        }
        void RoadCurve(Func<float,Vector3> point,float width,int steps,bool allowPark=false)
        {
            var a=point(0);bool previousWater=false;
            for(int i=1;i<=steps;i++)
            {
                var b=point(i/(float)steps);var mid=(a+b)*.5f;
                bool water=River(a)||River(mid)||River(b);
                bool valid=Mathf.Abs(mid.x)<4920&&Mathf.Abs(mid.z)<4920&&!water&&(allowPark||!Park(mid));
                if(valid)RegisterRoad(a,b,width);
                // Truncated major roads become T junctions on a bank boulevard;
                // that boulevard reaches all nine bridges on the same river side.
                if(allowPark&&width>=23)
                {
                    if(water&&!previousWater&&!River(a))JoinBank(a,width);
                    if(!water&&previousWater)JoinBank(a,width);
                }
                previousWater=water;a=b;
            }
        }
        void BuildRoadNetwork()
        {
            // Two riverbank boulevards, with distinct bank-following bends.
            for(int side=-1;side<=1;side+=2)
            {
                int bank=side;
                RoadCurve(t=>BankPoint(-4900+t*9800,bank),19,490,true);
            }
            // Finite diagonal routes follow the settled basin rather than an infinite lattice.
            var routes=new[]{
                new[]{new Vector3(-3100,0,-4200),new Vector3(-2200,0,-2600),new Vector3(-1100,0,-1050),new Vector3(80,0,100),new Vector3(1700,0,1800),new Vector3(3200,0,3100)},
                new[]{new Vector3(-3000,0,2700),new Vector3(-1900,0,1750),new Vector3(-650,0,450),new Vector3(850,0,-900),new Vector3(2300,0,-2500),new Vector3(4100,0,-4000)},
                new[]{new Vector3(-2700,0,-800),new Vector3(-1700,0,-300),new Vector3(-500,0,250),new Vector3(1600,0,650),new Vector3(3100,0,1200),new Vector3(4500,0,2100)}
            };
            foreach(var route in routes)for(int i=1;i<route.Length;i++)
            {
                var a=route[i-1];var b=route[i];var normal=Vector3.Cross(Vector3.up,(b-a).normalized);
                RoadCurve(t=>Vector3.Lerp(a,b,t)+normal*(Mathf.Sin(t*Mathf.PI)*55),26,Mathf.CeilToInt(Vector3.Distance(a,b)/30),true);
            }
            // Boroughs are finite, rotated neighbourhoods with loops and radial links.
            // Centres and radii are deliberately irregular and never repeated on a world grid.
            var centres=new[]{new Vector3(-2300,0,-3300),new Vector3(-1350,0,-2100),new Vector3(-1700,0,-650),new Vector3(-1350,0,1050),new Vector3(-2350,0,2300),new Vector3(-200,0,2400),new Vector3(1500,0,2500),new Vector3(3000,0,2600),new Vector3(3650,0,650),new Vector3(2300,0,-650),new Vector3(1400,0,-1950),new Vector3(3000,0,-2800),new Vector3(800,0,-3600),new Vector3(4200,0,-1400),new Vector3(700,0,400)};
            for(int borough=0;borough<centres.Length;borough++)
            {
                var centre=centres[borough];float angle=borough*.73f+.21f;
                var u=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var v=new Vector3(-u.z,0,u.x);
                float rx=390+(borough*113%300),rz=300+(borough*79%270);
                Vector3 Local(float x,float z)=>centre+u*x+v*z;
                RoadCurve(t=>{float a=t*Mathf.PI*2;float irregular=1+.07f*Mathf.Sin(a*3+borough);return Local(Mathf.Cos(a)*rx*irregular,Mathf.Sin(a)*rz*irregular);},14,110);
                for(int radial=0;radial<5;radial++)
                {
                    float a=angle+radial*Mathf.PI*.4f+.12f*Mathf.Sin(radial+borough);var end=Local(Mathf.Cos(a)*rx,Mathf.Sin(a)*rz);
                    RoadCurve(t=>Vector3.Lerp(centre,end,t)+v*(Mathf.Sin(t*Mathf.PI)*24),11,26);
                }
                // Unequal short residential lanes occupy only these borough footprints.
                // Each is terminated or joined at a loop; there are no global stripes.
                float offset=-rx*.72f;
                for(int lane=0;lane<9&&offset<rx*.8f;lane++)
                {
                    float along=offset;float extent=rz*Mathf.Sqrt(Mathf.Max(0,1-along*along/(rx*rx)))*.94f;
                    float skew=35*Mathf.Sin(borough+lane*1.7f);
                    RoadCurve(t=>Local(along+Mathf.Sin(t*Mathf.PI)*skew,-extent+t*extent*2),7.5f,35);
                    offset+=85+(borough*31+lane*47)%90;
                }
                // A shorter transverse street family changes scale and direction per borough.
                for(int lane=0;lane<3;lane++)
                {
                    float cross=(-.48f+lane*.43f)*rz;
                    float extent=rx*Mathf.Sqrt(1-cross*cross/(rz*rz))*.93f;
                    RoadCurve(t=>Local(-extent+t*extent*2,cross+28*Mathf.Sin(t*Mathf.PI*1.4f+borough)),8.5f,35);
                }
            }
            // Bridge approaches join both bank boulevards. Wide decks also reserve
            // the +/-25m navigation rows all the way onto dry land.
            for(int bridge=-4;bridge<=4;bridge++)
            {
                float z=bridge*1000,x=RiverX(z),half=RiverWidth(z)*.5f+80;
                RegisterRoad(new Vector3(x-half,0,z),new Vector3(x+half,0,z),BridgeHalfLength*2,true);
                for(int side=-1;side<=1;side+=2)
                {
                    int bank=side;
                    JoinBank(new Vector3(x+bank*half,0,z),23);
                    RoadCurve(t=>new Vector3(x+bank*(half+t*650),0,z+bank*95*Mathf.Sin(t*Mathf.PI*.5f)),23,35,true);
                }
            }
        }
    }
}
