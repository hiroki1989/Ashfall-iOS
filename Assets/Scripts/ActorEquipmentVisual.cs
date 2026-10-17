using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    [DefaultExecutionOrder(200)]
    public sealed class ActorEquipmentVisual : MonoBehaviour
    {
        readonly Dictionary<ArmorSlot,List<Renderer>> parts=new Dictionary<ArmorSlot,List<Renderer>>();
        readonly Dictionary<ArmorSlot,List<Transform>> details=new Dictionary<ArmorSlot,List<Transform>>();
        readonly Dictionary<Renderer,Color> defaults=new Dictionary<Renderer,Color>();
        AimedWeapon weapon;
        Transform body;
        readonly Transform[] upper=new Transform[2],lower=new Transform[2],hands=new Transform[2],elbows=new Transform[2];
        public Vector3 RightHandPosition=>Detailed!=null?Detailed.rightHand.position:Layered!=null?Layered.RightHandPosition:hands[1]==null?transform.position:hands[1].position;
        public Vector3 LeftHandPosition=>Detailed!=null?Detailed.leftHand.position:Layered!=null?Layered.LeftHandPosition:hands[0]==null?transform.position:hands[0].position;
        public Vector3 RightElbowPosition=>Detailed!=null?Detailed.rightForearm.position:Layered!=null?Layered.RightElbowPosition:elbows[1]==null?transform.position:elbows[1].position;
        public Vector3 LeftElbowPosition=>Detailed!=null?Detailed.leftForearm.position:Layered!=null?Layered.LeftElbowPosition:elbows[0]==null?transform.position:elbows[0].position;
        DetailedHero3D Detailed=>GetComponent<DetailedHeroPlayer3D>()?.body;
        LayeredPlayer2D Layered=>GetComponent<LayeredPlayer2D>();
        public Vector3 RightShoulderPosition=>Detailed!=null?Detailed.rightUpperArm.position:Layered!=null?Layered.RightShoulderPosition:ShoulderPosition(1);
        public Vector3 LeftShoulderPosition=>Detailed!=null?Detailed.leftUpperArm.position:Layered!=null?Layered.LeftShoulderPosition:ShoulderPosition(-1);
        public float ArmExtension {get;private set;}=1;
        AimedWeapon ActiveWeapon=>weapon!=null?weapon:(weapon=GetComponentInChildren<AimedWeapon>());
        public float GripError=>ActiveWeapon==null?0:Vector3.Distance(RightHandPosition,ActiveWeapon.GripPosition);
        public float SupportGripError=>ActiveWeapon==null?0:Vector3.Distance(LeftHandPosition,ActiveWeapon.SupportGripPosition);
        public bool ContinuousGeometry => Layered==null;
        public int AppliedArmorCount {get;private set;}
        public void Register(ArmorSlot slot,Transform part,bool variant=false)
        {
            if(!parts.TryGetValue(slot,out var list)){list=new List<Renderer>();parts.Add(slot,list);}
            foreach(var r in part.GetComponentsInChildren<Renderer>()){list.Add(r);defaults[r]=r.sharedMaterial.color;}
            if(variant){if(!details.TryGetValue(slot,out var adornments)){adornments=new List<Transform>();details.Add(slot,adornments);}adornments.Add(part);part.gameObject.SetActive(false);}
        }
        public void InitArms(VaultArt art,Transform torso,Color armor)
        {
            body=torso;
            for(int i=0;i<2;i++)
            {
                upper[i]=art.Part("Articulated upper arm",PrimitiveType.Capsule,Vector3.zero,new Vector3(.22f,.2f,.22f),armor,torso);
                lower[i]=art.Part("Articulated bracer",PrimitiveType.Capsule,Vector3.zero,new Vector3(.25f,.2f,.25f),armor,torso);
                hands[i]=art.Part("Grip locked gauntlet",PrimitiveType.Sphere,Vector3.zero,new Vector3(.2f,.18f,.25f),VaultArt.Dark,torso);
                elbows[i]=art.Part("Elbow joint",PrimitiveType.Sphere,Vector3.zero,new Vector3(.22f,.22f,.22f),armor,torso);
                VaultArt.SculptLimb(upper[i],false);VaultArt.SculptLimb(lower[i],true);
                elbows[i].localScale=Vector3.one*.15f;
                for(int plate=0;plate<3;plate++)
                {
                    var shell=art.Part("Bracer overlapping shell",PrimitiveType.Cube,new Vector3(0,-.5f+plate*.5f,.38f),new Vector3(.82f,.35f,.22f),new Color(.28f,.30f,.27f),lower[i]);
                    shell.localRotation=Quaternion.Euler(-8,0,0);
                }
                art.Part("Sleeve cuff strap",PrimitiveType.Cube,new Vector3(0,.72f,.28f),new Vector3(1.08f,.13f,.5f),VaultArt.Bronze,lower[i]);
                art.Part("Glove knuckle panel",PrimitiveType.Cube,new Vector3(0,.25f,.12f),new Vector3(.85f,.22f,.65f),VaultArt.Dark,hands[i]);
                for(int finger=0;finger<4;finger++)
                {
                    art.Part("Gloved finger",PrimitiveType.Capsule,new Vector3(-.32f+finger*.21f,-.08f,.32f),new Vector3(.17f,.38f,.27f),VaultArt.Dark,hands[i]).localRotation=Quaternion.Euler(52,0,0);
                    art.Part("Finger joint",PrimitiveType.Sphere,new Vector3(-.32f+finger*.21f,.09f,.38f),Vector3.one*.16f,VaultArt.Dark,hands[i]);
                }
                art.Part("Glove thumb",PrimitiveType.Capsule,new Vector3(.48f,-.12f,.1f),new Vector3(.18f,.35f,.21f),VaultArt.Dark,hands[i]).localRotation=Quaternion.Euler(30,0,-35);
                art.Part("Wrist cuff",PrimitiveType.Cylinder,new Vector3(0,-.74f,0),new Vector3(1.10f,.10f,1.10f),VaultArt.Dark,lower[i]);
                Register(ArmorSlot.Arms,upper[i]);Register(ArmorSlot.Arms,lower[i]);
            }
        }
        public void ApplyWeaponVisualSet(string visualSet)=>Layered?.SetWeaponVisualSet(visualSet);
        public void ApplyArmor(ArmorItem[] equipped)
        {
            var detailed=GetComponent<DetailedHeroPlayer3D>();if(detailed!=null){detailed.ApplyArmor(equipped);AppliedArmorCount=0;if(equipped!=null)foreach(var a in equipped)if(a!=null)AppliedArmorCount++;return;}
            GetComponent<PaintedActor>()?.ApplyArmor(equipped);
            if(Layered!=null){Layered.ApplyArmor(equipped);AppliedArmorCount=Layered.AppliedArmorCount;return;}
            AppliedArmorCount=0;
            foreach(var pair in parts)foreach(var r in pair.Value){var block=new MaterialPropertyBlock();block.SetColor("_Color",defaults[r]);r.SetPropertyBlock(block);}
            foreach(var list in details.Values)foreach(var part in list)part.gameObject.SetActive(false);
            if(equipped==null)return;
            foreach(var item in equipped)
            {
                if(item==null||!parts.TryGetValue(item.slot,out var list))continue;
                AppliedArmorCount++;
                foreach(var r in list){var block=new MaterialPropertyBlock();block.SetColor("_Color",Color.Lerp(defaults[r],item.color,r.sharedMaterial.name.Contains("metal")?.12f:.045f));r.SetPropertyBlock(block);}
                if(details.TryGetValue(item.slot,out var adornments))for(int i=0;i<adornments.Count;i++)
                {var part=adornments[i];part.gameObject.SetActive(true);part.localScale=Vector3.Scale(variantSizes[part],new Vector3(1+.035f*(item.visualVariant%3),1+.025f*(item.visualVariant%4),1+.025f*(item.visualVariant%2)));}
            }
        }
        readonly Dictionary<Transform,Vector3> variantSizes=new Dictionary<Transform,Vector3>();
        public void RememberVariants(){foreach(var list in details.Values)foreach(var part in list)variantSizes[part]=part.localScale;}
        Vector3 ShoulderPosition(float side)=>body==null?transform.position:body.TransformPoint(new Vector3(side*.32f,1.16f,0));
        void LateUpdate()=>SynchronizePose();
        public void SynchronizePose()
        {
            var detailed=GetComponent<DetailedHeroPlayer3D>();if(detailed!=null){detailed.Synchronize();return;}
            if(Layered!=null){Layered.RefreshPose();return;}
            if(body==null)return;
            if(weapon==null)weapon=GetComponentInChildren<AimedWeapon>();

            float scale=Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.x));ArmExtension=1;
            for(int i=0;i<2;i++)
            {
                if(hands[i]==null||upper[i]==null||lower[i]==null)continue;
                float side=i==0?-1:1;
                Vector3 shoulder=ShoulderPosition(side);
                Vector3 hand=weapon!=null?(i==0?weapon.SupportGripPosition:weapon.GripPosition):body.TransformPoint(new Vector3(side*.35f,.60f,.19f+Mathf.Sin(Time.time*3+i*3.14f)*.08f));
                Vector3 axis=hand-shoulder;float distance=axis.magnitude;
                Vector3 direction=distance>.0001f?axis/distance:transform.forward;
                // Adult upper arm and forearm lengths. Only unreachable legacy mounts extend the limb.
                float extension=Mathf.Max(1,distance/(.80f*scale)*1.001f);ArmExtension=Mathf.Max(ArmExtension,extension);
                float upperLength=.41f*scale*extension,lowerLength=.39f*scale*extension;
                Vector3 pole=Vector3.ProjectOnPlane(transform.right*side*.45f-Vector3.up,direction).normalized;
                float along=distance>.0001f?(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance):0;
                Vector3 elbow=shoulder+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
                Segment(upper[i],shoulder,elbow,.17f,scale);Segment(lower[i],elbow,hand,.18f,scale);
                elbows[i].position=elbow;hands[i].SetPositionAndRotation(hand,weapon!=null?weapon.transform.rotation:transform.rotation);
            }
        }
        static void Segment(Transform part,Vector3 start,Vector3 end,float width,float scale)
        {
            part.position=(start+end)*.5f;part.rotation=Quaternion.FromToRotation(Vector3.up,end-start);
            part.localScale=new Vector3(width,(end-start).magnitude/(2*scale),width);
        }
    }
}



