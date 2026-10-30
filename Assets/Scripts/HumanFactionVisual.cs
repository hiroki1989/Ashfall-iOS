using UnityEngine;
using System.Collections.Generic;

namespace NeonVault
{
    // Six human silhouettes share the survivor skeleton and its articulated gait.
    public sealed class HumanFactionVisual : MonoBehaviour
    {
        public DetailedHero3D Body { get; private set; }
        public Transform Muzzle { get; private set; }
        public int Faction { get; private set; }
        Vector3 previous;
        float phase, deathTime=-1;
        Foe owner;
        DetailedHeroEquipment3D.WeaponSockets gun;
        bool handgun;
        static Mesh sharedHelmetMesh, sharedTrimmedHeadMesh;
        static Material[] sharedHeadMaterials;
        static Bounds sharedHeadBounds;
        static SkinnedMeshRenderer CreateSharedHead(DetailedHero3D hero)
        {
            if(!sharedTrimmedHeadMesh || sharedHeadMaterials==null)return null;
            var root=new GameObject("Faction shared head");root.transform.SetParent(hero.transform,false);
            var skin=root.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=sharedTrimmedHeadMesh;
            skin.bones=CharacterRigStandard.GetBones(hero);skin.rootBone=hero.chest;
            skin.sharedMaterials=sharedHeadMaterials;skin.localBounds=sharedHeadBounds;
            return skin;
        }
        static readonly Mesh[] sharedArmorMeshes=new Mesh[5];
        static Material[] sharedArmorMaterials;
        static Bounds sharedArmorBounds;
        static SkinnedMeshRenderer[] CreateSharedArmor(DetailedHero3D hero)
        {
            var parts=new SkinnedMeshRenderer[5];var bones=CharacterRigStandard.GetBones(hero);
            for(int slot=1;slot<5;slot++)if(sharedArmorMeshes[slot])
            {
                var go=new GameObject("Faction shared armor / "+(ArmorSlot)slot);go.transform.SetParent(hero.transform,false);
                var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=sharedArmorMeshes[slot];skin.bones=bones;skin.rootBone=hero.chest;
                skin.sharedMaterials=sharedArmorMaterials;skin.localBounds=sharedArmorBounds;parts[slot]=skin;
            }
            return parts;
        }

