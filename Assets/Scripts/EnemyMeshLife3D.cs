using UnityEngine;
namespace NeonVault
{
    public sealed class EnemyMeshLife3D:MonoBehaviour
    {
        Foe foe; float death=-1,hit; Quaternion fall; Vector3 origin;
        public void Init(Foe value){foe=value;}
        public void NotifyHit(Vector3 point){hit=.12f;}
        public void BeginDeath()
        {
            death=0;origin=transform.position;fall=transform.rotation*Quaternion.Euler(80,0,foe.kind%2==0?18:-18);
            var motion=GetComponent<ActorMotion>();if(motion)motion.enabled=false;
            var gear=GetComponent<ActorEquipmentVisual>();if(gear)gear.enabled=false;
        }
        void LateUpdate()
        {
            if(death<0){hit=Mathf.Max(0,hit-Time.deltaTime);return;}
            death+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,death/.55f);
            transform.rotation=Quaternion.Slerp(transform.rotation,fall,t);
            transform.position=origin+Vector3.down*Mathf.Lerp(0,.35f,t);
        }
    }
}
