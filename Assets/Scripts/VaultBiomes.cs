using UnityEngine;
namespace NeonVault
{
    public sealed partial class VaultArt
    {
        // Silhouette detail stays above the walkable floor; these props add no collision.
        public void DungeonAtmosphere(int theme,SectorLayout layout)
        {
            RuinedDistrict(theme,layout);
            int index=0;
            foreach(var chamber in layout.chambers)
            {
                var p=layout.Center(chamber)+new Vector3(3,0,3);
                // Cell centres are positioned at actor height; landmarks sit on the floor.
                p.y=-.06f;
                var g=Group("Salvage landmark / "+theme,p);
                g.localRotation=Quaternion.Euler(0,(index++%4)*90,0);
                if(theme==1) PressurePlant(g);
                else if(theme==2) ContainmentPlant(g);
                else GeneratorPlant(g);
            }
        }
        // Faction dressing is non-colliding and uses the same safe landmark position as the existing plants.
        public void FactionHideout(DungeonFaction faction,SectorLayout layout)
        {
            if(faction==DungeonFaction.None)return;
            int i=0;
            foreach(var chamber in layout.chambers)
            {
                var p=layout.Center(chamber)+new Vector3(-3,0,3);p.y=-.06f;
                var g=Group(faction==DungeonFaction.Raiders?"Raider salvage barricade":"Gang territory marker",p);
                g.localRotation=Quaternion.Euler(0,(i++%4)*90,0);
                Color paint=faction==DungeonFaction.Raiders?new Color(.55f,.22f,.10f):new Color(.55f,.12f,.48f);
                Detail("Salvage footing",PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(1.35f,.2f,.45f),Dark,g);
                Detail("Salvage post",PrimitiveType.Cube,new Vector3(0,1.05f,0),new Vector3(.12f,1.9f,.12f),Bronze,g,new Vector3(0,0,8));
                Detail("Faction painted metal panel",PrimitiveType.Cube,new Vector3(0,1.25f,0),new Vector3(1.0f,.65f,.09f),paint,g,new Vector3(0,0,-7));
                Detail("Faction mark diagonal",PrimitiveType.Cube,new Vector3(0,1.25f,-.06f),new Vector3(.10f,.55f,.025f),Stone*1.7f,g,new Vector3(0,0,42));
                if(faction==DungeonFaction.Raiders)
                    Detail("Raider scrap blade",PrimitiveType.Cube,new Vector3(.48f,.55f,0),new Vector3(.14f,1.05f,.12f),Bronze,g,new Vector3(0,0,-23));
            }
        }
        void Detail(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color,Transform parent,Vector3 rotation=default)
        {Part(name,shape,position,scale,color,parent).localRotation=Quaternion.Euler(rotation);}
        void GeneratorPlant(Transform g)
        {
            Detail("Generator raised skid",PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(1.6f,.18f,1.1f),Dark,g);
            Detail("Generator engine casting",PrimitiveType.Cube,new Vector3(0,.59f,0),new Vector3(1.15f,.78f,.78f),Bronze*.65f,g);
            for(int i=0;i<7;i++)Detail("Individual cooling fin",PrimitiveType.Cube,new Vector3(0,.35f+i*.08f,-.43f),new Vector3(.89f,.025f,.1f),Dark,g);
            Detail("Cylindrical alternator",PrimitiveType.Cylinder,new Vector3(.73f,.57f,0),new Vector3(.64f,.23f,.64f),Stone,g,new Vector3(0,0,90));
            Detail("Exhaust riser",PrimitiveType.Cylinder,new Vector3(-.37f,1.15f,.19f),new Vector3(.12f,.32f,.12f),Dark,g);
            Detail("Exhaust cap",PrimitiveType.Cylinder,new Vector3(-.37f,1.49f,.19f),new Vector3(.20f,.026f,.20f),Bronze,g);
            Detail("Fuel reservoir",PrimitiveType.Cube,new Vector3(-.21f,1.03f,0),new Vector3(.74f,.14f,.59f),Stone,g);
            Detail("Fuel cap",PrimitiveType.Cylinder,new Vector3(-.31f,1.14f,.04f),new Vector3(.13f,.03f,.13f),Dark,g);
            for(int s=-1;s<=1;s+=2){Detail("Carry frame upright",PrimitiveType.Cylinder,new Vector3(s*.75f,.63f,.46f),new Vector3(.05f,.50f,.05f),Dark,g);Detail("Carry frame rail",PrimitiveType.Cylinder,new Vector3(s*.75f,1.13f,0),new Vector3(.05f,.46f,.05f),Dark,g,new Vector3(90,0,0));}
            for(int i=0;i<3;i++)Detail("Control dial",PrimitiveType.Cylinder,new Vector3(-.30f+i*.25f,.83f,-.455f),new Vector3(.10f,.012f,.10f),Stone*1.7f,g,new Vector3(90,0,0));
        }
        void PressurePlant(Transform g)
        {
            Detail("Tank concrete foundation",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(1.65f,.2f,1.2f),Stone,g);
            Detail("Pressure vessel",PrimitiveType.Cylinder,new Vector3(0,1.0f,0),new Vector3(.94f,.75f,.94f),Bronze*.8f,g);
            Detail("Dished vessel crown",PrimitiveType.Sphere,new Vector3(0,1.72f,0),new Vector3(.94f,.36f,.94f),Bronze*.8f,g);
            for(int i=0;i<3;i++)Detail("Pressure retaining band",PrimitiveType.Cylinder,new Vector3(0,.47f+i*.52f,0),new Vector3(1.01f,.035f,1.01f),Dark,g);
            Detail("Outlet elbow horizontal",PrimitiveType.Cylinder,new Vector3(.66f,1.27f,0),new Vector3(.16f,.29f,.16f),Dark,g,new Vector3(0,0,90));
            Detail("Outlet elbow vertical",PrimitiveType.Cylinder,new Vector3(.92f,.79f,0),new Vector3(.16f,.50f,.16f),Dark,g);
            Detail("Flange disc",PrimitiveType.Cylinder,new Vector3(.51f,1.27f,0),new Vector3(.29f,.04f,.29f),Stone,g,new Vector3(0,0,90));
            var valve=Group("Valve handwheel",new Vector3(.93f,1.1f,-.14f),g);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Detail("Handwheel rim",PrimitiveType.Cylinder,new Vector3(Mathf.Sin(a)*.17f,Mathf.Cos(a)*.17f,0),new Vector3(.035f,.075f,.035f),Bronze,valve,new Vector3(0,0,-i*45));}
            Detail("Valve spokes",PrimitiveType.Cube,Vector3.zero,new Vector3(.31f,.025f,.025f),Bronze,valve);
            Detail("Valve spindle",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.05f,.13f,.05f),Dark,valve,new Vector3(90,0,0));
            Detail("Pressure gauge casing",PrimitiveType.Cylinder,new Vector3(0,1.50f,-.50f),new Vector3(.26f,.04f,.26f),Dark,g,new Vector3(90,0,0));
            Detail("Pressure gauge face",PrimitiveType.Cylinder,new Vector3(0,1.50f,-.548f),new Vector3(.21f,.012f,.21f),Stone*2,g,new Vector3(90,0,0));
            Detail("Gauge needle",PrimitiveType.Cube,new Vector3(.025f,1.53f,-.565f),new Vector3(.015f,.10f,.01f),Dark,g,new Vector3(0,0,-35));
        }
        void ContainmentPlant(Transform g)
        {
            Detail("Reactor plinth",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(1.6f,.20f,1.6f),Stone,g);
            Detail("Shielded reactor vessel",PrimitiveType.Cylinder,new Vector3(0,.86f,0),new Vector3(.92f,.68f,.92f),Dark,g);
            for(int i=0;i<3;i++)
            {
                float y=.45f+i*.45f;
                Part("Isotope inspection slit",PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(.95f,.023f,.95f),Jade,g,true);
                Detail("Reactor shielding band",PrimitiveType.Cylinder,new Vector3(0,y+.085f,0),new Vector3(1.08f,.06f,1.08f),Bronze,g);
            }
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Detail("Reactor external brace",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.50f,.87f,Mathf.Cos(a)*.50f),new Vector3(.09f,1.35f,.09f),Stone,g,new Vector3(0,i*60,0));}
            Detail("Shielded reactor lid",PrimitiveType.Cylinder,new Vector3(0,1.59f,0),new Vector3(1.13f,.08f,1.13f),Bronze,g);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Detail("Lid retaining bolt",PrimitiveType.Cylinder,new Vector3(Mathf.Sin(a)*.43f,1.70f,Mathf.Cos(a)*.43f),new Vector3(.075f,.035f,.075f),Dark,g);}
            Detail("Reactor cable trunk",PrimitiveType.Cube,new Vector3(.81f,.24f,0),new Vector3(.69f,.16f,.26f),Dark,g);
            Detail("Control box",PrimitiveType.Cube,new Vector3(1.05f,.60f,0),new Vector3(.38f,.69f,.43f),Stone,g);
            for(int i=0;i<3;i++)Part("Control status lamp",PrimitiveType.Sphere,new Vector3(.95f+i*.10f,.77f,-.23f),Vector3.one*.05f,Jade,g,true);
        }
    }
}
