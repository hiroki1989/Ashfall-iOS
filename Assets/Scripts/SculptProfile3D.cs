using UnityEngine;

namespace NeonVault
{
    // Shape-preserving cubic interpolation in physical height. Easing both radius
    // and height with the same parameter still draws a straight conical segment.
    public static class SculptProfile3D
    {
        public static float RadiusAtHeight(float[] heights,float[] radii,float height)
        {
            int i=0;while(i<heights.Length-2&&height>heights[i+1])i++;
            float width=Mathf.Max(.00001f,heights[i+1]-heights[i]);
            float t=Mathf.Clamp01((height-heights[i])/width);
            float slope=(radii[i+1]-radii[i])/width;
            float left=i==0?slope:(radii[i]-radii[i-1])/Mathf.Max(.00001f,heights[i]-heights[i-1]);
            float right=i+2==heights.Length?slope:(radii[i+2]-radii[i+1])/Mathf.Max(.00001f,heights[i+2]-heights[i+1]);
            float m0=left*slope<=0?0:2*left*slope/(left+slope);
            float m1=right*slope<=0?0:2*right*slope/(right+slope);
            float t2=t*t,t3=t2*t;
            return Mathf.Max(0,(2*t3-3*t2+1)*radii[i]+(t3-2*t2+t)*width*m0+(-2*t3+3*t2)*radii[i+1]+(t3-t2)*width*m1);
        }
    }
}
