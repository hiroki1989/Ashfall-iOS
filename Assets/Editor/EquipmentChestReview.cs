#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NeonVault
{
    // Asset-level review without creating a game, invoking Awake, or touching saves.
    public static class EquipmentChestReview
    {
        [MenuItem("NEON VAULT/Review/Equipment chest assets")]
        public static void Run()
        {
            GameObject root=null;
            try
            {
                Check(VaultGame.OverworldChestCount==500,"500 persistent world chest slots");
                Check(new OverworldGeography().Chests.Length==500,"500 generated world chest sites");
                root=new GameObject("Equipment chest review");root.transform.position=new Vector3(13,0,27);
                EquipmentChestVisual.Create(root.transform);
                var visual=root.GetComponent<EquipmentChestVisual>();
                Check(visual!=null&&!visual.enabled,"Closed chest has no running animation");
                foreach(var animator in root.GetComponentsInChildren<Animator>(true))Check(!animator.enabled,"Imported animator is disabled");
                var renderers=root.GetComponentsInChildren<Renderer>();Check(renderers.Length>0,"Imported chest renders");
                Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Check(Mathf.Abs(Mathf.Max(bounds.size.x,bounds.size.z)-1.8f)<.02f,"Closed chest normalized to 1.8 metres");
                Check(Mathf.Abs(bounds.min.y)<.02f,"Closed chest rests on ground");
                var clip=(AnimationClip)typeof(EquipmentChestVisual).GetField("clip",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(visual);
                Check(clip!=null&&clip.length>0,"Authored opening clip available in player resources");
                Transform cap=Array.Find(root.GetComponentsInChildren<Transform>(),t=>t.name=="Bone_cap");
                Check(cap!=null,"Authored lid hinge exists");
                Quaternion closed=cap.localRotation;
                Transform model=root.transform.GetChild(0);Vector3 position=model.position,scale=model.localScale;
                typeof(EquipmentChestVisual).GetMethod("SamplePose",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(visual,new object[]{clip.length*.5f});
                Check(Quaternion.Angle(closed,cap.localRotation)>10,"Opening clip actually moves the lid");
                Check((model.position-position).sqrMagnitude<.00001f&&(model.localScale-scale).sqrMagnitude<.00001f,"Opening retains normalized placement and scale");
                EquipmentChestVisual.Open(root);Check(visual.enabled,"Interaction starts opening");
                Debug.Log("EQUIPMENT_CHEST_REVIEW_COMPLETED / prefab closed, opening hinge, ground, scale, count passed; saves untouched");
            }
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        }
        static void Check(bool success,string message)
        {if(!success)throw new InvalidOperationException("Equipment chest review: "+message);}
    }
}
#endif
