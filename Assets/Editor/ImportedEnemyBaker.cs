#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NeonVault
{
    public static class ImportedEnemyBaker
    {
        const string Output="Assets/Resources/ImportedEnemies";
        [MenuItem("NEON VAULT/Assets/Bake Imported Enemies")]
        public static void Bake()
        {
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            BakeOne("Enemy_0","Assets/ZombieMale_AAB/Prefabs/ZombieMale_AAB.prefab",2.05f);
            BakeOne("Enemy_4","Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE.prefab",1.9f);
            BakeOne("Enemy_7","Assets/ArtStore3D/Fat Zombie(Low Poly)/Prefab/FatZombie.prefab",2.5f);
            BakeOne("Enemy_3","Assets/OrkDestroyer2/Prefab/OrkDestroyer2.prefab",3.5f);
            foreach(int kind in new[]{1,2,5,6,8,9})
                BakeOne("Enemy_"+kind,kind==8?"Assets/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin3.prefab":"Assets/Mutant1/Prefab/Mutated1.prefab",kind==9?2.65f:2.16f);
            BakeOne("Enemy_10","Assets/ArtStore3D/Insurgent (Urban)/Prefab/InsurgentUrban.prefab",2.05f);
            BakeOne("Enemy_11","Assets/Croc men/Prefab/Skin_1.prefab",2.7f);
            BakeOne("RaiderBossNPC","Assets/Croc men/Prefab/Skin_1.prefab",2.1f);
            // Insurgent source here is the complete prefab included in the valid Fat Zombie package.
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void BakeAdjusted(){BakeOne("Enemy_3","Assets/OrkDestroyer2/Prefab/OrkDestroyer2.prefab",3.5f);BakeOne("Enemy_10","Assets/ArtStore3D/Insurgent (Urban)/Prefab/InsurgentUrban.prefab",2.05f);AssetDatabase.SaveAssets();}
        static void BakeOne(string name,string source,float height)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if(!prefab){Debug.LogWarning("Imported enemy source missing: "+source);return;}
            var root=new GameObject(name);var clone=UnityEngine.Object.Instantiate(prefab,root.transform,false);clone.name=prefab.name;
            try
            {
                if(source.Contains("OrkDestroyer2"))
                {
                    // Leggs1 has incompatible skin binding in the supplied prefab, including
                    // during fallback gait. Keep the coherent full bare body; preserve source assets.
                    foreach(var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if(skin.name=="Leggs1")skin.enabled=false;
                }
                // Source LOD screen thresholds/fade shaders were authored for different cameras.
                // Keep one coherent highest-detail set and remove the controller on this curated copy.
                foreach(var lodGroup in clone.GetComponentsInChildren<LODGroup>(true))
                {
                    var lods=lodGroup.GetLODs();
                    var first=lods.FirstOrDefault(l=>l.renderers!=null&&l.renderers.Any(r=>r));
                    var selected=first.renderers??new Renderer[0];
                    foreach(var lod in lods)foreach(var renderer in lod.renderers)
                        if(renderer)renderer.enabled=selected.Contains(renderer);
                    UnityEngine.Object.DestroyImmediate(lodGroup);
                }
                Transform rifle=null;
                if(name=="Enemy_10")
                {
                    var gun=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArtStore3D/Insurgent (Urban)/Prefab/Ak47.prefab");
                    if(gun)
                    {
                        rifle=new GameObject("Raider native AK47 grip").transform;rifle.SetParent(clone.transform,false);
                        var mesh=UnityEngine.Object.Instantiate(gun,rifle,false);
                        var filters=mesh.GetComponentsInChildren<MeshFilter>();
                        if(filters.Length>0&&filters[0].sharedMesh)
                        {
                            Bounds gunBounds=filters[0].sharedMesh.bounds;
                            Vector3 axis=gunBounds.size.x>gunBounds.size.z?Vector3.right:Vector3.forward;
                            mesh.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward);
                            float length=Mathf.Max(gunBounds.size.x,gunBounds.size.z);
                            mesh.transform.localScale=Vector3.one*(.88f/Mathf.Max(.001f,length));
                            mesh.transform.localPosition=-(mesh.transform.localRotation*(gunBounds.center*mesh.transform.localScale.x))+new Vector3(0,.075f,.17f);
                        }
                    }
                }
                foreach(var transform in clone.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))if(behaviour)UnityEngine.Object.DestroyImmediate(behaviour);
                foreach(var collider in clone.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                foreach(var body in clone.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                foreach(var joint in clone.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
                foreach(var camera in clone.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(camera);
                foreach(var light in clone.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light);
                foreach(var audio in clone.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);
                foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)if(mats[i])
                    {
                        var old=mats[i];var material=new Material(UnityEditor.AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader"));
                        material.name=name+"_"+old.name;
                        material.mainTexture=ReadTexture(old,"_MainTex","_BaseMap","_Albedo","_AlbedoMap","_BaseColorMap","_Diffuse","_DiffuseMap");
                        if(old.HasProperty("_MainTex")){material.mainTextureScale=old.GetTextureScale("_MainTex");material.mainTextureOffset=old.GetTextureOffset("_MainTex");}
                        material.color=old.HasProperty("_BaseColor")?old.GetColor("_BaseColor"):old.HasProperty("_Color")?old.color:Color.white;
                        Color opaque=material.color;opaque.a=1;material.color=opaque;
                        material.SetFloat("_Mode",0);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",0);material.SetFloat("_ZWrite",1);
                        material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.SetOverrideTag("RenderType","Opaque");material.renderQueue=-1;
                        var normal=ReadTexture(old,"_BumpMap","_NormalMap","_Normal");
                        if(normal){material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");}
                        material.SetFloat("_Glossiness",.2f);
                        string path=Output+"/"+name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(old))+".mat";
                        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(existing){EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);mats[i]=existing;}
                        else{AssetDatabase.CreateAsset(material,path);mats[i]=material;}
                    }
                    renderer.sharedMaterials=mats;
                }
                var visible=clone.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(!rifle||!r.transform.IsChildOf(rifle))).ToArray();
                if(visible.Length==0)throw new InvalidOperationException("No visible model: "+source);
                Bounds bounds=(source.Contains("Croc men")||source.Contains("OrkDestroyer2"))?RendererBounds(visible):ModelBounds(visible);
                if(bounds.size.y<.001f)throw new InvalidOperationException("Empty model bounds: "+source);
                clone.transform.localScale*=height/bounds.size.y;
                // Match the readable quarter-view silhouette of the existing hero.
                clone.transform.localScale=Vector3.Scale(clone.transform.localScale,new Vector3(1.16f,1,1.12f));
                bounds=(source.Contains("Croc men")||source.Contains("OrkDestroyer2"))?RendererBounds(visible):ModelBounds(visible);
                clone.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z);
                var driver=root.AddComponent<ImportedEnemyVisual>();driver.model=clone.transform;driver.targetHeight=height;
                if(rifle)
                {
                    driver.raiderRifle=rifle;
                    // Coordinates derive from the normalized gun mesh: rear pistol grip and fore-end.
                    var gripSocket=new GameObject("AK47 pistol grip contact").transform;
                    gripSocket.SetParent(rifle,false);gripSocket.localPosition=GunContact(rifle,.035f,false);
                    var supportSocket=new GameObject("AK47 fore-end contact").transform;
                    supportSocket.SetParent(rifle,false);supportSocket.localPosition=GunContact(rifle,.25f,true);
                    driver.rifleGripSocket=gripSocket;driver.rifleSupportSocket=supportSocket;
                    // Counter model normalization: weapon dimensions stay in metres.
                    rifle.localScale=new Vector3(1/clone.transform.localScale.x,1/clone.transform.localScale.y,1/clone.transform.localScale.z);
                }
                string folder=source.Substring(0,source.IndexOf('/',7));
                // Restrict discovery to the owning asset pack, never global unrelated clips.
                if(source.StartsWith("Assets/ArtStore3D/"))folder=source.Contains("Insurgent")?"Assets/ArtStore3D/Insurgent (Urban)":"Assets/ArtStore3D/Fat Zombie(Low Poly)";
                if(source.StartsWith("Assets/NewPunch/"))folder="Assets/NewPunch/ShirtlessZombieFree";
                var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{folder}).SelectMany(g=>AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g))).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")&&!c.name.ToLowerInvariant().Contains("cam")).Distinct().ToArray();
                driver.idleClip=Pick(clips,"idle");driver.walkClip=Pick(clips,"walk","run");driver.attackClip=Pick(clips,"attack","atack");driver.deathClip=Pick(clips,"death","die");
                if(source.Contains("OrkDestroyer2"))
                {
                    // All imported Ork takes remain quarantined until its skin binding is repaired.
                    driver.idleClip=PickModelClip(folder,"idle");driver.walkClip=driver.attackClip=driver.deathClip=null;
                }
                var humanoid=clone.GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.avatar&&a.avatar.isHuman);
                foreach(var animator in clone.GetComponentsInChildren<Animator>()){animator.applyRootMotion=false;animator.runtimeAnimatorController=null;animator.enabled=false;}
                if(humanoid&&new[]{driver.idleClip,driver.walkClip,driver.attackClip,driver.deathClip}.Any(c=>c&&c.humanMotion))
                {
                    string controllerPath=Output+"/"+name+".controller";
                    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    var machine=controller.layers[0].stateMachine;
                    foreach(var state in machine.states)machine.RemoveState(state.state);
                    string[] names={"idle","walk","attack","death"};
                    var motions=new[]{driver.idleClip,driver.walkClip,driver.attackClip,driver.deathClip};
                    for(int i=0;i<names.Length;i++)if(motions[i]){var state=machine.AddState(names[i]);state.motion=motions[i];if(i==0)machine.defaultState=state;}
                    humanoid.runtimeAnimatorController=controller;driver.nativeAnimator=humanoid;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+name+".prefab");
                Debug.Log("Imported enemy baked: "+name+" / clips="+clips.Length);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static Vector3 GunContact(Transform rifle,float longitudinal,bool support)
        {
            // Find the visible gun section in normalized mesh space, independent of source pivot.
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
            foreach(var filter in rifle.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point=rifle.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if(Mathf.Abs(point.z-longitudinal)>.035f)continue;
                    minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);
                    minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);
                }
            }
            if(float.IsInfinity(minY))return new Vector3(0,support?.045f:-.015f,longitudinal);
            // Left palm supports the wood from below; right palm contacts the pistol-grip centre.
            return new Vector3((minX+maxX)*.5f,support?minY-.008f:Mathf.Lerp(minY,maxY,.25f),longitudinal);
        }
        static Texture ReadTexture(Material source,params string[] properties)
        {
            foreach(string property in properties)
                if(source.HasProperty(property)&&source.GetTexture(property))return source.GetTexture(property);
            // Older packages retain texture slots even when their original shader is unavailable.
            var serialized=new SerializedObject(source);
            var entries=serialized.FindProperty("m_SavedProperties.m_TexEnvs");
            if(entries!=null)
                foreach(string property in properties)
                    for(int i=0;i<entries.arraySize;i++)
                    {
                        var entry=entries.GetArrayElementAtIndex(i);
                        if(entry.FindPropertyRelative("first").stringValue!=property)continue;
                        var texture=entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                        if(texture)return texture;
                    }
            return null;
        }
        static Bounds RendererBounds(Renderer[] renderers){Bounds result=renderers[0].bounds;foreach(var r in renderers)result.Encapsulate(r.bounds);return result;}
        static Bounds ModelBounds(Renderer[] renderers)
        {
            bool initialized=false;Bounds result=new Bounds();
            foreach(var renderer in renderers)
            {
                var skinned=renderer as SkinnedMeshRenderer;
                if(skinned&&skinned.sharedMesh)
                {
                    var mesh=new Mesh();
                    try
                    {
                        skinned.BakeMesh(mesh);
                        foreach(var vertex in mesh.vertices)
                        {
                            Vector3 point=skinned.transform.TransformPoint(vertex);
                            if(!initialized){result=new Bounds(point,Vector3.zero);initialized=true;}else result.Encapsulate(point);
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(mesh);}
                }
                else
                {
                    if(!initialized){result=renderer.bounds;initialized=true;}else result.Encapsulate(renderer.bounds);
                }
            }
            return result;
        }
        static AnimationClip PickModelClip(string folder,params string[] words)
        {
            var paths=AssetDatabase.FindAssets("t:Model",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p.Length).ThenBy(p=>p,StringComparer.Ordinal).ToArray();
            foreach(var word in words)foreach(var path in paths)
            {
                if(Path.GetFileNameWithoutExtension(path).IndexOf("@"+word,StringComparison.OrdinalIgnoreCase)<0)continue;
                var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));if(clip)return clip;
            }
            return null;
        }
        static AnimationClip Pick(AnimationClip[] clips,params string[] words)
        {foreach(string word in words){var clip=clips.FirstOrDefault(c=>c.name.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);if(clip)return clip;}return null;}
    }
}
#endif
