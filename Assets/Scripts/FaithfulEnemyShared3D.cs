using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault
{
    public sealed partial class FaithfulEnemy3D
    {
        // Immutable geometry descriptions, never live actor/prototype GameObjects.
        // Animation joints and gameplay owner always belong to the individual actor.
        sealed class SharedNode
        {
            public string name;public int parent;public Vector3 position,scale;public Quaternion rotation;
            public Mesh mesh;public Material[] materials;public ShadowCastingMode shadows;public bool receiveShadows,enabled;
        }
        sealed class SharedShape
        {
            public readonly List<SharedNode> nodes=new List<SharedNode>();
            public readonly List<Mesh> meshes=new List<Mesh>();public readonly List<Material> materials=new List<Material>();
            public int rig,pelvis,chest,head,muzzle;public int[] thighs,shins,feet,arms,forearms;
            public float girth,armLength,legLength,headHeight,baseLean;
        }
        static readonly Dictionary<int,SharedShape> sharedShapes=new Dictionary<int,SharedShape>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSharedGeometry()
        {
            ReleaseSharedGeometry();Application.quitting-=ReleaseSharedGeometry;Application.quitting+=ReleaseSharedGeometry;
        }
        static void ReleaseSharedGeometry()
        {
            foreach(var shape in sharedShapes.Values)
            {
                foreach(var mesh in shape.meshes)if(mesh)Object.Destroy(mesh);
                foreach(var material in shape.materials)if(material)Object.Destroy(material);
            }
            sharedShapes.Clear();
        }
        void CacheSharedGeometry()
        {
            var shape=new SharedShape{girth=girth,armLength=armLength,legLength=legLength,headHeight=headHeight,baseLean=baseLean};
            var indices=new Dictionary<Transform,int>();
            CaptureSharedNode(transform,-1,shape,indices);
            shape.rig=SharedIndex(rig,indices);shape.pelvis=SharedIndex(pelvis,indices);shape.chest=SharedIndex(chest,indices);shape.head=SharedIndex(head,indices);shape.muzzle=SharedIndex(muzzle,indices);
            shape.thighs=SharedIndices(thighs,indices);shape.shins=SharedIndices(shins,indices);shape.feet=SharedIndices(feet,indices);shape.arms=SharedIndices(arms,indices);shape.forearms=SharedIndices(forearms,indices);
            // Cache owns resources after successful capture. Actor destruction cannot invalidate peers.
            shape.meshes.AddRange(meshes);shape.materials.AddRange(materials);meshes.Clear();materials.Clear();sharedShapes.Add(kind,shape);
        }
        static int SharedIndex(Transform t,Dictionary<Transform,int> indices)=>t?indices[t]:-1;
        static int[] SharedIndices(Transform[] nodes,Dictionary<Transform,int> indices){var result=new int[nodes.Length];for(int i=0;i<nodes.Length;i++)result[i]=SharedIndex(nodes[i],indices);return result;}
        static void CaptureSharedNode(Transform parent,int parentIndex,SharedShape shape,Dictionary<Transform,int> indices)
        {
            for(int i=0;i<parent.childCount;i++)
            {
                var child=parent.GetChild(i);
                // Batch has already retired source meshes; Destroy runs at frame end.
                if(!child.gameObject.activeSelf)continue;
                var filter=child.GetComponent<MeshFilter>();var renderer=child.GetComponent<MeshRenderer>();
                int index=shape.nodes.Count;indices.Add(child,index);
                shape.nodes.Add(new SharedNode{name=child.name,parent=parentIndex,position=child.localPosition,rotation=child.localRotation,scale=child.localScale,mesh=filter?filter.sharedMesh:null,materials=renderer?renderer.sharedMaterials:null,enabled=renderer&&renderer.enabled,shadows=renderer?renderer.shadowCastingMode:ShadowCastingMode.On,receiveShadows=renderer&&renderer.receiveShadows});
                CaptureSharedNode(child,index,shape,indices);
            }
        }
        bool TryBuildSharedGeometry()
        {
            if(!sharedShapes.TryGetValue(kind,out var shape))return false;
            var nodes=new Transform[shape.nodes.Count];
            for(int i=0;i<nodes.Length;i++)
            {
                var source=shape.nodes[i];var node=new GameObject(source.name).transform;nodes[i]=node;
                node.SetParent(source.parent<0?transform:nodes[source.parent],false);node.localPosition=source.position;node.localRotation=source.rotation;node.localScale=source.scale;
                if(source.mesh){node.gameObject.AddComponent<MeshFilter>().sharedMesh=source.mesh;var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.materials;renderer.enabled=source.enabled;renderer.shadowCastingMode=source.shadows;renderer.receiveShadows=source.receiveShadows;}
            }
            rig=nodes[shape.rig];pelvis=nodes[shape.pelvis];chest=nodes[shape.chest];head=nodes[shape.head];muzzle=shape.muzzle<0?null:nodes[shape.muzzle];
            BindSharedJoints(thighs,shape.thighs,nodes);BindSharedJoints(shins,shape.shins,nodes);BindSharedJoints(feet,shape.feet,nodes);BindSharedJoints(arms,shape.arms,nodes);BindSharedJoints(forearms,shape.forearms,nodes);
            girth=shape.girth;armLength=shape.armLength;legLength=shape.legLength;headHeight=shape.headHeight;baseLean=shape.baseLean;
            Ready=true;previous=transform.position;LateUpdate();return true;
        }
        static void BindSharedJoints(Transform[] target,int[] indices,Transform[] nodes){for(int i=0;i<target.Length;i++)target[i]=indices[i]<0?null:nodes[indices[i]];}
    }
}
