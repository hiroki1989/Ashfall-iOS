using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor
{
    [InitializeOnLoad]
    public sealed class RuinedCityBuildingImporter : IPreprocessBuildWithReport
    {
        // New directory leaves the earlier generated assets intact and forces a
        // fresh prefab when the visual treatment changes.
        const string Output="Assets/Resources/ImportedWorld/RuinedCityBuildingsV2";
        static readonly string[] Sources={
            "Assets/POLYGON city pack/Prefabs/Buildings/Shop_A_prefab.prefab",
            "Assets/POLYGON city pack/Prefabs/Buildings/Shop_B_prefab.prefab",
            "Assets/POLYGON city pack/Prefabs/Buildings/Motel_prefab.prefab",
            "Assets/POLYGON city pack/Prefabs/Buildings/Car_repair_prefab.prefab",
            "Assets/POLYGON city pack/Prefabs/Buildings/Supermaket_prefab.prefab",
            "Assets/POLYGON city pack/Prefabs/Buildings/Fire_department_prefab.prefab"
        };
        static RuinedCityBuildingImporter(){EditorApplication.delayCall+=EnsureAssets;}
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report){EnsureAssets();}
        public static void EnsureAssets()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureAssets;return;}
            if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld","RuinedCityBuildingsV2");
            var shader=Shader.Find("NeonVault/RuinedDistrict");
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/Environment/RuinSurfaceAtlas-v1.png");
            if(!shader||!atlas){Debug.LogWarning("Ruined city shader or atlas unavailable");return;}
            var materials=new Material[3];
            for(int i=0;i<2;i++)
            {
                string path=Output+"/weathered-facade-"+i+".mat";
                materials[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!materials[i])
                {
                    materials[i]=new Material(shader){name="Weathered facade "+i,mainTexture=atlas,enableInstancing=true};
                    materials[i].color=i==0?new Color(.49f,.45f,.37f):new Color(.36f,.35f,.32f);
                    materials[i].SetVector("_Tile",i==0?new Vector4(0,.5f,0,0):new Vector4(.5f,0,0,0));
                    materials[i].SetFloat("_Scale",.65f);
                    AssetDatabase.CreateAsset(materials[i],path);
                }
            }
            string woodPath=Output+"/weathered-boards.mat";
            materials[2]=AssetDatabase.LoadAssetAtPath<Material>(woodPath);
            if(!materials[2])
            {
                var woodShader=Shader.Find("Standard");
                if(!woodShader){Debug.LogWarning("Standard shader unavailable for ruined boards");return;}
                materials[2]=new Material(woodShader){name="Weathered wooden boards",enableInstancing=true};
                materials[2].mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/DungeonWood.jpg");
                materials[2].color=new Color(.42f,.36f,.29f);materials[2].SetFloat("_Glossiness",.04f);
                AssetDatabase.CreateAsset(materials[2],woodPath);
            }
            for(int i=0;i<Sources.Length;i++)
            {
                string path=Output+"/city-"+i.ToString("00")+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]);if(!source)continue;
                var instance=PrefabUtility.InstantiatePrefab(source) as GameObject;if(!instance)continue;
                int parts=0;
                foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                    var damaged=CutRuinedCorner(filter.sharedMesh,i,parts);
                    if(damaged)filter.sharedMesh=damaged;
                    var slots=renderer.sharedMaterials;
                    for(int j=0;j<slots.Length;j++)slots[j]=materials[(i+j)%materials.Length];
                    renderer.sharedMaterials=slots;parts++;
                }
                if(parts>0)AddDamageDetails(instance,materials,i);
                if(parts>0)PrefabUtility.SaveAsPrefabAsset(instance,path);
                Object.DestroyImmediate(instance);
            }
            AssetDatabase.SaveAssets();
        }

        static Mesh CutRuinedCorner(Mesh source,int variant,int partIndex)
        {
            // Derive a new mesh: upper corner faces are removed along an uneven,
            // stepped diagonal. Source FBX and its shared mesh remain untouched.
            if(!source) return null;
            var vertices=source.vertices;
            if(vertices.Length<3) return null;
            var bounds=source.bounds;
            if(bounds.size.y<.5f || bounds.size.x<.5f) return null;
            var mesh=Object.Instantiate(source);
            mesh.name=source.name+" / fractured";
            int removed=0, remaining=0;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                if(source.GetTopology(sub)!=MeshTopology.Triangles)continue;
                var triangles=source.GetTriangles(sub);
                var kept=new System.Collections.Generic.List<int>(triangles.Length);
                for(int t=0;t+2<triangles.Length;t+=3)
                {
                    Vector3 c=(vertices[triangles[t]]+vertices[triangles[t+1]]+vertices[triangles[t+2]])/3f;
                    float x=Mathf.InverseLerp(bounds.min.x,bounds.max.x,c.x);
                    float y=Mathf.InverseLerp(bounds.min.y,bounds.max.y,c.y);
                    float z=Mathf.InverseLerp(bounds.min.z,bounds.max.z,c.z);
                    float edge=(variant%2==0?x:1-x);
                    float brokenLine=.80f-.34f*Mathf.Clamp01((edge-.54f)/.46f)+.035f*Mathf.Sin(z*19f+variant);
                    if(edge>.56f && y>brokenLine){removed++;continue;}
                    kept.Add(triangles[t]);kept.Add(triangles[t+1]);kept.Add(triangles[t+2]);remaining++;
                }
                mesh.SetTriangles(kept,sub,false);
            }
            if(removed==0 || remaining==0){Object.DestroyImmediate(mesh);return null;}
            mesh.RecalculateBounds();
            string path=Output+"/fractured-"+variant.ToString("00")+"-"+partIndex+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){Object.DestroyImmediate(mesh);return existing;}
            AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }

        static void AddDamageDetails(GameObject instance,Material[] materials,int variant)
        {
            // Put broken boards and masonry inside the footprint. They become
            // part of the same batched building and fade with every other part.
            var renderers=instance.GetComponentsInChildren<MeshRenderer>(true);
            if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Vector3 min=instance.transform.InverseTransformPoint(bounds.min);
            Vector3 max=instance.transform.InverseTransformPoint(bounds.max);
            Vector3 size=max-min;
            if(size.x<.1f||size.y<.1f||size.z<.1f)return;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cube);
            void Fragment(string name,Vector3 pos,Vector3 dimensions,Vector3 angles,int material)
            {
                var part=new GameObject(name);
                part.transform.SetParent(instance.transform,false);
                part.transform.localPosition=pos;
                part.transform.localRotation=Quaternion.Euler(angles);
                part.transform.localScale=dimensions;
                part.AddComponent<MeshFilter>().sharedMesh=mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial=materials[material];
            }
            float thickness=Mathf.Max(.03f,Mathf.Min(size.x,size.z)*.012f);
            // Crossing boards cover part of the front frontage as if scavengers
            // had closed ruined openings; varied angles keep buildings distinct.
            float front=min.z+thickness*.45f;
            for(int bay=0;bay<3;bay++)
            {
                float x=Mathf.Lerp(min.x,max.x,(bay+1f)/4f);
                float y=min.y+size.y*(.35f+.06f*((bay+variant)%3));
                float length=size.x*.19f;
                for(int b=0;b<2;b++)
                    Fragment("Ruin boarded frontage",new Vector3(x,y+(b-.5f)*size.y*.12f,front),
                        new Vector3(length,Mathf.Max(thickness,size.y*.035f),thickness),
                        new Vector3(0,0,(b==0?1:-1)*(8+variant*2)),2);
            }
            for(int j=0;j<8;j++)
            {
                float u=(j+1f)/9f;
                float x=Mathf.Lerp(min.x+size.x*.07f,max.x-size.x*.07f,u);
                float z=(j%2==0?min.z+size.z*.08f:max.z-size.z*.08f);
                float h=Mathf.Max(thickness,size.y*(.025f+.01f*(j%3)));
                Fragment("Ruin masonry debris",new Vector3(x,min.y+h*.35f,z),
                    new Vector3(size.x*.035f,h,size.z*.07f),new Vector3(0,(j*41+variant*17)%180,8*(j%3)),j%2);
            }
        }
    }
}
