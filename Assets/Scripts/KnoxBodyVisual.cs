using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Experimental retargeter for the authored Knox body. Equipment and animation
    // remain on DetailedHero3D; the imported joints follow those live joints.
    [DefaultExecutionOrder(450)]
    public sealed class KnoxBodyVisual : MonoBehaviour
    {
        DetailedHero3D hero;
        Transform imported;
        Transform leftArm,leftFore,leftHand,rightArm,rightFore,rightHand;
        Transform leftFinger,rightFinger;
        Transform leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot,head,spine;
        Quaternion headBase,spineBase,heroHeadBase,heroChestBase;
        Quaternion leftGripOffset,rightGripOffset,leftFootOffset,rightFootOffset;
        Vector3 leftPalm,rightPalm;
        readonly SkinnedMeshRenderer[] parts=new SkinnedMeshRenderer[7];
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        sealed class GripFinger { public Transform bone; public Quaternion rest; public Vector3 axis; public float angle; }
        readonly List<GripFinger> gripFingers=new List<GripFinger>();
        Material material;
        SkinnedMeshRenderer[] fittedArmor;
        Mesh chestOriginal,legOriginal,chestPartial,legPartial;

        public static KnoxBodyVisual Create(DetailedHero3D hero)
        {
            var prefab=Resources.Load<GameObject>("Characters/Knox/Knox");
            var texture=Resources.Load<Texture2D>("Characters/Knox/knox_a");
            if(!hero||!prefab||!texture)return null;
            // Follow the same heading as the procedural rig, including aimed
            // poses which turn the torso independently of the actor root.
            var root=new GameObject("Knox body retarget");root.transform.SetParent(hero.chest.parent,false);
            var v=root.AddComponent<KnoxBodyVisual>();v.hero=hero;
            var obj=Instantiate(prefab,root.transform,false);v.imported=obj.transform;
            foreach(var a in obj.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var a in obj.GetComponentsInChildren<Animation>(true))a.enabled=false;
            var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if(!skin||!skin.sharedMesh||!skin.sharedMesh.isReadable){DisposeRoot(root);return null;}
            float height=skin.bounds.size.y/Mathf.Max(.0001f,hero.transform.lossyScale.y);
            if(height<.1f){DisposeRoot(root);return null;}
            v.imported.localScale*=1.80f/height;
            float bottom=(skin.bounds.min.y-hero.transform.position.y)/Mathf.Max(.0001f,hero.transform.lossyScale.y);
            v.imported.localPosition-=Vector3.up*bottom;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
            if(!shader){DisposeRoot(root);return null;}
            v.material=new Material(shader){name="Knox body albedo",mainTexture=texture};
            if(v.material.HasProperty("_BaseMap"))v.material.SetTexture("_BaseMap",texture);
            if(v.material.HasProperty("_Smoothness"))v.material.SetFloat("_Smoothness",.12f);
            if(v.material.HasProperty("_Glossiness"))v.material.SetFloat("_Glossiness",.12f);
            if(v.material.HasProperty("_Metallic"))v.material.SetFloat("_Metallic",0f);
            var normal=Resources.Load<Texture2D>("Characters/Knox/knox_n");
            if(normal&&v.material.HasProperty("_BumpMap")){v.material.SetTexture("_BumpMap",normal);v.material.EnableKeyword("_NORMALMAP");}
            v.FindBones();
            if(!v.leftArm||!v.leftFore||!v.leftHand||!v.rightArm||!v.rightFore||!v.rightHand||!v.head){DisposeRoot(root);return null;}
            v.CacheGrip(v.leftHand,v.leftFinger,-1);v.CacheGrip(v.rightHand,v.rightFinger,1);
            v.headBase=Quaternion.Inverse(hero.transform.rotation)*v.head.rotation;
            if(v.spine)v.spineBase=Quaternion.Inverse(hero.transform.rotation)*v.spine.rotation;
            v.heroHeadBase=Quaternion.Inverse(hero.transform.rotation)*hero.head.rotation;
            v.heroChestBase=Quaternion.Inverse(hero.transform.rotation)*hero.chest.rotation;
            v.leftGripOffset=Quaternion.Inverse(hero.leftHand.rotation)*v.leftHand.rotation;
            v.rightGripOffset=Quaternion.Inverse(hero.rightHand.rotation)*v.rightHand.rotation;
            if(v.leftFoot)v.leftFootOffset=Quaternion.Inverse(hero.leftFoot.rotation)*v.leftFoot.rotation;
            if(v.rightFoot)v.rightFootOffset=Quaternion.Inverse(hero.rightFoot.rotation)*v.rightFoot.rotation;
            v.leftPalm=HandCenter(skin,v.leftHand);v.rightPalm=HandCenter(skin,v.rightHand);
            if(!v.Split(skin)){DisposeRoot(root);return null;}
            skin.enabled=false;v.Follow();return v;
        }
        static void DisposeRoot(GameObject root){if(Application.isPlaying)Destroy(root);else DestroyImmediate(root);}
        static Vector3 HandCenter(SkinnedMeshRenderer skin,Transform hand)
        {
            var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bones=skin.bones;
            Vector3 sum=Vector3.zero;float total=0;
            for(int i=0;i<vertices.Length;i++){
                var w=weights[i];if(w.boneIndex0>=bones.Length||bones[w.boneIndex0]!=hand||w.weight0<.5f)continue;
                sum+=hand.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]))*w.weight0;total+=w.weight0;
            }
            return total>0?sum/total:Vector3.zero;
        }
        void CacheGrip(Transform hand,Transform middle,int side)
        {
            if(!hand||!middle)return;
            Transform index=null,pinky=null;
            foreach(var bone in hand.GetComponentsInChildren<Transform>(true)){
                string n=bone.name.ToLowerInvariant();
                if(n.EndsWith("handindex1"))index=bone;
                if(n.EndsWith("handpinky1"))pinky=bone;
            }
            Vector3 forward=(middle.position-hand.position).normalized;
            Vector3 across=index&&pinky?(index.position-pinky.position).normalized:hand.right;
            Vector3 normal=Vector3.Cross(forward,across).normalized*side;
            foreach(var bone in hand.GetComponentsInChildren<Transform>(true)){
                string n=bone.name.ToLowerInvariant();
                bool digit=n.Contains("handindex")||n.Contains("handmiddle")||n.Contains("handring")||n.Contains("handpinky")||n.Contains("handthumb");
                if(!digit||bone.childCount==0||n.Contains("end"))continue;
                var direction=bone.GetChild(0).position-bone.position;
                var axis=Vector3.Cross(direction,-normal).normalized;if(axis.sqrMagnitude<.001f)continue;
                gripFingers.Add(new GripFinger{bone=bone,rest=bone.localRotation,axis=bone.InverseTransformDirection(axis),angle=n.Contains("thumb")?24:n.Contains("index")&&side>0?28:45});
            }
        }
        void FindBones()
        {
            foreach(var t in imported.GetComponentsInChildren<Transform>(true))
            {
                string n=t.name.ToLowerInvariant();int colon=n.LastIndexOf(':');if(colon>=0)n=n.Substring(colon+1);
                switch(n){case "leftarm":leftArm=t;break;case "leftforearm":leftFore=t;break;case "lefthand":leftHand=t;break;
                case "rightarm":rightArm=t;break;case "rightforearm":rightFore=t;break;case "righthand":rightHand=t;break;
                case "leftupleg":leftThigh=t;break;case "leftleg":leftShin=t;break;case "leftfoot":leftFoot=t;break;
                case "rightupleg":rightThigh=t;break;case "rightleg":rightShin=t;break;case "rightfoot":rightFoot=t;break;
                case "leftmiddle1":case "lefthandmiddle1":leftFinger=t;break;case "rightmiddle1":case "righthandmiddle1":rightFinger=t;break;
                case "head":head=t;break;case "spine2":spine=t;break;}
            }
        }
        static int Slot(Transform bone)
        {
            if(!bone)return 1;string n=bone.name.ToLowerInvariant();
            if(n.Contains("head")||n.Contains("neck"))return 0;
            if(n.Contains("hand")||n.Contains("finger")||n.Contains("thumb"))return 2;
            if(n.Contains("arm")||n.Contains("shoulder"))return 5;
            if(n.Contains("foot")||n.Contains("toe"))return 4;
            // The hips carry the shirt hem and belt. Keep them with the torso
            // so equipping trousers alone cannot remove the waist.
            if(n.Contains("hips"))return 1;
            if(n.Contains("leg"))return 3;
            return 1;
        }
        static int Dominant(BoneWeight w,Transform[] bones,float height)
        {
            int i=w.boneIndex0;float best=w.weight0;
            if(w.weight1>best){best=w.weight1;i=w.boneIndex1;}
            if(w.weight2>best){best=w.weight2;i=w.boneIndex2;}
            if(w.weight3>best)i=w.boneIndex3;
            int slot=i>=0&&i<bones.Length?Slot(bones[i]):1;
            // The upper trouser and belt region remains with the shirt when
            // leg equipment replaces the lower body. This keeps a continuous
            // cloth surface underneath the equipment's belt opening.
            if(slot==3&&height>.98f)return 1;
            // Keep the lower cuff with the boots when trousers are replaced.
            // The armor pants finish above this ankle transition.
            return slot==3&&height<.32f?4:slot;
        }
        bool Split(SkinnedMeshRenderer skin)
        {
            var original=skin.sharedMesh;var weights=original.boneWeights;var bones=skin.bones;
            if(weights.Length!=original.vertexCount||bones.Length==0)return false;
            var triangles=new List<int>[7];for(int s=0;s<7;s++)triangles[s]=new List<int>();
            var vertices=original.vertices;var height=new float[vertices.Length];
            for(int i=0;i<vertices.Length;i++)height[i]=(skin.transform.TransformPoint(vertices[i]).y-hero.transform.position.y)/Mathf.Max(.0001f,hero.transform.lossyScale.y);
            for(int sub=0;sub<original.subMeshCount;sub++)
            {
                var tris=original.GetTriangles(sub);
                for(int i=0;i+2<tris.Length;i+=3)
                {
                    int a=Dominant(weights[tris[i]],bones,height[tris[i]]),b=Dominant(weights[tris[i+1]],bones,height[tris[i+1]]),c=Dominant(weights[tris[i+2]],bones,height[tris[i+2]]);
                    int slot=a==b||a==c?a:b==c?b:a;
                    triangles[slot].Add(tris[i]);triangles[slot].Add(tris[i+1]);triangles[slot].Add(tris[i+2]);
                    // The upper trouser/waist strip must continue behind the
                    // jacket's cut hem when only the chest slot is equipped.
                    if(slot==1){
                        var center=(vertices[tris[i]]+vertices[tris[i+1]]+vertices[tris[i+2]])/3f;
                        float min=Mathf.Min(height[tris[i]],Mathf.Min(height[tris[i+1]],height[tris[i+2]]));
                        float max=Mathf.Max(height[tris[i]],Mathf.Max(height[tris[i+1]],height[tris[i+2]]));
                        var world=skin.transform.TransformPoint(center);var local=hero.transform.InverseTransformPoint(world);
                        if(min<1.06f&&max>.88f&&Mathf.Abs(local.x)<.24f&&Mathf.Abs(local.z)<.22f){
                            triangles[6].Add(tris[i]);triangles[6].Add(tris[i+1]);triangles[6].Add(tris[i+2]);
                        }
                    }
                }
            }
            for(int i=0;i<parts.Length;i++)
            {
                var mesh=ExtractSlot(original,triangles[i]);mesh.name="Knox slot "+i;ownedMeshes.Add(mesh);
                var go=new GameObject("Knox slot "+i);go.transform.SetParent(skin.transform.parent,false);
                go.transform.localPosition=skin.transform.localPosition;go.transform.localRotation=skin.transform.localRotation;go.transform.localScale=skin.transform.localScale;
                var part=go.AddComponent<SkinnedMeshRenderer>();part.sharedMesh=mesh;part.bones=bones;part.rootBone=skin.rootBone;
                part.sharedMaterial=material;part.localBounds=skin.localBounds;part.updateWhenOffscreen=true;parts[i]=part;
            }
            return true;
        }
        static Mesh ExtractSlot(Mesh original,List<int> indices)
        {
            // Each renderer gets only its own vertices; copying the whole body five
            // times needlessly multiplied mobile skinning and GPU memory costs.
            var vertices=original.vertices;var normals=original.normals;var uv=original.uv;
            var tangents=original.tangents;var weights=original.boneWeights;
            var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var u=new List<Vector2>();
            var t=new List<Vector4>();var w=new List<BoneWeight>();var result=new int[indices.Count];
            for(int k=0;k<indices.Count;k++)
            {
                int src=indices[k];if(!map.TryGetValue(src,out int dst))
                {
                    dst=v.Count;map.Add(src,dst);v.Add(vertices[src]);w.Add(weights[src]);
                    if(normals.Length==vertices.Length)n.Add(normals[src]);
                    if(uv.Length==vertices.Length)u.Add(uv[src]);
                    if(tangents.Length==vertices.Length)t.Add(tangents[src]);
                }
                result[k]=dst;
            }
            var mesh=new Mesh{indexFormat=original.indexFormat};mesh.SetVertices(v);
            if(n.Count==v.Count)mesh.SetNormals(n);if(u.Count==v.Count)mesh.SetUVs(0,u);if(t.Count==v.Count)mesh.SetTangents(t);
            mesh.boneWeights=w.ToArray();mesh.bindposes=original.bindposes;mesh.SetTriangles(result,0);mesh.RecalculateBounds();return mesh;
        }
        public void SetCovered(bool[] slots,bool sleeveless=false)
        {
            for(int i=0;i<parts.Length;i++)if(parts[i])parts[i].enabled=i==6?slots!=null&&slots.Length>3&&slots[1]&&!slots[3]:i==0||slots==null||(i==5?(slots.Length<=1||!slots[1]||sleeveless):(slots.Length<=i||!slots[i]));
            if(fittedArmor!=null&&slots!=null&&slots.Length>3){
                bool chest=slots[1], legs=slots[3];
                // SharedArmorSleeves owns the chest mesh while a sleeveless
                // product is active. Replacing it here would restore the long
                // sleeves on every frame and overlap the exposed Knox arms.
                if(!sleeveless&&fittedArmor.Length>1&&fittedArmor[1]&&chestOriginal){var mesh=chest&&!legs&&chestPartial?chestPartial:chestOriginal;if(fittedArmor[1].sharedMesh!=mesh)fittedArmor[1].sharedMesh=mesh;}
                if(fittedArmor.Length>3&&fittedArmor[3]&&legOriginal){var mesh=legs&&!chest&&legPartial?legPartial:legOriginal;if(fittedArmor[3].sharedMesh!=mesh)fittedArmor[3].sharedMesh=mesh;}
            }
        }
        public void PrepareSleevelessChest()
        {
            if(fittedArmor!=null&&fittedArmor.Length>1&&fittedArmor[1]&&chestOriginal)
                fittedArmor[1].sharedMesh=chestOriginal;
        }
        // The scan's synthetic hip facings extend beyond Knox's waist when
        // chest and trousers are equipped independently. Remove those facings
        // from the Knox-only copies; the base shirt supplies the covered waist.
        public void FitArmorBoundary(SkinnedMeshRenderer[] armor)
        {
            if(armor==null||armor.Length<=3)return;
            fittedArmor=armor;chestOriginal=armor[1]?armor[1].sharedMesh:null;legOriginal=armor[3]?armor[3].sharedMesh:null;
            if(chestOriginal){chestPartial=ClipChestWaist(chestOriginal);ownedMeshes.Add(chestPartial);}
            if(legOriginal){legPartial=CreateLegWaist(legOriginal,chestOriginal);ownedMeshes.Add(legPartial);}
        }

        static bool OuterWaist(Vector4 p)=>Mathf.Abs(p.x)>.235f||p.z>.13f||p.z<-.12f;
        static Mesh ClipChestWaist(Mesh src)
        {
            var mesh=Instantiate(src);mesh.name=src.name+" / Knox chest seam";
            var coords=new List<Vector4>();src.GetUVs(2,coords);if(coords.Count!=src.vertexCount)return mesh;
            var vertices=new List<Vector3>(src.vertices);var normals=new List<Vector3>(src.normals);
            var tangents=new List<Vector4>(src.tangents);var colors=new List<Color32>(src.colors32);
            var weights=new List<BoneWeight>(src.boneWeights);var channels=new List<Vector4>[8];
            for(int ch=0;ch<8;ch++){channels[ch]=new List<Vector4>();src.GetUVs(ch,channels[ch]);}
            int AddEdge(int a,int b,float t){
                int n=vertices.Count;vertices.Add(Vector3.Lerp(vertices[a],vertices[b],t));
                if(normals.Count==n)normals.Add(Vector3.Lerp(normals[a],normals[b],t).normalized);
                if(tangents.Count==n)tangents.Add(Vector4.Lerp(tangents[a],tangents[b],t));
                if(colors.Count==n)colors.Add(Color32.Lerp(colors[a],colors[b],t));
                if(weights.Count==n)weights.Add(new BoneWeight{boneIndex0=1,weight0=1});
                for(int ch=0;ch<8;ch++)if(channels[ch].Count==n)channels[ch].Add(Vector4.Lerp(channels[ch][a],channels[ch][b],t));
                return n;
            }
            var submeshes=new List<int>[src.subMeshCount];
            for(int sub=0;sub<src.subMeshCount;sub++){
                var tris=src.GetTriangles(sub);var dst=new List<int>();submeshes[sub]=dst;
                for(int i=0;i+2<tris.Length;i+=3){
                    int a=tris[i],b=tris[i+1],c=tris[i+2];var p=(coords[a]+coords[b]+coords[c])/3f;
                    float min=Mathf.Min(coords[a].y,Mathf.Min(coords[b].y,coords[c].y));
                    if(!OuterWaist(p)||min>=1.25f||p.y<.90f){dst.Add(a);dst.Add(b);dst.Add(c);continue;}
                    var corners=new[]{a,b,c};var poly=new List<int>(4);
                    for(int e=0;e<3;e++){
                        int x=corners[e],y=corners[(e+1)%3];float hx=coords[x].y,hy=coords[y].y;bool inside=hx>=1.25f,next=hy>=1.25f;
                        if(inside)poly.Add(x);if(inside!=next)poly.Add(AddEdge(x,y,(1.25f-hx)/(hy-hx)));
                    }
                    for(int j=1;j+1<poly.Count;j++){dst.Add(poly[0]);dst.Add(poly[j]);dst.Add(poly[j+1]);}
                }
            }
            mesh.SetVertices(vertices);if(normals.Count==vertices.Count)mesh.SetNormals(normals);if(tangents.Count==vertices.Count)mesh.SetTangents(tangents);
            if(colors.Count==vertices.Count)mesh.SetColors(colors);if(weights.Count==vertices.Count)mesh.boneWeights=weights.ToArray();
            for(int ch=0;ch<8;ch++)if(channels[ch].Count==vertices.Count)mesh.SetUVs(ch,channels[ch]);
            for(int sub=0;sub<submeshes.Length;sub++)mesh.SetTriangles(submeshes[sub],sub,false);mesh.RecalculateBounds();return mesh;
        }

        static Mesh CreateLegWaist(Mesh src,Mesh chest)
        {
            var mesh=Instantiate(src);mesh.name=src.name+" / Knox fitted waist";
            if(!chest)return mesh;
            var coords=new List<Vector4>();chest.GetUVs(2,coords);if(coords.Count!=chest.vertexCount)return mesh;
            var vertices=new List<Vector3>(src.vertices);var normals=new List<Vector3>(src.normals);
            var tangents=new List<Vector4>(src.tangents);var colors=new List<Color32>(src.colors32);
            var weights=new List<BoneWeight>(src.boneWeights);var channels=new List<Vector4>[8];var extraUv=new List<Vector4>[8];
            for(int ch=0;ch<8;ch++){channels[ch]=new List<Vector4>();extraUv[ch]=new List<Vector4>();src.GetUVs(ch,channels[ch]);chest.GetUVs(ch,extraUv[ch]);}
            var extraVertices=chest.vertices;var extraNormals=chest.normals;var extraTangents=chest.tangents;var extraColors=chest.colors32;var extraWeights=chest.boneWeights;
            var remap=new Dictionary<int,int>();
            int CopyVertex(int old){
                if(remap.TryGetValue(old,out int existing))return existing;
                int n=vertices.Count;remap.Add(old,n);vertices.Add(extraVertices[old]);
                if(normals.Count==n)normals.Add(extraNormals.Length==chest.vertexCount?extraNormals[old]:Vector3.up);
                if(tangents.Count==n)tangents.Add(extraTangents.Length==chest.vertexCount?extraTangents[old]:Vector4.zero);
                if(colors.Count==n)colors.Add(extraColors.Length==chest.vertexCount?extraColors[old]:new Color32(255,255,255,255));
                if(weights.Count==n)weights.Add(extraWeights.Length==chest.vertexCount?extraWeights[old]:new BoneWeight{boneIndex0=1,weight0=1});
                for(int ch=0;ch<8;ch++)if(channels[ch].Count==n)channels[ch].Add(extraUv[ch].Count==chest.vertexCount?extraUv[ch][old]:Vector4.zero);
                return n;
            }
            var submeshes=new List<int>[src.subMeshCount];
            for(int sub=0;sub<src.subMeshCount;sub++){
                var dst=new List<int>(src.GetTriangles(sub));submeshes[sub]=dst;
                if(sub>=chest.subMeshCount)continue;var tris=chest.GetTriangles(sub);
                for(int i=0;i+2<tris.Length;i+=3){
                    int a=tris[i],b=tris[i+1],c=tris[i+2];var p=(coords[a]+coords[b]+coords[c])/3f;
                    // Reuse the original scan's lower jacket/belt and pouch surfaces.
                    // The same faces already meet the trouser mesh in the full outfit.
                    float ArmWeight(BoneWeight w){float value=0;if(w.boneIndex0>=3&&w.boneIndex0<=8)value+=w.weight0;if(w.boneIndex1>=3&&w.boneIndex1<=8)value+=w.weight1;if(w.boneIndex2>=3&&w.boneIndex2<=8)value+=w.weight2;if(w.boneIndex3>=3&&w.boneIndex3<=8)value+=w.weight3;return value;}
                    if(extraWeights.Length==chest.vertexCount&&(ArmWeight(extraWeights[a])+ArmWeight(extraWeights[b])+ArmWeight(extraWeights[c]))/3f>.40f)continue;
                    if(p.y>=1.25f&&!(p.y<1.40f&&OuterWaist(p)))continue;
                    dst.Add(CopyVertex(a));dst.Add(CopyVertex(b));dst.Add(CopyVertex(c));
                }
            }
            mesh.SetVertices(vertices);if(normals.Count==vertices.Count)mesh.SetNormals(normals);if(tangents.Count==vertices.Count)mesh.SetTangents(tangents);
            if(colors.Count==vertices.Count)mesh.SetColors(colors);if(weights.Count==vertices.Count)mesh.boneWeights=weights.ToArray();
            for(int ch=0;ch<8;ch++)if(channels[ch].Count==vertices.Count)mesh.SetUVs(ch,channels[ch]);
            for(int sub=0;sub<submeshes.Length;sub++)mesh.SetTriangles(submeshes[sub],sub,false);mesh.RecalculateBounds();return mesh;
        }
        static void Aim(Transform bone,Transform child,Vector3 desired)
        {
            if(!bone||!child)return;var current=child.position-bone.position;
            if(current.sqrMagnitude>.000001f&&desired.sqrMagnitude>.000001f)bone.rotation=Quaternion.FromToRotation(current,desired)*bone.rotation;
        }
        static void Reach(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 pole)
        {
            if(!upper||!lower||!hand)return;
            Vector3 origin=upper.position, delta=target-origin;
            float a=Vector3.Distance(origin,lower.position), b=Vector3.Distance(lower.position,hand.position);
            if(a<.001f||b<.001f||delta.sqrMagnitude<.000001f)return;
            float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
            Vector3 axis=delta.normalized, bend=Vector3.ProjectOnPlane(pole,axis).normalized;
            if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(Vector3.down,axis).normalized;
            float along=(a*a-b*b+d*d)/(2*d);
            Vector3 elbow=origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Aim(upper,lower,elbow-origin); Aim(lower,hand,origin+axis*d-lower.position);
        }
        void LateUpdate(){Follow();}
        public void RefreshForPreview()
        {
            Follow();
            // A portrait camera renders immediately after posing within the same
            // frame. Refresh active skin renderers so that pose is not one frame old.
            foreach(var part in parts)if(part&&part.enabled){part.enabled=false;part.enabled=true;}
        }
        void Follow()
        {
            if(!hero||!imported)return;
            if(spine)spine.rotation=hero.transform.rotation*((Quaternion.Inverse(hero.transform.rotation)*hero.chest.rotation)*Quaternion.Inverse(heroChestBase))*spineBase;
            if(head)head.rotation=hero.transform.rotation*((Quaternion.Inverse(hero.transform.rotation)*hero.head.rotation)*Quaternion.Inverse(heroHeadBase))*headBase;
            var rig=hero.chest.parent;
            leftHand.rotation=hero.leftHand.rotation*leftGripOffset;rightHand.rotation=hero.rightHand.rotation*rightGripOffset;
            // Fingers point along the weapon rather than hanging vertically
            // from the imported A-pose wrist.
            if(leftFinger)Aim(leftHand,leftFinger,hero.rightHand.forward);
            if(rightFinger)Aim(rightHand,rightFinger,hero.rightHand.forward);
            var leftRotation=leftHand.rotation;var rightRotation=rightHand.rotation;
            for(int i=0;i<2;i++){
                Reach(leftArm,leftFore,leftHand,hero.leftHand.position-(leftHand.TransformPoint(leftPalm)-leftHand.position),rig.TransformDirection(new Vector3(-.55f,-.75f,-.12f)));
                leftHand.rotation=leftRotation;
                Reach(rightArm,rightFore,rightHand,hero.rightHand.position-(rightHand.TransformPoint(rightPalm)-rightHand.position),rig.TransformDirection(new Vector3(.45f,-.7f,-.2f)));
                rightHand.rotation=rightRotation;
            }
            foreach(var finger in gripFingers)if(finger.bone)finger.bone.localRotation=finger.rest*Quaternion.AngleAxis(finger.angle,finger.axis);
            // Use the armor rig's actual hips and ankles so mixed equipment
            // cannot leave the base boots inside the gap between trouser legs.
            if(leftThigh&&leftShin&&leftFoot){leftThigh.position=hero.leftThigh.position;Reach(leftThigh,leftShin,leftFoot,hero.leftFoot.position,rig.forward);leftFoot.rotation=hero.leftFoot.rotation*leftFootOffset;}
            if(rightThigh&&rightShin&&rightFoot){rightThigh.position=hero.rightThigh.position;Reach(rightThigh,rightShin,rightFoot,hero.rightFoot.position,rig.forward);rightFoot.rotation=hero.rightFoot.rotation*rightFootOffset;}
        }
        void OnDestroy()
        {
            foreach(var m in ownedMeshes)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}
            if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
}
