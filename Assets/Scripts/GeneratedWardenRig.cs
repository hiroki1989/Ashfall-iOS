using UnityEngine;
using System.Collections.Generic;

namespace NeonVault
{
    // Binds the textured scan to the same joints that drive the established player motion.
    // The imported FBX has no skeleton; its original UVs and triangles are retained.
    static class GeneratedWardenRig
    {
        static readonly Vector3[] Rest=CharacterRigStandard.RestPositions;

        public static SkinnedMeshRenderer Bind(GameObject source,DetailedHero3D hero,bool headOnly,bool includeArms=false,bool includeLegs=false)
        {
            if(!source||!hero)return null;
            var filter=source.GetComponentInChildren<MeshFilter>(true);
            var oldRenderer=filter?filter.GetComponent<MeshRenderer>():null;
            if(!filter||!oldRenderer||!filter.sharedMesh||!filter.sharedMesh.isReadable)return null;
            bool rebuiltHead=headOnly&&source.name.Contains("head-rebuilt");
            bool addNeckPatch=headOnly&&!rebuiltHead;
            var original=filter.sharedMesh;
            var nodes=CharacterRigStandard.GetBones(hero);
            var mesh=new Mesh{name="Warden articulated "+(headOnly?"head":"equipment"),indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            var originalVertices=original.vertices;var placed=new Vector3[originalVertices.Length];
            Matrix4x4 convert=hero.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            for(int i=0;i<placed.Length;i++)placed[i]=convert.MultiplyPoint3x4(originalVertices[i]);
            var sourcePositions=(Vector3[])placed.Clone();
            var originalUv=original.uv;var originalNormal=original.normals;
            // Landmarks measured on the source OBJ/FBX in Blender (90x FBX, y + 1).
            // The scan has a high collar at 1.84, shoulders at 1.62, belt at 1.25,
            // knees at .65 and ankles at .25; it is not the gameplay rest skeleton.
            var armInfluence=new float[placed.Length];
            var canonicalSource=new Dictionary<Vector3Int,int>();
            var sourceRepresentative=new int[placed.Length];
            if(!headOnly)for(int i=0;i<placed.Length;i++)
            {
                // UV and normal seams may duplicate a scan vertex. Resolve its fit once,
                // independent of triangle membership, and share the result across seams.
                Vector3 p=sourcePositions[i];
                var positionKey=new Vector3Int(Mathf.RoundToInt(p.x*1000000f),Mathf.RoundToInt(p.y*1000000f),Mathf.RoundToInt(p.z*1000000f));
                if(canonicalSource.TryGetValue(positionKey,out int representative))
                {
                    sourceRepresentative[i]=representative;
                    placed[i]=placed[representative];armInfluence[i]=armInfluence[representative];
                    continue;
                }
                canonicalSource.Add(positionKey,i);sourceRepresentative[i]=i;
                float sourceY=p.y;
                float axis=sourceY>=1.30f?Remap(sourceY,1.30f,1.62f,.37f,.265f)
                    :Remap(sourceY,1.10f,1.30f,.425f,.37f);
                axis=Mathf.Clamp(axis,.25f,.425f);
                float arm=includeArms?SourceArmInfluence(p,axis):0;
                armInfluence[i]=arm;
                placed[i]=FitEquipmentVertex(p,arm,includeLegs);
            }
            if(!headOnly&&includeArms)
            {
                ConstrainArmRegion(original,sourcePositions,sourceRepresentative,armInfluence);
                for(int i=0;i<placed.Length;i++)
                {
                    int representative=sourceRepresentative[i];
                    placed[i]=representative==i?FitEquipmentVertex(sourcePositions[i],armInfluence[i],includeLegs):placed[representative];
                }
            }
            var savedPositions=new Vector3[nodes.Length];
            var savedRotations=new Quaternion[nodes.Length];
            for(int i=0;i<nodes.Length;i++)
            {savedPositions[i]=nodes[i].localPosition;savedRotations[i]=nodes[i].localRotation;}
            try
            {
            // Bind in the common downward-arm reference pose, independently of the caller's
            // idle, aiming or preview pose. Parents precede children in this array.
            for(int i=0;i<nodes.Length;i++)
            {
                nodes[i].position=hero.transform.TransformPoint(Rest[i]);
                nodes[i].rotation=hero.transform.rotation;
            }
            var vertices=new List<Vector3>(placed.Length);var uv=new List<Vector2>(placed.Length);
            var normals=new List<Vector3>(placed.Length);var weights=new List<BoneWeight>(placed.Length);
            var sourceVertexPositions=new List<Vector3>(placed.Length);
            var sourceDiagnostics=new List<Vector4>(placed.Length);
            var remap=new Dictionary<int,int>(placed.Length);
            var fittedWeights=new BoneWeight[placed.Length];
            if(!headOnly)for(int i=0;i<placed.Length;i++)
            {
                int representative=sourceRepresentative[i];
                fittedWeights[i]=representative==i
                    ?BlendWeights(placed[i],includeArms,includeLegs,false,armInfluence[i],sourcePositions[i])
                    :fittedWeights[representative];
            }
            Matrix4x4 normalMatrix=convert.inverse.transpose;
            int Map(int oldIndex)
            {
                int key=oldIndex;
                if(remap.TryGetValue(key,out int mapped))return mapped;
                mapped=vertices.Count;remap.Add(key,mapped);
                Vector3 point=placed[oldIndex];
                vertices.Add(point);sourceVertexPositions.Add(sourcePositions[oldIndex]);
                Vector3 sourcePoint=sourcePositions[oldIndex];
                sourceDiagnostics.Add(new Vector4(sourcePoint.x,sourcePoint.y,sourcePoint.z,oldIndex));
                uv.Add(originalUv.Length==placed.Length?originalUv[oldIndex]:Vector2.zero);
                normals.Add(originalNormal.Length==placed.Length?normalMatrix.MultiplyVector(originalNormal[oldIndex]).normalized:Vector3.up);
                float headWeight=rebuiltHead?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.455f,1.565f,point.y)):1;
                weights.Add(headOnly?new BoneWeight{boneIndex0=2,weight0=headWeight,boneIndex1=0,weight1=1-headWeight}:fittedWeights[oldIndex]);
                return mapped;
            }
            mesh.subMeshCount=original.subMeshCount+(addNeckPatch?1:0);
            var triangles=new List<int>[mesh.subMeshCount];
            if(addNeckPatch)triangles[original.subMeshCount]=new List<int>(1008);
            int replacedNeckFaces=0;
            for(int s=0;s<original.subMeshCount;s++)
            {
                var sourceTriangles=original.GetTriangles(s);var target=new List<int>(sourceTriangles.Length);triangles[s]=target;
                for(int t=0;t+2<sourceTriangles.Length;t+=3)
                {
                    int a=sourceTriangles[t],b=sourceTriangles[t+1],c=sourceTriangles[t+2];
                    int bone=SelectBoneForRig((sourcePositions[a]+sourcePositions[b]+sourcePositions[c])/3f,hero,includeArms,includeLegs);
                    // The body export contains a complete figure. A head-only skin must contain
                    // only head triangles; mapping the whole export to the head bone creates a
                    // second full character that slides over the legacy rig.
                    Vector3 center=(sourcePositions[a]+sourcePositions[b]+sourcePositions[c])/3f;
                    if(!rebuiltHead&&(headOnly?!KeepHeadTriangle(center):!KeepTriangle(false,includeArms,includeLegs,bone,center)))continue;
                    int mappedA=Map(a),mappedB=Map(b),mappedC=Map(c);
                    // Keep mapped vertices for exactly the same head normalization bounds,
                    // but replace the old flat lower nape/side skin with the closed neck.
                    // The forward chin and facial surface remain from the original scan.
                    if(addNeckPatch&&ReplaceOriginalNape(center))
                    {replacedNeckFaces++;continue;}
                    target.Add(mappedA);target.Add(mappedB);target.Add(mappedC);
                }
            }
            if(headOnly)Debug.Log("Warden head: replaced "+replacedNeckFaces+" original lower nape/side faces; normalization vertices retained");
            int neckStart=-1;
            if(addNeckPatch&&vertices.Count>0)
            {
                var headBounds=new Bounds(vertices[0],Vector3.zero);
                for(int i=1;i<vertices.Count;i++)headBounds.Encapsulate(vertices[i]);
                // A shared head slot has a fixed physical height; normalize the imported
                // scan uniformly so masks do not get buried inside its oversized face.
                float headScale=CharacterRigStandard.HeadMeshHeight/Mathf.Max(.001f,headBounds.size.y);
                Vector3 headCenter=Rest[2]+new Vector3(0,.065f,.015f);
                for(int i=0;i<vertices.Count;i++)
                {
                    Vector3 point=(vertices[i]-headBounds.center)*headScale+headCenter;
                    Vector3 originalPoint=sourceVertexPositions[i];
                    float nx=originalPoint.x/.08f,nz=(originalPoint.z-.02f)/.075f;
                    if(originalPoint.y<1.78f&&nx*nx+nz*nz<=1f)
                    {
                        // Extend the scan's own lower neck into the coat and anchor its
                        // base to the chest, so head rotation cannot open the collar seam.
                        // Broaden only the original neck, tapering to the unchanged jaw.
                        float neckWidth=1f+.30f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.72f,1.78f,originalPoint.y)));
                        point.x*=neckWidth;
                        point.z=.015f+(point.z-.015f)*neckWidth;
                        float headWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.48f,1.56f,point.y));
                        point.y-=.035f*(1-headWeight);
                        weights[i]=new BoneWeight{boneIndex0=2,weight0=headWeight,boneIndex1=0,weight1=1-headWeight};
                    }
                    vertices[i]=point;
                }
                neckStart=vertices.Count;
                AddClosedNeck(vertices,uv,normals,weights,triangles[original.subMeshCount],sourcePositions,originalUv);
            }
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetNormals(normals);
            // Retain scan coordinates for direct diagnosis of baked deformation faces.
            // The painted shader does not read channel 2. Legacy neck additions have
            // no scan vertex and are deliberately excluded from this equipment channel.
            if(!headOnly)mesh.SetUVs(2,sourceDiagnostics);
            var colors=new Color32[vertices.Count];
            var neckShades=new Vector2[vertices.Count];
            float colorMin=1,colorMax=0,shadeMin=float.PositiveInfinity,shadeMax=float.NegativeInfinity;
            for(int i=0;i<colors.Length;i++)
            {
                if(neckStart<0||i<neckStart){colors[i]=new Color32(255,255,255,255);continue;}
                Vector3 p=vertices[i],n=normals[i];float t=Mathf.Clamp01((p.y-1.425f)/.18f);
                float rear=Mathf.Clamp01(-n.z);
                float ao=.10f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.35f,t)));
                float groove=rear*Mathf.Exp(-p.x*p.x/.00010f)*.12f*Mathf.Sin(t*Mathf.PI);
                float directional=Vector3.Dot(n,new Vector3(-.45f,.45f,.77f).normalized);
                float shade=.87f+.22f*directional-ao-groove;
                neckShades[i]=new Vector2(shade*1.4f,1);
                shadeMin=Mathf.Min(shadeMin,shade);shadeMax=Mathf.Max(shadeMax,shade);
                float warm=.015f*Mathf.Sin(t*Mathf.PI)*rear;
                colors[i]=(Color32)new Color(Mathf.Clamp01(shade+warm),Mathf.Clamp01(shade),Mathf.Clamp01(shade-warm),1);
            }
            mesh.colors32=colors;
            mesh.uv2=neckShades;
            if(neckStart>=0)
            {
                for(int i=neckStart;i<colors.Length;i++)
                {
                    colorMin=Mathf.Min(colorMin,Mathf.Min(colors[i].r,Mathf.Min(colors[i].g,colors[i].b))/255f);
                    colorMax=Mathf.Max(colorMax,Mathf.Max(colors[i].r,Mathf.Max(colors[i].g,colors[i].b))/255f);
                }
                Debug.Log("Warden neck shading: vertices="+(vertices.Count-neckStart)+" color="+colorMin.ToString("F3")+".."+colorMax.ToString("F3")
                    +" uv2.shade="+shadeMin.ToString("F3")+".."+shadeMax.ToString("F3")+" uv2.mask=1; original vertices mask=0");
            }
            mesh.boneWeights=weights.ToArray();
            for(int s=0;s<triangles.Length;s++)mesh.SetTriangles(triangles[s],s);
            // Canonical matrices include the complete real parent hierarchy.
            var binds=new Matrix4x4[nodes.Length];
            for(int i=0;i<binds.Length;i++)binds[i]=nodes[i].worldToLocalMatrix*hero.transform.localToWorldMatrix;
            mesh.bindposes=binds;mesh.RecalculateBounds();
            var skinObject=new GameObject("Generated Warden / articulated "+(headOnly?"face":"fieldkit"));
            skinObject.transform.SetParent(hero.transform,false);
            skinObject.layer=source.layer;
            var skin=skinObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh=mesh;skin.bones=nodes;skin.rootBone=hero.chest;
            if(addNeckPatch)
            {
                var materials=new Material[mesh.subMeshCount];
                var painted=oldRenderer.sharedMaterials;
                for(int i=0;i<original.subMeshCount;i++)materials[i]=painted[Mathf.Min(i,painted.Length-1)];
                // Reuse the painted skin atlas with the corrected smooth-normal UV2 shading.
                materials[original.subMeshCount]=painted[0];
                skin.sharedMaterials=materials;
                Debug.Log("Warden neck: independent lit submesh="+original.subMeshCount+" faces="+(triangles[original.subMeshCount].Count/3)
                    +" shader="+materials[original.subMeshCount].shader.name+" replaced original nape faces="+replacedNeckFaces);
            }
            else skin.sharedMaterials=oldRenderer.sharedMaterials;
            skin.updateWhenOffscreen=true;
            skin.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(3,4,3));
            // FBX exports can contain several child meshes. Hiding only the first renderer leaves
            // the untouched source shells (often a second head/helmet) visible over the skinned
            // replacement, which is the exact doubled-character failure this rig must avoid.
            foreach(var sourceRenderer in source.GetComponentsInChildren<Renderer>(true))
                if(sourceRenderer)sourceRenderer.enabled=false;
            // The source is an import template, never a second live character. Keeping it
            // inactive also prevents the legacy-appearance restore from re-enabling it.
            source.SetActive(false);
            return skin;
            }
            finally
            {
                for(int i=0;i<nodes.Length;i++)
                {nodes[i].localPosition=savedPositions[i];nodes[i].localRotation=savedRotations[i];}
            }
        }

        // The authored Warden outfit arrives as one scan mesh. Partition its already
        // rigged triangles into independent, animated equipment-slot meshes so one
        // continuous scan cannot cover the simple-clothes base or survive unequipping.
        // Slot indices follow ArmorSlot: 0=head (the separate mask), 1=chest,
        // 2=arms, 3=legs, 4=feet.
        public static SkinnedMeshRenderer[] SplitArmorBySlot(SkinnedMeshRenderer source,Transform parent)
        {
            var result=new SkinnedMeshRenderer[5];
            if(!source||!source.sharedMesh||!parent)return result;
            var original=source.sharedMesh;
            var sourceCoords=new List<Vector4>();original.GetUVs(2,sourceCoords);
            if(sourceCoords.Count!=original.vertexCount)
            {Debug.LogError("Warden outfit split skipped: source-coordinate channel is missing");return result;}
            var positions=new List<Vector4>();var normals=new List<Vector4>();var tangents=new List<Vector4>();
            var colors=new List<Color>();var uv=new List<Vector4>[8];
            for(int channel=0;channel<8;channel++){uv[channel]=new List<Vector4>();original.GetUVs(channel,uv[channel]);}
            var weights=original.boneWeights;
            var vertices=original.vertices;var vertexNormals=original.normals;var vertexTangents=original.tangents;
            var vertexColors=original.colors;
            for(int slot=1;slot<=4;slot++)
            {
                var remap=new Dictionary<int,int>();var partVertices=new List<Vector3>();var partNormals=new List<Vector3>();
                var partTangents=new List<Vector4>();var partColors=new List<Color>();var partWeights=new List<BoneWeight>();
                var partUv=new List<Vector4>[8];for(int ch=0;ch<8;ch++)partUv[ch]=new List<Vector4>();
                var submeshTriangles=new List<int>[original.subMeshCount];
                int Map(int old)
                {
                    if(remap.TryGetValue(old,out int mapped))return mapped;
                    mapped=partVertices.Count;remap.Add(old,mapped);
                    partVertices.Add(vertices[old]);
                    partNormals.Add(vertexNormals.Length==vertices.Length?vertexNormals[old]:Vector3.up);
                    if(vertexTangents.Length==vertices.Length)partTangents.Add(vertexTangents[old]);
                    if(vertexColors.Length==vertices.Length)partColors.Add(vertexColors[old]);
                    if(weights.Length==vertices.Length)partWeights.Add(weights[old]);
                    for(int ch=0;ch<8;ch++)if(uv[ch].Count==vertices.Length)partUv[ch].Add(uv[ch][old]);
                    return mapped;
                }
                int BeltVertex(Vector3 bary,int a,int b,int c)
                {
                    int index=partVertices.Count;
                    partVertices.Add(vertices[a]*bary.x+vertices[b]*bary.y+vertices[c]*bary.z);
                    partNormals.Add((vertexNormals[a]*bary.x+vertexNormals[b]*bary.y+vertexNormals[c]*bary.z).normalized);
                    if(vertexTangents.Length==vertices.Length)partTangents.Add(vertexTangents[a]*bary.x+vertexTangents[b]*bary.y+vertexTangents[c]*bary.z);
                    if(vertexColors.Length==vertices.Length)partColors.Add(vertexColors[a]*bary.x+vertexColors[b]*bary.y+vertexColors[c]*bary.z);
                    // The belt seam is pelvis rigid on both sides of this cut.
                    partWeights.Add(new BoneWeight{boneIndex0=1,weight0=1});
                    for(int ch=0;ch<8;ch++)if(uv[ch].Count==vertices.Length)
                        partUv[ch].Add(uv[ch][a]*bary.x+uv[ch][b]*bary.y+uv[ch][c]*bary.z);
                    return index;
                }
                int WaistFacing(int old)
                {
                    int index=partVertices.Count;var point=vertices[old];
                    // A scan has no fabric behind its outer hip pouches. Build
                    // the trousers' own inner facing from those existing faces,
                    // so removing the jacket cannot expose an empty hip band.
                    float radius=Mathf.Sqrt(point.x*point.x/(.225f*.225f)+point.z*point.z/(.115f*.115f));
                    if(radius>1f){point.x/=radius;point.z/=radius;}
                    point.y=Mathf.Min(point.y,1.04f);partVertices.Add(point);
                    partNormals.Add(vertexNormals[old]);
                    if(vertexTangents.Length==vertices.Length)partTangents.Add(vertexTangents[old]);
                    if(vertexColors.Length==vertices.Length)partColors.Add(vertexColors[old]);
                    partWeights.Add(weights[old]);
                    for(int ch=0;ch<8;ch++)if(uv[ch].Count==vertices.Length)partUv[ch].Add(uv[ch][old]);
                    return index;
                }
                for(int sub=0;sub<original.subMeshCount;sub++)
                {
                    var src=original.GetTriangles(sub);var dst=new List<int>();submeshTriangles[sub]=dst;
                    for(int i=0;i+2<src.Length;i+=3)
                    {
                        int a=src[i],b=src[i+1],c=src[i+2];
                        Vector3 p=(new Vector3(sourceCoords[a].x,sourceCoords[a].y,sourceCoords[a].z)
                            +new Vector3(sourceCoords[b].x,sourceCoords[b].y,sourceCoords[b].z)
                            +new Vector3(sourceCoords[c].x,sourceCoords[c].y,sourceCoords[c].z))/3f;
                        float ya=sourceCoords[a].y,yb=sourceCoords[b].y,yc=sourceCoords[c].y;
                        float arm=(BoneInfluence(weights[a],3,8)+BoneInfluence(weights[b],3,8)+BoneInfluence(weights[c],3,8))/3f;
                        if(slot==3&&arm<=.40f&&p.y<1.25f&&IsJacketWaistAttachment(p))
                        {dst.Add(WaistFacing(a));dst.Add(WaistFacing(b));dst.Add(WaistFacing(c));continue;}
                        if((slot==1||slot==3)&&arm<=.40f&&!IsJacketWaistAttachment(p)&&Mathf.Min(ya,Mathf.Min(yb,yc))<1.25f&&Mathf.Max(ya,Mathf.Max(yb,yc))>1.25f)
                        {
                            // Cut crossing faces at the belt rather than assigning
                            // entire triangles and leaving a jagged abdominal rim.
                            var polygon=new List<Vector3>();
                            var corners=new[]{Vector3.right,Vector3.up,Vector3.forward};
                            var heights=new[]{ya,yb,yc};
                            for(int edge=0;edge<3;edge++)
                            {
                                int next=(edge+1)%3;bool inside=slot==3?heights[edge]<=1.25f:heights[edge]>=1.25f;
                                bool nextInside=slot==3?heights[next]<=1.25f:heights[next]>=1.25f;
                                if(inside)polygon.Add(corners[edge]);
                                if(inside!=nextInside)polygon.Add(Vector3.Lerp(corners[edge],corners[next],(1.25f-heights[edge])/(heights[next]-heights[edge])));
                            }
                            for(int j=1;j+1<polygon.Count;j++)
                            {dst.Add(BeltVertex(polygon[0],a,b,c));dst.Add(BeltVertex(polygon[j],a,b,c));dst.Add(BeltVertex(polygon[j+1],a,b,c));}
                            continue;
                        }
                        if(ArmorSlotForTriangle(p,weights[a],weights[b],weights[c])!=slot)continue;
                        dst.Add(Map(a));dst.Add(Map(b));dst.Add(Map(c));
                    }
                }
                if(partVertices.Count==0)continue;
                var mesh=new Mesh{name="Warden armor slot / "+((ArmorSlot)slot),indexFormat=original.indexFormat};
                mesh.SetVertices(partVertices);mesh.SetNormals(partNormals);
                if(partTangents.Count==partVertices.Count)mesh.SetTangents(partTangents);
                if(partColors.Count==partVertices.Count)mesh.SetColors(partColors);
                if(partWeights.Count==partVertices.Count)mesh.boneWeights=partWeights.ToArray();
                for(int ch=0;ch<8;ch++)if(partUv[ch].Count==partVertices.Count)mesh.SetUVs(ch,partUv[ch]);
                mesh.subMeshCount=original.subMeshCount;
                for(int sub=0;sub<submeshTriangles.Length;sub++)mesh.SetTriangles(submeshTriangles[sub],sub,false);
                mesh.bindposes=original.bindposes;mesh.RecalculateBounds();
                var go=new GameObject("Warden armor / "+((ArmorSlot)slot));go.transform.SetParent(parent,false);
                var part=go.AddComponent<SkinnedMeshRenderer>();part.sharedMesh=mesh;part.bones=source.bones;
                part.rootBone=source.rootBone;part.sharedMaterials=source.sharedMaterials;part.updateWhenOffscreen=true;
                part.localBounds=source.localBounds;result[slot]=part;
            }
            Debug.Log("Warden equipment split: chest="+(result[1]!=null)+" arms="+(result[2]!=null)
                +" legs="+(result[3]!=null)+" feet="+(result[4]!=null));
            return result;
        }

        static float BoneInfluence(BoneWeight weight,int first,int last)
        {
            float value=0;
            if(weight.boneIndex0>=first&&weight.boneIndex0<=last)value+=weight.weight0;
            if(weight.boneIndex1>=first&&weight.boneIndex1<=last)value+=weight.weight1;
            if(weight.boneIndex2>=first&&weight.boneIndex2<=last)value+=weight.weight2;
            if(weight.boneIndex3>=first&&weight.boneIndex3<=last)value+=weight.weight3;
            return value;
        }

        static bool IsJacketWaistAttachment(Vector3 p)
            =>p.y>.95f&&p.y<1.40f&&(Mathf.Abs(p.x)>.235f||p.z>.13f||p.z<-.12f);

        static int ArmorSlotForTriangle(Vector3 p,BoneWeight a,BoneWeight b,BoneWeight c)
        {
            float arm=(BoneInfluence(a,3,8)+BoneInfluence(b,3,8)+BoneInfluence(c,3,8))/3f;
            float leg=(BoneInfluence(a,9,14)+BoneInfluence(b,9,14)+BoneInfluence(c,9,14))/3f;
            float x=Mathf.Abs(p.x),y=p.y;
            // Waist pouches and belt panels can extend beyond the arm's X range.
            // Their pelvis weights keep them in the chest slot, even beside a hand.
            // Shoulder and forearm sleeve fabric belongs to the jacket. Only the
            // distal glove/cuff follows the hands equipment slot; bone ownership
            // still follows the arm joints independently of equipment ownership.
            if(y>=.76f&&y<=1.72f&&x>=.12f&&arm>.40f)return y<CharacterArmFit.SourceCuffTop?2:1;
            // The scan's boots rise above the ankle. Keep the entire shaft with
            // the feet so legs-only does not stack it over the ordinary shoes.
            if(y<.43f)return 4;
            // Split at the actual trouser belt (target height 1.04m), not
            // above the abdominal pouches. The jacket retains its waist skirt.
            if(IsJacketWaistAttachment(p))return 1;
            if(y<1.25f)return 3;
            return 1;
        }

        static void AddClosedNeck(List<Vector3> vertices,List<Vector2> uv,List<Vector3> normals,
            List<BoneWeight> weights,List<int> triangles,Vector3[] source,Vector2[] sourceUv)
        {
            // The cropped scan has no continuous rear neck, particularly after mobile
            // simplification. Fill its interior with a short, capped anatomical surface;
            // both ends overlap existing head/coat geometry rather than forming a visible tube.
            const int sides=24,rows=7;
            // One interior front-neck sample avoids interpolating across unrelated
            // atlas islands, which produced dark vertical bands on the closed patch.
            Vector2 skinUv=Vector2.zero;float nearest=float.PositiveInfinity;
            Vector3 skinTarget=new Vector3(.055f,1.82f,.12f);
            for(int i=0;i<source.Length;i++)
            {
                if(source[i].y<1.78f||source[i].y>1.87f||source[i].x<.025f||source[i].x>.085f||source[i].z<.07f)continue;
                float distance=(source[i]-skinTarget).sqrMagnitude;
                if(distance<nearest){nearest=distance;skinUv=sourceUv.Length==source.Length?sourceUv[i]:Vector2.zero;}
            }
            int start=vertices.Count;
            for(int row=0;row<rows;row++)for(int col=0;col<sides;col++)
            {
                float t=row/(float)(rows-1),angle=col*Mathf.PI*2/sides;
                Vector3 point=NeckSurface(t,angle);
                Vector3 around=NeckSurface(t,angle+.01f)-NeckSurface(t,angle-.01f);
                Vector3 along=NeckSurface(t+.005f,angle)-NeckSurface(t-.005f,angle);
                // Normalize tangents first: the raw millimeter-scale cross product is
                // below Unity Vector3.normalized tolerance and otherwise becomes zero.
                Vector3 normal=Vector3.Cross(along.normalized,around.normalized).normalized;
                vertices.Add(point);normals.Add(normal);uv.Add(skinUv);
                float head=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.85f,t));
                weights.Add(new BoneWeight{boneIndex0=0,weight0=1-head,boneIndex1=2,weight1=head});
                if(row<rows-1)
                {
                    int a=start+row*sides+col,b=start+row*sides+(col+1)%sides;
                    triangles.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});
                }
            }
            // Caps guarantee a closed surface even if low-poly source faces disappear at
            // the crop plane. They remain inside the coat and lower skull respectively.
            for(int end=0;end<2;end++)
            {
                int center=vertices.Count,ring=start+(end==0?0:rows-1)*sides;
                vertices.Add(new Vector3(0,end==0?1.425f:1.605f,end==0?.014f:-.008f));
                normals.Add(end==0?Vector3.down:Vector3.up);uv.Add(skinUv);
                weights.Add(new BoneWeight{boneIndex0=end==0?0:2,weight0=1});
                for(int col=0;col<sides;col++)
                {
                    int a=ring+col,b=ring+(col+1)%sides;
                    if(end==0)triangles.AddRange(new[]{center,a,b});
                    else triangles.AddRange(new[]{center,b,a});
                }
            }
        }

        static Vector3 NeckSurface(float t,float angle)
        {
            // Flared trapezius, narrower middle and paired nape muscles around a shallow
            // center groove. Both ends overlap the coat/skull; no exposed long tube.
            float x=Mathf.Cos(angle),z=Mathf.Sin(angle),rear=Mathf.Max(0,-z);
            float middle=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI);
            float width=Mathf.LerpUnclamped(.066f,.060f,t)-.014f*middle;
            float depth=Mathf.LerpUnclamped(.057f,.058f,t)-.011f*middle;
            float groove=.0055f*Mathf.Exp(-x*x/.055f)*rear*middle;
            float paired=.004f*Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.48f)/.22f,2))*rear*middle;
            return new Vector3(x*width,Mathf.LerpUnclamped(1.425f,1.605f,t),
                Mathf.LerpUnclamped(.014f,-.008f,t)+z*(depth+paired-groove));
        }

        // Spatially adjacent glove and belt/pouch surfaces are different anatomical
        // regions. Follow the source surface to its sleeve or torso attachment instead
        // of allowing a waist strap's outer corner to be captured by the moving hand.
        static void ConstrainArmRegion(Mesh mesh,Vector3[] source,int[] representative,float[] arm)
        {
            var links=new List<int>[source.Length];
            for(int i=0;i<source.Length;i++)if(representative[i]==i)links[i]=new List<int>(8);
            void Link(int a,int b)
            {
                a=representative[a];b=representative[b];
                if(a==b)return;
                links[a].Add(b);links[b].Add(a);
            }
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                var indices=mesh.GetTriangles(sub);
                for(int i=0;i+2<indices.Length;i+=3)
                {Link(indices[i],indices[i+1]);Link(indices[i+1],indices[i+2]);Link(indices[i+2],indices[i]);}
            }
            float[] Distances(bool sleeve)
            {
                var distances=new float[source.Length];var queued=new bool[source.Length];
                var pending=new Queue<int>();
                for(int i=0;i<source.Length;i++)
                {
                    distances[i]=float.PositiveInfinity;
                    if(representative[i]!=i)continue;
                    Vector3 p=source[i];float x=Mathf.Abs(p.x);
                    bool seed=sleeve
                        ?arm[i]>.95f&&p.y>1.34f&&p.y<1.57f
                        :(x<.22f&&p.y>1f&&p.y<1.65f);
                    if(!seed)continue;
                    distances[i]=0;queued[i]=true;pending.Enqueue(i);
                }
                while(pending.Count>0)
                {
                    int a=pending.Dequeue();queued[a]=false;
                    foreach(int b in links[a])
                    {
                        float candidate=distances[a]+Vector3.Distance(source[a],source[b]);
                        if(candidate>=distances[b]-0.000001f)continue;
                        distances[b]=candidate;
                        if(!queued[b]){pending.Enqueue(b);queued[b]=true;}
                    }
                }
                return distances;
            }
            var sleeveDistance=Distances(true);var torsoDistance=Distances(false);
            int constrained=0;
            for(int i=0;i<source.Length;i++)
            {
                if(representative[i]!=i){arm[i]=arm[representative[i]];continue;}
                // Unconnected scan islands retain the positional fallback. Do not
                // invent a connection, remove their geometry or alter their UV seams.
                bool hasTorso=!float.IsInfinity(torsoDistance[i]);
                bool hasSleeve=!float.IsInfinity(sleeveDistance[i]);
                if(!hasTorso&&!hasSleeve)continue;
                float attachment=!hasTorso?1:!hasSleeve?0:
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.08f,torsoDistance[i]-sleeveDistance[i]));
                // This surface label is authoritative, not a cap on the spatial label.
                // The inner glove shares the sleeve's attachment even though the old
                // overlapping ellipses classified its adjacent corners as pelvis/hand.
                // Multiplying by that old label preserves the original 8mm discontinuity.
                float previous=arm[i];arm[i]=attachment;
                if(Mathf.Abs(previous-arm[i])>.1f)constrained++;
            }
            Debug.Log("Warden source surface: surface attachment reassigned "+constrained+" arm-region vertices; all faces retained");
        }

        static Vector3 FitEquipmentVertex(Vector3 p,float arm,bool includeLegs)
        {
            float sourceY=p.y;
            float axis=sourceY>=1.30f?Remap(sourceY,1.30f,1.62f,.37f,.265f)
                :Remap(sourceY,1.10f,1.30f,.425f,.37f);
            axis=Mathf.Clamp(axis,.25f,.425f);
            float armY=CharacterArmFit.Height(sourceY)+CharacterArmFit.CuffExtension(sourceY);
            float bodyY=sourceY>=1.62f?Remap(sourceY,1.62f,1.84f,1.427f,1.515f)
                :sourceY>=1.25f?Remap(sourceY,1.25f,1.62f,1.04f,1.427f)
                :sourceY>=1f?Remap(sourceY,1f,1.25f,.89f,1.04f)
                :sourceY>=.65f?Remap(sourceY,.65f,1f,.517f,.89f)
                :sourceY>=.25f?Remap(sourceY,.25f,.65f,.101f,.517f)
                :Remap(sourceY,.10f,.25f,.03f,.101f);
            float palm=Mathf.Lerp(.65f,1f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.06f,1.12f,sourceY)));
            float armX=Mathf.Sign(p.x)*(CharacterArmFit.Axis+(Mathf.Abs(p.x)-axis)*.82f*palm);
            float armDepth=SourceArmDepth(sourceY);
            p.z=Mathf.Lerp(p.z,(p.z-armDepth)*palm,arm);
            // Bring the jacket shoulder seam to the same .18m arm pivot.
            float chestWidth=Mathf.Lerp(1f,.70f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,1.62f,sourceY)));
            // Preserve 15cm of belt/hip height instead of flattening the entire
            // quarter-meter scan band into 2.5cm. Narrow pouch bulk slightly at the
            // waist while leaving the hip pivots and leg placement unchanged.
            float waist=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.96f,1.10f,sourceY))
                *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,1.40f,sourceY)));
            p.x=Mathf.Lerp(p.x*chestWidth*Mathf.Lerp(1f,.92f,waist),armX,arm);
            p.y=Mathf.Lerp(bodyY,armY,arm);
            // Curl only the distal scan glove. The wrist and palm remain on the shared
            // hand slot; a continuous arc turns the original straight fingertips inward.
            float curl=arm*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.04f,.94f,sourceY));
            p.y+=.060f*curl;
            p.z+=.050f*curl;
            if(includeLegs&&sourceY<1f)
            {
                float leg=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1f,sourceY));
                // Compress around each source leg axis without crossing the midline.
                // The inner interval maps continuously from 0 to the .105m hip axis;
                // the outer interval preserves the scan's thickness around that axis.
                float inward=.065f*Mathf.Clamp01(Mathf.Abs(p.x)/.17f)*leg;
                p.x-=Mathf.Sign(p.x)*inward;
            }
            return p;
        }

        // Cross-sections measured on the original scan distinguish the sleeves from
        // the jacket/backpack even when both have the same lateral coordinate.
        static float SourceArmDepth(float y)=>y>=1.30f
            ?Mathf.Lerp(-.02f,0,Mathf.InverseLerp(1.30f,1.62f,y))
            :Mathf.Lerp(.06f,-.02f,Mathf.InverseLerp(1.10f,1.30f,y));
        static float SourceArmInfluence(Vector3 p,float axis)
        {
            float x=Mathf.Abs(p.x),y=p.y;
            if(y<=.82f||y>=1.72f)return 0;
            float upper=Mathf.InverseLerp(1.20f,1.60f,y);
            float rx=Mathf.Lerp(.075f,.105f,upper),rz=Mathf.Lerp(.085f,.115f,upper);
            float dx=(x-axis)/rx,dz=(p.z-SourceArmDepth(y))/rz;
            float armDistance=dx*dx+dz*dz;
            float torsoWidth=y>1.45f?Mathf.Lerp(.225f,.29f,Mathf.InverseLerp(1.45f,1.68f,y)):.245f;
            float tx=x/torsoWidth,tz=(p.z-.015f)/.18f;
            float torsoDistance=tx*tx+tz*tz;
            float influence=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.25f,torsoDistance-armDistance));
            influence*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,.90f,y));
            // The upper rear jacket and collar belong to the chest. Their wide surface
            // used to be swept into the upper arms, leaving a horizontal hole behind.
            float collar=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.60f,1.72f,y));
            float rear=Mathf.Lerp(1f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.13f,-.075f,p.z)),
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.42f,1.52f,y)));
            return influence*collar*rear;
        }

        static float Remap(float value,float fromA,float fromB,float toA,float toB)
            =>Mathf.LerpUnclamped(toA,toB,(value-fromA)/(fromB-fromA));

        static BoneWeight BlendWeights(Vector3 p,bool includeArms,bool includeLegs,bool headOnly,float armInfluence,Vector3 source)
        {
            if(headOnly)return new BoneWeight{boneIndex0=2,weight0=1};
            // Weights depend only on position, never on a triangle's classification. This
            // keeps coincident vertices in the scan's disconnected islands moving together.
            float sourceY=source.y;int side=source.x>=0?6:3;
            if(includeArms&&armInfluence>0)
            {
                return CharacterArmFit.Weight(p,side,armInfluence);
            }
            // Belt and hip garments remain on the pelvis even after restoring their
            // visible height; classification must use source anatomy, not fitted Y.
            if(sourceY>=1.08f&&sourceY<=1.27f)return new BoneWeight{boneIndex0=1,weight0=1};
            if(includeLegs&&sourceY<1.08f)
            {
                int a=source.x>=0?12:9,b=a+1;
                float t=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.59f,p.y));
                if(p.y<.30f){a++;b++;t=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.09f,.23f,p.y));}
                // Use source anatomy for the complete hip transition. A hard
                // sourceY=1 cutoff previously switched adjacent 12mm faces from
                // .85 thigh weight to pelvis-only and stretched the running hem.
                float leg=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1.08f,sourceY));
                // A shared crotch seam belongs to the pelvis, then continuously enters
                // its source-side leg; no abrupt left/right bone switch at x=0.
                leg*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.07f,Mathf.Abs(source.x)));
                return new BoneWeight{boneIndex0=a,weight0=leg*(1-t),boneIndex1=b,weight1=leg*t,boneIndex2=1,weight2=1-leg};
            }
            float chest=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.07f,1.23f,p.y));
            return new BoneWeight{boneIndex0=0,weight0=chest,boneIndex1=1,weight1=1-chest};
        }
        static Transform Bone(DetailedHero3D h,int i)=>i switch
        {
            0=>h.chest,1=>h.pelvis,2=>h.head,
            3=>h.leftUpperArm,4=>h.leftForearm,5=>h.leftHand,
            6=>h.rightUpperArm,7=>h.rightForearm,8=>h.rightHand,
            9=>h.leftThigh,10=>h.leftShin,11=>h.leftFoot,
            12=>h.rightThigh,13=>h.rightShin,_=>h.rightFoot
        };

        static bool ReplaceOriginalNape(Vector3 p)
        {
            // Remove the original planar lower nape before exposing the lit replacement.
            // Ear/jaw sides are protected above their base; forward facial paint is intact.
            if(p.y>=1.82f||p.z>=.10f)return false;
            bool earOrJawSide=Mathf.Abs(p.x)>.065f&&p.y>1.765f&&p.z>.005f;
            bool forwardChin=p.z>.065f&&p.y>1.715f;
            return !earOrJawSide&&!forwardChin;
        }

        static bool KeepHeadTriangle(Vector3 p)
        {
            // Measured on the source head/shoulder GLB: chin ~1.72, rear collar ~1.75.
            // The former 1.46 cutoff retained the complete shirt section in the head slot.
            float front=Mathf.Clamp01(p.z/.09f);
            float cut=Mathf.Lerp(1.77f,1.71f,front)+Mathf.Max(0,Mathf.Abs(p.x)-.07f)*.6f*front;
            float nx=p.x/.065f,nz=(p.z-.02f)/.065f;
            bool neck=p.y>=1.675f&&nx*nx+nz*nz<=1f;
            return p.y>=cut||neck;
        }

        static bool KeepTriangle(bool headOnly,bool includeArms,bool includeLegs,int bone,Vector3 center)
        {
            if(headOnly)return bone==2;
            if(includeArms&&includeLegs)return true; // Armor scan has a collar, but no head.
            bool arm=includeArms&&bone>=3&&bone<=8;
            bool leg=includeLegs&&bone>=9&&bone<=14;
            bool torso=bone==0||(bone==1&&center.y>=.90f);
            // Remove scan-island gloves beside the waist; these lie below the belt and outside
            // the pelvis silhouette, unlike the actual arms attached at the shoulder.
            if(!arm&&!leg&&center.y<1.12f&&Mathf.Abs(center.x)>.20f)return false;
            return torso||arm||leg;
        }

        static int SelectBoneForRig(Vector3 p,DetailedHero3D hero,bool includeArms,bool includeLegs)
        {
            float x=Mathf.Abs(p.x);
            if(p.y>1.46f&&x<.24f)return 2;
            if(includeArms&&p.y>.85f&&p.y<1.66f&&x>.155f)
                return NearestBone(p,hero,3,8);
            // Let the thigh cloth overlap into the belt/pelvis band. The previous .82m cut
            // left a clean horizontal hole above both knees in the inventory portrait.
            if(includeLegs&&p.y<.92f&&x>.08f)
                return NearestBone(p,hero,9,14);
            return SelectBone(p);
        }
        static int NearestBone(Vector3 point,DetailedHero3D hero,int first,int last)
        {
            int selected=first;float best=float.PositiveInfinity;
            for(int i=first;i<=last;i++)
            {
                Transform bone=i switch
                {
                    3=>hero.leftUpperArm,4=>hero.leftForearm,5=>hero.leftHand,
                    6=>hero.rightUpperArm,7=>hero.rightForearm,8=>hero.rightHand,
                    9=>hero.leftThigh,10=>hero.leftShin,11=>hero.leftFoot,
                    12=>hero.rightThigh,13=>hero.rightShin,_=>hero.rightFoot
                };
                float distance=(point-hero.transform.InverseTransformPoint(bone.position)).sqrMagnitude;
                if(distance<best){best=distance;selected=i;}
            }
            return selected;
        }

        static int SelectBone(Vector3 p)
        {
            float x=Mathf.Abs(p.x);bool right=p.x>=0;
            // The imported head reaches the lower jaw at roughly 1.47m in rig space.
            // Keeping the old high cutoff detached the face from its actual helmet shell.
            if(p.y>1.46f&&x<.24f)return 2;
            if(p.y>1.00f&&x>.21f)
            {
                int upper=right?6:3,fore=right?7:4,hand=right?8:5;
                if(p.y>1.32f)return upper;
                if(p.y>1.18f)return fore;
                return hand;
            }
            // The generated fieldkit has a belt/hip silhouette from about 0.90-1.12m.
            // Preserve it on the pelvis rather than classifying it as thigh or dropping it.
            if(p.y<.78f&&x>.10f)
            {
                int thigh=right?12:9,shin=right?13:10,foot=right?14:11;
                if(p.y>.54f)return thigh;
                if(p.y>.16f)return shin;
                return foot;
            }
            if(p.y<1.16f)return 1;
            return 0;
        }
        public static bool IsLimbRenderer(Renderer renderer,DetailedHero3D hero)
        {
            if(!renderer||!hero||!hero.IsAnatomyRenderer(renderer))return false;
            // The equipment generator also hangs oversized sci-fi plates below these joints.
            // Keep only the anatomy's immediate sleeve/trouser/glove/boot meshes.
            var parent=renderer.transform.parent;
            return parent==hero.leftUpperArm||parent==hero.rightUpperArm
                ||parent==hero.leftForearm||parent==hero.rightForearm
                ||parent==hero.leftHand||parent==hero.rightHand
                ||parent==hero.leftThigh||parent==hero.rightThigh
                ||parent==hero.leftShin||parent==hero.rightShin
                ||parent==hero.leftFoot||parent==hero.rightFoot
                ||parent==hero.leftToe||parent==hero.rightToe;
        }
        static bool IsHandRenderer(Renderer renderer,DetailedHero3D hero)
            =>renderer&&hero&&(renderer.transform.parent==hero.leftHand||renderer.transform.parent==hero.rightHand);
        static bool IsArmRenderer(Renderer renderer,DetailedHero3D hero)
            =>renderer&&hero&&(renderer.transform.IsChildOf(hero.leftUpperArm)||renderer.transform.IsChildOf(hero.rightUpperArm)
                ||renderer.transform.IsChildOf(hero.leftForearm)||renderer.transform.IsChildOf(hero.rightForearm)
                ||renderer.transform.IsChildOf(hero.leftHand)||renderer.transform.IsChildOf(hero.rightHand));
        static bool IsLegRenderer(Renderer renderer,DetailedHero3D hero)
            =>renderer&&hero&&(renderer.transform.IsChildOf(hero.leftThigh)||renderer.transform.IsChildOf(hero.rightThigh));
        static bool IsLegacyHeadAccessory(Renderer renderer,DetailedHero3D hero)
            =>renderer&&hero&&renderer.transform.IsChildOf(hero.head)&&!hero.IsAnatomyRenderer(renderer);
        public static void SetLegacyGeometryVisible(DetailedHero3D hero,bool generated,
            SkinnedMeshRenderer head,SkinnedMeshRenderer armor,GameObject mask,bool generatedArms=false,bool generatedLegs=false)
        {
            if(!hero)return;
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer)continue;
                if(renderer==head||renderer==armor)continue;
                if(mask&&renderer.transform.IsChildOf(mask.transform))continue;
                bool bridge=renderer.gameObject.name.StartsWith("Warden ")&&renderer.gameObject.name.Contains("shoulder lining");
                bool oldUpperSleeve=renderer.gameObject.name.Contains("Jacket sleeve deltoid bicep");
                bool oldArm=IsArmRenderer(renderer,hero);
                bool headAccessory=IsLegacyHeadAccessory(renderer,hero);
                // Keep only the legacy animated sleeve/forearm/hand meshes where the scan
                // does not yet provide articulated arms. Manufacturer armor is not anatomy:
                // leaving it enabled stacks the old shoulder/cuff shells over the new fieldkit.
                bool legacyArmAnatomy=oldArm&&IsLimbRenderer(renderer,hero)&&hero.IsAnatomyRenderer(renderer);
                bool oldLeg=IsLegRenderer(renderer,hero);
                renderer.enabled=!generated||bridge||(headAccessory&&!mask)||(!generatedArms&&!generatedLegs&&IsLimbRenderer(renderer,hero))
                    ||(generatedArms&&!oldUpperSleeve&&!oldArm&&!generatedLegs)
                    ||(generatedLegs&&legacyArmAnatomy&&!generatedArms);
            }
        }

        // Mixed loadouts keep the ordinary rig and non-Warden attachments. Only
        // anatomy occupied by the visible scan slots is removed from the render.
        public static void RestoreBaseAnatomy(DetailedHero3D hero)
        {
            if(!hero)return;
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>(true))
                if(renderer&&hero.IsAnatomyRenderer(renderer))renderer.enabled=true;
        }
        public static void SetSlotGeometryVisible(DetailedHero3D hero,bool[] slots,
            SkinnedMeshRenderer head,SkinnedMeshRenderer armor,GameObject mask,bool scanFeet=false)
        {
            if(!hero)return;
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer||renderer==head||renderer==armor)continue;
                if(mask&&renderer.transform.IsChildOf(mask.transform))continue;
                if(renderer.gameObject.name.StartsWith("Warden ")&&renderer.gameObject.name.Contains("shoulder lining"))continue;
                if(!hero.IsAnatomyRenderer(renderer))continue;
                var t=renderer.transform;
                bool isHead=t.IsChildOf(hero.head);
                bool isArm=t.IsChildOf(hero.leftUpperArm)||t.IsChildOf(hero.rightUpperArm);
                bool isFoot=t.IsChildOf(hero.leftFoot)||t.IsChildOf(hero.rightFoot);
                bool isLeg=!isFoot&&(t.IsChildOf(hero.leftThigh)||t.IsChildOf(hero.rightThigh));
                bool isChest=t.IsChildOf(hero.chest)&&!isHead&&!isArm;
                bool isPelvis=t.IsChildOf(hero.pelvis)&&!isLeg&&!isFoot;
                bool sleeve=isArm&&IsBaseSleeve(renderer);
                bool hand=t.IsChildOf(hero.leftHand)||t.IsChildOf(hero.rightHand)
                    ||isArm&&renderer.name.Contains("Exposed forearm tendons");
                bool covered=slots[1]&&isChest||(slots[1]||slots[3])&&isPelvis
                    ||slots[1]&&sleeve||slots[2]&&hand||slots[3]&&isLeg||(slots[4]||scanFeet)&&isFoot;
                if(covered)renderer.enabled=false;
            }
        }
        public static bool IsBaseSleeve(Renderer renderer)
        {
            if(!renderer)return false;
            foreach(var material in renderer.sharedMaterials)
            {
                if(!material)continue;
                if(material==DetailedHeroMaterials3D.Get("Cloth")||material==DetailedHeroMaterials3D.Get("Stitch")
                    ||material.name.StartsWith("Detailed survivor / Cloth")
                    ||material.name.StartsWith("Detailed survivor / Stitch")
                    ||material.name.StartsWith("Faded olive woven canvas")
                    ||material.name.StartsWith("Canvas folded seam"))return true;
            }
            return false;
        }
        public static bool HasArticulatedArms(SkinnedMeshRenderer renderer)
        {
            if(!renderer||!renderer.sharedMesh)return false;
            foreach(var weight in renderer.sharedMesh.boneWeights)
                if((weight.weight0>.5f&&weight.boneIndex0>=3&&weight.boneIndex0<=8)
                    ||(weight.weight1>.5f&&weight.boneIndex1>=3&&weight.boneIndex1<=8)
                    ||(weight.weight2>.5f&&weight.boneIndex2>=3&&weight.boneIndex2<=8)
                    ||(weight.weight3>.5f&&weight.boneIndex3>=3&&weight.boneIndex3<=8))return true;
            return false;
        }
        public static bool HasArticulatedLegs(SkinnedMeshRenderer renderer)
        {
            if(!renderer||!renderer.sharedMesh)return false;
            foreach(var weight in renderer.sharedMesh.boneWeights)
                if((weight.weight0>.5f&&weight.boneIndex0>=9&&weight.boneIndex0<=14)
                    ||(weight.weight1>.5f&&weight.boneIndex1>=9&&weight.boneIndex1<=14)
                    ||(weight.weight2>.5f&&weight.boneIndex2>=9&&weight.boneIndex2<=14)
                    ||(weight.weight3>.5f&&weight.boneIndex3>=9&&weight.boneIndex3<=14))return true;
            return false;
        }
        public static int CountVisibleLegacy(DetailedHero3D hero,SkinnedMeshRenderer head,
            SkinnedMeshRenderer armor,GameObject mask)
        {
            if(!hero)return 0;
            int count=0;
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                if(renderer==head||renderer==armor)continue;
                if(mask&&renderer.transform.IsChildOf(mask.transform))continue;
                if(renderer.gameObject.name.StartsWith("Warden ")&&renderer.gameObject.name.Contains("shoulder lining"))continue;
                count++;
            }
            return count;
        }
        public static Material[][] CaptureMaterials(Renderer[] renderers)
        {
            var originals=new Material[renderers.Length][];
            for(int i=0;i<renderers.Length;i++)originals[i]=renderers[i]?renderers[i].sharedMaterials:null;
            return originals;
        }
        public static GameObject[] CreateJointBridges(DetailedHero3D hero)
        {
            if(!hero)return new GameObject[0];
            var cloth=DetailedHeroMaterials3D.Get("ClothDark");
            var result=new GameObject[2];
            result[0]=Bridge("Warden left sleeve shoulder lining",hero.leftUpperArm,
                new Vector3(0,-.025f,0),new Vector3(.14f,.15f,.14f),cloth);
            result[1]=Bridge("Warden right sleeve shoulder lining",hero.rightUpperArm,
                new Vector3(0,-.025f,0),new Vector3(.14f,.15f,.14f),cloth);
            return result;
        }
        static GameObject Bridge(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var bridge=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bridge.name=name;bridge.transform.SetParent(parent,false);
            bridge.transform.localPosition=position;bridge.transform.localScale=scale;
            bridge.layer=parent.gameObject.layer;
            var collider=bridge.GetComponent<Collider>();if(collider){if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);}
            bridge.GetComponent<Renderer>().sharedMaterial=material;
            bridge.SetActive(false);
            return bridge;
        }
        public static void SetJointBridges(GameObject[] bridges,bool enabled)
        {if(bridges!=null)foreach(var bridge in bridges)if(bridge)bridge.SetActive(enabled);}
        public static void SetLimbMaterials(Renderer[] renderers,Material[][] originals,DetailedHero3D hero,bool generated)
        {
            if(renderers==null||originals==null)return;
            for(int i=0;i<renderers.Length;i++)
            {
                var renderer=renderers[i];if(!IsLimbRenderer(renderer,hero)||originals[i]==null)continue;
                if(!generated){renderer.sharedMaterials=originals[i];continue;}
                var recolored=new Material[originals[i].Length];
                for(int j=0;j<recolored.Length;j++)
                {
                    var original=originals[i][j];string name=original?original.name.ToLowerInvariant():"";
                    string material=name.Contains("steel")||name.Contains("blue")||name.Contains("metal")?"Steel":
                        name.Contains("leather")||name.Contains("pad")?"LeatherDark":
                        name.Contains("skin")?"Skin":"ClothDark";
                    recolored[j]=DetailedHeroMaterials3D.Get(material);
                }
                renderer.sharedMaterials=recolored;
            }
        }
    }
}











