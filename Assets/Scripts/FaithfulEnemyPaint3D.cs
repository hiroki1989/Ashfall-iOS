using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public sealed partial class FaithfulEnemy3D
    {
        // Bind-pose UVs are baked once into the shared meshes. World-space projection
        // would make the painted folds slide across animated arms and legs.
        void ApplyAuthoredEnemyPaint()
        {
            var atlas=Resources.Load<Texture2D>("Art/Enemy3DRebuild/Enemy_"+kind+"/projection-atlas-filled");
            if(!atlas)return;
            var headAtlas=Resources.Load<Texture2D>("Art/Enemy3DRebuild/Enemy_"+kind+"/head-atlas-filled");
            foreach(var material in materials)
            {
                if(!material||!material.HasProperty("_EnemyAtlas"))continue;
                material.SetTexture("_EnemyAtlas",atlas);
                material.SetFloat("_EnemyProjectionEnabled",1);
                if(material.name.Contains("diseased anatomy")||material.name.Contains("ragged fabric")||
                    material.name.Contains("torn tissue")||(kind==8&&material.name.Contains("battered armor")))
                    material.SetFloat("_CreatureSurface",-1);
                material.SetFloat("_AtlasStrength",0);
            }
            foreach(var filter in rig.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.gameObject.activeSelf||!filter.sharedMesh)continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer||!renderer.sharedMaterial)continue;
                string pigmentName=renderer.sharedMaterial.name;
                // The hand often hangs below the hips: joint identity, not height,
                // determines which authored sheet is projected onto each limb.
                bool leg=filter.transform.IsChildOf(thighs[0])||filter.transform.IsChildOf(thighs[1]);
                bool headPart=filter.transform.IsChildOf(head);
                bool weapon=(kind==2||kind==5)&&muzzle&&filter.transform.IsChildOf(muzzle.parent);
                bool paint=!weapon&&(pigmentName.Contains("diseased anatomy")||
                    pigmentName.Contains("ragged fabric")||pigmentName.Contains("torn tissue")||
                    (kind==8&&pigmentName.Contains("battered armor")));
                if(headPart)
                {
                    paint=headAtlas&&(pigmentName.Contains("diseased anatomy")||
                        pigmentName.Contains("ivory bone"));
                    if(paint)
                    {
                        var headMaterial=new Material(renderer.sharedMaterial){name=renderer.sharedMaterial.name+" / authored head",hideFlags=HideFlags.DontSave};
                        headMaterial.SetTexture("_EnemyAtlas",headAtlas);
                        renderer.sharedMaterial=headMaterial;materials.Add(headMaterial);
                    }
                }
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;
                var uv=new List<Vector2>(vertices.Length);
                var masks=new List<Vector4>(vertices.Length);
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 point=rig.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                    Vector3 normal=rig.InverseTransformDirection(filter.transform.TransformDirection(normals[i])).normalized;
                    Vector4 projected;
                    if(headPart)
                    {
                        Vector3 local=head.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                        projected=EnemyHeadUV(local,normal,paint?1f:0f);
                    }
                    else projected=EnemyPaintUV(point,normal,leg,paint?1f:0f);
                    uv.Add(new Vector2(projected.x,projected.y));
                    masks.Add(new Vector4(projected.z,0,0,0));
                }
                mesh.SetUVs(2,uv);
                mesh.SetUVs(3,masks);
            }
        }
        static Vector4 EnemyHeadUV(Vector3 p,Vector3 n,float mask)
        {
            int tile=n.z>.55f*Mathf.Abs(n.x)?0:n.z<-.55f*Mathf.Abs(n.x)?2:1;
            float u=tile==1?.5f+p.z/.22f:tile==2?.5f-p.x/.22f:.5f+p.x/.22f;
            float v=(p.y+.09f)/.35f;
            const float cell=1f/3f,inset=.012f;
            return new Vector4(tile*cell+inset+Mathf.Clamp01(u)*(cell-2*inset),
                inset+Mathf.Clamp01(v)*(1-2*inset),mask,0);
        }
        Vector4 EnemyPaintUV(Vector3 p,Vector3 n,bool leg,float mask)
        {
            int col,row;float u,v=0;
            if(!leg)
            {
                // Upper atlas contains the head, jacket, arms and waist together.
                float side=Mathf.Abs(n.x),front=n.z;
                if(front>.62f*side){col=0;row=0;u=.5f+p.x/1.34f;}
                else if(front<-.62f*side){col=0;row=1;u=.5f-p.x/1.34f;}
                else {col=2;row=0;u=.5f+(n.x>=0?p.z:-p.z)/.72f;}
                v=Mathf.Clamp01((p.y-.75f)/1.03f)*.66f;
            }
            else
            {
                bool left=p.x<0;float center=(left?-1:1)*girth*.54f;
                float side=Mathf.Abs(n.x),front=n.z;
                if(front>.58f*side){col=left?1:2;row=1;u=.5f+(p.x-center)/.39f;}
                else if(front<-.58f*side){col=left?3:0;row=left?2:3;u=.5f-(p.x-center)/.39f;}
                else if(n.x>=0){col=left?3:0;row=left?1:2;u=.5f+p.z/.39f;}
                else {col=left?1:2;row=2;u=.5f-p.z/.39f;}
                v=(p.y+.08f)/1.05f;
            }
            u=Mathf.Clamp01(u);v=Mathf.Clamp01(v);
            // Map only the authored illustration, never the triangulated filler
            // surrounding its silhouette. Leg pictures use less than half a cell.
            Vector4 bounds=EnemyProjectionBounds.Tiles[kind,row*4+col];
            const float cell=.25f;
            return new Vector4((col+Mathf.Lerp(bounds.x,bounds.z,u))*cell,
                1-(row+1)*cell+Mathf.Lerp(bounds.y,bounds.w,v)*cell,mask,0);
        }
    }
}
