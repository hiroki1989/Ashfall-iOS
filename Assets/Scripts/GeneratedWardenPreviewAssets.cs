using UnityEngine;

namespace NeonVault
{
    /// <summary>Loads the mobile-optimized Hunyuan Warden set for the equipment inspection preview.</summary>
    static class GeneratedWardenPreviewAssets
    {
        const string Root="Art/GeneratedWarden/";
        // Bump the preference when the approved Warden model becomes the default. Older
        // installs may have persisted "legacy look" while the generated set was still
        // being tuned; carrying that opt-out forward made the inventory show the unrelated
        // procedural blue armor even when the starter Warden set was equipped.
        const string PreferenceKey="Vault.GeneratedWardenPreview.v2";
        public static bool Enabled=>PlayerPrefs.GetInt(PreferenceKey,1)!=0;
        public static void SetEnabled(bool enabled){PlayerPrefs.SetInt(PreferenceKey,enabled?1:0);PlayerPrefs.Save();}
        // One meter-space import contract shared by the portrait and the gameplay actor.
        public static GameObject CreateCharacterPart(string model,DetailedHero3D hero)
        {
            if(model=="body"&&Resources.Load<GameObject>(Root+"head-rebuilt"))
                return Create("head-rebuilt","head-rebuilt-atlas",hero.transform,Vector3.zero,Vector3.one);
            return Create(model,model+"-atlas-4096",hero.transform,
                new Vector3(0,model=="body"?1.09f:1f,0),Vector3.one*90f);
        }
        public static GameObject Create(string modelName,string textureName,Transform parent,Vector3 position,Vector3 scale)
        {
            var prefab=Resources.Load<GameObject>(Root+modelName+"-rebuilt");
            bool rebuiltModel=prefab;
            if(!prefab)prefab=Resources.Load<GameObject>(Root+modelName+"-mobile");
            bool mobileModel=prefab;
            if(!prefab)prefab=Resources.Load<GameObject>(Root+modelName+"-rig-ready");
            if(!prefab)prefab=Resources.Load<GameObject>(Root+modelName);
            if(!prefab){Debug.LogError("Missing generated Warden model: "+modelName);return null;}
            var instance=UnityEngine.Object.Instantiate(prefab,parent,false);instance.name="Warden / "+modelName;
            instance.transform.localPosition=position;instance.transform.localScale=scale;
            var texture=rebuiltModel?Resources.Load<Texture2D>(Root+modelName+"-rebuilt-atlas")
                :mobileModel?Resources.Load<Texture2D>(Root+modelName+"-mobile-atlas"):null;
            if(!texture)texture=Resources.Load<Texture2D>(Root+textureName);
            if(texture){texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=8;texture.mipMapBias=.45f;}
            var shader=Resources.Load<Shader>("GeneratedWardenTexture");if(!shader)shader=Resources.Load<Shader>("VaultSurface");
            if(texture&&shader){var material=new Material(shader){name="Warden / "+modelName+" atlas",mainTexture=texture};material.SetTexture("_MainTex",texture);if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.28f);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.28f);foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;}
            else Debug.LogError("Missing generated Warden texture or shader: "+textureName);
            return instance;
        }
        public static GameObject CreateRestingWeapon(string modelName,Transform parent,bool sidearm)
        {
            // Hunyuan exports the guns on a different forward axis from the gameplay rig.
            // Keep their silhouettes within the character's natural reach and turn the muzzle
            // across the torso instead of letting the long axis point out of frame.
            var instance=Create(modelName,modelName+"-atlas-4096",parent,Vector3.zero,Vector3.one*(sidearm?10f:32f));
            if(instance)instance.transform.localRotation=WeaponLowReadyRotation;
            return instance;
        }
        public static GameObject CreateRestingWeapon(string modelName,Transform parent,bool sidearm,float sizeMultiplier)
        {
            var instance=Create(modelName,modelName+"-atlas-4096",parent,Vector3.zero,Vector3.one*(sidearm?10f:32f)*sizeMultiplier);
            if(instance)instance.transform.localRotation=WeaponLowReadyRotation;
            return instance;
        }
        public static void PlaceWeaponInLowReady(GameObject visual,Transform characterRoot)
        {
            if(!visual||!characterRoot)return;
            bool sidearm=visual.name.IndexOf("sidearm",System.StringComparison.OrdinalIgnoreCase)>=0;
            visual.transform.localRotation=WeaponLowReadyRotation;
            var renderers=visual.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
            // Match the normal relaxed low-ready rig: stock stays near the shoulder and the
            // receiver sits just in front of the waist. The previous 0.82m forward / 1.52m high
            // anchor detached the weapon from the arms and made the rifle dominate the portrait.
            Vector3 gripCenter=sidearm?new Vector3(.07f,1.18f,.43f):new Vector3(.04f,1.20f,.50f);
            visual.transform.position+=characterRoot.TransformPoint(gripCenter)-bounds.center;
        }
        public static void PlaceWeaponOnSockets(GameObject visual,DetailedHeroEquipment3D.WeaponSockets sockets)
        {
            if(!visual||sockets==null||!sockets.trigger||!sockets.support||!sockets.muzzle||!sockets.stock)return;
            bool rifle=visual.name.IndexOf("rifle",System.StringComparison.OrdinalIgnoreCase)>=0;
            if(rifle)
            {
                // Authored landmarks on the source rifle, rather than its bounds centre.
                // The OBJ muzzle is -X; Unity FBX import mirrors that to +X.
                // Detect which imported transverse axis is up;
                // this handles the FBX conversion without an extra guessed 90 degree turn.
                Bounds local=LocalBounds(visual,visual.transform);
                bool yUp=local.size.y>=local.size.z;
                Vector3 up=yUp?Vector3.up:Vector3.forward;
                Vector3 SourcePoint(float x,float y,float z)
                {
                    float unit=local.size.x/1.9936f;
                    return local.center-Vector3.right*(x+.0037f)*unit
                        +up*(y+.0041f)*unit+(yUp?Vector3.forward:Vector3.down)*z*unit;
                }
                Vector3 palm=SourcePoint(.44f,-.04f,.01f);
                Vector3 support=SourcePoint(-.25f,-.035f,.01f);
                Vector3 target=sockets.support.position-sockets.trigger.position;
                if(target.sqrMagnitude>.0001f)
                    visual.transform.rotation=Quaternion.LookRotation(target.normalized,sockets.root.up)
                        *Quaternion.Inverse(Quaternion.LookRotation((support-palm).normalized,up));
                visual.transform.position+=sockets.trigger.position-visual.transform.TransformPoint(palm);
                return;
            }
            // Sidearms follow the same full 3D weapon frame used by the legacy
            // pistol pose, including the lowered idle pitch. Match the palm, not bounds.
            Bounds pistolBounds=LocalBounds(visual,visual.transform);
            bool pistolYUp=pistolBounds.size.y>=pistolBounds.size.z;
            Vector3 pistolUp=pistolYUp?Vector3.up:Vector3.forward;
            float pistolUnit=pistolBounds.size.x/1.987566f;
            Vector3 pistolPalm=pistolBounds.center-Vector3.right*.55f*pistolUnit
                +pistolUp*(-.18f)*pistolUnit;
            visual.transform.rotation=sockets.root.rotation
                *Quaternion.Inverse(Quaternion.LookRotation(Vector3.right,pistolUp));
            Vector3 palmTarget=sockets.trigger.position+sockets.root.up*(.012f*sockets.root.lossyScale.y);
            visual.transform.position+=palmTarget-visual.transform.TransformPoint(pistolPalm);
        }
        public static GameObject CreateFittedMask(Transform head)
        {
            if(!head)return null;
            // The mask FBX is authored against the full-character source scale. Use its
            // fitted accessory scale, then align its bounds to the nose/mouth plane.
            var wrapper=new GameObject("Warden / fitted head socket");wrapper.transform.SetParent(head,false);
            var visual=Create("mask","mask-atlas-4096",wrapper.transform,Vector3.zero,Vector3.one*12f);
            if(!visual){UnityEngine.Object.Destroy(wrapper);return null;}
            var renderers=visual.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return visual;
            Bounds bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
            // The generated head's face sits forward of the legacy head pivot. Align the
            // respirator to the lower-face plane while keeping its top below the eyes.
            // Normalize the accessory in head-local space; actor scale and facing must not
            // alter the fitted dimensions. This same socket is used in both screens.
            Bounds local=LocalBounds(visual,head);
            if(local.size.y>.0001f)wrapper.transform.localScale=Vector3.one*(CharacterRigStandard.MaskHeight/local.size.y);
            local=LocalBounds(visual,head);
            wrapper.transform.localPosition+=CharacterRigStandard.MaskCenter-local.center;
            return wrapper;
        }
        static Bounds LocalBounds(GameObject visual,Transform frame)
        {
            var bounds=new Bounds();bool first=true;
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.sharedMesh)continue;var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=frame.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }
        static Quaternion WeaponLowReadyRotation=>Quaternion.Euler(20f,150f,-40f)*Quaternion.AngleAxis(-90f,Vector3.right);
    }
}
