using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Angular help is strongest for a deliberate nearby aim and never reaches
        // enemies beyond the normal combat view or through sector geometry.
        Vector3 AutoAimDirection(Vector3 origin,Vector3 intended,float accuracy)
        {
            if(Layout==null||intended.sqrMagnitude<.0001f)return intended;
            intended.y=0;intended.Normalize();
            Foe selected=null;Vector3 selectedDirection=Vector3.zero;
            float bestScore=float.PositiveInfinity;
            foreach(var foe in Enemies)
            {
                if(foe==null||foe.dead)continue;
                Vector3 target=foe.transform.position;target.y=origin.y;
                Vector3 toward=target-origin;float distance=toward.magnitude;
                if(distance<.8f||distance>18f)continue;
                Vector3 direction=toward/distance;
                float angle=Vector3.Angle(intended,direction);
                float nearFactor=Mathf.Lerp(1f,.45f,Mathf.InverseLerp(5f,18f,distance));
                // The top end is intentionally steep: ordinary 80-85 accuracy
                // guns receive about 10-13 degrees at 5 m, while only a true
                // 100-accuracy build can reach the requested 30-degree cap.
                float precision=Mathf.Clamp01(accuracy/100f);
                float tolerance=Mathf.Lerp(3f,30f,Mathf.Pow(precision,6f))*nearFactor;
                if(angle>tolerance||!Layout.ClearLine(origin,target,.12f))continue;
                // Prefer the enemy closest to the player's line, then the nearer
                // enemy when angles are effectively equal.
                float score=angle+distance*.015f;
                if(score>=bestScore)continue;
                selected=foe;selectedDirection=direction;bestScore=score;
            }
            return selected!=null?selectedDirection:intended;
        }
    }
}
