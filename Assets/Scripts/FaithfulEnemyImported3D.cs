using UnityEngine;

namespace NeonVault
{
    public sealed partial class FaithfulEnemy3D
    {
        // Independent authored parts. An incomplete set never hides the old enemy;
        // all thirteen meshes must exist before the new rig is constructed.
        static readonly string[] ImportedPartNames={
            "Head","Chest","Pelvis",
            "UpperArm_L","UpperArm_R","Forearm_L","Forearm_R",
            "Thigh_L","Thigh_R","Shin_L","Shin_R","Foot_L","Foot_R"
        };
        public bool ImportedShapeActive {get;private set;}

        internal static bool ImportedAssetsAvailable(int enemyKind)
        {
            string root="Art/Enemy3DRebuild/NewMesh/Enemy_"+enemyKind+"/";
            foreach(var name in ImportedPartNames)if(!Resources.Load<GameObject>(root+name))return false;
            return Resources.Load<Texture2D>(root+"texture-atlas")&&Resources.Load<Texture2D>(root+"texture-atlas-body")&&Resources.Load<Texture2D>(root+"texture-boots")&&
                Resources.Load<Shader>("Shaders/EnemyInkPaint3D");
        }

        bool TryBuildImportedEnemy()
        {
            string root="Art/Enemy3DRebuild/NewMesh/Enemy_"+kind+"/";
            var parts=new GameObject[ImportedPartNames.Length];
            for(int i=0;i<parts.Length;i++)
            {
                parts[i]=Resources.Load<GameObject>(root+ImportedPartNames[i]);
                if(!parts[i])return false;
            }
            var paint=Resources.Load<Texture2D>(root+"texture-atlas");
            var bodyPaint=Resources.Load<Texture2D>(root+"texture-atlas-body");
            var bootPaint=Resources.Load<Texture2D>(root+"texture-boots");
            var shader=Resources.Load<Shader>("Shaders/EnemyInkPaint3D");
            if(!paint||!bodyPaint||!bootPaint||!shader)return false;
            var material=new Material(shader){name="Enemy "+kind+" / illustrated paint",hideFlags=HideFlags.DontSave};
            material.SetTexture("_MainTex",paint);
            material.SetFloat("_Outline",.0016f);
            materials.Add(material);
            var bodyMaterial=new Material(shader){name="Enemy "+kind+" / clean torso paint",hideFlags=HideFlags.DontSave};
            bodyMaterial.SetTexture("_MainTex",bodyPaint);
            bodyMaterial.SetFloat("_Outline",.0016f);
            materials.Add(bodyMaterial);
            var bootMaterial=new Material(shader){name="Enemy "+kind+" / worn boot leather",hideFlags=HideFlags.DontSave};
            bootMaterial.SetTexture("_MainTex",bootPaint);
            bootMaterial.SetFloat("_Outline",.0016f);
            materials.Add(bootMaterial);

            bool brute=kind==3,gaunt=kind==4||kind==9;
            girth=brute?.39f:kind==7?.35f:kind==6?.31f:kind==8?.28f:kind==2?.26f:gaunt?.175f:.215f;
            armLength=kind==4?.40f:brute?.36f:.30f;
            legLength=brute?.40f:.43f;
            headHeight=kind==4?1.62f:1.78f;
            baseLean=kind==4?26:kind==9?13:brute?8:kind==7?3:8;
            rig=Joint("Complete articulated enemy / authored",transform,Vector3.zero);
            rig.localScale=Vector3.one*(heights[kind]/2.12f);
            pelvis=Joint("Pelvis",rig,V(0,.96f,0));
            chest=Joint("Chest",rig,V(0,1.34f,0));
            head=Joint("Head",chest,V(0,headHeight-1.34f,.02f));
            head.localScale=V(1,.88f,1);
            AttachImportedPart(parts[0],head,material);
            AttachImportedPart(parts[1],chest,bodyMaterial);
            AttachImportedPart(parts[2],pelvis,bodyMaterial);
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                float hip=girth*.54f;
                thighs[i]=Joint((side<0?"Left":"Right")+" thigh",pelvis,V(side*hip,-.025f,0));
                shins[i]=Joint("Shin",thighs[i],V(0,-legLength,0));
                feet[i]=Joint("Foot",shins[i],V(0,-legLength,0));
                arms[i]=Joint((side<0?"Left":"Right")+" upper arm",chest,V(side*(girth+.025f),.30f,0));
                forearms[i]=Joint("Elbow and forearm",arms[i],V(0,-armLength,0));
                AttachImportedPart(parts[3+i],arms[i],material);
                AttachImportedPart(parts[5+i],forearms[i],material);
                AttachImportedPart(parts[7+i],thighs[i],material);
                AttachImportedPart(parts[9+i],shins[i],material);
                AttachImportedPart(parts[11+i],feet[i],bootMaterial);
            }
            // Gameplay attack and hit coordinates remain attached to the new head.
            muzzle=Joint("Mouth attack origin",head,V(0,-.01f,.14f));
            ImportedShapeActive=true;
            Ready=true;previous=transform.position;LateUpdate();
            return true;
        }
        static void AttachImportedPart(GameObject prefab,Transform joint,Material paint)
        {
            var part=Object.Instantiate(prefab,joint,false);
            part.name=prefab.name+" / authored geometry";
            part.transform.localPosition=Vector3.zero;
            part.transform.localRotation=Quaternion.identity;
            part.transform.localScale=Vector3.one;
            foreach(var renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)slots[i]=paint;
                renderer.sharedMaterials=slots;
            }
        }
    }
}