        // faction: 0 military, 1 raiders, 2 gang. variant: 0 or 1.
        // Parent origin is the ground contact point; caller owns actor positioning.
        public static HumanFactionVisual Create(Transform parent, int faction, int variant)
        {
            faction=Mathf.Clamp(faction,0,2); variant=Mathf.Clamp(variant,0,1);
            var root=new GameObject("Human faction " + faction + " / " + variant);
            root.transform.SetParent(parent,false);
            var visual=root.AddComponent<HumanFactionVisual>();
            visual.Faction=faction;
            visual.Body=DetailedHero3D.Create(root.transform,key=>Palette(faction,variant,key));
            var legacy=visual.Body.GetComponentsInChildren<Renderer>(true);
            bool sharedBodyReady=sharedTrimmedHeadMesh && sharedHeadMaterials!=null && sharedArmorMeshes[1] && sharedArmorMeshes[2] && sharedArmorMeshes[3] && sharedArmorMeshes[4];
            var common=sharedBodyReady?CreateSharedHead(visual.Body):GeneratedCommonBodyRig.Create(visual.Body);
            var items=visual.BuildKit(faction,variant);
            if(common)
            {

                bool replacesArms=sharedBodyReady || GeneratedCommonBodyRig.ReplacesLegacyArms(common);
                foreach(var renderer in legacy)
                    if(renderer&&(!GeneratedCommonBodyRig.IsLegacyArmRenderer(renderer,visual.Body)||replacesArms))renderer.enabled=false;
                if(!sharedBodyReady)GeneratedCommonBodyRig.SetVisible(common,true);
                var covered=new bool[5];
                var fallback=new ArmorItem[5];
                // Use the player's articulated Warden equipment, not the
                // older procedural armor geometry, on the same survivor skeleton.
                SkinnedMeshRenderer[] parts;
                if(sharedArmorMeshes[1]&&sharedArmorMeshes[2]&&sharedArmorMeshes[3]&&sharedArmorMeshes[4])parts=CreateSharedArmor(visual.Body);
                else
                {
                    var armor=GeneratedWardenPreviewAssets.CreateCharacterPart("armor",visual.Body);
                    var scan=GeneratedWardenRig.Bind(armor,visual.Body,false,true,true);
                    parts=GeneratedWardenRig.SplitArmorBySlot(scan,visual.Body.transform);
                    if(scan){scan.enabled=false;sharedArmorMaterials=scan.sharedMaterials;sharedArmorBounds=scan.localBounds;}
                    else if(armor)armor.SetActive(false);
                    for(int slot=1;slot<parts.Length;slot++)if(parts[slot])sharedArmorMeshes[slot]=parts[slot].sharedMesh;
                }
                for(int slot=1;slot<parts.Length;slot++)
                {
                    if(parts[slot])parts[slot].enabled=true;
                    if(!parts[slot])fallback[slot]=items[slot];
                    covered[slot]=parts[slot] || fallback[slot]!=null;
                }
                root.AddComponent<HumanFactionArmorTextures>().Apply(parts,faction,variant);
                if(!sharedBodyReady)GeneratedCommonBodyRig.SetCoveredSlots(common,covered);
                DetailedHeroArsenal3D.ApplyArmor(visual.Body,fallback);
                ManufacturerEquipment3D.ApplyArmor(visual.Body,fallback);
                // ArmorVisuals.SetItems can re-enable the original trouser and seam
                // renderers. Suppress the captured base again after the armor pass,
                // matching the player order, without hiding newly created equipment.
                foreach(var renderer in legacy)
                    if(renderer&&(!GeneratedCommonBodyRig.IsLegacyArmRenderer(renderer,visual.Body)||replacesArms))renderer.enabled=false;
                if(!sharedBodyReady)GeneratedCommonBodyRig.SetCoveredSlots(common,covered);
            }
            else
            {
                Debug.LogWarning("Human faction: common body unavailable; using legacy survivor body.");
                DetailedHeroArsenal3D.ApplyArmor(visual.Body,items);
                ManufacturerEquipment3D.ApplyArmor(visual.Body,items);
            }
            if(common&&!sharedBodyReady)visual.TrimHeadUnderHelmet(common);
            // ApplyArmor re-enables legacy hair when the head slot is empty,
            // even when the common body replaced the legacy head. Hide it after
            // both armor passes for both common and fallback body paths.
            foreach(var renderer in visual.Body.head.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.name.IndexOf("hair",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                   renderer.name.IndexOf("scalp",System.StringComparison.OrdinalIgnoreCase)>=0)
                    renderer.enabled=false;
            visual.AddHelmet(faction);
            GeneratedWardenPreviewAssets.CreateFittedMask(visual.Body.head);
            // The same articulated character and outfit are used on iOS. Limit
            // per-vertex skinning work and avoid animating offscreen renderers.
            if(Application.isMobilePlatform)
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {skin.quality=SkinQuality.Bone2;skin.updateWhenOffscreen=false;}
            visual.owner=parent.GetComponentInParent<Foe>();
            visual.previous=root.transform.position;
            return visual;
        }
        // The approved head has hair and face in one mesh. Remove only the upper
        // portion hidden inside the helmet; do not alter the shared source mesh.
        void TrimHeadUnderHelmet(SkinnedMeshRenderer common)
        {
            foreach(var renderer in common.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(renderer.name!="Common body / approved shared head" || !renderer.sharedMesh)continue;
                var source=renderer.sharedMesh;
                if(sharedTrimmedHeadMesh){renderer.sharedMesh=sharedTrimmedHeadMesh;break;}
                var positions=source.vertices;
                sharedTrimmedHeadMesh=Instantiate(source);
                sharedTrimmedHeadMesh.name="Human faction / helmet-trimmed head";
                sharedHeadBounds=renderer.localBounds;
                var sourceMaterials=renderer.sharedMaterials;
                sharedHeadMaterials=new Material[sourceMaterials.Length];
                for(int i=0;i<sourceMaterials.Length;i++)if(sourceMaterials[i])sharedHeadMaterials[i]=new Material(sourceMaterials[i]){name="Faction shared head material"};
                // Bound head vertices are in the hero's rest-space coordinates.
                float cutoff=CharacterRigStandard.RestPositions[2].y+.14f;
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var triangles=source.GetTriangles(sub);
                    var kept=new List<int>(triangles.Length);
                    for(int i=0;i+2<triangles.Length;i+=3)
                    {
                        int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                        if(positions[a].y>cutoff || positions[b].y>cutoff || positions[c].y>cutoff)continue;
                        kept.Add(a);kept.Add(b);kept.Add(c);
                    }
                    sharedTrimmedHeadMesh.SetTriangles(kept,sub,false);
                }
                renderer.sharedMesh=sharedTrimmedHeadMesh;
                break;
            }
        }
        // Reuse the existing portrait helmet shell profile without its separate
        // full-face respirator, so the fitted Warden mask remains visible.
        void AddHelmet(int faction)
        {
            int rows=Application.isMobilePlatform?12:28, segments=Application.isMobilePlatform?24:64;
            var vertices=new Vector3[(rows+1)*(segments+1)];
            var triangles=new int[rows*segments*6];
            for(int row=0;row<=rows;row++)for(int i=0;i<=segments;i++)
            {
                float u=Mathf.Max(.004f,row/(float)rows), angle=i*Mathf.PI*2/segments;
                float rear=Mathf.Max(0,-Mathf.Sin(angle)), side=Mathf.Abs(Mathf.Cos(angle));
                // A low brow and slightly dropped rear rim cover the hair while
                // leaving the mask, ears and neck clear. A dome, not a tall hood.
                float bottom=.116f-.052f*rear-.045f*side;
                float radius=Mathf.Sin(u*Mathf.PI*.5f);
                float y=bottom+(.244f-bottom)*Mathf.Cos(u*Mathf.PI*.5f);
                float x=Mathf.Cos(angle)*.120f*radius;
                float z=.018f+Mathf.Sin(angle)*.122f*radius;
                vertices[row*(segments+1)+i]=new Vector3(x,y,z);
            }
            int index=0;
            for(int row=0;row<rows;row++)for(int i=0;i<segments;i++)
            {
                int a=row*(segments+1)+i,b=a+segments+1;
                triangles[index++]=a;triangles[index++]=a+1;triangles[index++]=b;
                triangles[index++]=a+1;triangles[index++]=b+1;triangles[index++]=b;
            }
            if(!sharedHelmetMesh)
            {
                sharedHelmetMesh=new Mesh{name="Human faction / helmet shell"};
                sharedHelmetMesh.vertices=vertices;sharedHelmetMesh.triangles=triangles;
                sharedHelmetMesh.RecalculateNormals();sharedHelmetMesh.RecalculateBounds();
            }
            var shell=new GameObject("Human faction / helmet");
            shell.transform.SetParent(Body.head,false);
            shell.AddComponent<MeshFilter>().sharedMesh=sharedHelmetMesh;
            var helmetRenderer=shell.AddComponent<MeshRenderer>();
            helmetRenderer.sharedMaterial=DetailedHeroMaterials3D.Get("ProductSteel");
            var helmetTint=new MaterialPropertyBlock();
            helmetTint.SetColor("_Color",faction==0?new Color(.67f,.76f,.56f):faction==1?new Color(.80f,.47f,.34f):new Color(.52f,.66f,.83f));
            helmetRenderer.SetPropertyBlock(helmetTint);
            var rim=DetailedHeroMaterials3D.Get("Dark");
            var hardware=DetailedHeroMaterials3D.Get("ProductSteel");
            // Thin rolled perimeter and side hardware make the dome read as
            // a protective helmet, while staying above the fitted face mask.
            for(int i=0;i<segments;i++)
            {
                Vector3 a=vertices[rows*(segments+1)+i], b=vertices[rows*(segments+1)+i+1];
                DetailedHeroArsenal3D.Rod("Helmet rolled rim",shell.transform,rim,a,b,.003f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                var mount=DetailedHeroArsenal3D.Tube("Helmet side fastener",shell.transform,hardware,Vector3.zero,.010f,.004f,.05f,-1,12);
                mount.localPosition=new Vector3(side*.121f,.098f,.018f);
                mount.localRotation=Quaternion.LookRotation(Vector3.right*side);
                DetailedHeroArsenal3D.Rod("Helmet side rail",shell.transform,rim,
                    new Vector3(side*.116f,.105f,-.035f),new Vector3(side*.116f,.115f,.075f),.004f);
            }

        }
        static Material Palette(int faction,int variant,string key)
        {
            if(key=="Cloth") return DetailedHeroMaterials3D.Get(faction==0?(variant==0?"MirelineOlive":"NavyCanvas"):faction==1?(variant==0?"LeatherDark":"CinderOrange"):(variant==0?"NavyCanvas":"CinderOrange"));
            if(key=="ClothDark") return DetailedHeroMaterials3D.Get(faction==0?"NavyCanvas":faction==1?"LeatherDark":"Dark");
            return DetailedHeroMaterials3D.Get(key);
        }
        ArmorItem[] BuildKit(int faction,int variant)
        {
            // Existing player equipment supplies tailored garments, armor, pouches and
            // boots. Distinct styles cover the shared body's clothing by slot.
            int style=faction==0?variant:faction==1?2+variant:variant==0?1:3;
            var items=new ArmorItem[5];
            for(int slot=1;slot<3;slot++)items[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=style};
            var steel=DetailedHeroMaterials3D.Get("ProductSteel");
            var accent=DetailedHeroMaterials3D.Get(faction==0?"MirelineOlive":faction==1?"Rust":"CinderOrange");
            if(faction==1)
            {
                // Riveted irregular scrap plates make the raiders asymmetric.
                var shoulder=variant==0?Body.leftUpperArm:Body.rightUpperArm;
                DetailedHeroArsenal3D.Shell("Raider salvaged shoulder",shoulder,accent,new Vector3(0,-.07f,.09f),new Vector2(.19f,.17f),.055f);
                DetailedHeroArsenal3D.Shell("Raider salvaged chest",Body.chest,steel,new Vector3(variant==0?-.08f:.08f,.015f,.16f),new Vector2(.20f,.25f),.045f);
            }
            else if(faction==2)
            {
                // Bright shoulder patch and diagonal webbing remain legible overhead.
                DetailedHeroArsenal3D.Shell("Gang shoulder colors",Body.leftUpperArm,accent,new Vector3(0,-.075f,.085f),new Vector2(.16f,.16f),.035f);
                DetailedHeroArsenal3D.Line("Gang diagonal harness",Body.chest,accent,.014f,new Vector3(-.17f,.17f,.15f),new Vector3(.13f,-.19f,.15f));
                if(variant==1)DetailedHeroArsenal3D.Shell("Gang back emblem",Body.chest,accent,new Vector3(0,.02f,-.18f),new Vector2(.22f,.24f),-.02f);
            }
            handgun=faction==2&&variant==0;
            gun=handgun?DetailedHeroEquipment3D.BuildPistol(Body.transform,DetailedHeroMaterials3D.Get):
                DetailedHeroEquipment3D.BuildRifle(Body.transform,DetailedHeroMaterials3D.Get);
            Muzzle=gun.muzzle;
            return items;
        }
        void LateUpdate()
        {
            if(!Body)return;
            float dt=Time.deltaTime;Vector3 delta=transform.position-previous;delta.y=0;previous=transform.position;
            float speed=dt>0?delta.magnitude/dt:0;
            if(speed>.05f)phase+=dt*Mathf.Clamp(speed*.55f,.5f,2.5f);
            if(owner&&owner.dead&&deathTime<0)deathTime=0;
            if(deathTime>=0)deathTime+=dt;
            Vector3 direction=transform.forward;
            DetailedHeroPlayer3D.Pose(Body,gun,handgun,true,direction,speed>.05f?phase:-1,
                dt>0?delta/dt:Vector3.zero,Mathf.Clamp01(speed/2));
            if(deathTime>=0)
            {
                float fall=Mathf.SmoothStep(0,1,Mathf.Clamp01(deathTime/.55f));
                Body.transform.localRotation=Quaternion.Euler(fall*75,0,fall*18);
                Body.transform.localPosition=Vector3.down*(fall*.12f);
            }
        }
    }
}




