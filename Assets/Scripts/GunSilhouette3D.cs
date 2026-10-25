using UnityEngine;

namespace NeonVault
{
    // Approved illustration alpha becomes a thin but closed volume. It keeps
    // mechanical holes and the actual stock/barrel outline in all camera views.
    internal static class GunSilhouette3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static bool TryBuildRegular(Transform parent,EquipmentRecipe recipe,out DetailedHeroEquipment3D.WeaponSockets result)
        {
            result=null;
            string key=WeaponFourViewAtlas.ModelKey(recipe.id);
            if(string.IsNullOrEmpty(key))return false;
            var prefab=Resources.Load<GameObject>("GunSilhouettes/"+key);
            if(!prefab)return false;
            var filter=prefab.GetComponentInChildren<MeshFilter>(true);
            if(!filter||!filter.sharedMesh)return false;

            int c=Mathf.Clamp(recipe.gunClass,0,5),s=Mathf.Clamp(recipe.series,0,11)%4;
            float rear=-.34f,end=.64f,grip=-.045f,support=.26f;
            switch(c)
            {
                case 0:end=s==1?.55f:s==3?.79f:s==2?.71f:.64f;if(s==1){rear=-.25f;grip=.065f;}break;
                case 1:rear=-.10f;end=s==0?.23f:s==1?.31f:s==2?.37f:.42f;support=-.025f;break;
                case 2:end=s==2?.46f:s==1?.64f:.73f;support=s==2?.21f:.34f;break;
                case 3:end=s==3?.87f:.77f;support=.33f;break;
                case 4:rear=-.26f;end=s==0?.40f:.48f;support=.20f;break;
                case 5:end=s==2?.82f:s==3?1.02f:.94f;support=.34f;if(s==2){rear=-.25f;grip=.065f;}break;
            }
            float edition=recipe.seriesVariant==1?.85f:recipe.seriesVariant==2?1.04f:recipe.seriesVariant==3?1.17f:1f;
            rear*=edition;end*=edition;grip*=edition;support*=edition;
            var root=new GameObject("Distinct product / illustration-shaped / "+recipe.id).transform;
            root.SetParent(parent,false);
            var body=new GameObject("Closed painted firearm volume").transform;
            body.SetParent(root,false);
            body.localPosition=V(0,0,rear);
            body.localScale=V(1,end-rear,end-rear);
            body.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
            body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=DetailedHeroMaterials3D.Get("ProductSteel");
            // Imported meshes have Read/Write disabled on iOS. Do not pass them
            // through the procedural mesh combiner used by the older builder.
            result=new DetailedHeroEquipment3D.WeaponSockets{
                root=root,
                trigger=Socket(root,"Trigger grip socket",V(0,-.095f,grip)),
                support=Socket(root,"Support grip socket",V(0,-.062f,support)),
                stock=Socket(root,"Shoulder stock socket",V(0,-.025f,rear)),
                muzzle=Socket(root,"True +Z firing axis",V(0,0,end))};
            return true;
        }
        static Transform Socket(Transform parent,string name,Vector3 position)
        {var socket=new GameObject(name).transform;socket.SetParent(parent,false);socket.localPosition=position;return socket;}
    }
}
