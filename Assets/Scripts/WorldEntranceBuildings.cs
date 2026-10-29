using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    /// <summary>Streamed site facades. Primitive shells are an initial placeholder when no asset adapter succeeds.</summary>
    public static class WorldEntranceBuildings
    {
        public static Func<string,Transform,Vector3,int,bool> AssetAdapter;
        const float LoadRadius=150,UnloadRadius=180;
        sealed class SiteState
        {
            public Transform root;
            public OverworldGeography geography;
            public Material[] materials;
            public readonly Dictionary<int,GameObject> active=new Dictionary<int,GameObject>();
        }
        static readonly Dictionary<Transform,SiteState> worlds=new Dictionary<Transform,SiteState>();
        public static void Build(Transform root,List<Material> materials,OverworldGeography geo)
        {
            if(root==null||geo==null)return;
            var stale=new List<Transform>();foreach(var entry in worlds)if(entry.Value.root==null)stale.Add(entry.Key);
            foreach(var key in stale)worlds.Remove(key);
            var id=root;if(worlds.TryGetValue(id,out var previous))foreach(var obj in previous.active.Values)if(obj!=null)UnityEngine.Object.Destroy(obj);
            Color[] palette={new Color(.31f,.30f,.27f),new Color(.19f,.20f,.18f),new Color(.055f,.065f,.065f),new Color(.34f,.19f,.12f),new Color(.49f,.40f,.27f)};
            var mats=new Material[palette.Length];
            Shader shader=Resources.Load<Shader>("VaultSurface")??Shader.Find("Standard");
            var weatheredConcrete=Resources.Load<Texture2D>("Art/WastelandFloor");
            var rust=Resources.Load<Texture2D>("Art/WastelandSurface");
            for(int i=0;i<mats.Length;i++){mats[i]=new Material(shader){name="Streamed entrance / "+i,color=palette[i],enableInstancing=true};if(i==0&&weatheredConcrete)mats[i].mainTexture=weatheredConcrete;else if((i==1||i==3)&&rust)mats[i].mainTexture=rust;materials.Add(mats[i]);}
            worlds[id]=new SiteState{root=root,geography=geo,materials=mats};
            // Initial focus is the nearby player; do not instantiate all 107 sites here.
        }
        public static void UpdateFocus(Transform root,Vector3 focus)
        {
            if(root==null||!worlds.TryGetValue(root,out var state))return;
            var remove=new List<int>();foreach(var pair in state.active){Vector3 p=SitePosition(state.geography,pair.Key);p.y=focus.y;if((p-focus).sqrMagnitude>UnloadRadius*UnloadRadius)remove.Add(pair.Key);}
            foreach(int key in remove){var obj=state.active[key];if(obj!=null){obj.SetActive(false);UnityEngine.Object.Destroy(obj);}state.active.Remove(key);}
            int count=state.geography.Camps.Length+state.geography.Entrances.Length;
            for(int key=0;key<count;key++)if(!state.active.ContainsKey(key)){Vector3 p=SitePosition(state.geography,key);p.y=focus.y;if((p-focus).sqrMagnitude<=LoadRadius*LoadRadius)state.active[key]=CreateSite(state,key);}
        }
        static Vector3 SitePosition(OverworldGeography geo,int key)=>key<geo.Camps.Length?geo.Camps[key]:geo.Entrances[key-geo.Camps.Length];
        public static bool Blocked(Vector3 position,float radius,OverworldGeography geo)
        {
            if(geo==null)return false;
            int count=geo.Camps.Length+geo.Entrances.Length;
            for(int key=0;key<count;key++)
            {
                Vector3 p=SitePosition(geo,key);float dx=Mathf.Abs(position.x-p.x),z=position.z-p.z;
                if(dx<6+radius&&z>2-radius&&z<10+radius)return true;
                // Camp defenses flank the approach while leaving its centre open.
                if(key<geo.Camps.Length&&dx>3-radius&&dx<6+radius&&z>-2.2f-radius&&z<2+radius)return true;
            }
            return false;
        }
        static GameObject CreateSite(SiteState state,int key)
        {
            bool camp=key<state.geography.Camps.Length;int style=camp?4:(key-state.geography.Camps.Length)%4;
            var site=new GameObject(camp?"Raider headquarters / camp "+key:"Dungeon building / "+(key-state.geography.Camps.Length));
            site.transform.SetParent(state.root,false);Vector3 threshold=SitePosition(state.geography,key);threshold.y=0;site.transform.localPosition=threshold;
            // Adapter contract: imported exterior fits the xﾂｱ6,z2..10 footprint. Threshold remains unobstructed.
            bool adapted;
            if(!camp){RuinedCityArchitecture.Place(site.transform,new Rect(-6,2,12,8),(key%5==0)?6:key%6);adapted=true;}
            else { BuildShell(site.transform,state.materials,style,true); BuildCampFortification(site.transform,state.materials); adapted=true; }
            if(!adapted)BuildShell(site.transform,state.materials,style,camp);
            if(camp){DressCampExterior(site.transform,key);WeatherCampFacade(site.transform,state.materials,key);}
            // A readable dark doorway and recessed jambs are always present, including imported exteriors.
            Box(site.transform,state.materials, "Door recess",new Vector3(0,1.3f,2.03f),new Vector3(2.3f,2.6f,.12f),2);
            Box(site.transform,state.materials,"Door left jamb",new Vector3(-1.35f,1.5f,1.85f),new Vector3(.32f,3,.45f),1);
            Box(site.transform,state.materials,"Door right jamb",new Vector3(1.35f,1.5f,1.85f),new Vector3(.32f,3,.45f),1);
            Box(site.transform,state.materials,"Door header",new Vector3(0,3,1.85f),new Vector3(3,.32f,.45f),1);
            Box(site.transform,state.materials,"Door threshold",new Vector3(0,.08f,1),new Vector3(2.8f,.12f,1.9f),0);
            Box(site.transform,state.materials,"Entrance lamp",new Vector3(0,3.3f,1.64f),new Vector3(.65f,.16f,.18f),4);
            var batch=site.GetComponent<ImportedWorldBatch>();if(!batch)batch=site.AddComponent<ImportedWorldBatch>();
            batch.RegisterSiteRenderers();
            return site;
        }
        // A complete roof, side walls and a raised lookout make the camp read as
        // a defended building from the quarter-view camera, even with no imported facade.
        static void BuildCampFortification(Transform root,Material[] mats)
        {
            Box(root,mats,"HQ main roof",new Vector3(0,5.35f,6),new Vector3(11.5f,.35f,7.4f),1);
            Box(root,mats,"HQ broken front parapet west",new Vector3(-3.65f,5.75f,2.35f),new Vector3(4.4f,.8f,.38f),0);
            Box(root,mats,"HQ broken front parapet east",new Vector3(3.95f,5.65f,2.35f),new Vector3(3.7f,.6f,.38f),0);
            Box(root,mats,"HQ broken west parapet",new Vector3(-5.65f,5.72f,7.1f),new Vector3(.38f,.75f,5.1f),0);
            Box(root,mats,"HQ roof east parapet",new Vector3(5.65f,5.85f,6),new Vector3(.38f,1,7.5f),0);
            Box(root,mats,"HQ roof rear parapet",new Vector3(0,5.85f,9.65f),new Vector3(11.7f,1,.38f),0);
            Box(root,mats,"HQ lookout tower",new Vector3(3.3f,6.7f,7.2f),new Vector3(3.6f,2.6f,3.4f),0);
            Box(root,mats,"HQ lookout roof",new Vector3(3.3f,8.1f,7.2f),new Vector3(4.1f,.3f,3.9f),1);
            Box(root,mats,"HQ lookout slit front",new Vector3(3.3f,7,5.47f),new Vector3(2.3f,.35f,.08f),2);
            Box(root,mats,"HQ lookout slit side",new Vector3(5.12f,7,7.2f),new Vector3(.08f,.35f,2),2);
            for(int side=-1;side<=1;side+=2)
            {
                Box(root,mats,"HQ reinforced entrance pier",new Vector3(side*2.05f,1.8f,1.9f),new Vector3(.65f,3.6f,.8f),1);
                Box(root,mats,"HQ frontage armor plate",new Vector3(side*4.3f,1.15f,1.9f),new Vector3(2.8f,2.3f,.25f),3);
                Box(root,mats,"HQ frontage firing slit",new Vector3(side*4.3f,3.4f,1.98f),new Vector3(2.2f,.35f,.12f),2);
            }
            Box(root,mats,"HQ entrance canopy",new Vector3(0,3.65f,1.7f),new Vector3(4.8f,.3f,1.3f),1);
        }
        // Supplies and defenses identify the outdoor entrance as an occupied base.
        // They flank the approach and leave the central threshold unobstructed.
        static void DressCampExterior(Transform root,int key)
        {
            string[] cargo={"pbr_box_supply","pbr_box_ammo","pbr_box_med"};
            PlaceCampProp(root,"props/"+cargo[key%cargo.Length],new Vector3(-4.4f,0,1.1f),key*90,1.2f,1.15f);
            PlaceCampProp(root,"props/barrel_open",new Vector3(4.4f,0,1.1f),key*90,1.05f,1.2f);
            PlaceCampProp(root,"props/SM_fence_stand_small",new Vector3(-4.3f,0,-1f),90,2.3f,1.8f);
            PlaceCampProp(root,"PolyHaven/concrete_road_barrier",new Vector3(4.4f,0,-1f),90,2.1f,1.05f);
        }
        // Weathering belongs to the facade and remains clear of the entrance lane.
        static void WeatherCampFacade(Transform root,Material[] mats,int key)
        {
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*4.25f;
                for(int i=0;i<4;i++)
                {
                    float px=x+side*(i-1.5f)*.46f;
                    var stain=Box(root,mats,"Weathered facade runoff",new Vector3(px,3.8f-(i%2)*.23f,1.96f),new Vector3(.08f+(i%3)*.04f,.75f+i*.26f,.025f),2);
                    stain.transform.localRotation=Quaternion.Euler(0,0,side*(i%2==0?3:-4));
                }
                for(int i=0;i<3;i++)
                {
                    var patch=Box(root,mats,"Corroded repair patch",new Vector3(x+side*(i-1)*.7f,1.1f+i*.43f,1.74f),new Vector3(.68f,.13f,.06f),3);
                    patch.transform.localRotation=Quaternion.Euler(0,0,side*(i*9-8));
                }
                for(int i=0;i<3;i++)
                {
                    var rebar=Box(root,mats,"Exposed roof rebar",new Vector3(side*(1.6f+i*.30f),5.7f+i*.08f,2.30f),new Vector3(.07f,.95f,.07f),3);
                    rebar.transform.localRotation=Quaternion.Euler(0,0,side*(18+i*6));
                }
                PlaceCampProp(root,"PolyHaven/concrete_road_barrier",new Vector3(side*5.1f,0,1.55f),key*31+side*12,1.25f,.6f);
            }
        }
        static void PlaceCampProp(Transform root,string path,Vector3 position,float yaw,float footprint,float height)
        {
            var source=Resources.Load<GameObject>("ImportedWorld/"+path);if(!source)return;
            var item=UnityEngine.Object.Instantiate(source,root,false);item.name="Camp defense / "+source.name;
            item.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var renderers=item.GetComponentsInChildren<Renderer>();if(renderers.Length==0){UnityEngine.Object.Destroy(item);return;}
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=Mathf.Min(footprint/Mathf.Max(.01f,bounds.size.x),footprint/Mathf.Max(.01f,bounds.size.z),height/Mathf.Max(.01f,bounds.size.y));
            item.transform.localScale*=scale;
            bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            var target=root.TransformPoint(position);item.transform.position+=new Vector3(target.x-bounds.center.x,target.y-bounds.min.y,target.z-bounds.center.z);
            foreach(var collider in item.GetComponentsInChildren<Collider>())collider.enabled=false;
        }
        static void BuildShell(Transform root,Material[] mats,int style,bool camp)
        {
            float height=style==2?4.1f:style==3?3.5f:5.2f;
            // Solid facade wings and side/back walls; the doorway is not a gate standing in open ground.
            Box(root,mats,"Facade left",new Vector3(-3.7f,height*.5f,2.4f),new Vector3(4.6f,height,.7f),0);
            Box(root,mats,"Facade right",new Vector3(3.7f,height*.5f,2.4f),new Vector3(4.6f,height,.7f),0);
            Box(root,mats,"Facade lintel",new Vector3(0,height-.65f,2.4f),new Vector3(2.8f,1.3f,.7f),0);
            Box(root,mats,"West wall",new Vector3(-5.65f,height*.5f,6),new Vector3(.7f,height,8),0);
            Box(root,mats,"East wall",new Vector3(5.65f,height*.5f,6),new Vector3(.7f,height,8),0);
            Box(root,mats,"Rear wall",new Vector3(0,height*.5f,9.65f),new Vector3(12,height,.7f),0);
            Box(root,mats,"Broken roof west",new Vector3(-3.7f,height,6),new Vector3(4.6f,.3f,8),1);
            Box(root,mats,"Broken roof rear",new Vector3(1,height-.2f,8.8f),new Vector3(5,.3f,2.3f),1);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*3.75f;
                Box(root,mats,"Window inset",new Vector3(x,2.25f,1.99f),new Vector3(2.5f,1.5f,.1f),2);
                Box(root,mats,"Window sill",new Vector3(x,1.5f,1.8f),new Vector3(2.8f,.16f,.42f),1);
                Box(root,mats,"Window mullion",new Vector3(x,2.25f,1.86f),new Vector3(.13f,1.5f,.14f),1);
                var board=Box(root,mats,"Broken window board",new Vector3(x,2.25f,1.74f),new Vector3(2.8f,.23f,.16f),3);board.transform.localRotation=Quaternion.Euler(0,0,side*15);
            }
            if(style==0) // Freight garage: roll-up panel, recessed personnel entrance.
            {
                for(int i=0;i<8;i++)Box(root,mats,"Warehouse shutter slat",new Vector3(3.8f,.8f+i*.25f,1.7f),new Vector3(3.1f,.12f,.12f),1);
                Box(root,mats,"Loading canopy",new Vector3(0,3.5f,1.8f),new Vector3(10,.2f,2.3f),1);
            }
            else if(style==1) // Storehouse / depot.
            {
                Box(root,mats,"Depot cornice",new Vector3(0,height-.4f,1.9f),new Vector3(12.2f,.7f,.7f),1);
                Box(root,mats,"Vent",new Vector3(-4,height+.5f,7),new Vector3(1.2f,1.2f,1.2f),1);
            }
            else if(style==2) // Diner storefront with a rusted continuous awning.
            {
                Box(root,mats,"Diner awning",new Vector3(0,3.3f,1.5f),new Vector3(11.8f,.3f,2.2f),3);
                Box(root,mats,"Diner blank sign fascia",new Vector3(0,3.8f,2),new Vector3(10,.6f,.35f),4);
            }
            else if(style==3) // Collapsed station / cellar access frontage.
            {
                Box(root,mats,"Station arch band",new Vector3(0,3.3f,1.9f),new Vector3(9,.5f,.45f),1);
                for(int side=-1;side<=1;side+=2)Box(root,mats,"Station masonry pier",new Vector3(side*5.2f,2,1.9f),new Vector3(.6f,4,.9f),0);
            }
            else // Raider HQ is a fortified urban building, not a tent or isolated banner.
            {
                Box(root,mats,"HQ upper floor facade",new Vector3(0,6.1f,2.4f),new Vector3(12,1.8f,.7f),0);
                Box(root,mats,"HQ upper broken slab",new Vector3(-2,7.1f,6),new Vector3(8,.3f,8),1);
                for(int i=0;i<4;i++)Box(root,mats,"HQ upper window",new Vector3(-4.5f+i*3,6.1f,1.99f),new Vector3(1.5f,.95f,.1f),2);
                Box(root,mats,"Raider rust emblem",new Vector3(-2.1f,3.8f,1.9f),new Vector3(.6f,1.2f,.18f),3);
            }
        }
        static GameObject Box(Transform parent,Material[] mats,string name,Vector3 position,Vector3 scale,int material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            var collider=obj.GetComponent<Collider>();if(collider!=null)UnityEngine.Object.Destroy(collider);
            obj.GetComponent<Renderer>().sharedMaterial=mats[material];return obj;
        }
    }
}
