using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Collision, navigation and rendering share this connected irregular chamber graph.
    public sealed class SectorLayout
    {
        // Legacy maximum extents; consumers should use this sector's Columns / Rows.
        public const int Width=67, Depth=67;
        public readonly int Columns, Rows;
        public const float Cell=2.2f;
        public readonly bool[,] open;
        public readonly List<Vector2Int> chambers=new List<Vector2Int>();
        public readonly List<Rect> walls=new List<Rect>();
        public readonly List<Vector3> covers=new List<Vector3>();
        readonly int[,] distances;
        readonly bool[,] entryZone;
        readonly bool[,] explored, visible;
        Vector2Int discoveryCell=new Vector2Int(-1,-1);
        float discoveryRadius=-1;
        static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        int entryIndex, exitIndex;
        public int EntryChamberIndex=>entryIndex;
        public int ExitChamberIndex=>exitIndex;
        public readonly int ExitPathCells;
        public Vector3 Entry=>Center(chambers[entryIndex]);
        public Vector3 Exit=>Center(chambers[exitIndex]);
        public int WalkableCells { get {int count=0;foreach(bool value in open)if(value)count++;return count;} }
        public SectorLayout(System.Random random,int theme=0)
        {
            // Scatter rooms instead of keeping a repeated row/column lattice.
            int variant=random.Next(4);
            Columns=variant==0?67:variant==1?29:variant==2?49:41;
            Rows=variant==0?29:variant==1?67:variant==2?41:49;
            open=new bool[Columns,Rows];distances=new int[Columns,Rows];entryZone=new bool[Columns,Rows];
            explored=new bool[Columns,Rows];visible=new bool[Columns,Rows];
            int roomCount=random.Next(15,21);
            for(int i=0;i<roomCount;i++)
            {
                Vector2Int best=new Vector2Int();int bestSeparation=-1;
                // Best of several random sites keeps room centres separated without equal spacing.
                for(int trial=0;trial<32;trial++)
                {
                    var c=new Vector2Int(random.Next(6,Columns-6),random.Next(6,Rows-6));int separation=int.MaxValue;
                    foreach(var other in chambers){int dx=c.x-other.x,dy=c.y-other.y;separation=Math.Min(separation,dx*dx+dy*dy);}
                    if(separation>bestSeparation){best=c;bestSeparation=separation;}
                }
                chambers.Add(best);Room(best,random.Next(3,6),random.Next(3,6),(random.Next(5)+theme)%5);
            }
            var perimeter=new List<int>();
            for(int i=0;i<chambers.Count;i++){var c=chambers[i];if(c.x<Columns*.25f||c.x>Columns*.75f||c.y<Rows*.25f||c.y>Rows*.75f)perimeter.Add(i);}
            entryIndex=perimeter[random.Next(perimeter.Count)];
            // A minimum spanning tree guarantees connectivity; a few extra links create loops.
            var links=new List<Vector2Int>();var joined=new bool[chambers.Count];joined[0]=true;
            for(int count=1;count<chambers.Count;count++)
            {
                int from=-1,to=-1,best=int.MaxValue;
                for(int i=0;i<chambers.Count;i++)if(joined[i])for(int j=0;j<chambers.Count;j++)if(!joined[j])
                {int score=Math.Abs(chambers[i].x-chambers[j].x)+Math.Abs(chambers[i].y-chambers[j].y);if(score<best){best=score;from=i;to=j;}}
                joined[to]=true;links.Add(new Vector2Int(from,to));
            }
            var extra=new List<Vector2Int>();
            for(int i=0;i<chambers.Count;i++)for(int j=i+1;j<chambers.Count;j++)
            {bool used=false;foreach(var link in links)if((link.x==i&&link.y==j)||(link.x==j&&link.y==i)){used=true;break;}if(!used)extra.Add(new Vector2Int(i,j));}
            // Prefer nearby alternatives, so loops do not flatten the entire sector into highways.
            extra.Sort((a,b)=>LinkLength(a).CompareTo(LinkLength(b)));
            int loops=random.Next(2,5);for(int i=0;i<loops&&extra.Count>0;i++){int chosen=random.Next(Math.Min(extra.Count,8));links.Add(extra[chosen]);extra.RemoveAt(chosen);}
            foreach(var link in links)BentCorridor(chambers[link.x],chambers[link.y],random);
            // Short connected side galleries and alcoves provide optional dead ends.
            for(int i=0;i<random.Next(4,8);i++)
            {
                var c=chambers[random.Next(chambers.Count)];var d=Directions[random.Next(Directions.Length)];
                int length=random.Next(5,10);var end=new Vector2Int(c.x+d.x*length,c.y+d.y*length);end.x=Math.Max(3,Math.Min(Columns-4,end.x));end.y=Math.Max(3,Math.Min(Rows-4,end.y));
                Corridor(c,end);Room(end,2,2,random.Next(2));
            }
            // A landing protects the first approach, including its immediate corridors.
            for(int x=1;x<Columns-1;x++)for(int y=1;y<Rows-1;y++)entryZone[x,y]=(Center(new Vector2Int(x,y))-Entry).sqrMagnitude<225;
            BuildDistanceField(Entry);
            int far=0;foreach(int i in perimeter){var c=chambers[i];far=Math.Max(far,distances[c.x,c.y]);}
            // Randomize among distant end rooms: goals are reachable and never on the landing.
            var exits=new List<int>();
            foreach(int i in perimeter)
            {
                var c=chambers[i];int distance=distances[c.x,c.y];
                if(i!=entryIndex&&distance>=Math.Max(18,Mathf.CeilToInt(far*.68f))&&(Center(c)-Entry).sqrMagnitude>=625)exits.Add(i);
            }
            if(exits.Count==0)foreach(int i in perimeter){var c=chambers[i];if(i!=entryIndex&&distances[c.x,c.y]==far)exits.Add(i);}
            exitIndex=exits[random.Next(exits.Count)];
            foreach(var c in chambers)
            {
                if(c==chambers[entryIndex]||c==chambers[exitIndex])continue;
                var p=c+new Vector2Int(random.Next(2)==0?-2:2,random.Next(2)==0?-1:1);
                if(!IsOpen(p.x,p.y))continue;open[p.x,p.y]=false;
                if(!Connected()){open[p.x,p.y]=true;continue;}covers.Add(Center(p));
            }
            // Merge equal wall runs vertically: fewer render objects in the larger sector.
            var active=new Dictionary<Vector2Int,int>();
            for(int y=0;y<Rows;y++)
            {
                var next=new Dictionary<Vector2Int,int>();
                for(int x=0;x<Columns;)
                {
                    if(open[x,y]||IsCover(x,y)){x++;continue;}int start=x;while(x<Columns&&!open[x,y]&&!IsCover(x,y))x++;
                    var key=new Vector2Int(start,x-start);int index;
                    if(active.TryGetValue(key,out index)){var r=walls[index];r.height+=Cell;walls[index]=r;}
                    else {index=walls.Count;walls.Add(new Rect((start-Columns*.5f)*Cell,(y-Rows*.5f)*Cell,(x-start)*Cell,Cell));}next[key]=index;
                }
                active=next;
            }
            BuildDistanceField(Entry);
            var exitCell=chambers[exitIndex];ExitPathCells=distances[exitCell.x,exitCell.y];
        }
        int LinkLength(Vector2Int link){var a=chambers[link.x];var b=chambers[link.y];return Math.Abs(a.x-b.x)+Math.Abs(a.y-b.y);}
        void BentCorridor(Vector2Int a,Vector2Int b,System.Random random)
        {
            if(random.Next(2)==0)
            {int mid=(a.x+b.x)/2+random.Next(-2,3);var first=new Vector2Int(mid,a.y);var second=new Vector2Int(mid,b.y);Corridor(a,first);Corridor(first,second);Corridor(second,b);}
            else
            {int mid=(a.y+b.y)/2+random.Next(-2,3);var first=new Vector2Int(a.x,mid);var second=new Vector2Int(b.x,mid);Corridor(a,first);Corridor(first,second);Corridor(second,b);}
        }
        public bool IsExplored(int x,int y)=>IsOpen(x,y)&&explored[x,y];
        public bool IsVisible(int x,int y)=>IsOpen(x,y)&&visible[x,y];
        public bool IsVisible(Vector3 position){var c=ToCell(position);return IsVisible(c.x,c.y);}
        // Radius is measured in cells; cache a snapped origin to avoid a full scan every frame.
        public bool Discover(Vector3 position,float radius=5)
        {
            var c=ToCell(position);if(!IsOpen(c.x,c.y)||(c==discoveryCell&&radius==discoveryRadius))return false;
            discoveryCell=c;discoveryRadius=radius;Array.Clear(visible,0,visible.Length);bool changed=false;
            int range=Mathf.CeilToInt(radius);var origin=Center(c);
            for(int x=Math.Max(0,c.x-range);x<=Math.Min(Columns-1,c.x+range);x++)for(int y=Math.Max(0,c.y-range);y<=Math.Min(Rows-1,c.y+range);y++)
            {
                int dx=x-c.x,dy=y-c.y;if(dx*dx+dy*dy>radius*radius||!IsOpen(x,y)||!ClearLine(origin,Center(new Vector2Int(x,y))))continue;
                visible[x,y]=true;if(!explored[x,y]){explored[x,y]=true;changed=true;}
            }
            return changed;
        }
        void Room(Vector2Int c,int rx,int ry,int shape)
        {
            for(int x=-rx;x<=rx;x++)for(int y=-ry;y<=ry;y++)
            {
                bool fill=shape==0?true:shape==1?(x*x/(float)(rx*rx)+y*y/(float)(ry*ry)<=1.2f):shape==2?(x<=0||y<=0):shape==3?(Math.Abs(x)<=2||Math.Abs(y)<=2):(Math.Abs(x)<rx||Math.Abs(y)<ry-1);
                if(fill)Carve(c.x+x,c.y+y,1,1);
            }
        }
        public bool SpawnSafe(Vector3 p){var c=ToCell(p);return IsOpen(c.x,c.y)&&!entryZone[c.x,c.y]&&(p-Entry).sqrMagnitude>=144&&!Blocked(p,.65f);}
        bool IsCover(int x,int y){foreach(var p in covers)if(ToCell(p)==new Vector2Int(x,y))return true;return false;}
        void Carve(int x,int y,int w,int d){for(int a=x;a<x+w;a++)for(int b=y;b<y+d;b++)if(a>0&&a<Columns-1&&b>0&&b<Rows-1)open[a,b]=true;}
        void Corridor(Vector2Int a,Vector2Int b){Carve(Math.Min(a.x,b.x)-1,Math.Min(a.y,b.y)-1,Math.Abs(a.x-b.x)+3,Math.Abs(a.y-b.y)+3);}
        public Vector3 Center(Vector2Int c)=>new Vector3((c.x+.5f-Columns*.5f)*Cell,.65f,(c.y+.5f-Rows*.5f)*Cell);
        public Vector2Int ToCell(Vector3 p)=>new Vector2Int(Mathf.FloorToInt(p.x/Cell+Columns*.5f),Mathf.FloorToInt(p.z/Cell+Rows*.5f));
        public bool IsOpen(int x,int y)=>x>=0&&y>=0&&x<Columns&&y<Rows&&open[x,y];
        public bool Blocked(Vector3 p,float radius){var a=ToCell(p-new Vector3(radius,0,radius));var b=ToCell(p+new Vector3(radius,0,radius));for(int x=a.x;x<=b.x;x++)for(int y=a.y;y<=b.y;y++)if(!IsOpen(x,y))return true;return false;}
        public bool Connected(){BuildDistanceField(Entry);for(int x=0;x<Columns;x++)for(int y=0;y<Rows;y++)if(open[x,y]&&distances[x,y]<0)return false;return true;}
        public void BuildDistanceField(Vector3 target){for(int x=0;x<Columns;x++)for(int y=0;y<Rows;y++)distances[x,y]=-1;var c=ToCell(target);if(!IsOpen(c.x,c.y))return;var queue=new Queue<Vector2Int>();queue.Enqueue(c);distances[c.x,c.y]=0;while(queue.Count>0){var p=queue.Dequeue();foreach(var d in Directions){var n=p+d;if(IsOpen(n.x,n.y)&&distances[n.x,n.y]<0){distances[n.x,n.y]=distances[p.x,p.y]+1;queue.Enqueue(n);}}}}
        public Vector3 Chase(Vector3 from,Vector3 target){var c=ToCell(from);if(!IsOpen(c.x,c.y))return Vector3.zero;int best=distances[c.x,c.y];Vector2Int next=c;foreach(var d in Directions){var n=c+d;if(IsOpen(n.x,n.y)&&distances[n.x,n.y]>=0&&(best<0||distances[n.x,n.y]<best)){next=n;best=distances[n.x,n.y];}}Vector3 goal=next==c?target:Center(next);goal.y=from.y;return(goal-from).normalized;}
        public bool ClearLine(Vector3 a,Vector3 b,float radius=.15f){int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/.5f);for(int i=1;i<=steps;i++)if(Blocked(Vector3.Lerp(a,b,(float)i/steps),radius))return false;return true;}
        public string Signature(){var s=new System.Text.StringBuilder();s.Append(Columns).Append('x').Append(Rows).Append(':').Append(entryIndex).Append(':').Append(exitIndex).Append(':');foreach(bool b in open)s.Append(b?'1':'0');return s.ToString();}
    }
}
