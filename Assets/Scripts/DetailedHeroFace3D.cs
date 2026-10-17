using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public sealed partial class DetailedHero3D
    {
        static readonly float[] FaceY={0f,.021f,.048f,.081f,.12f,.16f,.20f,.223f,.237f};
        static readonly float[] FaceX={.036f,.060f,.077f,.088f,.090f,.089f,.078f,.051f,.004f};
        static readonly float[] FaceZ={.046f,.059f,.067f,.072f,.078f,.080f,.078f,.055f,.004f};
        static float FacialLobe(float x,float y,float cx,float cy,float sx,float sy)
        {return Mathf.Exp(-Mathf.Pow((x-cx)/sx,2)-Mathf.Pow((y-cy)/sy,2));}
        static float FaceRelief(float x,float y,float facing)
        {
            float ax=Mathf.Abs(x);
            return Mathf.Pow(Mathf.Max(0,facing),4)*(
                .026f*FacialLobe(x,y,0,.098f,.013f,.034f)
                +.030f*FacialLobe(x,y,0,.070f,.017f,.013f)
                +.010f*FacialLobe(ax,y,.019f,.064f,.009f,.007f)
                +.013f*FacialLobe(ax,y,.055f,.092f,.021f,.021f)
                -.011f*FacialLobe(ax,y,.040f,.121f,.022f,.012f)
                +.012f*FacialLobe(ax,y,.039f,.138f,.025f,.010f)
                -.007f*FacialLobe(ax,y,.056f,.054f,.021f,.023f)
                +.010f*FacialLobe(x,y,0,.012f,.031f,.015f)
                -.003f*FacialLobe(x,y,0,.049f,.006f,.010f));
        }
        Vector3 FacePoint(float x,float y,float lift=.0008f)
        {
            float rx=SculptProfile3D.RadiusAtHeight(FaceY,FaceX,y+.025f);
            float rz=SculptProfile3D.RadiusAtHeight(FaceY,FaceZ,y+.025f);
            float f=Mathf.Sqrt(Mathf.Max(.01f,1-x*x/(rx*rx)));
            return V(x,y,rz*f+FaceRelief(x,y,f)+lift);
        }
        Material FaceMaterial(string name,Color color,float gloss=.10f)
        {
            var m=new Material(skin){name=name,color=color};materials.Add(m);
            if(m.HasProperty("_Outline"))m.SetFloat("_Outline",0);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",gloss);
            return m;
        }
        void FacePatch(string name,Vector2 center,Vector2 radius,Material m,float lift)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int rings=8,sides=40;
            vertices.Add(FacePoint(center.x,center.y,lift));uv.Add(Vector2.one*.5f);
            for(int r=1;r<=rings;r++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides,t=r/(float)rings;
                // Almond eye aperture narrows towards both canthi.
                float x=Mathf.Cos(a),y=Mathf.Sin(a)*(name.Contains("eye")?(.60f+.40f*Mathf.Abs(Mathf.Sin(a))):1);
                vertices.Add(FacePoint(center.x+x*radius.x*t,center.y+y*radius.y*t,lift));uv.Add(new Vector2(x,y)*t*.5f+Vector2.one*.5f);
            }
            for(int i=0;i<sides;i++)triangles.AddRange(new[]{0,1+i,2+i});
            for(int r=0;r<rings-1;r++)for(int i=0;i<sides;i++){int a=1+r*(sides+1)+i,b=a+sides+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            // Facing +Z. The ring fan above follows +Z throughout.
            MeshObject(name,head,Vector3.zero,vertices,uv,triangles,m);
        }
        void BuildWeatheredFace()
        {
            var shadow=FaceMaterial("Warm recessed facial creases",new Color(.36f,.225f,.145f));
            var sclera=FaceMaterial("Aged ivory sclera",new Color(.69f,.66f,.57f),.38f);
            var iris=FaceMaterial("Muted grey green iris",new Color(.13f,.19f,.17f),.42f);
            var pupil=FaceMaterial("Pupil and nostril interior",new Color(.025f,.022f,.018f));
            var beard=FaceMaterial("Charcoal brown beard stubble",new Color(.22f,.16f,.115f));
            var lip=FaceMaterial("Desaturated weathered lips",new Color(.39f,.235f,.17f));
            foreach(int s in new[]{-1,1})
            {
                FacePatch("Recessed eye aperture",new Vector2(s*.037f,.120f),new Vector2(.0165f,.0042f),shadow,.0011f);
                FacePatch("Narrow ivory eye",new Vector2(s*.037f,.120f),new Vector2(.015f,.0033f),sclera,.0015f);
                FacePatch("Iris",new Vector2(s*.036f,.120f),new Vector2(.0037f,.0031f),iris,.0019f);
                FacePatch("Pupil",new Vector2(s*.036f,.120f),new Vector2(.0016f,.0026f),pupil,.0022f);
                Curve("Heavy angled eyebrow",head,new[]{FacePoint(s*.017f,.139f,.0018f),FacePoint(s*.037f,.143f,.0018f),FacePoint(s*.060f,.136f,.0018f)},.0014f,beard,true);
                Curve("Lower eyelid",head,new[]{FacePoint(s*.020f,.117f),FacePoint(s*.039f,.114f),FacePoint(s*.054f,.119f)},.00045f,skin);
                FacePatch("Recessed nostril",new Vector2(s*.014f,.061f),new Vector2(.0046f,.0019f),pupil,.0012f);
                Curve("Nasolabial weather crease",head,new[]{FacePoint(s*.022f,.061f),FacePoint(s*.028f,.048f),FacePoint(s*.030f,.033f)},.00028f,shadow,true);
                Curve("Cheek to jaw plane",head,new[]{FacePoint(s*.060f,.077f),FacePoint(s*.057f,.057f),FacePoint(s*.045f,.025f)},.00025f,shadow,true);
            }
            Curve("Closed asymmetric mouth seam",head,new[]{FacePoint(-.025f,.036f,.0014f),FacePoint(-.009f,.035f,.0014f),FacePoint(.004f,.0365f,.0014f),FacePoint(.025f,.034f,.0014f)},.0009f,shadow);
            Curve("Natural lower lip",head,new[]{FacePoint(-.019f,.031f,.001f),FacePoint(0,.0295f,.001f),FacePoint(.018f,.030f,.001f)},.0012f,lip,true);
            // Scattered short hairs lie on the face surface instead of a painted helmet-like beard band.
            var random=new System.Random(21017);
            for(int i=0;i<340;i++)
            {
                float x=Mathf.Lerp(-.063f,.063f,(float)random.NextDouble()),y=Mathf.Lerp(-.009f,.065f,(float)random.NextDouble());
                bool moustache=y>.043f&&y<.057f&&Mathf.Abs(x)<.026f;
                bool jaw=y<.029f||Mathf.Abs(x)>.036f&&y<.055f;
                if(!moustache&&!jaw||Mathf.Abs(x)>.047f&&y<.007f)continue;
                Curve("Fine irregular two-day stubble",head,new[]{FacePoint(x,y,.0008f),FacePoint(x+.0004f,y+.0015f,.0008f)},.00023f,beard);
            }
            Curve("Old healed cheek scar",head,new[]{FacePoint(-.059f,.099f),FacePoint(-.055f,.086f),FacePoint(-.048f,.073f)},.00032f,lip,true);
        }
    }
}
