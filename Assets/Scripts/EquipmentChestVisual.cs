using UnityEngine;

namespace NeonVault
{
    /// <summary>The orange boxes_pack supply variant is reserved for equipment chests.</summary>
    public sealed class EquipmentChestVisual : MonoBehaviour
    {
        public const string ResourcePath="ImportedWorld/props/pbr_box_supply";
        public const string NormalResourcePath="ImportedWorld/props/pbr_box_ammo";
        GameObject model;
        GameObject animationRoot;
        AnimationClip clip;
        Vector3 animationPosition,animationScale;
        Quaternion animationRotation;
        float elapsed;
        bool opening;

        public static bool IsReserved(GameObject prefab)=>prefab&&
            (prefab.name=="pbr_box_supply"||prefab.name=="box_supply"||prefab.name=="pbr_box_ammo"||prefab.name=="box_ammo");

        public static void Create(Transform parent, bool rare=false)
        {
            var source=Resources.Load<GameObject>(rare?ResourcePath:NormalResourcePath);
            if(!source){Debug.LogError("Equipment chest prefab missing: "+ResourcePath);return;}
            var visual=parent.gameObject.AddComponent<EquipmentChestVisual>();
            visual.model=Instantiate(source,parent,false);
            foreach(var animator in visual.model.GetComponentsInChildren<Animator>(true))
            {
                var controller=animator.runtimeAnimatorController;
                animator.enabled=false;
                if(controller!=null)foreach(var candidate in controller.animationClips)
                    if(candidate&&candidate.name.Contains("opened_closed")){visual.clip=candidate;visual.animationRoot=animator.gameObject;break;}
            }
            foreach(var animation in visual.model.GetComponentsInChildren<Animation>(true))
            {animation.Stop();animation.enabled=false;}
            foreach(var collider in visual.model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            // Sample only the closed pose; never let the imported looping controller run.
            if(visual.clip)
            {
                var t=visual.animationRoot.transform;
                visual.animationPosition=t.localPosition;visual.animationRotation=t.localRotation;visual.animationScale=t.localScale;
                visual.SamplePose(0);
            }
            var renderers=visual.model.GetComponentsInChildren<Renderer>();
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                visual.model.transform.localScale*=1.8f/Mathf.Max(.01f,Mathf.Max(bounds.size.x,bounds.size.z));
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Vector3 origin=parent.position;
                visual.model.transform.position+=new Vector3(origin.x-bounds.center.x,origin.y-bounds.min.y,origin.z-bounds.center.z);
            }
            visual.enabled=false;
        }
        public static void SetRare(GameObject root)
        {
            if(!root)return;
            var old=root.GetComponent<EquipmentChestVisual>();
            if(old){if(old.model){old.model.SetActive(false);Destroy(old.model);}Destroy(old);}
            Create(root.transform,true);
        }
        public static void Open(GameObject root)
        {
            if(!root)return;
            var visuals=root.GetComponents<EquipmentChestVisual>();
            var visual=visuals.Length>0?visuals[visuals.Length-1]:null;
            if(!visual||visual.opening)return;
            visual.opening=true;visual.elapsed=0;visual.enabled=true;
        }
        void SamplePose(float time)
        {
            clip.SampleAnimation(animationRoot,time);
            // Animation belongs to the nested skeleton. Retain the authored root
            // placement and scale, leaving the outer normalized model untouched.
            var t=animationRoot.transform;
            t.localPosition=animationPosition;t.localRotation=animationRotation;t.localScale=animationScale;
        }
        void Update()
        {
            // The authored clip opens then closes over two seconds. Play its
            // opening half once and retain that pose until this world unloads.
            if(!clip||!model){enabled=false;return;}
            float end=clip.length*.5f;
            elapsed=Mathf.Min(end,elapsed+Time.deltaTime);
            SamplePose(elapsed);
            if(elapsed>=end)enabled=false;
        }
    }
}
