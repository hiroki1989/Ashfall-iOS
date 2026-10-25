using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Additive catalog geometry; baseline sockets and garment shapes are never moved.
    public static class ManufacturerEquipment3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Transform Root(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        public static string WeaponKey(Gun gun)=>gun==null?"default":gun.catalogId+"/"+gun.id+"/"+gun.modelVariant+"/"+gun.visualSet+"/"+gun.manufacturerId+"/"+gun.designVariant;
        public static DetailedHeroEquipment3D.WeaponSockets BuildWeapon(Transform parent,Gun gun)
        {
            int bespoke=BespokeLegendaryDesigns.Index(gun);if(bespoke>=0){var unique=BespokeLegendaryModels3D.BuildWeapon(parent,gun,bespoke);EquipmentProjection3D.Apply(unique.root,DistinctEquipmentDesigns.For(gun),true);return unique;}
            var recipe=DistinctEquipmentDesigns.For(gun);
            if(recipe!=null){var product=DistinctEquipmentModels3D.BuildWeapon(parent,gun,recipe);EquipmentProjection3D.Apply(product.root,recipe,true);return product;}
            int model=gun==null?12:VaultLanguage.DisplayModel(gun);
            var weapon=DetailedHeroArsenal3D.BuildWeapon(parent,model,gun?.visualSet);
            if(gun!=null){var root=Root("Manufacturer weapon fittings",weapon.root);root.localPosition=V(0,.063f,.015f);if(Arsenal.IsOneHanded(model))root.localScale=Vector3.one*.65f;Build(root,gun.manufacturerId,gun.designVariant);}
            return weapon;
        }
        public static string MeleeKey(MeleeWeaponItem item)=>item==null?"":item.id+"/"+item.family+"/"+item.manufacturerId+"/"+item.designVariant;
        public static Transform BuildMelee(Transform parent,MeleeWeaponItem item)
        {
            if(item==null)return null;MeleeWeaponCatalog.InferIdentity(item);
            var weapon=DetailedHeroArsenal3D.BuildMelee(parent,item.family);
            var fittings=Root("Manufacturer melee guard fittings",weapon);
            // Small blade-root attachments leave the original hand pivot and wrapped handle clear.
            fittings.localPosition=V(0,.028f,.075f);fittings.localScale=Vector3.one*.38f;
            Build(fittings,item.manufacturerId,item.designVariant);EquipmentProjection3D.ApplyMelee(weapon,item.family);return weapon;
        }
        public static void ApplyArmor(DetailedHero3D body,ArmorItem[] items)
        {
            if(!body)return;var component=body.GetComponent<ArmorAttachments>();if(!component)component=body.gameObject.AddComponent<ArmorAttachments>();component.Set(body,items);
        }
        public sealed class MeshLifetime:MonoBehaviour
        {
            public readonly List<Mesh> meshes=new List<Mesh>();
            public readonly List<Material> materials=new List<Material>();
            void OnDestroy(){foreach(var mesh in meshes)if(mesh){if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);}foreach(var material in materials)if(material){if(Application.isPlaying)Object.Destroy(material);else Object.DestroyImmediate(material);}}
        }
        public sealed class ArmorAttachments:MonoBehaviour
        {
            readonly string[] keys=new string[5];
            readonly List<Transform>[] roots={new List<Transform>(),new List<Transform>(),new List<Transform>(),new List<Transform>(),new List<Transform>()};
            readonly GeneratedEquipmentAssetFactory.Visual[] generated=new GeneratedEquipmentAssetFactory.Visual[5];
            public void Set(DetailedHero3D body,ArmorItem[] items)
            {
                for(int slot=0;slot<5;slot++){
                    ArmorItem item=null;if(items!=null)foreach(var candidate in items)if(candidate!=null&&(int)candidate.slot==slot){item=candidate;break;}
                    string key=item==null?"":item.catalogId+"/"+item.id+"/"+ManufacturerBranding.Maker(item)+"/"+item.designVariant+"/"+item.visualVariant+"/"+GeneratedEquipmentAssetCatalog.TryArmor(item,out _);
                    if(keys[slot]==key)continue;keys[slot]=key;
                    foreach(var root in roots[slot])if(root){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}roots[slot].Clear();
                    generated[slot]?.Dispose();generated[slot]=null;
                    if(item==null)continue;
                    // A product-specific mesh owns this slot when both its mesh and atlas exist.
                    // Keep the procedural builder only for products without authored geometry.
                    if(GeneratedEquipmentAssetCatalog.TryArmor(item,out var authored))
                    {
                        generated[slot]=GeneratedEquipmentAssetFactory.Create(authored,body);
                        if(generated[slot]!=null)continue;
                    }
                    // Cinder menu-reference set is owned exclusively by ArmorVisuals baseline rig.
                    int menuSet=BespokeLegendaryDesigns.Index(item);if(menuSet>=20&&menuSet<=24)continue;
                    var recipe=DistinctEquipmentDesigns.For(item);
                    if(recipe!=null){
                        switch((ArmorSlot)slot){
                            case ArmorSlot.Head:AddDistinct(body.head);break;
                            case ArmorSlot.Chest:AddDistinct(body.chest);break;
                            case ArmorSlot.Arms:AddDistinct(body.leftForearm);AddDistinct(body.rightForearm);break;
                            case ArmorSlot.Legs:AddDistinct(body.leftShin);AddDistinct(body.rightShin);break;
                            case ArmorSlot.Feet:AddDistinct(body.leftFoot);AddDistinct(body.rightFoot);break;
                        }
                        int bespoke=BespokeLegendaryDesigns.Index(item);roots[slot].AddRange(bespoke>=0?BespokeLegendaryModels3D.ApplyArmorSupplement(body,item,bespoke):DistinctEquipmentModels3D.ApplyArmorSupplement(body,item,recipe));
                        continue;
                    }
                    switch((ArmorSlot)slot){
                        case ArmorSlot.Head:Add(body.head,V(.092f,.065f,.018f),new Vector3(0,0,-90),.55f);break;
                        case ArmorSlot.Chest:Add(body.chest,V(0,.055f,.19f),V(90,0,0),1.25f);break;
                        case ArmorSlot.Arms:Add(body.leftForearm,V(0,-.16f,.074f),V(90,0,0),.65f);Add(body.rightForearm,V(0,-.16f,.074f),V(90,0,0),.65f);break;
                        case ArmorSlot.Legs:Add(body.leftShin,V(0,-.15f,.105f),V(90,0,0),.70f);Add(body.rightShin,V(0,-.15f,.105f),V(90,0,0),.70f);break;
                        case ArmorSlot.Feet:Add(body.leftFoot,V(0,-.015f,-.065f),V(90,180,0),.5f);Add(body.rightFoot,V(0,-.015f,-.065f),V(90,180,0),.5f);break;
                    }
                    void AddDistinct(Transform bone){if(bone){var armor=BespokeLegendaryDesigns.Index(item)>=0?BespokeLegendaryModels3D.BuildArmor(bone,item,BespokeLegendaryDesigns.Index(item)):DistinctEquipmentModels3D.BuildArmor(bone,item,recipe);EquipmentProjection3D.Apply(armor,recipe,false);roots[slot].Add(armor);}}
                    void Add(Transform bone,Vector3 position,Vector3 rotation,float scale){if(!bone)return;var root=Root("Catalog "+item.slot+" fittings",bone);root.localPosition=position;root.localEulerAngles=rotation;root.localScale=Vector3.one*scale;roots[slot].Add(root);Build(root,ManufacturerBranding.Maker(item),item.designVariant);}
                }
            }
            void OnDestroy(){foreach(var visual in generated)visual?.Dispose();}
        }
        // Ten intentionally different layouts: a badge, rail, paired wings, bridge,
        // stepped fin, cage, rear bank, staggered bank, long spine and crown.
        static Vector3[] Layout(int design)
        {
            switch(design){
                case 1:return new[]{V(0,0,-.05f),V(0,0,.025f)};
                case 2:return new[]{V(-.055f,0,0),V(.055f,0,0)};
                case 3:return new[]{V(-.045f,0,-.045f),V(.045f,0,-.045f),V(0,.025f,.03f)};
                case 4:return new[]{V(0,0,-.07f),V(0,.018f,0),V(0,.036f,.07f)};
                case 5:return new[]{V(-.045f,0,-.055f),V(.045f,0,-.055f),V(-.045f,0,.055f),V(.045f,0,.055f)};
                case 6:return new[]{V(-.055f,0,-.065f),V(0,0,-.065f),V(.055f,0,-.065f)};
                case 7:return new[]{V(-.04f,0,-.06f),V(.04f,.012f,-.015f),V(-.04f,0,.03f),V(.04f,.012f,.075f)};
                case 8:return new[]{V(0,0,-.1f),V(0,0,-.035f),V(0,0,.035f),V(0,0,.1f)};
                default:return new[]{V(-.05f,0,-.05f),V(.05f,0,-.05f),V(-.05f,0,.05f),V(.05f,0,.05f),V(0,.035f,0)};
            }
        }
        static void Build(Transform root,string manufacturer,int design)
        {
            int maker=ManufacturerCatalog.Index(manufacturer);if(maker<0)maker=0;int generation=Mathf.Max(0,design)/10;design=Mathf.Max(0,design)%10;
            if(generation>0){root.localScale=Vector3.Scale(root.localScale,new Vector3(ProductGeneration.Output(generation*10),1+generation*.025f,ProductGeneration.Cycle(generation*10)));root.localRotation*=Quaternion.Euler(0,(generation%3-1)*7,0);Box(root,"Generation cooling bridge","Steel",V(0,.045f,0),V(.045f,.008f,.035f+generation*.006f));}
            root.gameObject.AddComponent<MeshLifetime>();
            // The colored inset stays small; structural parts keep their steel, brass,
            // leather and glass material response instead of one overall tint.
            Box(root,"Maker brass badge","Brass",V(.035f,0,-.065f),V(.034f,.008f,.026f));
            Box(root,"Maker inset "+manufacturer,maker==1||maker==4?"AmberGlass":"TealGlass",V(.035f,.006f,-.065f),V(.023f,.004f,.016f));
            for(int i=0;i<=maker;i++)Box(root,"Maker etched index","Dark",V(.024f+i*.004f,.009f,-.065f),V(.0015f,.001f,.01f));
            LogoDecal(root,manufacturer);
            if(design==0){Batch(root);return;}
            var points=Layout(design);for(int i=0;i<points.Length;i++){
                var unit=Root("Configuration "+design+" / fitting "+i,root);unit.localPosition=points[i];
                if(design==2)unit.localEulerAngles=V(0,i==0?-18:18,0);
                switch(maker){
                    case 0:
                        Box(unit,"Precision rail base","Dark",V(0,0,0),V(.043f,.014f,.062f));
                        for(int n=0;n<4;n++)Box(unit,"Machined rail tooth","Steel",V(0,.012f,(n-1.5f)*.015f),V(.057f,.009f,.007f));
                        Box(unit,"Range optic","TealGlass",V(0,.023f,.02f),V(.021f,.012f,.013f));break;
                    case 1:
                        Box(unit,"Heavy steel reinforcement","Steel",V(0,0,0),V(.062f,.029f,.066f));
                        for(int sign=-1;sign<=1;sign+=2)Box(unit,"Brass structural brace","Brass",V(sign*.023f,.019f,0),V(.012f,.016f,.078f));
                        Cylinder(unit,"Dark locking pin","Dark",V(0,.02f,0),.012f,.022f);break;
                    case 2:
                        Box(unit,"Coil ceramic bed","Dark",V(0,0,0),V(.056f,.016f,.073f));
                        for(int n=0;n<3;n++){Cylinder(unit,"Copper coil winding","Brass",V((n-1)*.018f,.019f,0),.009f,.054f);Cylinder(unit,"Coil luminous core","TealGlass",V((n-1)*.018f,.019f,.005f),.005f,.058f);}break;
                    case 3:
                        Box(unit,"Biological leather cradle","LeatherDark",V(0,0,0),V(.046f,.014f,.072f));
                        Cylinder(unit,"Living amber canister","AmberGlass",V(0,.021f,0),.018f,.058f);
                        for(int sign=-1;sign<=1;sign+=2)Cylinder(unit,"Canister steel seal","Steel",V(0,.021f,sign*.026f),.021f,.009f);
                        Box(unit,"Vitality sight gauge","TealGlass",V(0,.041f,0),V(.009f,.005f,.032f));break;
                    case 4:
                        Box(unit,"Salvage backing","Rust",V(0,0,0),V(.07f,.012f,.073f));
                        for(int n=0;n<3;n++){var plate=Box(unit,"Overlapping heatshield",n==1?"Brass":"Steel",V((n-1)*.018f,.012f+n*.003f,(n-1)*.008f),V(.027f,.008f,.061f));plate.localRotation=Quaternion.Euler(0,n*9-9,0);}
                        Cylinder(unit,"Reclaimed fastener","Dark",V(.021f,.027f,.023f),.006f,.009f);break;
                    case 5:
                        Box(unit,"Ballistic padded support","LeatherDark",V(0,0,0),V(.068f,.012f,.067f));
                        for(int n=0;n<2;n++){var plate=Box(unit,"Sloped shield plate","Steel",V((n==0?-1:1)*.017f,.016f,0),V(.033f,.014f,.064f));plate.localRotation=Quaternion.Euler(0,0,n==0?18:-18);}
                        Box(unit,"Brass protective ridge","Brass",V(0,.029f,0),V(.012f,.011f,.071f));break;
                }
            }
            Batch(root);
        }
        static void LogoDecal(Transform root,string manufacturer)
        {
            var shader=Shader.Find("Standard");if(!shader)return;
            var material=new Material(shader){name="Cutout maker emblem / "+manufacturer,hideFlags=HideFlags.DontSave};
            material.SetFloat("_Mode",1);material.SetFloat("_Cutoff",.4f);
            material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite",1);material.SetOverrideTag("RenderType","TransparentCutout");
            material.EnableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            material.color=ManufacturerCatalog.Color(manufacturer);
            // Texture belongs to ManufacturerBranding's shared cache, never this owner.
            material.mainTexture=ManufacturerBranding.Logo(manufacturer);
            var center=V(.035f,.025f,-.065f);const float half=.020f;
            var mesh=new Mesh{name="Upward maker decal quad"};
            mesh.vertices=new[]{center+V(-half,0,-half),center+V(-half,0,half),center+V(half,0,half),center+V(half,0,-half)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var decal=Root("Manufacturer actual alpha logo",root);
            decal.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            decal.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            var owner=root.GetComponent<MeshLifetime>();owner.meshes.Add(mesh);owner.materials.Add(material);
        }
        static Transform Box(Transform parent,string name,string material,Vector3 center,Vector3 size)
        {
            var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+Vector3.Scale(size*.5f,V((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            int[] indices={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};return Mesh(parent,name,material,v,indices);
        }
        static void Cylinder(Transform parent,string name,string material,Vector3 center,float radius,float length)
        {
            const int sides=12;var v=new Vector3[sides*2+2];var indices=new List<int>();
            for(int side=0;side<2;side++)for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;v[side*sides+i]=center+V(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,(side-.5f)*length);}
            v[24]=center-V(0,0,length*.5f);v[25]=center+V(0,0,length*.5f);
            for(int i=0;i<sides;i++){int next=(i+1)%sides;indices.AddRange(new[]{i,next,i+sides,next,next+sides,i+sides,24,next,i,25,i+sides,next+sides});}
            Mesh(parent,name,material,v,indices.ToArray());
        }
        static Transform Mesh(Transform parent,string name,string material,Vector3[] vertices,int[] triangles)
        {
            var t=Root(name,parent);var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=DetailedHeroMaterials3D.Get(material);parent.GetComponentInParent<MeshLifetime>().meshes.Add(mesh);return t;
        }
        static void Batch(Transform root)
        {
            var owner=root.GetComponent<MeshLifetime>();var groups=new Dictionary<Material,List<CombineInstance>>();var remove=new List<Transform>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh)continue;
                if(!groups.TryGetValue(renderer.sharedMaterial,out var list)){list=new List<CombineInstance>();groups[renderer.sharedMaterial]=list;}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});remove.Add(filter.transform);
            }
            foreach(var group in groups){var child=Root("Batched fittings / "+group.Key.name,root);var mesh=new Mesh{name=child.name};mesh.CombineMeshes(group.Value.ToArray(),true,true);owner.meshes.Add(mesh);child.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;child.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;}
            // Source meshes remain owned until this attachment is removed; source renderers
            // disappear immediately, avoiding duplicate geometry during the destroy frame.
            foreach(var child in remove){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}
        }
    }

    // Applies the recipe illustration inside the existing material pass. Coordinates
    // are transformed into equipment-root space so animated sockets do not make it swim.
    internal static class EquipmentProjection3D
    {
        [System.Serializable] sealed class Key { public string maker,kind,slot; public int @class,series,index,body,attachment; }
        [System.Serializable] sealed class Uv { public float x,y,width,height; }
        [System.Serializable] sealed class FamilyEntry { public string kind,sourceSheetResourcePath; public Key key; public Uv shaderSampleUv; }
        [System.Serializable] sealed class Manifest { public FamilyEntry[] items; }
        static Manifest manifest;
        static bool manifestRead;
        static readonly Dictionary<string,Rect[]> expandedGunBounds=new Dictionary<string,Rect[]>();
        static readonly string[] expandedGunClasses={"assault","handgun","shotgun","lmg","smg","sniper"};
        static readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        internal static MaterialPropertyBlock SharedBlock=>block;

        public static bool TryResolveMelee(MeleeWeaponFamily family,out string resourcePath,out Rect sampleUv)
        {
            resourcePath="Art/MeleeWeaponIcons";float cell=1f/MeleeWeaponCatalog.FamilyCount;
            int sourceCell=MeleeAtlasCell((int)family);
            float inset=cell-(cell/1.05f);float height=1f/1.05f;
            sampleUv=new Rect(sourceCell*cell+inset*.5f,(1f-height)*.5f,cell-inset,height);
            return Resources.Load<Texture2D>(resourcePath)!=null&&sourceCell>=0;
        }

        public static int MeleeAtlasCell(int family)=>family>=0&&family<MeleeWeaponCatalog.FamilyCount?new[]{0,0,1,2,3}[family]:-1;

        public static void ApplyMelee(Transform root,MeleeWeaponFamily family)
        {
            if(!root||HasProjection(root))return;
            if(!TryResolveMelee(family,out var path,out var crop))return;
            var sheet=Resources.Load<Texture2D>(path);if(!sheet)return;
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);bool applied=false;
            var rootBounds=new Bounds();bool hasRootBounds=false;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                var part=TransformBounds(filter.sharedMesh.bounds,root.worldToLocalMatrix*renderer.transform.localToWorldMatrix);
                if(!hasRootBounds){rootBounds=part;hasRootBounds=true;}else rootBounds.Encapsulate(part);
            }
            if(!hasRootBounds)return;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                bool supports=false;foreach(var material in renderer.sharedMaterials)if(material&&material.shader&&material.shader.name==DetailedHeroMaterials3D.ShaderName){supports=true;break;}
                if(!supports)continue;
                renderer.GetPropertyBlock(block);block.SetTexture("_ProjectionSheet",sheet);block.SetVector("_ProjectionUv",RectVector(crop));
                block.SetFloat("_AccessoryProjectionEnabled",0);block.SetVector("_ProjectionMin",rootBounds.min);
                block.SetVector("_ProjectionSize",new Vector4(Mathf.Max(rootBounds.size.x,.0001f),Mathf.Max(rootBounds.size.y,.0001f),Mathf.Max(rootBounds.size.z,.0001f),0));
                block.SetMatrix("_ProjectionWorldToRoot",root.worldToLocalMatrix);block.SetMatrix("_ProjectionWorldNormalToRoot",root.localToWorldMatrix.transpose);
                block.SetFloat("_ProjectionMode",2);block.SetFloat("_ProjectionEnabled",1);renderer.SetPropertyBlock(block);applied=true;
            }
            if(applied)
            {var follower=root.GetComponent<EquipmentProjectionFollower>();if(!follower)follower=root.gameObject.AddComponent<EquipmentProjectionFollower>();follower.Initialize(root,renderers);}
        }

        public static bool TryResolve(EquipmentRecipe recipe,bool gun,out string resourcePath,out Rect sampleUv)
        {
            resourcePath=null;sampleUv=default;
            var entry=Find(recipe,gun,false);
            if(entry==null)return false;
            resourcePath=entry.sourceSheetResourcePath;
            sampleUv=RectOf(entry.shaderSampleUv);
            return !string.IsNullOrEmpty(resourcePath)&&entry.shaderSampleUv!=null;
        }

        public static bool TryResolveAccessory(EquipmentRecipe recipe,out string resourcePath,out Rect sampleUv)
        {
            resourcePath=null;sampleUv=default;var entry=Find(recipe,false,true);if(entry==null)return false;
            resourcePath=entry.sourceSheetResourcePath;sampleUv=RectOf(entry.shaderSampleUv);
            return !string.IsNullOrEmpty(resourcePath)&&entry.shaderSampleUv!=null;
        }

        public static string DescribeLookup(EquipmentRecipe recipe,bool gun)
        {
            if(recipe==null)return "recipe=null";
            string maker=ManufacturerCatalog.Id(recipe.maker),slot=SlotName(recipe.slot);
            return gun
                ?$"maker={maker} kind={(recipe.legendary?"gun-index":"regular-no-kind")} class={recipe.gunClass} series={recipe.series} index={recipe.index%5} legendary={recipe.legendary}"
                :$"maker={maker} kind={(recipe.legendary?"armor-index":"regular-no-kind")} slot={slot} body={recipe.body} attachment={recipe.attachment} index={recipe.index%5} legendary={recipe.legendary}";
        }

        public static bool HasAccessoryProjection(Transform root)
        {
            if(!root)return false;
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f||!f.sharedMesh)continue;var marks=new List<Vector4>();f.sharedMesh.GetUVs(3,marks);
                if(marks.Count==0)continue;bool stamped=false;foreach(var mark in marks)if(mark.x>.5f){stamped=true;break;}
                if(!stamped)continue;var renderer=f.GetComponent<MeshRenderer>();if(!renderer)continue;
                renderer.GetPropertyBlock(block);if(block.GetTexture("_AccessoryProjectionSheet"))return true;
            }
            return false;
        }

        public static bool HasProjection(Transform root)
        {
            if(!root)return false;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer)continue;
                renderer.GetPropertyBlock(block);
                if(block.GetFloat("_ProjectionEnabled")>.5f)return true;
            }
            return false;
        }

        public static void Apply(Transform root,EquipmentRecipe recipe,bool gun)
        {
            if(!root||recipe==null||HasProjection(root))return;
            var entry=Find(recipe,gun,false);
            if(entry==null||entry.shaderSampleUv==null)return;
            var sheet=Resources.Load<Texture2D>(entry.sourceSheetResourcePath);
            if(!sheet)return;
            string fourViewKey=null;
            Vector4 fourViewSideUv=new Vector4(.02f,.35f,.96f,.30f);
            Texture2D fourView=gun?WeaponFourViewAtlas.Retain(recipe.id,out fourViewKey,out fourViewSideUv):null;
            var accessoryEntry=!gun&&!recipe.legendary?Find(recipe,false,true):null;
            var accessorySheet=accessoryEntry==null?null:Resources.Load<Texture2D>(accessoryEntry.sourceSheetResourcePath);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);bool hasProjectionRenderer=false;
            var rootBounds=new Bounds();bool hasRootBounds=false;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                var part=TransformBounds(filter.sharedMesh.bounds,root.worldToLocalMatrix*renderer.transform.localToWorldMatrix);
                if(!hasRootBounds){rootBounds=part;hasRootBounds=true;}else rootBounds.Encapsulate(part);
            }
            if(!hasRootBounds)return;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                bool supports=false;foreach(var material in renderer.sharedMaterials)if(material&&material.shader&&material.shader.name==DetailedHeroMaterials3D.ShaderName){supports=true;break;}
                if(!supports)continue;
                renderer.GetPropertyBlock(block);
                block.SetTexture("_ProjectionSheet",fourView?fourView:sheet);
                block.SetVector("_ProjectionUv",fourView?new Vector4(0,0,1,1):RectVector(Overscan(RectOf(entry.shaderSampleUv),1.05f)));
                if(fourView)block.SetVector("_ProjectionSideUv",fourViewSideUv);
                block.SetFloat("_AccessoryProjectionEnabled",accessorySheet&&accessoryEntry.shaderSampleUv!=null?1:0);
                if(accessorySheet&&accessoryEntry.shaderSampleUv!=null){block.SetTexture("_AccessoryProjectionSheet",accessorySheet);block.SetVector("_AccessoryProjectionUv",RectVector(RectOf(accessoryEntry.shaderSampleUv)));}
                block.SetVector("_ProjectionMin",rootBounds.min);
                block.SetVector("_ProjectionSize",new Vector4(Mathf.Max(rootBounds.size.x,.0001f),Mathf.Max(rootBounds.size.y,.0001f),Mathf.Max(rootBounds.size.z,.0001f),0));
                block.SetMatrix("_ProjectionWorldToRoot",root.worldToLocalMatrix);block.SetMatrix("_ProjectionWorldNormalToRoot",root.localToWorldMatrix.transpose);
                block.SetFloat("_ProjectionMode",fourView?3:gun?0:1);block.SetFloat("_ProjectionEnabled",1);
                renderer.SetPropertyBlock(block);
                hasProjectionRenderer=true;
            }
            if(hasProjectionRenderer)
            {
                var follower=root.GetComponent<EquipmentProjectionFollower>();
                if(!follower)follower=root.gameObject.AddComponent<EquipmentProjectionFollower>();
                follower.Initialize(root,renderers);
                if(fourView)follower.OwnAtlas(fourViewKey);
            }
            else if(fourView)WeaponFourViewAtlas.Release(fourViewKey);
        }

        public static void StampAccessoryGeometry(Transform armorRoot,Transform accessory,EquipmentRecipe recipe)
        {
            if(!armorRoot||!accessory||recipe==null)return;
            var entry=Find(recipe,false,true);if(entry==null||entry.shaderSampleUv==null)return;
            var sheet=Resources.Load<Texture2D>(entry.sourceSheetResourcePath);if(!sheet)return;
            var bounds=new Bounds();bool hasBounds=false;
            foreach(var f in accessory.GetComponentsInChildren<MeshFilter>(true))if(f&&f.sharedMesh)
            {var b=TransformBounds(f.sharedMesh.bounds,armorRoot.worldToLocalMatrix*f.transform.localToWorldMatrix);if(!hasBounds){bounds=b;hasBounds=true;}else bounds.Encapsulate(b);}
            if(!hasBounds)return;
            foreach(var f in accessory.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f||!f.sharedMesh)continue;
                // These meshes are generated per equipment instance; write the two projection
                // channels in place so the subsequent compact pass owns one native mesh only.
                var mesh=f.sharedMesh;
                var toRoot=armorRoot.worldToLocalMatrix*f.transform.localToWorldMatrix;var vertices=mesh.vertices;
                var uv=new List<Vector4>(vertices.Length);var marker=new List<Vector4>(vertices.Length);
                for(int i=0;i<vertices.Length;i++)
                {var p=toRoot.MultiplyPoint3x4(vertices[i]);uv.Add(new Vector4(Mathf.InverseLerp(bounds.min.x,bounds.max.x,p.x),Mathf.InverseLerp(bounds.min.y,bounds.max.y,p.y),0,0));marker.Add(Vector4.one);}
                mesh.SetUVs(2,uv);mesh.SetUVs(3,marker);
                var renderer=f.GetComponent<MeshRenderer>();if(renderer){renderer.GetPropertyBlock(block);block.SetTexture("_AccessoryProjectionSheet",sheet);block.SetVector("_AccessoryProjectionUv",RectVector(RectOf(entry.shaderSampleUv)));renderer.SetPropertyBlock(block);}
            }
        }

        // Transform a local-space AABB into equipment-root space. Summing the
        // absolute transformed half-axes handles rotation, scale and shear.
        static Bounds TransformBounds(Bounds source,Matrix4x4 matrix)
        {
            Vector3 center=matrix.MultiplyPoint3x4(source.center);
            Vector3 extents=source.extents;
            Vector3 x=matrix.MultiplyVector(new Vector3(extents.x,0,0));
            Vector3 y=matrix.MultiplyVector(new Vector3(0,extents.y,0));
            Vector3 z=matrix.MultiplyVector(new Vector3(0,0,extents.z));
            Vector3 worldExtents=new Vector3(
                Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),
                Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),
                Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
            return new Bounds(center,worldExtents*2f);
        }

        static FamilyEntry Find(EquipmentRecipe r,bool gun,bool accessory)
        {
            // The original family manifest predates the added series. Resolve
            // their painted side profiles directly, so every generated atlas
            // has a matching shader source and never falls back to bare metal.
            if(gun&&!accessory&&(r.series>=4&&!r.legendary||r.legendary&&r.index>=GunSeriesDesigns.FirstNewLegendary))
            {
                bool legendary=r.legendary;
                string path=legendary
                    ?"Art/BespokeLegendariesExpanded/"+ManufacturerCatalog.Id(r.maker)+"-legendaries-v2"
                    :"Art/GunSeriesExpanded/"+ManufacturerCatalog.Id(r.maker)+"-"+expandedGunClasses[Mathf.Clamp(r.gunClass,0,5)]+"-series-v2";
                int cell=legendary?LegendaryCatalog.VariantFromCatalogIndex(r.index)-5:r.series-4;
                if(!expandedGunBounds.TryGetValue(path,out var bounds))
                {
                    var data=Resources.Load<TextAsset>(path+"-bounds");
                    bounds=data?DistinctEquipmentPortraits.ParseBounds(data.text,legendary?10:8):null;
                    expandedGunBounds[path]=bounds;
                }
                if(bounds==null||cell<0||cell>=bounds.Length)return null;
                Rect crop=bounds[cell];
                return new FamilyEntry{kind="gun",sourceSheetResourcePath=path,shaderSampleUv=new Uv{x=crop.x,y=crop.y,width=crop.width,height=crop.height}};
            }
            if(!manifestRead)
            {
                manifestRead=true;
                var text=Resources.Load<TextAsset>("Art/GeneratedEquipment/Family/family-manifest");
                if(text)try{manifest=JsonUtility.FromJson<Manifest>(text.text);}catch(System.Exception){manifest=null;}
            }
            if(manifest?.items==null)return null;
            string maker=ManufacturerCatalog.Id(r.maker),slot=SlotName(r.slot);
            FamilyEntry match=null;int matchRank=-1;
            foreach(var e in manifest.items)
            {
                if(e==null||e.key==null||e.shaderSampleUv==null||e.key.maker!=maker)continue;
                bool matches=false;
                if(accessory){matches=e.kind=="armor"&&e.key.kind=="armor-accessory"&&e.key.slot==slot&&e.key.attachment==r.attachment;}
                if(gun)
                {if(r.legendary)matches=e.kind=="gun"&&e.key.kind=="gun"&&e.key.index==r.index%5;else matches=e.kind=="gun"&&string.IsNullOrEmpty(e.key.kind)&&e.key.@class==r.gunClass&&e.key.series==r.series;}
                else if(!accessory){if(r.legendary)matches=e.kind=="armor"&&e.key.kind=="armor"&&e.key.index==r.index%5;else matches=e.kind=="armor"&&string.IsNullOrEmpty(e.key.kind)&&e.key.slot==slot&&e.key.body==r.body;}
                if(!matches)continue;
                // Match the existing portrait resolver: prefer GroundedEquipment when
                // present for main surfaces; accessory details come from DistinctEquipment.
                int rank=e.sourceSheetResourcePath!=null&&e.sourceSheetResourcePath.Contains("/GroundedEquipment/")?2:1;
                if(rank<matchRank)continue;
                if(rank==matchRank&&match!=null&&(match.sourceSheetResourcePath!=e.sourceSheetResourcePath||!SameUv(match.shaderSampleUv,e.shaderSampleUv)))return null;
                match=e;matchRank=rank;
            }
            return match;
        }

        static bool SameUv(Uv a,Uv b)=>a!=null&&b!=null&&Mathf.Approximately(a.x,b.x)&&Mathf.Approximately(a.y,b.y)&&Mathf.Approximately(a.width,b.width)&&Mathf.Approximately(a.height,b.height);

        static string SlotName(int slot)=>slot==0?"head":slot==1?"torso":slot==2?"arms":slot==3?"legs":"feet";
        static Rect RectOf(Uv u)=>u==null?default:new Rect(u.x,u.y,u.width,u.height);
        static Rect Overscan(Rect source,float scale){var center=source.center;source.size/=Mathf.Max(1,scale);source.center=center;return source;}
        static Vector4 RectVector(Rect r)=>new Vector4(r.x,r.y,r.width,r.height);

    }

    // Root transforms can change with an animated equipment bone. Keep the shader's
    // world-to-root matrices current without adding projection varyings or surfaces.
    sealed class EquipmentProjectionFollower:MonoBehaviour
    {
        Transform root;Renderer[] renderers;
        string atlasKey;
        public void Initialize(Transform projectionRoot,Renderer[] projectionRenderers){root=projectionRoot;renderers=projectionRenderers;}
        public void OwnAtlas(string key){if(atlasKey==key){WeaponFourViewAtlas.Release(key);return;}WeaponFourViewAtlas.Release(atlasKey);atlasKey=key;}
        void OnDestroy(){WeaponFourViewAtlas.Release(atlasKey);}
        void LateUpdate()
        {
            if(!root||renderers==null)return;
            var worldToRoot=root.worldToLocalMatrix;var worldNormalToRoot=root.localToWorldMatrix.transpose;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;renderer.GetPropertyBlock(EquipmentProjection3D.SharedBlock);
                if(EquipmentProjection3D.SharedBlock.GetFloat("_ProjectionEnabled")<.5f)continue;
                EquipmentProjection3D.SharedBlock.SetMatrix("_ProjectionWorldToRoot",worldToRoot);
                EquipmentProjection3D.SharedBlock.SetMatrix("_ProjectionWorldNormalToRoot",worldNormalToRoot);
                renderer.SetPropertyBlock(EquipmentProjection3D.SharedBlock);
            }
        }
    }
}
