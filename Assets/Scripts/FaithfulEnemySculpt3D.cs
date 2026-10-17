using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public sealed partial class FaithfulEnemy3D
    {
        static float SoftProfile(float[] values,float t)
        {
            float f=Mathf.Clamp01(t)*(values.Length-1);int i=Mathf.Min(values.Length-2,(int)f);float q=f-i;
            return Mathf.Lerp(values[i],values[i+1],q*q*(3-2*q));
        }
        static float Lobe(float x,float y,float cx,float cy,float rx,float ry)
        {float a=(x-cx)/rx,b=(y-cy)/ry;return Mathf.Exp(-a*a-b*b);}

        void HarnessRibbon(Transform parent,Vector3[] path,float width)
        {
            var v=new List<Vector3>();var ix=new List<int>();
            for(int j=0;j<path.Length;j++)
            {
                Vector3 direction=(path[Mathf.Min(path.Length-1,j+1)]-path[Mathf.Max(0,j-1)]).normalized;
                Vector3 normal=V(path[j].x*.3f,0,path[j].z).normalized;
                Vector3 side=Vector3.Cross(direction,normal).normalized*width*.5f;
                v.Add(path[j]-side);v.Add(path[j]+side);
                if(j<path.Length-1){int k=j*2;ix.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}
            }
            MeshPart("Flat fitted stitched harness webbing",parent,leather,v,ix,true);
            foreach(int edge in new[]{0,1})
            {
                var points=new Vector3[path.Length];for(int j=0;j<points.Length;j++)points[j]=v[j*2+edge];
                Curve("Harness edge stitching",parent,points,.0011f,bone);
            }
        }

        void GroundContact(float strike)
        {
            float lowest=float.PositiveInfinity,shoe=kind==3?.12f:.075f;
            foreach(var foot in feet)
            {
                Vector3 center=foot.TransformPoint(V(0,-.008f,.038f));
                float x=foot.TransformVector(Vector3.right*shoe).y,y=foot.TransformVector(Vector3.up*.067f).y,z=foot.TransformVector(Vector3.forward*.145f).y;
                lowest=Mathf.Min(lowest,center.y-Mathf.Sqrt(x*x+y*y+z*z));
            }
            // Sector floor surface is 9 cm below the actor's legacy ground origin.
            // Preserve the pouncer's brief attack lift while grounding its resting stance.
            float floor=transform.position.y-.09f+(kind==4?strike*.06f:0);
            rig.position+=Vector3.up*(floor-lowest);
        }

        static bool BoneAperture(float radius,float angle)
        {
            float x=Mathf.Cos(angle)*radius,y=Mathf.Sin(angle)*radius;
            return Lobe(x,y,.34f,.30f,.14f,.19f)>.37f||Lobe(x,y,-.38f,.25f,.12f,.16f)>.37f||Lobe(x,y,.31f,-.40f,.10f,.15f)>.37f;
        }
        void SolidPlate(string name,Transform parent,Material material,List<Vector3> vertices,List<int> triangles)
        {
            var boundary=new Dictionary<long,Vector2Int>();int faceCount=triangles.Count,vertexCount=vertices.Count;
            for(int i=0;i<faceCount;i+=3)for(int j=0;j<3;j++)
            {
                int a=triangles[i+j],b=triangles[i+(j+1)%3];long key=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
                if(boundary.ContainsKey(key))boundary.Remove(key);else boundary.Add(key,new Vector2Int(a,b));
            }
            for(int i=0;i<vertexCount;i++)vertices.Add(vertices[i]-Vector3.forward*(material==bone?.009f:.005f));
            for(int i=0;i<faceCount;i+=3)triangles.AddRange(new[]{triangles[i]+vertexCount,triangles[i+2]+vertexCount,triangles[i+1]+vertexCount});
            foreach(var edge in boundary.Values){int a=edge.x,b=edge.y;triangles.AddRange(new[]{a,a+vertexCount,b,b,a+vertexCount,b+vertexCount});}
            MeshPart(name,parent,material,vertices,triangles);
        }

        void FoldedLapel(int side)
        {
            float width=girth/.215f;
            var v=new List<Vector3>{V(side*.044f*width,.34f,.098f),V(side*.126f*width,.22f,.178f),V(side*.105f*width,-.11f,.157f),V(side*.073f*width,.15f,.189f)};
            var ix=side>0?new List<int>{0,2,1,0,3,2}:new List<int>{0,1,2,0,2,3};
            int faces=ix.Count;for(int i=0;i<4;i++)v.Add(v[i]-Vector3.forward*.003f);
            for(int i=0;i<faces;i+=3)ix.AddRange(new[]{ix[i]+4,ix[i+2]+4,ix[i+1]+4});
            MeshPart("Folded thin torn cloth lapel",chest,cloth,v,ix);
            Curve("Lapel sewn edge",chest,new[]{v[0],v[1],v[2]},.0014f,leather);
        }

        void SculptFace(bool skull)
        {
            // One continuous craniofacial surface, including cheek, nose, orbit and mandible.
            // A real opening leaves the teeth behind the lips rather than pasted onto a ball.
            const int rows=96,sides=128;
            var v=new List<Vector3>();var ix=new List<int>();
            var widths=new[]{.030f,.054f,.070f,.087f,.088f,.084f,.071f,.045f,.003f};
            var depths=new[]{.036f,.053f,.063f,.073f,.078f,.077f,.069f,.047f,.003f};
            var heights=new[]{-.074f,-.050f,-.014f,.040f,.095f,.145f,.192f,.224f,.238f};
            bool masked=kind==2||kind==5,closed=kind==9||masked;
            float mouthY=kind==7?-.001f:.005f,mouthX=kind==4?.034f:.037f,mouthH=kind==7?.022f:.035f;
            var material=skull?bone:flesh;
            for(int row=0;row<=rows;row++)for(int col=0;col<=sides;col++)
            {
                float t=row/(float)rows,a=col*Mathf.PI*2/sides,y=SoftProfile(heights,t);
                float x=Mathf.Cos(a)*SculptProfile3D.RadiusAtHeight(heights,widths,y),z=Mathf.Sin(a)*SculptProfile3D.RadiusAtHeight(heights,depths,y);
                float front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a)),5);
                float nose=.021f*Lobe(x,y,0,.106f,.014f,.037f)+.030f*Lobe(x,y,0,.072f,.018f,.014f);
                if(skull)nose=-.015f*Lobe(x,y,0,.080f,.015f,.024f);
                float cheek=.012f*Lobe(Mathf.Abs(x),y,.058f,.075f,.023f,.023f);
                float orbit=-.020f*Lobe(Mathf.Abs(x),y,.038f,.122f,.023f,.017f);
                float brow=.010f*Lobe(Mathf.Abs(x),y,.037f,.149f,.028f,.010f);
                float philtrum=.004f*Lobe(x,y,0,.043f,.010f,.014f);
                float hollow=-.009f*Lobe(Mathf.Abs(x),y,.065f,.035f,.025f,.035f);
                float chin=.009f*Lobe(x,y,0,-.044f,.032f,.018f);
                if(kind==7){cheek+=.012f*Lobe(Mathf.Abs(x),y,.055f,.025f,.040f,.054f);chin+=.012f*Lobe(x,y,0,-.059f,.050f,.025f);}
                z+=front*(nose+cheek+orbit+brow+philtrum+hollow+chin);
                v.Add(V(x,y,z));
                if(row==rows||col==sides)continue;
                float my=SoftProfile(heights,(row+.5f)/rows),mx=Mathf.Cos(a+Mathf.PI/sides)*SculptProfile3D.RadiusAtHeight(heights,widths,my);
                bool aperture=!closed&&Mathf.Sin(a)>.55f&&mx*mx/(mouthX*mouthX)+(my-mouthY)*(my-mouthY)/(mouthH*mouthH)<1;
                if(!aperture){int k=row*(sides+1)+col,b=k+sides+1;Quad(ix,k,b,k+1,b+1,false);}
            }
            int bottom=v.Count;v.Add(V(0,heights[0],0));int crown=v.Count;v.Add(V(0,heights[heights.Length-1],0));
            for(int col=0;col<sides;col++)ix.AddRange(new[]{bottom,col,col+1,crown,rows*(sides+1)+col+1,rows*(sides+1)+col});
            MeshPart("Continuous sculpted cranium / orbital and nasal anatomy",head,material,v,ix);
            if(skull)Ellipsoid("Recessed nasal skull opening",head,V(0,.080f,.063f),V(.011f,.018f,.006f),dark,24,20);
            for(int side=-1;side<=1;side+=2)
            {
                Ellipsoid("Recessed eyeball",head,V(side*.038f,.122f,.064f),skull?V(.021f,.015f,.009f):V(.015f,.010f,.009f),dark,32,20);
                if(kind!=3)Ellipsoid("Clouded small iris",head,V(side*.038f,.122f,.073f),V(.006f,.006f,.0025f),eye,24,16);
                Curve("Upper orbital lid",head,new[]{V(side*.020f,.123f,.068f),V(side*.037f,.132f,.074f),V(side*.055f,.125f,.068f)},.0022f,material);
                Curve("Lower orbital lid",head,new[]{V(side*.020f,.118f,.068f),V(side*.038f,.114f,.071f),V(side*.056f,.121f,.067f)},.0018f,material);
                Ellipsoid("Nostril opening",head,V(side*.012f,.061f,.098f),V(.006f,.0037f,.0025f),dark,20,12);
                Ellipsoid("Ear cartilage",head,V(side*.089f,.101f,-.005f),V(.015f,.029f,.018f),material,32,24);
                Curve("Ear helix fold",head,new[]{V(side*.095f,.122f,.006f),V(side*.102f,.105f,.011f),V(side*.097f,.082f,.007f)},.0028f,material);
                Curve("Sunken nasolabial crease",head,new[]{V(side*.019f,.060f,.085f),V(side*.030f,.041f,.079f),V(side*.040f,.015f,.069f)},.0012f,scar);
            }
            if(!masked)
            {
                if(closed)Curve("Thin closed mouth",head,new[]{V(-.027f,.009f,.069f),V(0,.005f,.075f),V(.027f,.009f,.069f)},.0017f,dark);
                else
                {
                    Ellipsoid("Deep oral cavity",head,V(0,mouthY,.049f),V(mouthX*.98f,mouthH*.98f,.013f),dark,40,28);
                    var lip=new Vector3[25];for(int j=0;j<lip.Length;j++){float a=j*Mathf.PI*2/(lip.Length-1);lip[j]=V(Mathf.Cos(a)*mouthX,mouthY+Mathf.Sin(a)*mouthH,.066f+Mathf.Sin(a)*.003f);}
                    Curve("Continuous uneven lip border",head,lip,.0027f,material);
                    for(int row=0;row<2;row++)for(int tooth=0;tooth<8;tooth++)
                    {
                        float x=(tooth-3.5f)*.0074f;
                        Ellipsoid("Teeth set inside alveolar arch",head,V(x,mouthY+(row==0?mouthH*.61f:-mouthH*.62f),.059f-Mathf.Abs(x)*.10f),V(.0032f,.0055f+(tooth%3)*.0007f,.0036f),bone,16,12);
                    }
                }
            }
        }

        Vector3 AnatomicSurface(string name,Vector3 p,float angle,float u)
        {
            float front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(angle)),3),back=Mathf.Pow(Mathf.Max(0,-Mathf.Sin(angle)),3);
            if(name=="Tapered thorax")
            {
                float mass=kind==3?1.7f:kind==6?1.35f:1;
                p.z+=front*mass*(.030f*Lobe(Mathf.Abs(p.x),p.y,girth*.42f,.205f,girth*.38f,.083f)
                    +.012f*Lobe(Mathf.Abs(p.x),p.y,girth*.23f,.020f,girth*.17f,.11f)
                    -.009f*Lobe(p.x,p.y,0,.17f,.014f,.13f));
                p.z+=back*(.020f*Lobe(Mathf.Abs(p.x),p.y,girth*.48f,.20f,girth*.40f,.12f)-.012f*Lobe(p.x,p.y,0,.15f,.020f,.21f));
                if(kind==3||kind==6)
                    for(int row=0;row<3;row++)p.z+=front*.017f*Lobe(Mathf.Abs(p.x),p.y,girth*.24f,.065f-row*.085f,girth*.21f,.038f);
                p.z+=front*.009f*Lobe(Mathf.Abs(p.x),p.y,girth*.52f,.30f,girth*.43f,.020f);
            }
            if(name.Contains("biceps"))
                p.z+=front*.016f*Mathf.Pow(Mathf.Sin(u*Mathf.PI),2)-back*.006f*Mathf.Sin(u*Mathf.PI*2);
            if(name.Contains("flexor"))
                p.x+=Mathf.Cos(angle)*.006f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(angle*3+u*2);
            if(name.Contains("Thigh muscle"))
                p.z+=front*.013f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(u*Mathf.PI);
            if(name.Contains("Calf and tibia"))
                p.z+=back*.012f*Mathf.Exp(-Mathf.Pow((u-.65f)/.22f,2));
            return p;
        }

        void SculptDigits(Transform hand,float muscle,int side,bool gloved)
        {
            var material=kind==8?scar:gloved?leather:flesh;float length=kind==4?.175f:kind==8?.145f:kind==3?.135f:.10f;
            for(int finger=0;finger<4;finger++)
            {
                float x=(finger-1.5f)*muscle*.31f,len=length*(1-Mathf.Abs(finger-1.2f)*.10f);
                Vector3 a=V(x,-.075f,.015f),b=V(x*1.23f,-.075f-len*.62f,.036f),c=V(x*1.32f,-.075f-len,.066f);
                TaperedDigit(hand,a,b,c,muscle*.13f,material);
                if(kind==4||kind==8||kind==9)Cone("Terminal curved keratin claw",hand,c,c+V(x*.18f,-.025f,.035f),.007f,kind==8?scar:bone);
            }
            // The thumb arises from the side of the palm, with its own opposed metacarpal.
            TaperedDigit(hand,V(-side*muscle*.46f,-.018f,.025f),V(-side*muscle*.83f,-.064f,.053f),V(-side*muscle*.69f,-.105f,.082f),muscle*.16f,material);
        }
        void TaperedDigit(Transform parent,Vector3 a,Vector3 b,Vector3 c,float radius,Material material)
        {
            const int rows=20,sides=12;var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<=rows;row++)
            {
                float t=row/(float)rows;Vector3 q=(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c;
                Vector3 axis=(2*(1-t)*(b-a)+2*t*(c-b)).normalized,across=Vector3.Cross(axis,Vector3.forward).normalized,normal=Vector3.Cross(axis,across);
                float r=radius*Mathf.Lerp(1,.47f,t)*(1+.14f*Mathf.Exp(-Mathf.Pow((t-.48f)/.08f,2)));
                if(t>.84f)r*=Mathf.Sqrt(Mathf.Max(.0001f,1-Mathf.Pow((t-.84f)/.16f,2)));
                for(int col=0;col<=sides;col++)
                {
                    float angle=col*Mathf.PI*2/sides;v.Add(q+across*Mathf.Cos(angle)*r+normal*Mathf.Sin(angle)*r*.82f);
                    if(row<rows&&col<sides){int k=row*(sides+1)+col;Quad(ix,k,k+sides+1,k+1,k+sides+2,true);}
                }
            }
            MeshPart("Tapered articulated phalanges",parent,material,v,ix);
        }

        void SculptTrousers(Transform thigh,Transform shin,float radius)
        {
            var material=kind==2?cloth:kind==5?cloth:kind==4?cloth:kind==9?leather:leather;
            Loft("Torn trouser fabric around exposed kneecap",thigh,V(0,-legLength,0),material,
                new[]{0f,legLength*.15f,legLength*.40f,legLength*.75f,legLength},
                new[]{radius*.82f,radius*.90f,radius*1.09f,radius*1.14f,radius*.86f},
                new[]{radius*.84f,radius*.91f,radius*1.02f,radius*1.06f,radius*.87f},64,.005f);
            for(int side=-1;side<=1;side+=2)
                Curve("Torn knee fabric edge",thigh,new[]{V(side*radius*.80f,-legLength*.90f,radius*.44f),V(side*radius*.62f,-legLength*.77f,radius*.70f),V(side*radius*.28f,-legLength*.79f,radius*.88f)},.002f,material);
            if(kind!=4)for(int side=-1;side<=1;side+=2)
                Rag("Torn trouser calf fabric",shin,V(side*radius*.58f,-.055f,radius*.52f),radius*.90f,legLength*.40f,material,side+kind);
        }
    }
}
