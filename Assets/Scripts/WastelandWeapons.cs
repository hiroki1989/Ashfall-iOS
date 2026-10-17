using UnityEngine;
namespace NeonVault
{
    public sealed partial class VaultArt
    {
        void AddWastelandWeaponVariants(Transform gun)
        {
            var models=gun.gameObject.AddComponent<WastelandWeaponModels>();
            for(int n=0;n<12;n++)
            {
                var g=Group("Salvaged weapon model "+n,Vector3.zero,gun);models.models[n]=g.gameObject;
                if(n==0){BuildPlayerCarbine(g);g.gameObject.SetActive(true);continue;}
                float barrelLength=n==2?.72f:n==1?.16f:n==5?.22f:n==8?.62f:.38f;
                var barrel=Part("Steel barrel",PrimitiveType.Cylinder,new Vector3(0,0,.72f+barrelLength*.5f),new Vector3(.11f,barrelLength*.5f,.11f),Stone,g);barrel.localRotation=Quaternion.Euler(90,0,0);
                Part("Salvaged receiver",PrimitiveType.Cube,new Vector3(0,0,.39f),new Vector3(n==6?.32f:.20f,.19f,n==1?.35f:.63f),Dark,g);
                Part("Wooden stock",PrimitiveType.Cube,new Vector3(0,0,-.09f),new Vector3(.14f,.17f,n==1?.18f:.35f),Bronze*.65f,g);
                Part("Magazine",PrimitiveType.Cube,new Vector3(0,-.18f,.40f),new Vector3(.14f,n==1?.35f:.25f,.14f),Stone,g).localRotation=Quaternion.Euler(-12,0,0);
                Part("Leather pistol grip",PrimitiveType.Cube,new Vector3(.1f,-.14f,.22f),new Vector3(.12f,.23f,.15f),Bronze*.5f,g);
                if(n==2){Part("Scope tube",PrimitiveType.Cylinder,new Vector3(0,.20f,.45f),new Vector3(.11f,.21f,.11f),Dark,g).localRotation=Quaternion.Euler(90,0,0);Part("Scope lens",PrimitiveType.Sphere,new Vector3(0,.20f,.67f),new Vector3(.085f,.085f,.015f),Jade,g,true);}
                if(n==3||n==4||n==5){Part("Pump foregrip",PrimitiveType.Cube,new Vector3(0,-.085f,.72f),new Vector3(.22f,.14f,.32f),Bronze,g);for(int i=0;i<6;i++)Part("Pump grooves",PrimitiveType.Cube,new Vector3(0,-.15f,.59f+i*.047f),new Vector3(.23f,.013f,.015f),Dark,g);}
                if(n==5)Part("Second shotgun barrel",PrimitiveType.Cylinder,new Vector3(.12f,0,.92f),new Vector3(.11f,.23f,.11f),Stone,g).localRotation=Quaternion.Euler(90,0,0);
                if(n==6){for(int i=0;i<7;i++)Part("Linked brass ammunition",PrimitiveType.Cylinder,new Vector3(-.22f-i*.03f,-i*.022f,.39f),new Vector3(.035f,.075f,.035f),Gold,g);Part("Drum belt box",PrimitiveType.Cube,new Vector3(-.21f,-.13f,.42f),new Vector3(.2f,.24f,.3f),Bronze,g);}
                if(n==7)for(int i=0;i<5;i++)Part("Needle loading rack",PrimitiveType.Cylinder,new Vector3(-.1f+i*.05f,.15f,.5f),new Vector3(.018f,.16f,.018f),Gold,g).localRotation=Quaternion.Euler(90,0,0);
                if(n>=8){for(int i=0;i<5;i++){Part("Copper induction coil",PrimitiveType.Cylinder,new Vector3(0,0,.65f+i*.11f),new Vector3(.25f,.023f,.25f),Bronze,g).localRotation=Quaternion.Euler(90,0,0);}for(int i=0;i<3;i++){Part("Vacuum glass tube",PrimitiveType.Capsule,new Vector3((i-1)*.13f,.19f,.41f),new Vector3(.07f,.12f,.07f),Jade,g,true);Part("Tube socket",PrimitiveType.Cylinder,new Vector3((i-1)*.13f,.11f,.41f),new Vector3(.085f,.025f,.085f),Bronze,g);}}
                if(n==9)for(int s=-1;s<=1;s+=2)Part("Split induction jaw",PrimitiveType.Cube,new Vector3(s*.17f,0,1.03f),new Vector3(.06f,.24f,.28f),Bronze,g);
                if(n==10)Part("Isotope drum",PrimitiveType.Cylinder,new Vector3(0,.06f,.64f),new Vector3(.39f,.15f,.39f),Stone,g);
                if(n==11)Part("Vacuum intake bell",PrimitiveType.Cylinder,new Vector3(0,0,1.07f),new Vector3(.40f,.10f,.40f),Bronze,g).localRotation=Quaternion.Euler(90,0,0);
                for(int i=0;i<4;i++)Part("Receiver bolt",PrimitiveType.Sphere,new Vector3(.105f,.055f,.2f+i*.11f),Vector3.one*.022f,Gold,g);
                RefineSalvagedWeapon(g,n,barrelLength);
                g.gameObject.SetActive(n==0);
            }
        }
        void RefineSalvagedWeapon(Transform g,int model,float barrelLength)
        {
            foreach(string name in new[]{"Salvaged receiver","Wooden stock","Pump foregrip"})
            {
                var part=g.Find(name);if(part==null)continue;
                var scale=part.localScale;part.localScale=new Vector3(scale.x,scale.z,scale.y);part.localRotation=Quaternion.Euler(90,0,0);
                part.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Forged contoured "+name,new[]{-.5f,-.41f,-.24f,.20f,.43f,.5f},new[]{.29f,.46f,.50f,.50f,.42f,.27f},new[]{.30f,.45f,.50f,.48f,.41f,.27f},12,false);TrackMesh(part,part.GetComponent<MeshFilter>().sharedMesh);
            }
            float muzzle=.72f+barrelLength;
            Part("Steel muzzle collar",PrimitiveType.Cylinder,new Vector3(0,0,muzzle-.035f),new Vector3(.145f,.04f,.145f),Stone,g).localRotation=Quaternion.Euler(90,0,0);
            Part("Black muzzle bore",PrimitiveType.Cylinder,new Vector3(0,0,muzzle+.007f),new Vector3(.066f,.008f,.066f),Dark,g).localRotation=Quaternion.Euler(90,0,0);
            Part("Front blade sight",PrimitiveType.Cube,new Vector3(0,.093f,muzzle-.12f),new Vector3(.025f,.068f,.035f),Stone,g);
            Part("Rear notch sight",PrimitiveType.Cube,new Vector3(0,.119f,.24f),new Vector3(.085f,.055f,.045f),Stone,g);
            Part("Leather stock shoulder pad",PrimitiveType.Cube,new Vector3(0,0,model==1?-.185f:-.285f),new Vector3(.16f,.20f,.05f),Dark,g);
            for(int s=-1;s<=1;s+=2)
            {
                Part("Receiver ejection recess",PrimitiveType.Cube,new Vector3(s*.107f,.024f,.45f),new Vector3(.01f,.065f,.15f),Dark,g);
                Part("Machined ejection lip",PrimitiveType.Cube,new Vector3(s*.115f,.060f,.45f),new Vector3(.016f,.009f,.17f),Stone,g);
                Part("Steel charging lever",PrimitiveType.Cylinder,new Vector3(s*.135f,.04f,.35f),new Vector3(.04f,.038f,.04f),Stone,g).localRotation=Quaternion.Euler(0,0,90);
                for(int j=0;j<4;j++)Part("Handguard vent recess",PrimitiveType.Cube,new Vector3(s*.10f,.01f,.65f+j*.046f),new Vector3(.009f,.054f,.022f),Dark,g);
                Filigree("Leather stock carved inlay",g,Bronze,.006f,new Vector3(s*.074f,.035f,-.2f),new Vector3(s*.079f,.046f,-.1f),new Vector3(s*.079f,.02f,.035f));
            }
            Filigree("Steel trigger guard",g,Stone,.012f,new Vector3(.075f,-.085f,.20f),new Vector3(.075f,-.20f,.25f),new Vector3(.075f,-.20f,.35f),new Vector3(.075f,-.08f,.35f));
            Part("Steel curved trigger",PrimitiveType.Cube,new Vector3(.075f,-.13f,.285f),new Vector3(.018f,.070f,.022f),Bronze,g).localRotation=Quaternion.Euler(-17,0,0);
            if(model>=8)
            {
                for(int s=-1;s<=1;s+=2)Filigree("Insulated power cable",g,Dark,.018f,new Vector3(s*.12f,.15f,.33f),new Vector3(s*.22f,.17f,.45f),new Vector3(s*.22f,.05f,.60f),new Vector3(s*.14f,.03f,.82f));
                Part("Vacuum tube copper cage",PrimitiveType.Cube,new Vector3(0,.32f,.41f),new Vector3(.37f,.022f,.10f),Bronze,g);
            }
        }
    }
    public sealed class WastelandWeaponModels:MonoBehaviour {public GameObject[] models=new GameObject[12];}
}
