using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Geometry is fitted to the existing animated joints. No second body or broad
    // shoulder/back panels: those hid the source design in multi-angle review.
    public sealed partial class FaithfulEnemy3D
    {
        void ApplyEnemyShapeRebuild()
        {
            ContourTorso();
            SculptWaistJunction();
            for(int i=0;i<2;i++)
            {
                SculptHandBridge(i);
                SculptFootOutline(i);
            }
            if(kind==8)for(int side=-1;side<=1;side+=2)ContourArmoredFlank(side);
            // Large shoulder shells tested poorly from the front and while walking.
            // Keep the smaller original joint-following armor until a proper sculpt is ready.
        }

        void ContourArmoredFlank(int side)
        {
            // The illustrated cuirass wraps around the ribs. The old construction
            // only had front and back faceplates, leaving a flesh-only side view.
            const int rows=8,cols=12;
            var v=new List<Vector3>((rows+1)*(cols+1));var ix=new List<int>(rows*cols*6);
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float t=j/(float)rows,a=Mathf.Lerp(-1.32f,1.32f,i/(float)cols);
                float y=Mathf.Lerp(-.27f,.32f,t);
                float radius=girth*(.99f+.045f*Mathf.Sin(t*3.14f));
                float x=side*radius*Mathf.Cos(a*.52f);
                float z=.167f*Mathf.Sin(a);
                // Shallow overlapping rows follow the layered front/back plates.
                x+=side*.009f*Mathf.Sin(t*18f)*Mathf.Sin(Mathf.PI*t);
                v.Add(V(x,y,z));
                if(j<rows&&i<cols){int k=j*(cols+1)+i;Quad(ix,k,k+cols+1,k+1,k+cols+2,true);}
            }
            MeshPart("Rust cuirass wrap / "+side,chest,steel,v,ix,true);
        }

        void ContourTorso()
        {
            float[] width={1.035f,1.04f,1.095f,1.10f,.94f,1.04f,1.12f,1.07f,1.10f,.95f};
            float[] depth={1.025f,1.07f,1.09f,1.08f,.90f,1.035f,1.12f,1.055f,1.11f,.98f};
            Material surface=kind==2||kind==5?cloth:kind==8?steel:flesh;
            const int rings=13,sides=32;
            var v=new List<Vector3>((rings+1)*(sides+1));var ix=new List<int>(rings*sides*6);
            for(int j=0;j<=rings;j++)
            {
                float t=j/(float)rings,y=Mathf.Lerp(-.33f,.40f,t),u=y+.33f;
                float rx=SculptProfile3D.RadiusAtHeight(new[]{0f,.12f,.28f,.48f,.61f,.70f},
                    new[]{girth*.66f,girth*.75f,girth,girth*1.04f,girth*.90f,girth*.28f},u)*width[kind];
                float rz=SculptProfile3D.RadiusAtHeight(new[]{0f,.12f,.28f,.48f,.61f,.70f},
                    new[]{.13f,.145f,.17f,.18f,.145f,.07f},u)*depth[kind];
                for(int i=0;i<=sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,ca=Mathf.Cos(a),sa=Mathf.Sin(a);
                    float front=Mathf.Max(0,sa),back=Mathf.Max(0,-sa),lobe=1f;
                    switch(kind)
                    {
                        case 0: lobe+=front*.045f*Mathf.Sin(t*22+a*3)-.035f*Lobe(ca*rx,y,0,.05f,.07f,.12f);break;
                        case 1: lobe+=.15f*Lobe(ca*rx,y,.12f,.19f,.15f,.22f);break;
                        case 2: lobe+=.055f*Mathf.Pow(Mathf.Abs(ca),4)*Mathf.Sin(t*4);break;
                        case 3: lobe+=.11f*Lobe(Mathf.Abs(ca*rx),y,girth*.7f,.24f,.16f,.20f);break;
                        case 4: lobe-=.10f*Lobe(ca*rx,y,0,-.02f,.09f,.20f);break;
                        case 5: lobe+=.045f*back*Lobe(ca*rx,y,0,.23f,.2f,.18f);break;
                        case 6: lobe+=.12f*Lobe(Mathf.Abs(ca*rx),y,girth*.52f,.14f,.17f,.17f);break;
                        case 7: lobe+=.14f*front*Lobe(ca*rx,y,0,-.14f,.23f,.19f);break;
                        case 8: lobe+=.065f*Mathf.Sin(t*27)*front;break;
                        case 9: lobe+=.10f*back*Lobe(ca*rx,y,0,.20f,.12f,.22f);break;
                    }
                    float side=1+.018f*Mathf.Sin(a*5+y*15+kind*2)*Mathf.Sin(Mathf.PI*t);
                    float x=ca*rx*lobe*side,z=sa*rz*lobe*side;
                    z+=front*.012f*Lobe(Mathf.Abs(x),y,girth*.48f,.22f,girth*.45f,.10f);
                    z-=back*.016f*Lobe(Mathf.Abs(x),y,girth*.50f,.22f,girth*.42f,.13f);
                    v.Add(V(x,y,z));
                    if(j<rings&&i<sides){int k=j*(sides+1)+i;Quad(ix,k,k+sides+1,k+1,k+sides+2,false);}
                }
            }
            MeshPart("Rebuilt anatomical contour / "+kind,chest,surface,v,ix);
        }

        void SculptWaistJunction()
        {
            // A narrow shape just BELOW the old belt closes the thorax/pelvis join.
            // It is deliberately inset at the belt height so straps and pouches stay visible.
            const int rings=4,sides=28;var v=new List<Vector3>((rings+1)*(sides+1));var ix=new List<int>(rings*sides*6);
            float[] ys={-.045f,-.025f,.003f,.027f,.043f};
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides,y=ys[j],t=j/(float)rings;
                float r=1+.025f*Mathf.Sin(a*5+kind)*Mathf.Sin(t*Mathf.PI);
                float rx=girth*Mathf.Lerp(.70f,.77f,t)*r;
                float rz=Mathf.Lerp(.125f,.145f,t)*r;
                v.Add(V(Mathf.Cos(a)*rx,y,Mathf.Sin(a)*rz));
                if(j<rings&&i<sides){int k=j*(sides+1)+i;Quad(ix,k,k+sides+1,k+1,k+sides+2,false);}
            }
            MeshPart("Waist to pelvis transition / "+kind,pelvis,kind==2||kind==5?cloth:flesh,v,ix);
        }

        void SculptHandBridge(int index)
        {
            Transform palm=forearms[index].Find("Palm and fingers");if(!palm)return;
            float muscle=kind==3?.16f:kind==6?.145f:kind==2?.12f:kind==7?.13f:kind==4||kind==9?.059f:.084f;
            Material surface=kind==2||kind==5?leather:kind==8?scar:flesh;
            const int rows=5,cols=8;var v=new List<Vector3>((rows+1)*(cols+1));var ix=new List<int>(rows*cols*6);
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float t=j/(float)rows,u=i/(float)cols*2-1;
                float width=muscle*Mathf.Lerp(.48f,.54f,t),x=u*width;
                float y=Mathf.Lerp(.005f,-.087f,t);
                float z=.027f+.021f*Mathf.Sqrt(Mathf.Max(0,1-u*u))
                    +.007f*Mathf.Sin(Mathf.PI*t)+.002f*Mathf.Cos(u*12+index);
                v.Add(V(x,y,z));
                if(j<rows&&i<cols){int k=j*(cols+1)+i;Quad(ix,k,k+cols+1,k+1,k+cols+2,true);}
            }
            MeshPart("Fitted metacarpal surface / "+kind+" / "+index,palm,surface,v,ix,true);
        }

        void SculptFootOutline(int index)
        {
            // Flat sole-to-instep profile preserves all existing toes, claws and laces.
            // The silhouette differs between barefoot monsters and booted enemies.
            bool bare=kind==3||kind==4;float shoe=kind==3?.12f:.075f;
            const int rings=8,sides=16;var v=new List<Vector3>((rings+1)*(sides+1));var ix=new List<int>(rings*sides*6);
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                float t=j/(float)rings,a=i*Mathf.PI*2/sides;
                float z=Mathf.Lerp(-.070f,.158f,t);
                float width=shoe*(.73f+.22f*Mathf.Sin(t*Mathf.PI));
                if(bare)width*=t>.60f?1+.15f*(t-.60f)/.40f:1;
                float arch=Mathf.Sin(Mathf.PI*t);
                float x=Mathf.Cos(a)*width,y=-.030f+Mathf.Sin(a)*(.026f+.013f*arch);
                if(t>.75f)y+=.008f*(t-.75f)/.25f;
                v.Add(V(x,y,z));
                if(j<rings&&i<sides){int k=j*(sides+1)+i;Quad(ix,k,k+sides+1,k+1,k+sides+2,true);}
            }
            MeshPart("Fitted foot and sole / "+kind+" / "+index,feet[index],bare?flesh:kind==8?steel:leather,v,ix);
        }
    }
}
