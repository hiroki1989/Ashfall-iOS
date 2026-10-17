using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    public sealed partial class VaultArt
    {
        Material DistrictMaterial(int surface,Color tint)
        {
            string key="District surface / "+surface+" / "+tint;
            if(cache.TryGetValue(key,out var existing))return existing;
            var atlas=Resources.Load<Texture2D>("Art/Environment/RuinSurfaceAtlas-v1");var shader=Resources.Load<Shader>("RuinedDistrict");
            if(atlas==null||shader==null)return null;
            var material=new Material(shader){name=key,mainTexture=atlas,color=Color.Lerp(Color.white,tint,.45f)};
            material.SetVector("_Tile",new Vector4(surface%2*.5f,surface<2?.5f:0,surface==1?.32f:0,0));
            material.SetFloat("_Scale",surface==3?.35f:surface==2?.7f:.55f);
            cache.Add(key,material);owned.Add(material);return material;
        }
        // All tall dressing is confined to existing solid cells. Navigation and gun rays
        // continue to use SectorLayout, and traversable ground receives only flat chips.
        void RuinedDistrict(int theme,SectorLayout layout)
        {
            var district=Group("Ruined district / collapsed civic and industrial fabric",Vector3.zero);
            int sites=0;
            for(int y=1;y<layout.Rows-1;y++)for(int x=1;x<layout.Columns-1;x++)
            {
                if(layout.open[x,y])continue;
                bool edge=layout.open[x-1,y]||layout.open[x+1,y]||layout.open[x,y-1]||layout.open[x,y+1];
                if(!edge||(x*17+y*31+theme*7)%5!=0||sites>=82)continue;
                var p=layout.Center(new Vector2Int(x,y));p.y=0;
                var g=Group("Broken frontage / "+sites,p,district);
                // Face an adjacent open cell without projecting beyond the solid footprint.
                float yaw=layout.open[x,y-1]?0:layout.open[x-1,y]?90:layout.open[x,y+1]?180:270;
                g.localRotation=Quaternion.Euler(0,yaw,0);
                int kind=(sites+theme*2)%5;
                for(int support=-1;support<=1;support+=2)RuinBlock(g,"Broken masonry footing",new Vector3(support*.72f,.40f,.18f),new Vector3(.34f,.92f,.55f),Stone*.82f);
                if(kind==0)RuinFacade(g);
                else if(kind==1)RuinTransformer(g);
                else if(kind==2)RuinPipeworks(g);
                else if(kind==3)RuinStorefront(g);
                else RuinCollapsedFloor(g);
                sites++;
            }
            int chips=0;
            for(int y=1;y<layout.Rows-1;y++)for(int x=1;x<layout.Columns-1;x++)
            {
                if(!layout.open[x,y]||(x*7+y*13+theme)%4!=0||chips>=360)continue;
                if(layout.open[x-1,y]&&layout.open[x+1,y]&&layout.open[x,y-1]&&layout.open[x,y+1])continue;
                var p=layout.Center(new Vector2Int(x,y));p.y=0;
                for(int k=0;k<3;k++)
                {
                    float a=(float)random.NextDouble()*360;
                    var chip=Part("Concrete rubble chip",PrimitiveType.Cube,p+new Vector3((float)random.NextDouble()*1.3f-.65f,-.0375f,(float)random.NextDouble()*1.3f-.65f),new Vector3(.12f+(float)random.NextDouble()*.25f,.045f,.09f+(float)random.NextDouble()*.24f),k==0?Stone:Dark,district);
                    chip.localRotation=Quaternion.Euler(0,a,0);
                }
                chips++;
            }
            // Hundreds of authored details become a small set of material batches.
            BatchRuins(district);
        }
        void RuinBlock(Transform g,string name,Vector3 p,Vector3 scale,Color c,Vector3 rotation=default)
        {Detail(name,PrimitiveType.Cube,p,scale,c,g,rotation);}
        void RuinFacade(Transform g)
        {
            for(int s=-1;s<=1;s+=2)
            {
                RuinBlock(g,"Spalled concrete window pier",new Vector3(s*.72f,1.43f,.12f),new Vector3(.33f,1.28f,.42f),Stone);
                RuinBlock(g,"Rusted window jamb",new Vector3(s*.50f,1.56f,-.13f),new Vector3(.05f,1.1f,.06f),Bronze);
                for(int r=0;r<3;r++)RuinBlock(g,"Exposed rebar",new Vector3(s*.72f+r*.055f,2.19f,.12f),new Vector3(.026f,.40f+r*.12f,.026f),Bronze,new Vector3(0,0,s*8+r*4));
            }
            RuinBlock(g,"Concrete window sill",new Vector3(0,1.02f,0),new Vector3(1.8f,.21f,.61f),Stone);
            RuinBlock(g,"Fractured concrete lintel",new Vector3(-.18f,2.11f,.08f),new Vector3(1.47f,.23f,.48f),Stone,new Vector3(0,0,-6));
            RuinBlock(g,"Broken window mullion",new Vector3(.03f,1.54f,-.13f),new Vector3(.04f,1.0f,.04f),Dark,new Vector3(0,0,13));
            for(int i=0;i<5;i++)
            {
                var brick=Part("Broken masonry brick",PrimitiveType.Cube,new Vector3(-.72f+i*.32f,.86f,-.30f),new Vector3(.25f,.11f,.18f),Stone*.8f,g);
                brick.localRotation=Quaternion.Euler(0,i*27,4);
                string key="Ruin exposed brick";
                if(!cache.TryGetValue(key,out var brickMat)){brickMat=new Material(EnvironmentMaterial("concrete",Stone)){name=key};cache.Add(key,brickMat);owned.Add(brickMat);}
                brick.GetComponent<Renderer>().sharedMaterial=brickMat;
            }
        }
        void RuinTransformer(Transform g)
        {
            RuinBlock(g,"Abandoned switch cabinet",new Vector3(0,1.37f,.12f),new Vector3(1.35f,1.12f,.85f),new Color(.28f,.34f,.29f));
            RuinBlock(g,"Open rusted access door",new Vector3(-.72f,1.4f,-.38f),new Vector3(.6f,.91f,.045f),Bronze,new Vector3(0,36,0));
            for(int i=0;i<6;i++)RuinBlock(g,"Cabinet ventilation slit",new Vector3(.27f,1.1f+i*.09f,-.32f),new Vector3(.53f,.028f,.025f),Dark);
            for(int i=0;i<3;i++)
            {
                Detail("Ceramic transformer insulator",PrimitiveType.Cylinder,new Vector3(-.44f+i*.44f,2.03f,.12f),new Vector3(.19f,.12f,.19f),Stone,g);
                for(int r=0;r<3;r++)Detail("Insulator disc",PrimitiveType.Cylinder,new Vector3(-.44f+i*.44f,1.98f+r*.07f,.12f),new Vector3(.27f,.016f,.27f),Stone,g);
            }
            RuinBlock(g,"Cabinet torn cable",new Vector3(.48f,1.01f,-.46f),new Vector3(.035f,.48f,.035f),Dark,new Vector3(0,0,23));
        }
        void RuinPipeworks(Transform g)
        {
            for(int i=0;i<3;i++)
            {
                float x=-.62f+i*.60f,h=1.08f+i*.35f;
                Detail("Corroded riser pipe",PrimitiveType.Cylinder,new Vector3(x,.8f+h*.5f,.24f),new Vector3(.20f,h*.5f,.20f),Bronze,g);
                for(int j=0;j<2;j++)Detail("Bolted pipe flange",PrimitiveType.Cylinder,new Vector3(x,.99f+j*h*.65f,.24f),new Vector3(.34f,.055f,.34f),Dark,g);
                RuinBlock(g,"Pipe support bracket",new Vector3(x,1.05f,-.04f),new Vector3(.32f,.10f,.58f),Dark);
            }
            RuinBlock(g,"Ruptured corrugated backing",new Vector3(0,1.32f,.68f),new Vector3(1.82f,1.0f,.055f),Bronze);
            for(int i=0;i<10;i++)RuinBlock(g,"Sheet metal corrugation",new Vector3(-.84f+i*.185f,1.34f,.62f),new Vector3(.045f,1.03f,.055f),Bronze*.75f);
        }
        void RuinStorefront(Transform g)
        {
            RuinBlock(g,"Shop concrete rear wall",new Vector3(0,1.37f,.66f),new Vector3(1.85f,1.12f,.23f),Stone*.7f);
            for(int i=0;i<7;i++)RuinBlock(g,"Bent shutter slat",new Vector3(0,1.04f+i*.14f,.44f),new Vector3(1.65f,.095f,.09f),Bronze*.65f,new Vector3(0,0,i>4?-8:0));
            RuinBlock(g,"Collapsed awning steel",new Vector3(-.08f,1.99f,0),new Vector3(1.77f,.085f,1.25f),new Color(.28f,.31f,.26f),new Vector3(12,0,-7));
            for(int i=0;i<5;i++)RuinBlock(g,"Awning exposed rib",new Vector3(-.72f+i*.35f,1.94f,0),new Vector3(.035f,.055f,1.1f),Dark,new Vector3(12,0,-7));
            RuinBlock(g,"Empty weathered sign board",new Vector3(0,2.24f,.48f),new Vector3(1.62f,.26f,.085f),Bronze*.6f,new Vector3(0,0,-5));
        }
        void RuinCollapsedFloor(Transform g)
        {
            for(int i=0;i<6;i++)
            {
                float x=-.69f+i*.27f;
                RuinBlock(g,"Collapsed concrete slab",new Vector3(x,.91f+i%3*.10f,.04f),new Vector3(.41f,.15f,1.56f-i%2*.32f),i%2==0?Stone:Stone*.7f,new Vector3(i*4,9-i*4,i%2==0?12:-17));
                RuinBlock(g,"Torn floor reinforcing bar",new Vector3(x,1.17f+i%2*.12f,.12f),new Vector3(.024f,.024f,1.71f),Bronze,new Vector3(0,i*7,0));
            }
            RuinBlock(g,"Severed structural beam",new Vector3(0,1.53f,.46f),new Vector3(1.76f,.14f,.19f),Dark,new Vector3(0,0,19));
        }
        void BatchRuins(Transform district)
        {
            var batches=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in district.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                if(!batches.TryGetValue(renderer.sharedMaterial,out var list)){list=new List<CombineInstance>();batches.Add(renderer.sharedMaterial,list);}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=district.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                renderer.enabled=false;
            }
            // Retire the primitive holders after baking; their colliders are already removed by Part.
            var children=new List<GameObject>();foreach(Transform child in district)children.Add(child.gameObject);
            foreach(var child in children)Object.Destroy(child);
            foreach(var pair in batches)
            {
                var mesh=new Mesh{name="Baked ruined district details",indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                var batch=Group("District material batch",Vector3.zero,district);
                batch.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var material=pair.Key;
                var atlas=Resources.Load<Texture2D>("Art/Environment/RuinSurfaceAtlas-v1");
                var shader=Resources.Load<Shader>("RuinedDistrict");
                if(atlas!=null&&shader!=null&&!pair.Key.name.StartsWith("District surface /"))
                {
                    material=new Material(shader){name="Ruin weathering / "+pair.Key.name};
                    material.mainTexture=atlas;
                    bool concrete=pair.Key.name.ToLowerInvariant().Contains("concrete");
                    bool rubber=pair.Key.name.ToLowerInvariant().Contains("rubber");
                    bool brick=pair.Key.name=="Ruin exposed brick";
                    material.SetVector("_Tile",brick?Vector4.zero:concrete?new Vector4(0,.5f,0,0):rubber?new Vector4(.5f,0,0,0):new Vector4(.5f,.5f,.35f,0));
                    material.color=Color.Lerp(Color.white,pair.Key.color,.24f);owned.Add(material);
                }
                batch.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;TrackMesh(batch,mesh);
            }
        }
    }
}
