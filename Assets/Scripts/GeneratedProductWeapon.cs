using UnityEngine;

namespace NeonVault
{
    // Product meshes carry explicit grip and barrel landmarks; bounds are never a grip.
    public sealed class GeneratedProductWeapon : MonoBehaviour
    {
        string catalogId;
        Transform trigger, support, muzzle;
        Material atlasMaterial;

        static Transform Marker(GameObject model, string name)
        {
            foreach(var t in model.GetComponentsInChildren<Transform>(true))
                if(t.name==name||t.name=="weapon_"+name)return t;
            return null;
        }

        public static bool HasMarkers(GameObject model)
        {
            if(!model)return false;
            var t=Marker(model,"trigger");var s=Marker(model,"support");var m=Marker(model,"muzzle");
            return t&&s&&m&&(m.position-t.position).sqrMagnitude>1e-10f
                &&Vector3.Cross((m.position-t.position).normalized,(s.position-t.position).normalized).sqrMagnitude>1e-6f;
        }

        public static void Synchronize(ref GeneratedProductWeapon visual, Gun item,
            DetailedHeroEquipment3D.WeaponSockets sockets, Transform parent, bool enabled)
        {
            GeneratedEquipmentAssetCatalog.Entry entry=default(GeneratedEquipmentAssetCatalog.Entry);
            bool available=enabled&&GeneratedEquipmentAssetCatalog.TryExactGun(item,out entry);
            if(visual&&(!available||visual.catalogId!=item.catalogId))
            {visual.gameObject.SetActive(false);Object.Destroy(visual.gameObject);visual=null;}
            if(!available||sockets==null||!sockets.root||!sockets.trigger||!sockets.support||!sockets.muzzle)return;
            if(!visual)
            {
                var shader=Resources.Load<Shader>("GeneratedWardenTexture");
                if(!shader)shader=Resources.Load<Shader>("VaultSurface");
                if(!shader)return;
                var instance=Object.Instantiate(Resources.Load<GameObject>(entry.modelPath),parent,false);
                instance.name="Generated product gun / "+entry.catalogId;
                visual=instance.AddComponent<GeneratedProductWeapon>();visual.catalogId=entry.catalogId;
                visual.trigger=Marker(instance,"trigger");visual.support=Marker(instance,"support");visual.muzzle=Marker(instance,"muzzle");
                visual.atlasMaterial=new Material(shader){name="Product gun atlas / "+entry.catalogId,
                    mainTexture=Resources.Load<Texture2D>(entry.atlasPath)};
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)materials[i]=visual.atlasMaterial;
                    renderer.sharedMaterials=materials;
                }
            }
            // Work in the visual parent's space so actor scale and rotation cancel out.
            var root=visual.transform;root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;root.localScale=Vector3.one;
            Vector3 a=root.InverseTransformPoint(visual.trigger.position);
            Vector3 forward=root.InverseTransformPoint(visual.muzzle.position)-a;
            Vector3 hand=root.InverseTransformPoint(visual.support.position)-a;
            Vector3 target=parent.InverseTransformPoint(sockets.trigger.position);
            Vector3 targetForward=parent.InverseTransformPoint(sockets.muzzle.position)-target;
            Vector3 targetHand=parent.InverseTransformPoint(sockets.support.position)-target;
            Vector3 sourceUp=Vector3.ProjectOnPlane(hand,forward);
            Vector3 targetUp=Vector3.ProjectOnPlane(targetHand,targetForward);
            bool valid=forward.sqrMagnitude>1e-10f&&targetForward.sqrMagnitude>1e-10f
                &&sourceUp.sqrMagnitude>1e-10f&&targetUp.sqrMagnitude>1e-10f;
            visual.gameObject.SetActive(valid);
            if(!valid)return;
            root.localRotation=Quaternion.LookRotation(targetForward,targetUp)*Quaternion.Inverse(Quaternion.LookRotation(forward,sourceUp));
            root.localScale=Vector3.one*(targetForward.magnitude/forward.magnitude);
            root.localPosition=target-root.localRotation*Vector3.Scale(a,root.localScale);
            foreach(var renderer in sockets.root.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        }

        void OnDestroy(){if(atlasMaterial)Object.Destroy(atlasMaterial);}
    }
}
