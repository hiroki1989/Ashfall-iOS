using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Accuracy controls both target cone and reach. At 100, any visible
        // enemy inside the 30-degree cone can be acquired; lower accuracy
        // progressively narrows both, with occlusion still respected.
        Foe aimTarget;
        public Vector3 AimReticleWorldPosition => aimTarget != null && !aimTarget.dead
            ? new Vector3(aimTarget.transform.position.x, PlayerWeapon.MuzzlePosition.y, aimTarget.transform.position.z)
            : PlayerPosition + aim * AimReticleRadius(aim) + Vector3.up * .9f;
        public bool HasAimTarget => aimTarget != null && !aimTarget.dead;
        float AimReticleRadius(Vector3 direction)
        {
            if (gun == null) return 6f;
            float t = Mathf.Clamp01((EffectiveAccuracy - 35f) / 65f);
            float desired = Mathf.Lerp(4.2f, IsOverworld ? 11.2f : 9.6f, t * t * .55f + t * .45f);
            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            if (flat.sqrMagnitude < .0001f) flat = Vector3.right;
            flat.Normalize();
            if (cam != null)
            {
                Vector3 anchor = PlayerPosition + Vector3.up * .9f;
                Vector3 v0 = cam.WorldToViewportPoint(anchor);
                Vector3 v1 = cam.WorldToViewportPoint(anchor + flat);
                Vector2 dv = new Vector2(v1.x - v0.x, v1.y - v0.y);
                float maxR = desired;
                if (dv.x > .0001f) maxR = Mathf.Min(maxR, (.93f - v0.x) / dv.x);
                else if (dv.x < -.0001f) maxR = Mathf.Min(maxR, (v0.x - .07f) / -dv.x);
                if (dv.y > .0001f) maxR = Mathf.Min(maxR, (.90f - v0.y) / dv.y);
                else if (dv.y < -.0001f) maxR = Mathf.Min(maxR, (v0.y - .10f) / -dv.y);
                desired = Mathf.Clamp(maxR, 2.5f, desired);
            }
            return desired;
        }

        Vector3 AutoAimDirection(Vector3 origin,Vector3 intended,float accuracy)
        {
            aimTarget=null;
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
            aimTarget=selected;
            return selected!=null?selectedDirection:intended;
        }
    }
}
