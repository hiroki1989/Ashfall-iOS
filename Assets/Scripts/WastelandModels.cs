using UnityEngine;
namespace NeonVault
{
    public sealed partial class VaultArt
    {
        Transform WastelandCharacter(Vector3 position,bool hero,int kind)
        {
            if(hero)return SculptedPlayer(position);
            var actor=Group(hero?"Runner / wasteland survivor":"Vault sentinel",position);
            var rig=actor.gameObject.AddComponent<ActorMotion>();
            Color cloth=hero?new Color(.36f,.40f,.31f):new Color(.30f,.28f,.22f);
            Color skin=hero?new Color(.65f,.49f,.34f):kind%3==0?new Color(.235f,.265f,.195f):kind%3==1?new Color(.29f,.245f,.205f):new Color(.26f,.275f,.26f);
            var equipment=actor.gameObject.AddComponent<ActorEquipmentVisual>();
            rig.body=Group("Torso rig",Vector3.zero,actor);
            rig.leftLeg=Group("Left leg",new Vector3(-.15f,.40f,0),actor);rig.rightLeg=Group("Right leg",new Vector3(.15f,.40f,0),actor);
            for(int s=-1;s<=1;s+=2)
            {
                var leg=s<0?rig.leftLeg:rig.rightLeg;
                var thigh=Sculpt("Trouser thigh",new Vector3(0,-.17f,0),leg,cloth,new[]{-.29f,-.20f,0,.12f,.22f},new[]{.095f,.12f,.145f,.15f,.10f},new[]{.10f,.12f,.14f,.13f,.09f});equipment.Register(ArmorSlot.Legs,thigh);
                var shin=Sculpt("Trouser shin",new Vector3(0,-.63f,.015f),leg,cloth*.8f,new[]{-.25f,-.18f,0,.16f,.22f},new[]{.07f,.10f,.11f,.10f,.08f},new[]{.07f,.09f,.10f,.10f,.08f});equipment.Register(ArmorSlot.Legs,shin);
                var boot=Part("Boot",PrimitiveType.Cube,new Vector3(0,-.95f,.075f),new Vector3(.23f,.20f,.37f),Dark,leg);boot.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Rounded field boot",new[]{-.5f,-.32f,.2f,.5f},new[]{.35f,.5f,.43f,.30f},new[]{.41f,.52f,.40f,.26f});TrackMesh(boot,boot.GetComponent<MeshFilter>().sharedMesh);equipment.Register(ArmorSlot.Feet,boot);
                Part("Boot sole",PrimitiveType.Cube,new Vector3(0,-1.04f,.07f),new Vector3(.25f,.045f,.39f),new Color(.13f,.12f,.10f),leg);
                var kneepad=Sculpt("Equipment kneepad",new Vector3(0,-.39f,.13f),leg,Bronze,new[]{-.025f,-.016f,.009f,.021f},new[]{.060f,.096f,.088f,.050f},new[]{.080f,.125f,.112f,.064f});kneepad.localRotation=Quaternion.Euler(90,0,0);equipment.Register(ArmorSlot.Legs,kneepad,true);
                equipment.Register(ArmorSlot.Feet,Part("Equipment toe cap",PrimitiveType.Cube,new Vector3(0,-.91f,.23f),new Vector3(.24f,.10f,.11f),Bronze,leg),true);
                var knee=Group("Articulated knee",new Vector3(0,-.40f,0),leg);if(s<0)rig.leftKnee=knee;else rig.rightKnee=knee;
                foreach(string partName in new[]{"Trouser shin","Boot","Boot sole","Equipment toe cap"}){var part=leg.Find(partName);if(part!=null){var local=part.localPosition;part.SetParent(knee,false);part.localPosition=local+Vector3.up*.4f;}}
                Filigree("Trouser stitched seam",leg,cloth*1.5f,.009f,new Vector3(s*.12f,.02f,0),new Vector3(s*.13f,-.18f,0),new Vector3(s*.10f,-.45f,0),new Vector3(s*.08f,-.82f,0));
                Part("Utility belt pouch",PrimitiveType.Cube,new Vector3(s*.29f,.48f,.08f),new Vector3(.16f,.22f,.16f),Bronze*.65f,rig.body);
                Part("Pouch flap",PrimitiveType.Cube,new Vector3(s*.29f,.55f,.17f),new Vector3(.17f,.055f,.04f),Bronze,rig.body);
                for(int r=0;r<3;r++)Part("Pouch rivet",PrimitiveType.Sphere,new Vector3(s*.29f,.53f-r*.035f,.195f),Vector3.one*.018f,Gold,rig.body);
                var shoulder=Sculpt("Equipment shoulder plate",new Vector3(s*.34f,1.155f,0),rig.body,Bronze,new[]{-.025f,-.012f,.018f,.038f},new[]{.085f,.133f,.122f,.081f},new[]{.10f,.155f,.140f,.083f});shoulder.localRotation=Quaternion.Euler(0,0,-s*18);equipment.Register(ArmorSlot.Arms,shoulder,true);
            }
            var torso=Sculpt("Field jacket",Vector3.zero,rig.body,cloth,new[]{.40f,.48f,.57f,.72f,.9f,1.08f,1.17f,1.24f},new[]{.25f,.28f,.24f,.26f,.31f,.33f,.28f,.155f},new[]{.145f,.18f,.145f,.177f,.205f,.193f,.155f,.10f});equipment.Register(ArmorSlot.Chest,torso);
            Part("Belt",PrimitiveType.Cube,new Vector3(0,.46f,0),new Vector3(.57f,.075f,.37f),Dark,rig.body);
            Part("Belt buckle",PrimitiveType.Cube,new Vector3(0,.46f,.195f),new Vector3(.10f,.07f,.025f),Bronze,rig.body);
            equipment.Register(ArmorSlot.Chest,Part("Equipment salvage vest",PrimitiveType.Cube,new Vector3(0,.87f,.19f),new Vector3(.48f,.48f,.10f),Bronze,rig.body),true);
            for(int s=-1;s<=1;s+=2)
            {
                Filigree("Jacket shoulder stitching",rig.body,cloth*1.4f,.01f,new Vector3(s*.10f,1.18f,.11f),new Vector3(s*.27f,1.12f,.17f),new Vector3(s*.27f,.65f,.16f));
                Part("Chest pocket",PrimitiveType.Cube,new Vector3(s*.15f,.98f,.20f),new Vector3(.18f,.19f,.045f),cloth*.75f,rig.body);
                Part("Pocket button",PrimitiveType.Sphere,new Vector3(s*.15f,1.02f,.23f),Vector3.one*.025f,Bronze,rig.body);
            }
            Part("Neck",PrimitiveType.Cylinder,new Vector3(0,1.32f,0),new Vector3(.17f,.09f,.17f),skin,rig.body);
            var head=Sculpt("Helmet skull",new Vector3(0,1.54f,0),rig.body,skin,new[]{-.18f,-.14f,-.05f,.10f,.18f,.20f},new[]{.075f,.125f,.15f,.15f,.09f,.005f},new[]{.07f,.115f,.14f,.14f,.08f,.005f});equipment.Register(ArmorSlot.Head,head);
            if(hero)
            {
                Part("Respirator face seal",PrimitiveType.Sphere,new Vector3(0,1.48f,.13f),new Vector3(.27f,.23f,.13f),Dark,rig.body);
                for(int s=-1;s<=1;s+=2)
                {
                    Part("Visor goggle rim",PrimitiveType.Sphere,new Vector3(s*.075f,1.58f,.132f),new Vector3(.13f,.105f,.05f),Bronze,rig.body);
                    Part("Visor glass",PrimitiveType.Sphere,new Vector3(s*.075f,1.58f,.16f),new Vector3(.095f,.073f,.025f),new Color(.065f,.11f,.105f),rig.body);
                    var filter=Part("Respirator filter",PrimitiveType.Cylinder,new Vector3(s*.13f,1.45f,.14f),new Vector3(.11f,.065f,.11f),Bronze,rig.body);filter.localRotation=Quaternion.Euler(70,0,s*35);
                    for(int j=0;j<3;j++)Part("Filter vents",PrimitiveType.Cube,new Vector3(s*.13f,1.43f+j*.02f,.205f),new Vector3(.067f,.006f,.012f),Dark,rig.body);
                }
                equipment.Register(ArmorSlot.Head,Part("Equipment salvage helmet",PrimitiveType.Sphere,new Vector3(0,1.68f,-.015f),new Vector3(.34f,.18f,.34f),cloth,rig.body),true);
            }
            else
            {
                for(int s=-1;s<=1;s+=2){Part("Clouded infected eye",PrimitiveType.Sphere,new Vector3(s*.066f,1.59f,.13f),new Vector3(.028f,.019f,.017f),new Color(.20f,.30f,.13f),rig.body,true);Part("Zombie cheek bone",PrimitiveType.Sphere,new Vector3(s*.09f,1.51f,.09f),new Vector3(.075f,.045f,.055f),skin*.7f,rig.body);}
                Part("Infected mouth",PrimitiveType.Cube,new Vector3(0,1.46f,.12f),new Vector3(.14f,.04f,.028f),Dark,rig.body);
                for(int n=0;n<4;n++)Part("Exposed tooth",PrimitiveType.Cube,new Vector3(-.05f+n*.033f,1.465f,.14f),new Vector3(.017f,.025f,.012f),new Color(.68f,.63f,.46f),rig.body);
                if(kind%3==1&&kind!=7)for(int j=0;j<4;j++)Part("Swollen irradiated scar",PrimitiveType.Sphere,new Vector3((j%2==0?-1:1)*.25f,.78f+j*.07f,.18f),Vector3.one*(.035f+j*.006f),skin*.7f,rig.body);
                if(kind==2||kind==8){for(int q=0;q<3;q++)Part("Infected ballistic plate",PrimitiveType.Cube,new Vector3(0,.72f+q*.16f,.2f),new Vector3(.46f,.13f,.08f),Dark,rig.body);Part("Soldier helmet",PrimitiveType.Sphere,new Vector3(0,1.70f,-.02f),new Vector3(.36f,.20f,.34f),cloth,rig.body);}
                if(kind==5){Part("Infected bandolier",PrimitiveType.Cube,new Vector3(0,.98f,.22f),new Vector3(.50f,.08f,.05f),Bronze,rig.body).localRotation=Quaternion.Euler(0,0,35);}
                if(kind==6){for(int j=0;j<4;j++)Sculpt("Swollen isotope growth",new Vector3((j%2==0?-1:1)*.3f,1.03f+j*.06f,0),rig.body,skin,new[]{-.07f,-.02f,.075f,.13f},new[]{.03f,.085f,.06f,.003f},new[]{.025f,.075f,.045f,.003f});}
                if(kind==3)actor.localScale=Vector3.one*1.6f;
            }
            rig.cape=Group("Jacket rear hem",new Vector3(0,.5f,-.16f),rig.body);Part("Split coat hem",PrimitiveType.Cube,new Vector3(0,-.13f,0),new Vector3(.49f,.26f,.04f),cloth,rig.cape);
            TailorWastelandCharacter(rig,equipment,hero,kind,cloth,skin);
            Transform armRoot=hero?rig.body:RefineInfectedSilhouette(rig,kind,cloth,skin);
            equipment.InitArms(this,armRoot,cloth);equipment.RememberVariants();return actor;
        }
        Transform RefineInfectedSilhouette(ActorMotion rig,int kind,Color cloth,Color skin)
        {
            var body=rig.body;
            if(kind==3)
            {
                for(int s=-1;s<=1;s+=2)
                {
                    var shoulder=Sculpt("Mutant swollen deltoid",new Vector3(s*.33f,1.13f,-.03f),body,skin,new[]{-.23f,-.14f,.02f,.17f,.25f},new[]{.09f,.20f,.25f,.22f,.055f},new[]{.075f,.14f,.185f,.155f,.055f});shoulder.localRotation=Quaternion.Euler(0,0,-s*24);
                    var ridge=Sculpt("Swollen shoulder bone ridge",new Vector3(s*.36f,1.27f,-.055f),body,new Color(.36f,.32f,.25f),new[]{-.08f,-.02f,.06f,.14f},new[]{.05f,.09f,.06f,.003f},new[]{.065f,.11f,.075f,.003f});ridge.localRotation=Quaternion.Euler(-18,0,-s*30);
                    Filigree("Swollen shoulder fissure",body,skin*.35f,.013f,new Vector3(s*.29f,1.33f,.095f),new Vector3(s*.44f,1.21f,.145f),new Vector3(s*.48f,1.08f,.1f));
                }
                Sculpt("Mutant swollen upper back",new Vector3(0,1.08f,-.16f),body,skin*.78f,new[]{-.22f,-.10f,.12f,.26f,.35f},new[]{.24f,.36f,.38f,.26f,.04f},new[]{.10f,.21f,.24f,.19f,.03f});
            }
            if(kind==7||kind==9)
            {
                // Remove the intact cylinder of clothing: the exposed flesh must be the actual silhouette.
                foreach(var child in body.GetComponentsInChildren<Renderer>(true))
                {
                    string n=child.name;
                    if(n=="Field jacket"||n=="Chest pocket"||n=="Pocket button"||n.StartsWith("Jacket zipper")||n.Contains("compression fold")||n=="Equipment salvage vest"||n=="Leather harness raised piping"||n.StartsWith("Harness")||n=="Belt"||n=="Belt buckle")child.enabled=false;
                }
                float girth=kind==7?1f:.82f;
                var abdomen=Sculpt("Bloated infected abdomen",new Vector3(0,.78f,.12f),body,skin,new[]{-.36f,-.26f,-.10f,.10f,.28f,.34f},new[]{.15f,.32f,.40f,.38f,.26f,.15f},new[]{.10f,.24f,.32f,.29f,.16f,.09f});abdomen.localScale=new Vector3(girth,1,girth);
                for(int s=-1;s<=1;s+=2)
                {
                    Sculpt("Torn jacket side panel",new Vector3(s*.29f,.86f,-.04f),body,cloth,new[]{-.3f,-.16f,.02f,.24f,.35f},new[]{.035f,.06f,.075f,.065f,.025f},new[]{.14f,.17f,.14f,.1f,.06f});
                    Filigree("Jacket ragged split edge",body,cloth*.6f,.022f,new Vector3(s*.25f,1.13f,.12f),new Vector3(s*.31f,1.02f,.19f),new Vector3(s*.34f,.84f,.26f),new Vector3(s*.28f,.55f,.17f));
                    Filigree("Swollen abdomen wound",body,new Color(.12f,.065f,.05f),.012f,new Vector3(s*.08f,1.02f,.28f),new Vector3(s*.15f,.89f,.42f),new Vector3(s*.10f,.74f,.43f),new Vector3(s*.16f,.63f,.33f));
                    for(int j=0;j<2;j++)Part("Swollen necrotic blister",PrimitiveType.Sphere,new Vector3(s*(.14f+j*.045f),.75f+j*.13f,.39f),new Vector3(.065f,.08f,.033f),skin*.6f,body);
                }
                if(kind==9)Sculpt("Swollen carrier spinal ridge",new Vector3(0,1.12f,-.15f),body,skin*.75f,new[]{-.23f,-.1f,.12f,.31f},new[]{.09f,.16f,.12f,.015f},new[]{.07f,.20f,.22f,.035f});
            }
            // A permanent pose below the animated torso avoids the walk cycle resetting the hunched anatomy.
            if(kind==3||kind==7||kind==9)
            {
                var children=new Transform[body.childCount];for(int i=0;i<children.Length;i++)children[i]=body.GetChild(i);
                var posture=Group("Infected permanent hunched posture",Vector3.zero,body);foreach(var child in children)child.SetParent(posture,false);
                posture.localRotation=Quaternion.Euler(kind==9?18:kind==3?11:7,0,kind==9?-4:0);
                if(kind==9)posture.localScale=new Vector3(.90f,1.08f,1);
                return posture;
            }
            return body;
        }
        // Clothing is a fitted shell over the anatomy, with separate seams, lapels and load-bearing straps.
        void TailorWastelandCharacter(ActorMotion rig,ActorEquipmentVisual equipment,bool hero,int kind,Color cloth,Color skin)
        {
            var body=rig.body;
            var vest=body.Find("Equipment salvage vest");
            vest.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Anatomic salvage carrier",new[]{-.5f,-.39f,-.15f,.22f,.44f,.5f},new[]{.31f,.46f,.50f,.48f,.35f,.21f},new[]{.20f,.43f,.50f,.45f,.29f,.15f},16);
            TrackMesh(vest,vest.GetComponent<MeshFilter>().sharedMesh);
            // Unarmored chest remains readable; jacket closure and diagonal harness avoid a featureless oval.
            Filigree("Jacket zipper tape",body,Dark,.018f,new Vector3(0,.5f,.18f),new Vector3(0,.76f,.206f),new Vector3(0,1.06f,.203f));
            for(int j=0;j<12;j++)Part("Jacket zipper tooth",PrimitiveType.Cube,new Vector3(0,.55f+j*.04f,.225f),new Vector3(.027f,.007f,.008f),Bronze,body);
            for(int s=-1;s<=1;s+=2)
            {
                var collar=Sculpt("Raised jacket collar",new Vector3(s*.12f,1.235f,.015f),body,cloth,new[]{-.09f,-.02f,.07f,.11f},new[]{.065f,.085f,.076f,.04f},new[]{.10f,.11f,.07f,.02f});collar.localRotation=Quaternion.Euler(-12,0,-s*22);
                var lapel=Part("Folded jacket lapel",PrimitiveType.Cube,new Vector3(s*.085f,1.10f,.19f),new Vector3(.09f,.22f,.035f),cloth*1.2f,body);lapel.localRotation=Quaternion.Euler(-12,s*12,-s*22);
                Filigree("Leather harness raised piping",body,Bronze*.65f,.027f,new Vector3(s*.23f,1.22f,.12f),new Vector3(s*.21f,1.01f,.23f),new Vector3(s*.20f,.73f,.22f),new Vector3(s*.21f,.48f,.19f));
                Part("Harness adjustment buckle",PrimitiveType.Cube,new Vector3(s*.21f,.84f,.25f),new Vector3(.077f,.055f,.022f),Bronze,body);
                Part("Harness buckle inset",PrimitiveType.Cube,new Vector3(s*.21f,.84f,.265f),new Vector3(.044f,.028f,.012f),Dark,body);
                for(int i=0;i<3;i++)
                {
                    Filigree("Jacket compression fold",body,cloth*.85f,.012f,new Vector3(s*.055f,.58f+i*.09f,.195f),new Vector3(s*.16f,.61f+i*.09f,.195f),new Vector3(s*.24f,.58f+i*.09f,.175f));
                    var leg=s<0?rig.leftLeg:rig.rightLeg;
                    Filigree("Trouser knee fold",leg,cloth*.7f,.009f,new Vector3(-.075f,-.29f-i*.045f,.09f),new Vector3(0,-.31f-i*.045f,.127f),new Vector3(.07f,-.29f-i*.045f,.095f));
                }
                var sideFold=Sculpt("Jacket side compression fold",new Vector3(s*.263f,.68f,.025f),body,cloth*.85f,new[]{-.14f,-.055f,.015f,.075f,.14f},new[]{.018f,.035f,.047f,.025f,.008f},new[]{.105f,.13f,.137f,.117f,.055f});sideFold.localRotation=Quaternion.Euler(0,0,s*12);
                var boot=(s<0?rig.leftKnee:rig.rightKnee).Find("Boot");
                for(int j=0;j<4;j++)Part("Boot crossed lace",PrimitiveType.Cube,new Vector3(0,.17f+j*.11f,.35f-j*.04f),new Vector3(.63f,.035f,.025f),Bronze*.8f,boot).localRotation=Quaternion.Euler(0,0,(j%2==0?1:-1)*15);
                Part("Boot leather ankle welt",PrimitiveType.Cylinder,new Vector3(0,.25f,0),new Vector3(.85f,.055f,.79f),Dark,boot);
                var shoulder=body.Find("Equipment shoulder plate");
                // Salvage plates have a lip and rivets, rather than a spherical blob.
                var plate=Part("Shoulder rolled edge",PrimitiveType.Cube,new Vector3(s*.345f,1.13f,.15f),new Vector3(.25f,.035f,.035f),Bronze,body);equipment.Register(ArmorSlot.Arms,plate,true);
                for(int j=0;j<3;j++){var rivet=Part("Shoulder steel rivet",PrimitiveType.Sphere,new Vector3(s*(.27f+j*.055f),1.19f,.14f),Vector3.one*.022f,Gold,body);equipment.Register(ArmorSlot.Arms,rivet,true);}
            }
            var pack=Sculpt("Leather field backpack",new Vector3(0,.89f,-.225f),body,Bronze,new[]{-.29f,-.23f,.19f,.28f},new[]{.16f,.225f,.205f,.13f},new[]{.055f,.12f,.11f,.045f});
            Part("Backpack flap",PrimitiveType.Cube,new Vector3(0,1.06f,-.33f),new Vector3(.38f,.15f,.055f),Bronze*.7f,body);
            for(int s=-1;s<=1;s+=2)Filigree("Backpack leather strap",body,Dark,.025f,new Vector3(s*.13f,1.14f,-.32f),new Vector3(s*.13f,.94f,-.36f),new Vector3(s*.13f,.68f,-.33f));
            Part("Bedroll jacket canvas",PrimitiveType.Cylinder,new Vector3(0,.61f,-.31f),new Vector3(.18f,.25f,.18f),cloth,body).localRotation=Quaternion.Euler(0,0,90);
            var hem=rig.cape.Find("Split coat hem");hem.gameObject.SetActive(false);
            for(int s=-1;s<=1;s+=2){var tail=Sculpt("Jacket split tailored hem",new Vector3(s*.13f,-.09f,0),rig.cape,cloth,new[]{-.20f,-.11f,.04f,.1f},new[]{.085f,.12f,.13f,.12f},new[]{.022f,.035f,.032f,.026f});tail.localRotation=Quaternion.Euler(-5,0,s*7);}
            if(hero)
            {
                Filigree("Respirator rubber head strap",body,Dark,.018f,new Vector3(-.13f,1.57f,.10f),new Vector3(-.158f,1.56f,-.07f),new Vector3(0,1.55f,-.145f),new Vector3(.158f,1.56f,-.07f),new Vector3(.13f,1.57f,.1f));
                Part("Respirator nose bridge",PrimitiveType.Sphere,new Vector3(0,1.52f,.20f),new Vector3(.075f,.115f,.065f),Dark,body);
                Part("Respirator exhale housing",PrimitiveType.Cylinder,new Vector3(0,1.45f,.22f),new Vector3(.095f,.022f,.095f),Bronze,body).localRotation=Quaternion.Euler(90,0,0);
                for(int j=0;j<4;j++)Part("Respirator exhale vent",PrimitiveType.Cube,new Vector3(0,1.425f+j*.016f,.248f),new Vector3(.055f,.005f,.007f),Dark,body);
                Part("Goggle bridge",PrimitiveType.Cube,new Vector3(0,1.58f,.17f),new Vector3(.039f,.021f,.03f),Bronze,body);
                for(int s=-1;s<=1;s+=2)
                {
                    var glint=Part("Goggle glass reflection",PrimitiveType.Sphere,new Vector3(s*.075f-.019f,1.598f,.173f),new Vector3(.011f,.020f,.005f),new Color(.29f,.43f,.40f),body);glint.GetComponent<Renderer>().sharedMaterial=WastelandMaterial("Neck optical reflection",new Color(.29f,.43f,.40f));
                    var ear=Sculpt("Helmet ear protector",new Vector3(s*.148f,1.60f,-.025f),body,cloth,new[]{-.1f,-.07f,.06f,.09f},new[]{.018f,.032f,.032f,.01f},new[]{.025f,.065f,.057f,.015f});equipment.Register(ArmorSlot.Head,ear,true);
                    Filigree("Helmet rolled rim",body,Bronze,.011f,new Vector3(s*.16f,1.665f,-.06f),new Vector3(s*.145f,1.665f,.075f),new Vector3(s*.07f,1.665f,.15f),new Vector3(0,1.665f,.16f));
                }
            }
            else
            {
                Part("Infected nose cartilage",PrimitiveType.Sphere,new Vector3(0,1.54f,.145f),new Vector3(.047f,.07f,.061f),skin*.8f,body);
                Part("Infected chin",PrimitiveType.Sphere,new Vector3(0,1.415f,.092f),new Vector3(.13f,.07f,.09f),skin*.65f,body);
                for(int s=-1;s<=1;s+=2){Part("Infected torn ear",PrimitiveType.Sphere,new Vector3(s*.15f,1.555f,0),new Vector3(.045f,.085f,.045f),skin*.8f,body);Filigree("Infected brow ridge",body,skin*.6f,.014f,new Vector3(s*.02f,1.626f,.12f),new Vector3(s*.065f,1.622f,.145f),new Vector3(s*.108f,1.60f,.115f));}
                Filigree("Infected torn cheek scar",body,new Color(.24f,.12f,.08f),.011f,new Vector3(-.10f,1.55f,.122f),new Vector3(-.07f,1.52f,.145f),new Vector3(-.11f,1.48f,.13f));
            }
        }
    }
}

