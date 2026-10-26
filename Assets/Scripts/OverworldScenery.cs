using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault
{
    public static class OverworldScenery
    {
        // Optional free-asset adapter. Return true only when an instance was placed.
        public static Func<string,Transform,Vector3,int,bool> AssetAdapter;
        public static void Build(Transform root,List<Material> materials,OverworldGeography geography)
        {var stream=root.gameObject.AddComponent<OverworldStream>();stream.Initialize(geography,materials);stream.TickFocus(geography.Camps[0]);}
        public static void UpdateFocus(Transform root,Vector3 position)
        {var stream=root.GetComponent<OverworldStream>();if(stream!=null)stream.TickFocus(position);}
    }
    public sealed class OverworldStream : MonoBehaviour
    {
        public const int ChunkRadius=1,MaximumLoadedChunks=9;
        readonly Dictionary<Vector2Int,GameObject> chunks=new Dictionary<Vector2Int,GameObject>();
        readonly Queue<Vector2Int> pendingChunks=new Queue<Vector2Int>();
        OverworldGeography geography;Material[] materials;Mesh cube,sphere;Vector2Int focus=new Vector2Int(int.MinValue,int.MinValue);
        public int LoadedChunkCount=>chunks.Count;
        public bool Ready=>pendingChunks.Count==0;
        public static long LastGenerationMilliseconds;
        public void Initialize(OverworldGeography value,List<Material> owned)
        {
            geography=value;var colors=new[]{new Color(.43f,.41f,.34f),new Color(.25f,.30f,.18f),new Color(.21f,.22f,.21f),new Color(.16f,.25f,.26f),new Color(.43f,.30f,.22f),new Color(.30f,.35f,.19f),new Color(.24f,.16f,.10f),new Color(.57f,.51f,.38f)};
            materials=new Material[colors.Length];for(int i=0;i<colors.Length;i++){materials[i]=new Material(Resources.Load<Shader>("RuinedDistrict")??Shader.Find("Standard")){name="Stream shared / "+i,color=colors[i],enableInstancing=true};materials[i].mainTexture=Resources.Load<Texture2D>("Art/Environment/RuinSurfaceAtlas-v1");if(materials[i].HasProperty("_Tile"))materials[i].SetVector("_Tile",i==2?new Vector4(.5f,0,0,0):i==4?Vector4.zero:new Vector4(0,.5f,0,0));owned.Add(materials[i]);}
            ImportedWorldAssets.ApplyOutdoorSurfaces(materials);
            var river=ImportedRiverWater.Create(owned);if(river!=null)materials[3]=river;
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;Destroy(primitive);primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=primitive.GetComponent<MeshFilter>().sharedMesh;Destroy(primitive);
        }
        public void TickFocus(Vector3 position)
        {
            var next=new Vector2Int(Mathf.FloorToInt(position.x/100),Mathf.FloorToInt(position.z/100));if(next==focus)return;focus=next;var generationTimer=System.Diagnostics.Stopwatch.StartNew();
            var remove=new List<Vector2Int>();foreach(var pair in chunks)if(Mathf.Abs(pair.Key.x-next.x)>ChunkRadius||Mathf.Abs(pair.Key.y-next.y)>ChunkRadius)remove.Add(pair.Key);
            foreach(var key in remove){Destroy(chunks[key]);chunks.Remove(key);}
            pendingChunks.Clear();for(int ring=0;ring<=ChunkRadius;ring++)for(int x=next.x-ring;x<=next.x+ring;x++)for(int z=next.y-ring;z<=next.y+ring;z++)if(Mathf.Max(Mathf.Abs(x-next.x),Mathf.Abs(z-next.y))==ring&&x>=-50&&x<50&&z>=-50&&z<50&&!chunks.ContainsKey(new Vector2Int(x,z)))pendingChunks.Enqueue(new Vector2Int(x,z));
            if(chunks.Count==0&&pendingChunks.Count>0){var key=pendingChunks.Dequeue();chunks.Add(key,CreateChunk(key.x,key.y));}
            LastGenerationMilliseconds=generationTimer.ElapsedMilliseconds;
        }
        void Update(){if(pendingChunks.Count==0)return;var key=pendingChunks.Dequeue();if(!chunks.ContainsKey(key))chunks.Add(key,CreateChunk(key.x,key.y));}
        GameObject CreateChunk(int x,int z)
        {
            var obj=new GameObject("Stream / "+x+" / "+z);obj.transform.SetParent(transform,false);var life=obj.AddComponent<OverworldMeshLifetime>();
            var batches=new List<CombineInstance>[materials.Length];for(int i=0;i<batches.Length;i++)batches[i]=new List<CombineInstance>();
            void Box(Vector3 p,Vector3 size,int mat,float yaw=0,bool round=false){batches[mat].Add(new CombineInstance{mesh=round?sphere:cube,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,yaw,0),size)});}
            // Exact curved ribbons are clipped into each 100m batch, not rasterized into nav cells.
            for(float a=x*100;a<(x+1)*100;a+=20)for(float b=z*100;b<(z+1)*100;b+=20)
            {var p=new Vector3(a+10,-.12f,b+10);Box(p,new Vector3(20,.2f,20),geography.Park(p)?1:0);}
            void Ribbon(Func<float,Vector3> center,float from,float to,float width,int mat,float level)
            {
                for(float t=from;t<to;t+=5)
                {
                    var a=center(t);var b=center(t+5);var middle=(a+b)*.5f;if(mat==2&&geography.River(middle)&&!geography.Bridge(middle.z))continue;
                    var sideA=Vector3.Cross(Vector3.up,(center(t+.1f)-center(t-.1f)).normalized)*width*.5f;var sideB=Vector3.Cross(Vector3.up,(center(t+5.1f)-center(t+4.9f)).normalized)*width*.5f;var polygon=new List<Vector3>{a-sideA,b-sideB,b+sideB,a+sideA};
                    for(int edge=0;edge<4&&polygon.Count>0;edge++)
                    {
                        var input=polygon;polygon=new List<Vector3>();float boundary=edge==0?x*100:edge==1?(x+1)*100:edge==2?z*100:(z+1)*100;
                        float Coord(Vector3 v)=>edge<2?v.x:v.z;bool Inside(Vector3 v)=>edge==0||edge==2?Coord(v)>=boundary:Coord(v)<=boundary;
                        var previous=input[input.Count-1];bool was=Inside(previous);
                        foreach(var current in input){bool now=Inside(current);if(was!=now){float amount=(boundary-Coord(previous))/(Coord(current)-Coord(previous));polygon.Add(Vector3.Lerp(previous,current,amount));}if(now)polygon.Add(current);previous=current;was=now;}
                    }
                    if(polygon.Count<3)continue;var vertices=polygon.ToArray();for(int i=0;i<vertices.Length;i++)vertices[i].y=level;
                    var triangles=new int[(vertices.Length-2)*3];for(int i=0;i<vertices.Length-2;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
                    var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x/4,vertices[i].z/4);var mesh=new Mesh{vertices=vertices,triangles=triangles,uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();life.Meshes.Add(mesh);batches[mat].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
                }
            }
            Ribbon(t=>new Vector3(geography.RiverX(t),0,t),z*100-10,(z+1)*100+10,60,3,-.009f);
            for(int street=Mathf.FloorToInt(x*100/100f)-1;street<=Mathf.CeilToInt((x+1)*100/100f)+1;street++)
            {int lane=street;Ribbon(t=>new Vector3(geography.StreetX(lane,t),0,t),z*100-15,(z+1)*100+15,geography.StreetWidth(lane)+3,7,-.005f);Ribbon(t=>new Vector3(geography.StreetX(lane,t),0,t),z*100-15,(z+1)*100+15,geography.StreetWidth(lane),2,0);}
            for(int street=Mathf.FloorToInt(z*100/100f)-1;street<=Mathf.CeilToInt((z+1)*100/100f)+1;street++)
            {int lane=street;Ribbon(t=>new Vector3(t,0,geography.StreetZ(lane,t)),x*100-15,(x+1)*100+15,geography.StreetWidth(lane)+3,7,-.004f);Ribbon(t=>new Vector3(t,0,geography.StreetZ(lane,t)),x*100-15,(x+1)*100+15,geography.StreetWidth(lane),2,.001f);}
            for(int bridge=-4;bridge<=4;bridge++){float from=Mathf.Max(z*100,bridge*1000-45),to=Mathf.Min((z+1)*100,bridge*1000+45);if(to>from)Ribbon(t=>new Vector3(geography.RiverX(t),0,t),from,to,60,7,.002f);}            var data=geography.GetChunk(x,z);int index=0;
            foreach(var rect in data.Buildings)
            {
                Vector3 p=new Vector3(rect.center.x,0,rect.center.y);int seed=unchecked(x*1973+z*9277+index++*101);float h=4.5f+Mathf.Abs(seed%4)*1.7f;
                int district=geography.District(p);
                if(district==0||district==1||district==2){RuinedCityArchitecture.Place(obj.transform,rect,district==1?7:district==2?6:Mathf.Abs(seed%6));continue;}
                if(ImportedWorldAssets.PlaceBuilding(obj.transform,rect,seed))continue;
                Box(p+Vector3.up*.15f,new Vector3(rect.width,.3f,rect.height),0);
                Box(p+new Vector3(-rect.width*.5f+.25f,h*.5f,0),new Vector3(.5f,h,rect.height),4);
                Box(p+new Vector3(rect.width*.5f-.25f,h*.35f,0),new Vector3(.5f,h*.7f,rect.height),0);
                Box(p+new Vector3(0,h*.35f,rect.height*.5f-.25f),new Vector3(rect.width,h*.7f,.5f),4);
                for(int post=0;post<4;post++)Box(p+new Vector3(-rect.width*.42f+post*rect.width*.28f,h*.4f,-rect.height*.5f+.3f),new Vector3(.55f,h*.8f,.6f),0);
                Box(p+new Vector3(0,h*.8f,-rect.height*.5f+.3f),new Vector3(rect.width,.35f,.8f),0);
                for(int rubble=0;rubble<12;rubble++){float a=rubble*2.399f;Box(p+new Vector3(Mathf.Cos(a)*rect.width*.48f,.1f,Mathf.Sin(a)*rect.height*.48f),new Vector3(.4f,.2f,.27f),rubble%2==0?4:0,rubble*31);}
            }
            foreach(var tree in data.Trees)
            {
                int seed=unchecked(x*1973+z*9277+index++*101);if(OverworldScenery.AssetAdapter?.Invoke("tree",obj.transform,tree,seed)==true)continue;
                float h=5+Mathf.Abs(seed%5)*.6f;Box(tree+Vector3.up*h*.4f,new Vector3(.38f,h*.8f,.38f),6);
                for(int lobe=0;lobe<6;lobe++){float a=lobe*2.399f+seed;var offset=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Box(tree+offset*.7f+Vector3.up*(h*.53f+lobe*.23f),new Vector3(2,.17f,.17f),6,-a*Mathf.Rad2Deg);Box(tree+offset*(1.1f+lobe%2*.4f)+Vector3.up*(h*.65f+lobe*.23f),new Vector3(2.3f,1.7f,2),lobe%2==0?1:5,seed+lobe*37,true);}
            }
            foreach(var car in data.Cars)
            {if(OverworldScenery.AssetAdapter?.Invoke("car",obj.transform,car,index++)==true)continue;Box(car+Vector3.up*.4f,new Vector3(1.8f,.6f,4.8f),6);Box(car+Vector3.up*.95f,new Vector3(1.55f,.55f,2),2);for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)Box(car+new Vector3(side*.85f,.3f,end*1.5f),new Vector3(.22f,.6f,.65f),2,0,true);}
            var rng=new System.Random(unchecked(OverworldGeography.Seed+x*331+z*7741));
            // Passable ankle-height plants replace upright primitive grass. No unregistered rocks.
            for(int i=0;i<400;i++){
                var p=new Vector3(x*100+(float)rng.NextDouble()*100,0,z*100+(float)rng.NextDouble()*100);
                if(!geography.River(p)&&geography.RoadClearance(p)>1.5f&&!geography.SiteProtected(p,2))ImportedWorldAssets.PlaceGroundcover(obj.transform,p,i+x*47+z*11);
            }
            for(int parkX=Mathf.FloorToInt(x*100/250f)-1;parkX<=Mathf.FloorToInt((x+1)*100/250f);parkX++)for(int parkZ=Mathf.FloorToInt(z*100/250f)-1;parkZ<=Mathf.FloorToInt((z+1)*100/250f);parkZ++){
                var centre=new Vector3(parkX*250+150,0,parkZ*250+150);if(centre.x<x*100||centre.x>=(x+1)*100||centre.z<z*100||centre.z>=(z+1)*100||!geography.PocketPark(centre)||geography.SiteProtected(centre,10))continue;
                Box(centre+Vector3.down*.005f,new Vector3(24,.015f,2.5f),7);Box(centre+Vector3.down*.004f,new Vector3(2.5f,.015f,24),7);
                for(int seat=0;seat<8;seat++){float a=seat*Mathf.PI*.25f;var bench=Resources.Load<GameObject>("ImportedWorld/props/bench");if(bench)ImportedWorldBatch.Add(obj.transform,bench,centre+new Vector3(Mathf.Cos(a)*10,0,Mathf.Sin(a)*10),Quaternion.Euler(0,-a*Mathf.Rad2Deg-90,0),Vector3.one);}
            }
            // Broken masonry gathers along ruined foundations instead of uniform scattering.
            foreach(var building in data.Buildings)for(int corner=0;corner<4;corner++){
                var anchor=new Vector3(corner%2==0?building.xMin-.8f:building.xMax+.8f,0,corner<2?building.yMin-.8f:building.yMax+.8f);
                for(int piece=0;piece<6;piece++){
                    var p=anchor+new Vector3(((float)rng.NextDouble()-.5f)*2.8f,0,((float)rng.NextDouble()-.5f)*2.8f);
                    if(geography.RoadClearance(p)<1||geography.SiteProtected(p,1)||geography.River(p))continue;
                    OverworldScenery.AssetAdapter?.Invoke("debris",obj.transform,p,index++);
                }
            }
            // Imported street furniture sits off the roadway; low debris remains passable.
            for(int i=0;i<240;i++){
                var p=new Vector3(x*100+(float)rng.NextDouble()*100,0,z*100+(float)rng.NextDouble()*100);
                if(geography.River(p)||geography.SiteProtected(p,2))continue;
                bool occupied=false;foreach(var building in data.Buildings)if(new Rect(building.xMin-1,building.yMin-1,building.width+2,building.height+2).Contains(new Vector2(p.x,p.z))){occupied=true;break;}if(occupied)continue;
                float clearance=geography.RoadClearance(p);
                if(clearance>1.6f&&clearance<3.5f)OverworldScenery.AssetAdapter?.Invoke("street",obj.transform,p,i+x*113+z*71);
                else if(clearance>3.5f)OverworldScenery.AssetAdapter?.Invoke("debris",obj.transform,p,i+x*57+z*31);
            }
            for(int mat=0;mat<batches.Length;mat++)
            {if(batches[mat].Count==0)continue;var mesh=new Mesh{name="Stream mesh",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batches[mat].ToArray(),true,true);if(mat==0||mat==1||mat==2||mat==7){var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];for(int v=0;v<vertices.Length;v++)uv[v]=new Vector2(vertices[v].x/4,vertices[v].z/4);mesh.uv=uv;}mesh.RecalculateBounds();life.Meshes.Add(mesh);var child=new GameObject("Material "+mat);child.transform.SetParent(obj.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[mat];renderer.shadowCastingMode=mat==3?ShadowCastingMode.Off:ShadowCastingMode.On;}
            return obj;
        }
    }
    public sealed class OverworldMeshLifetime:MonoBehaviour
    {
        public readonly List<Mesh> Meshes=new List<Mesh>();
        void OnDestroy(){foreach(var mesh in Meshes)if(mesh!=null)Destroy(mesh);}
    }
}


