using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Accuracy controls both target cone and reach. At 100, any visible
        // enemy inside the 30-degree cone can be acquired; lower accuracy
        // progressively narrows both, with occlusion still respected.
        Vector3 AutoAimDirection(Vector3 origin,Vector3 intended,float accuracy)
        {
            if(Layout==null||intended.sqrMagnitude<.0001f)return intended;
            intended.y=0;intended.Normalize();
            float precision=Mathf.Clamp01(accuracy/100f);
            var balance=VaultBalanceProfile.Current;
            float minAngle=balance!=null?balance.autoAimMinAngle:3f;
            float maxAngle=balance!=null?balance.autoAimMaxAngle:30f;
            float minDistance=balance!=null?balance.autoAimMinDistance:5f;
            float maxDistance=balance!=null?balance.autoAimMaxDistance:160f;
            float tolerance=Mathf.Lerp(minAngle,maxAngle,precision*precision*precision);
            // At maximum accuracy the camera frame and cone, rather than a
            // hidden distance cap, define the target set.
            float reach=precision>=.999f?float.PositiveInfinity:Mathf.Lerp(minDistance,maxDistance,precision*precision*precision*precision);
            Foe selected=null;Vector3 selectedDirection=Vector3.zero;
            float bestScore=float.PositiveInfinity;
            foreach(var foe in Enemies)
            {
                if(foe==null||foe.dead)continue;
                if(cam!=null){Vector3 viewport=cam.WorldToViewportPoint(foe.transform.position+Vector3.up*.6f);if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)continue;}
                Vector3 target=foe.transform.position;target.y=origin.y;
                Vector3 toward=target-origin;float distance=toward.magnitude;
                if(distance<.8f||distance>reach)continue;
                Vector3 direction=toward/distance;
                float angle=Vector3.Angle(intended,direction);
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
