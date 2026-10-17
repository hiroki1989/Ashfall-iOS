using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public sealed partial class VaultArt
    {
        // Adult anatomy and equipment are authored separately. Dimensions remain compatible with the two-bone rig.
        Transform SculptedPlayer(Vector3 position)
        {
            var actor=Group("Runner / sculpted wasteland survivor",position);
            var motion=actor.gameObject.AddComponent<ActorMotion>();
            var equipment=actor.gameObject.AddComponent<ActorEquipmentVisual>();
            var canvas=new Color(.40f,.43f,.31f);var leather=new Color(.32f,.22f,.13f);
            var steel=new Color(.43f,.46f,.43f);var rust=new Color(.42f,.26f,.13f);
            motion.body=Group("Torso rig",Vector3.zero,actor);
            motion.leftLeg=Group("Left leg",new Vector3(-.15f,.40f,0),actor);
            motion.rightLeg=Group("Right leg",new Vector3(.15f,.40f,0),actor);
            var torso=PlayerLoft("Player tailored jacket",motion.body,Vector3.zero,canvas,
                new[]{.40f,.47f,.55f,.68f,.80f,.95f,1.07f,1.16f,1.23f,1.27f},
                new[]{.22f,.245f,.225f,.225f,.24f,.282f,.296f,.28f,.205f,.115f},
                new[]{.135f,.15f,.137f,.143f,.165f,.19f,.18f,.15f,.11f,.08f},.006f,.76f);
            equipment.Register(ArmorSlot.Chest,torso);
            var pelvis=PlayerLoft("Player trouser fitted pelvis",motion.body,Vector3.zero,canvas,
                new[]{.29f,.34f,.41f,.47f,.51f},new[]{.10f,.18f,.235f,.237f,.19f},new[]{.081f,.12f,.14f,.136f,.10f},.004f,.80f);
            equipment.Register(ArmorSlot.Legs,pelvis);
            PlayerBelt(motion.body,canvas,leather,steel);
            PlayerJacketTailoring(motion.body,canvas,leather,steel);
            PlayerHead(motion.body,equipment,canvas,steel,rust);
            PlayerVest(motion.body,equipment,canvas,leather,steel,rust);
            for(int s=-1;s<=1;s+=2)
            {
                var leg=s<0?motion.leftLeg:motion.rightLeg;
                var thigh=PlayerLoft("Player trouser thigh",leg,new Vector3(0,-.16f,-.005f),canvas,
                    new[]{-.26f,-.20f,-.12f,-.01f,.10f,.19f,.25f},
                    new[]{.078f,.093f,.115f,.136f,.137f,.12f,.085f},
                    new[]{.084f,.105f,.122f,.139f,.138f,.115f,.085f},.010f,.82f);
                equipment.Register(ArmorSlot.Legs,thigh);
                var knee=Group("Articulated knee",new Vector3(0,-.40f,0),leg);
                if(s<0)motion.leftKnee=knee;else motion.rightKnee=knee;
                var shin=PlayerLoft("Player trouser lower leg",knee,new Vector3(0,-.22f,0),canvas,
                    new[]{-.26f,-.21f,-.14f,-.04f,.07f,.16f,.24f},
                    new[]{.066f,.080f,.085f,.092f,.10f,.094f,.082f},
                    new[]{.065f,.077f,.085f,.105f,.103f,.086f,.08f},.008f,.86f);
                equipment.Register(ArmorSlot.Legs,shin);
                PlayerStitch("Trouser outseam",leg,canvas*.75f,.0035f,
                    new Vector3(s*.122f,.01f,0),new Vector3(s*.135f,-.12f,.01f),new Vector3(s*.096f,-.36f,.01f));
                PlayerStitch("Trouser calf seam",knee,canvas*.75f,.0035f,
                    new Vector3(s*.09f,0,.01f),new Vector3(s*.098f,-.2f,.018f),new Vector3(s*.074f,-.4f,.01f));
                PlayerBoot(knee,equipment,leather,steel,rust);
                var armor=Group("Player knee and shin armor",Vector3.zero,knee);
                PlayerPlate("Knee dished steel plate",armor,new Vector3(0,0,.084f),steel,.172f,.203f,.052f);
                PlayerPlate("Shin shaped metal shield",armor,new Vector3(0,-.235f,.09f),steel,.138f,.235f,.026f);
                foreach(float y in new[]{.07f,-.07f,-.17f,-.32f})
                {
                    PlayerLoft("Leg leather fixing strap",armor,new Vector3(0,y,0),leather,
                        new[]{-.017f,.017f},new[]{.108f,.108f},new[]{.104f,.104f},0,.8f);
                    PlayerRivet(armor,new Vector3(.064f,y,.14f),steel,.014f);
                }
                for(int a=0;a<6;a++){float angle=a*Mathf.PI/3;PlayerRivet(armor,new Vector3(Mathf.Sin(angle)*.064f,Mathf.Cos(angle)*.074f,.119f),steel,.012f);}
                equipment.Register(ArmorSlot.Legs,armor,true);
                var shoulder=Group("Player fitted shoulder armor",new Vector3(s*.31f,1.16f,0),motion.body);
                var cap=PlayerLoft("Shoulder compound metal cup",shoulder,new Vector3(s*.025f,-.025f,0),steel,
                    new[]{-.11f,-.08f,-.035f,.01f,.045f,.065f},new[]{.11f,.138f,.145f,.13f,.095f,.028f},
                    new[]{.118f,.149f,.15f,.136f,.10f,.025f},0,.80f);
                cap.localRotation=Quaternion.Euler(0,0,-s*28);
                PlayerStitch("Shoulder rolled metal rim",shoulder,rust,.009f,new Vector3(-.10f,-.07f,.10f),new Vector3(0,-.10f,.148f),new Vector3(.10f,-.07f,.10f));
                for(int j=0;j<3;j++)PlayerRivet(shoulder,new Vector3(-.075f+j*.075f,-.025f,.147f),steel,.014f);
                equipment.Register(ArmorSlot.Arms,shoulder,true);
            }
            motion.cape=Group("Jacket rear hem",new Vector3(0,.47f,-.135f),motion.body);
            for(int s=-1;s<=1;s+=2)
            {
                var hem=PlayerPlate("Tailored canvas split hem",motion.cape,new Vector3(s*.11f,-.07f,0),canvas,.20f,.20f,.017f);
                hem.localRotation=Quaternion.Euler(10,180,s*5);
            }
            equipment.InitArms(this,motion.body,canvas);
            RefinePlayerArms(motion.body,equipment,canvas,leather,steel,rust);
            equipment.RememberVariants();return actor;
        }

        void PlayerHead(Transform body,ActorEquipmentVisual equipment,Color canvas,Color steel,Color rust)
        {
            // Seat the head into the raised collar; the skull dimensions and shoulder sockets stay unchanged.
            body=Group("Player fitted head rig",new Vector3(0,-.028f,0),body);
            PlayerLoft("Player neck",body,new Vector3(0,1.33f,0),new Color(.51f,.39f,.28f),
                new[]{-.075f,0,.065f},new[]{.065f,.062f,.07f},new[]{.063f,.06f,.065f},0,.95f);
            var skull=PlayerLoft("Player anatomic skull",body,Vector3.zero,new Color(.51f,.39f,.28f),
                new[]{1.398f,1.413f,1.447f,1.48f,1.526f,1.572f,1.618f,1.665f,1.698f,1.719f},
                new[]{.045f,.077f,.096f,.111f,.118f,.121f,.119f,.098f,.061f,.004f},
                new[]{.051f,.070f,.085f,.095f,.104f,.109f,.115f,.102f,.066f,.004f},0,.95f);
            equipment.Register(ArmorSlot.Head,skull);
            // The mask's cheeks, nose bridge and chin are one shaped shell, not a ball.
            var mask=PlayerLoft("Respirator shaped face seal",body,new Vector3(0,1.49f,.08f),Dark,
                new[]{-.088f,-.071f,-.025f,.025f,.060f,.105f},
                new[]{.050f,.077f,.115f,.12f,.083f,.030f},new[]{.050f,.073f,.09f,.087f,.065f,.025f},0,.76f);
            for(int s=-1;s<=1;s+=2)
            {
                var goggle=Group("Player goggle assembly",new Vector3(s*.061f,1.585f,.130f),body);
                goggle.localRotation=Quaternion.Euler(0,s*8,0);
                PlayerRing("Goggle steel eyepiece",goggle,Vector3.zero,steel,.058f,.049f,.010f);
                var lens=PlayerLoft("Player isotope glass lens",goggle,new Vector3(0,0,.004f),new Color(.18f,.25f,.14f),
                    new[]{0f,.003f,.008f,.012f},new[]{.047f,.047f,.031f,.002f},new[]{.047f,.047f,.031f,.002f},0,1f);
                lens.localRotation=Quaternion.Euler(90,0,0);lens.localScale=new Vector3(1,1,.86f);
                PlayerStitch("Player goggle glass reflection",goggle,new Color(.47f,.56f,.24f),.0018f,
                    new Vector3(-.034f,.017f,.011f),new Vector3(-.027f,.027f,.013f),new Vector3(-.015f,.032f,.014f),new Vector3(-.005f,.033f,.014f));
                PlayerRing("Goggle inset gasket",goggle,new Vector3(0,0,.003f),Dark,.049f,.045f,.003f);
                PlayerRivet(goggle,new Vector3(0,.055f,.006f),steel,.009f);
                var filter=Group("Side filter metal assembly",new Vector3(s*.109f,1.468f,.13f),body);
                filter.localRotation=Quaternion.Euler(0,s*27,0);
                PlayerDisc("Filter metal chamber",filter,Vector3.zero,steel,.050f,.044f,.033f);
                PlayerRing("Filter steel screw rim",filter,new Vector3(0,0,.035f),rust,.052f,.042f,.009f);
                PlayerDisc("Filter vent dark inset",filter,new Vector3(0,0,.037f),Dark,.042f,.040f,.002f);
                for(int j=-2;j<=2;j++)
                {
                    float x=j*.013f;float half=Mathf.Sqrt(.039f*.039f-x*x);
                    PlayerStitch("Filter metal grille",filter,steel,.0024f,new Vector3(x,-half,.044f),new Vector3(x,half,.044f));
                }
                for(int j=-2;j<=2;j++)
                {
                    float y=j*.013f;float half=Mathf.Sqrt(.039f*.039f-y*y);
                    PlayerStitch("Filter metal mesh crossbar",filter,steel*.8f,.002f,new Vector3(-half,y,.043f),new Vector3(half,y,.043f));
                }
                PlayerStitch("Respirator rubber head strap",body,Dark,.009f,new Vector3(s*.1f,1.57f,.1f),new Vector3(s*.132f,1.563f,-.005f),new Vector3(s*.099f,1.554f,-.097f),new Vector3(0,1.55f,-.122f));
            }
            PlayerStitch("Goggle metal bridge",body,steel,.006f,new Vector3(-.014f,1.591f,.148f),new Vector3(0,1.599f,.159f),new Vector3(.014f,1.591f,.148f));
            var muzzle=Group("Mask central metal grille",new Vector3(0,1.459f,.18f),body);
            PlayerRing("Mask central steel ring",muzzle,Vector3.zero,steel,.042f,.031f,.010f);
            PlayerDisc("Respirator exhale vent inset",muzzle,new Vector3(0,0,-.001f),Dark,.031f,.039f,.006f);
            muzzle.localScale=new Vector3(1,1.22f,1);
            for(int j=-2;j<=2;j++)PlayerStitch("Mask steel vertical grille",muzzle,steel,.0024f,new Vector3(j*.010f,-.031f,.008f),new Vector3(j*.010f,.031f,.008f));
            var helm=Group("Player salvage helmet equipment",Vector3.zero,body);
            PlayerLoft("Player dished helmet shell",helm,Vector3.zero,steel,
                new[]{1.591f,1.609f,1.635f,1.666f,1.695f,1.717f,1.73f},
                new[]{.132f,.135f,.129f,.115f,.088f,.051f,.003f},new[]{.131f,.138f,.135f,.121f,.090f,.05f,.003f},.0006f,.94f);
            var brim=PlayerLoft("Helmet flared metal brim",helm,new Vector3(0,1.606f,.006f),rust,
                new[]{-.007f,0f,.005f},new[]{.143f,.15f,.135f},new[]{.144f,.154f,.134f},0,.85f);
            for(int s=-1;s<=1;s+=2)
            {
                var ear=PlayerPlate("Helmet hinged ear steel",helm,new Vector3(s*.125f,1.568f,-.014f),steel,.12f,.15f,.010f);
                ear.localRotation=Quaternion.Euler(0,s*90,0);
                PlayerRivet(helm,new Vector3(s*.135f,1.60f,.072f),steel,.013f);
                for(int j=0;j<4;j++)
                {
                    float a=(j+1)*Mathf.PI/5;
                    PlayerRivet(helm,new Vector3(s*Mathf.Sin(a)*.139f,1.617f,Mathf.Cos(a)*.139f),steel,.009f);
                }
            }
            // Narrow riveted reinforcement follows the compound dome from brow to nape.
            var band=new List<Vector3>();
            for(int j=0;j<=20;j++){float a=j*Mathf.PI/20;band.Add(new Vector3(0,1.604f+Mathf.Sin(a)*.134f,Mathf.Cos(a)*.139f));}
            PlayerRibbon("Helmet crown steel reinforcement",helm,steel,band.ToArray(),.027f,.005f);
            foreach(int j in new[]{1,5,10,15,19})PlayerRivet(helm,band[j]+new Vector3(0,.003f,.003f),rust,.009f);
            equipment.Register(ArmorSlot.Head,helm,true);
        }

        void PlayerJacketTailoring(Transform body,Color canvas,Color leather,Color steel)
        {
            PlayerStitch("Jacket front zipper tape",body,Dark,.009f,new Vector3(0,.46f,.154f),new Vector3(0,.72f,.153f),new Vector3(0,.98f,.195f),new Vector3(0,1.15f,.161f));
            for(int i=0;i<24;i++)Part("Jacket zipper tooth",PrimitiveType.Cube,new Vector3(0,.52f+i*.024f,.164f+Mathf.Sin((i/23f)*Mathf.PI)*.027f),new Vector3(.016f,.005f,.007f),steel,body);
            for(int s=-1;s<=1;s+=2)
            {
                var collar=PlayerLoft("Jacket shaped collar",body,new Vector3(s*.092f,1.27f,-.01f),canvas,
                    new[]{-.058f,-.03f,.02f,.075f},new[]{.045f,.057f,.055f,.036f},new[]{.062f,.072f,.061f,.033f},.002f,.72f);
                collar.localRotation=Quaternion.Euler(-12,0,-s*16);
                var lapel=PlayerPlate("Jacket folded lapel",body,new Vector3(s*.095f,1.136f,.151f),canvas*.85f,.095f,.18f,.015f);
                lapel.localRotation=Quaternion.Euler(-15,s*20,-s*22);
                PlayerStitch("Jacket shoulder stitched seam",body,canvas*.68f,.0025f,new Vector3(s*.108f,1.236f,.072f),new Vector3(s*.223f,1.186f,.122f),new Vector3(s*.284f,1.12f,.074f));
                PlayerStitch("Jacket side tailored seam",body,canvas*.75f,.0027f,new Vector3(s*.274f,1.10f,.087f),new Vector3(s*.251f,.94f,.11f),new Vector3(s*.208f,.7f,.087f),new Vector3(s*.23f,.49f,.05f));
                var pocket=PlayerPlate("Canvas chest pocket",body,new Vector3(s*.135f,.979f,.175f),canvas,.15f,.17f,.013f);
                pocket.localRotation=Quaternion.Euler(0,s*18,0);
                var flap=PlayerPlate("Canvas chest pocket flap",body,new Vector3(s*.135f,1.041f,.184f),canvas*.75f,.155f,.052f,.010f);
                flap.localRotation=Quaternion.Euler(0,s*18,0);
                PlayerRivet(body,new Vector3(s*.135f,1.044f,.196f),steel,.009f);
                PlayerRibbon("Leather vest harness",body,leather,new[]{new Vector3(s*.18f,1.22f,.095f),new Vector3(s*.20f,1.07f,.18f),new Vector3(s*.205f,.86f,.194f),new Vector3(s*.19f,.51f,.147f)},.035f,.010f);
                PlayerBuckle(body,new Vector3(s*.205f,.86f,.213f),steel,.048f,.057f);
            }
            var pack=PlayerLoft("Leather fitted backpack",body,new Vector3(0,.89f,-.214f),leather,
                new[]{-.25f,-.20f,-.03f,.16f,.23f,.26f},new[]{.11f,.16f,.18f,.178f,.147f,.09f},new[]{.022f,.06f,.07f,.07f,.044f,.016f},.002f,.60f);
            PlayerPlate("Backpack leather top flap",body,new Vector3(0,1.052f,-.275f),leather*.8f,.30f,.13f,.02f).localRotation=Quaternion.Euler(0,180,0);
            for(int s=-1;s<=1;s+=2)PlayerRibbon("Backpack leather fixing strap",body,leather,new[]{new Vector3(s*.09f,1.14f,-.24f),new Vector3(s*.095f,.96f,-.287f),new Vector3(s*.093f,.7f,-.276f)},.027f,.009f);
        }

        void PlayerBelt(Transform body,Color canvas,Color leather,Color steel)
        {
            PlayerLoft("Belt",body,new Vector3(0,.47f,0),leather,new[]{-.029f,-.018f,.018f,.029f},new[]{.24f,.249f,.249f,.24f},new[]{.15f,.162f,.162f,.15f},0,.74f);
            PlayerBuckle(body,new Vector3(0,.47f,.172f),steel,.074f,.054f);
            for(int s=-1;s<=1;s+=2)
            {
                var pouch=Group("Belt utility leather pouch",new Vector3(s*.258f,.485f,.035f),body);
                pouch.localRotation=Quaternion.Euler(0,s*44,0);
                PlayerLoft("Leather belt pouch stitched body",pouch,Vector3.zero,leather,new[]{-.085f,-.073f,.025f,.075f,.086f},new[]{.046f,.063f,.065f,.063f,.041f},new[]{.04f,.052f,.055f,.048f,.024f},.002f,.55f);
                PlayerPlate("Leather belt pouch folded flap",pouch,new Vector3(0,.037f,.051f),leather,.125f,.09f,.010f);
                PlayerRivet(pouch,new Vector3(0,.015f,.066f),steel,.012f);
            }
        }

        void PlayerVest(Transform body,ActorEquipmentVisual equipment,Color canvas,Color leather,Color steel,Color rust)
        {
            var vest=Group("Player salvage armored carrier",Vector3.zero,body);
            PlayerLoft("Canvas vest fitted shell",vest,Vector3.zero,canvas,
                new[]{.54f,.65f,.8f,.96f,1.08f,1.16f,1.215f},
                new[]{.228f,.238f,.254f,.285f,.291f,.248f,.17f},new[]{.153f,.166f,.191f,.21f,.2f,.168f,.105f},.002f,.76f);
            PlayerPlate("Chest salvage curved steel panel",vest,new Vector3(0,.988f,.205f),steel,.445f,.265f,.028f);
            PlayerPlate("Chest upper reinforcement steel strip",vest,new Vector3(0,1.112f,.187f),rust,.411f,.045f,.009f);
            PlayerPlate("Chest lower reinforcement steel strip",vest,new Vector3(0,.876f,.213f),rust,.426f,.035f,.009f);
            for(int s=-1;s<=1;s+=2)
            {
                PlayerRibbon("Carrier leather shoulder strap",vest,leather,new[]{new Vector3(s*.223f,.7f,.217f),new Vector3(s*.231f,1.03f,.217f),new Vector3(s*.19f,1.203f,.128f),new Vector3(s*.19f,1.203f,-.08f),new Vector3(s*.22f,.91f,-.2f)},.047f,.012f);
                PlayerBuckle(vest,new Vector3(s*.226f,1.075f,.235f),steel,.06f,.084f);
                for(int j=0;j<3;j++)PlayerRivet(vest,new Vector3(s*(.17f-j*.035f),1.091f,.225f),steel,.011f);
                var pouch=Group("Carrier front canvas pouch",new Vector3(s*.15f,.69f,.203f),vest);
                PlayerLoft("Canvas armor pouch fitted body",pouch,Vector3.zero,canvas,
                    new[]{-.14f,-.124f,-.07f,.06f,.123f,.14f},new[]{.065f,.085f,.087f,.084f,.082f,.064f},
                    new[]{.039f,.057f,.061f,.059f,.055f,.032f},.0015f,.58f);
                PlayerPlate("Pouch scavenged metal flap",pouch,new Vector3(0,.066f,.058f),steel,.161f,.115f,.012f);
                PlayerStitch("Canvas armor pouch edge stitching",pouch,canvas*.65f,.003f,new Vector3(-.076f,.01f,.053f),new Vector3(-.074f,-.12f,.058f),new Vector3(.074f,-.12f,.058f),new Vector3(.076f,.01f,.053f));
                PlayerRivet(pouch,new Vector3(0,.065f,.079f),rust,.015f);
            }
            for(int s=-1;s<=1;s+=2)
            {
                var cartridge=PlayerLoft("Carrier metal cartridge case",vest,new Vector3(s*.032f,.69f,.226f),rust,new[]{-.12f,-.11f,.09f,.115f},new[]{.019f,.023f,.023f,.018f},new[]{.026f,.032f,.032f,.024f},0,.64f);
                PlayerBuckle(vest,new Vector3(s*.032f,.708f,.266f),steel,.04f,.053f);
            }
            equipment.Register(ArmorSlot.Chest,vest,true);
        }

        void PlayerBoot(Transform knee,ActorEquipmentVisual equipment,Color leather,Color steel,Color rust)
        {
            var boot=Group("Player anatomically shaped field boot",new Vector3(0,-.553f,.039f),knee);
            // Longitudinal heel, instep and toe sections form a real shoe silhouette.
            var shoe=PlayerLoft("Boot",boot,Vector3.zero,leather,
                new[]{-.145f,-.13f,-.095f,-.035f,.035f,.095f,.165f,.207f,.22f},
                new[]{.061f,.082f,.089f,.094f,.10f,.106f,.109f,.09f,.047f},
                new[]{.055f,.071f,.096f,.108f,.081f,.063f,.060f,.051f,.027f},.001f,.60f);
            shoe.localRotation=Quaternion.Euler(90,0,0);
            PlayerLoft("Boot leather ankle shaft",boot,new Vector3(0,.085f,-.046f),leather,
                new[]{-.03f,.025f,.085f,.15f,.166f},new[]{.09f,.086f,.079f,.075f,.068f},new[]{.094f,.085f,.076f,.075f,.066f},.003f,.72f);
            var sole=PlayerLoft("Boot rubber sole",boot,new Vector3(0,-.066f,0),Dark,
                new[]{-.158f,-.142f,-.09f,.10f,.19f,.227f,.238f},new[]{.07f,.091f,.105f,.116f,.115f,.077f,.02f},new[]{.025f,.028f,.027f,.025f,.021f,.017f,.005f},0,.59f);
            sole.localRotation=Quaternion.Euler(90,0,0);
            for(int s=-1;s<=1;s+=2)
            {
                PlayerStitch("Boot leather welt stitching",boot,rust,.003f,new Vector3(s*.081f,-.041f,-.139f),new Vector3(s*.105f,-.042f,.04f),new Vector3(s*.113f,-.047f,.175f),new Vector3(s*.076f,-.05f,.216f));
                for(int j=0;j<5;j++)
                {
                    float y=.01f+j*.025f,z=.121f-j*.019f;
                    PlayerRivet(boot,new Vector3(s*.044f,y,z),steel,.010f);
                    PlayerStitch("Boot crossed canvas lace",boot,new Color(.46f,.42f,.30f),.0036f,new Vector3(s*.041f,y,z+.006f),new Vector3(-s*.041f,y+.024f,z-.01f));
                }
                for(int j=0;j<7;j++)Part("Boot sole tread notch",PrimitiveType.Cube,new Vector3(s*.107f,-.078f,-.07f+j*.038f),new Vector3(.025f,.018f,.019f),Dark,boot);
            }
            equipment.Register(ArmorSlot.Feet,boot);
            var cap=Group("Player fitted toe and heel steel armor",Vector3.zero,boot);
            var toe=PlayerLoft("Boot shaped metal toe cap",cap,new Vector3(0,.006f,.135f),steel,
                new[]{-.043f,-.010f,.050f,.085f,.103f},new[]{.114f,.121f,.117f,.094f,.035f},new[]{.073f,.075f,.073f,.059f,.029f},0,.67f);
            toe.localRotation=Quaternion.Euler(90,0,0);
            PlayerPlate("Boot heel metal counter",cap,new Vector3(0,.005f,-.136f),rust,.16f,.115f,.019f).localRotation=Quaternion.Euler(0,180,0);
            for(int s=-1;s<=1;s+=2)PlayerRivet(cap,new Vector3(s*.093f,.023f,.125f),steel,.012f);
            equipment.Register(ArmorSlot.Feet,cap,true);
        }

        void RefinePlayerArms(Transform body,ActorEquipmentVisual equipment,Color canvas,Color leather,Color steel,Color rust)
        {
            var roots=new List<Transform>();foreach(var t in body.GetComponentsInChildren<Transform>(true))
                if(t.name=="Articulated upper arm"||t.name=="Articulated bracer"||t.name=="Grip locked gauntlet"||t.name=="Elbow joint")roots.Add(t);
            foreach(var limb in roots)
            {
                // InitArms registered these renderers. Retain inactive objects so ApplyArmor's renderer table remains valid.
                for(int i=limb.childCount-1;i>=0;i--)limb.GetChild(i).gameObject.SetActive(false);
                Mesh mesh;
                if(limb.name=="Articulated upper arm")
                {
                    mesh=PlayerLoftMesh("Player tailored upper sleeve",new[]{-1f,-.83f,-.57f,-.28f,.10f,.42f,.70f,.9f,1f},new[]{.32f,.42f,.51f,.54f,.56f,.52f,.47f,.37f,.23f},new[]{.33f,.44f,.51f,.50f,.53f,.48f,.43f,.32f,.22f},.027f,.86f);
                    PlayerStitch("Sleeve longitudinal stitched seam",limb,canvas*.7f,.017f,new Vector3(.39f,-.7f,0),new Vector3(.55f,-.1f,0),new Vector3(.47f,.65f,0));
                }
                else if(limb.name=="Articulated bracer")
                {
                    mesh=PlayerLoftMesh("Player shaped forearm sleeve",new[]{-1f,-.82f,-.5f,-.12f,.24f,.61f,.84f,1f},new[]{.29f,.34f,.4f,.51f,.55f,.53f,.4f,.28f},new[]{.30f,.36f,.42f,.49f,.5f,.48f,.41f,.26f},.023f,.86f);
                    limb.GetComponent<Renderer>().sharedMaterial=WastelandMaterial("Canvas forearm sleeve",canvas);
                    var armor=Group("Player wrapped bracer equipment",Vector3.zero,limb);
                    PlayerLoft("Bracer compound metal sleeve",armor,Vector3.zero,steel,new[]{-.85f,-.72f,-.4f,.03f,.4f,.73f,.85f},new[]{.35f,.41f,.48f,.56f,.59f,.55f,.44f},new[]{.35f,.40f,.46f,.55f,.58f,.53f,.43f},0,.78f);
                    for(int j=0;j<3;j++)
                    {
                        float y=-.58f+j*.59f;
                        PlayerLoft("Bracer leather binding strap",armor,new Vector3(0,y,0),leather,new[]{-.07f,.07f},new[]{j==0?.43f:j==1?.58f:.57f,j==0?.43f:j==1?.58f:.57f},new[]{j==0?.43f:j==1?.59f:.56f,j==0?.43f:j==1?.59f:.56f},0,.78f);
                        PlayerBuckle(armor,new Vector3(0,y,.58f),steel,.38f,.19f);
                    }
                    PlayerPlate("Bracer shaped steel dorsal plate",armor,new Vector3(0,.05f,.54f),steel,.79f,.77f,.09f);
                    for(int s=-1;s<=1;s+=2)
                    {
                        PlayerPlate("Bracer isotope protective metal rib",armor,new Vector3(s*.43f,.04f,.36f),rust,.12f,.64f,.08f);
                        // Small recessed isotope tubes sit beside the dorsal plate, protected by longitudinal steel ribs.
                        PlayerLoft("Player isotope glass bracer tube",armor,new Vector3(s*.45f,.04f,.505f),new Color(.44f,.57f,.17f),
                            new[]{-.29f,-.27f,0f,.27f,.29f},new[]{.030f,.050f,.050f,.050f,.030f},new[]{.030f,.050f,.050f,.050f,.030f},0,1f);
                        for(int capEnd=-1;capEnd<=1;capEnd+=2)
                            PlayerLoft("Bracer isotope metal socket",armor,new Vector3(s*.45f,.04f+capEnd*.29f,.505f),steel,
                                new[]{-.041f,.041f},new[]{.063f,.063f},new[]{.063f,.063f},0,.86f);
                        for(int rail=-1;rail<=1;rail+=2)
                            PlayerStitch("Bracer isotope protective steel rail",armor,steel,.015f,
                                new Vector3(s*.45f+rail*.066f,-.29f,.535f),new Vector3(s*.45f+rail*.066f,.37f,.535f));
                        for(int j=0;j<3;j++)PlayerRivet(armor,new Vector3(s*.26f,-.27f+j*.28f,.645f),steel,.07f);
                    }
                    equipment.Register(ArmorSlot.Arms,armor,true);
                }
                else if(limb.name=="Grip locked gauntlet")
                {
                    mesh=PlayerLoftMesh("Player fitted glove palm",new[]{-.48f,-.39f,-.14f,.17f,.36f,.46f},new[]{.29f,.40f,.47f,.43f,.38f,.22f},new[]{.24f,.32f,.38f,.38f,.3f,.20f},.006f,.62f);
                    limb.localScale=new Vector3(.16f,.14f,.2f);
                    for(int j=0;j<4;j++)
                    {
                        var finger=PlayerLoft("Gloved shaped finger",limb,new Vector3(-.30f+j*.20f,-.08f,.29f),Dark,new[]{-.24f,-.19f,0f,.13f,.19f},new[]{.057f,.081f,.085f,.079f,.04f},new[]{.05f,.08f,.091f,.078f,.039f},.002f,.8f);
                        finger.localRotation=Quaternion.Euler(62,0,0);
                        PlayerPlate("Glove knuckle leather panel",limb,new Vector3(-.30f+j*.20f,.19f,.36f),leather,.155f,.16f,.034f);
                    }
                    var thumb=PlayerLoft("Gloved curved thumb",limb,new Vector3(.42f,-.14f,.09f),Dark,new[]{-.22f,-.12f,.04f,.2f},new[]{.04f,.09f,.09f,.04f},new[]{.05f,.09f,.08f,.035f},0,.8f);thumb.localRotation=Quaternion.Euler(30,0,-38);
                    PlayerLoft("Glove leather wrist cuff",limb,new Vector3(0,-.39f,0),leather,new[]{-.11f,.03f,.06f},new[]{.27f,.33f,.29f},new[]{.27f,.34f,.3f},0,.72f);
                }
                else mesh=PlayerLoftMesh("Player bent elbow fabric",new[]{-.48f,-.28f,.04f,.32f,.48f},new[]{.19f,.39f,.48f,.35f,.17f},new[]{.19f,.38f,.44f,.35f,.17f},.012f,.90f);
                limb.GetComponent<MeshFilter>().sharedMesh=mesh;TrackMesh(limb,mesh);
            }
        }

        Transform PlayerLoft(string name,Transform parent,Vector3 position,Color color,float[] y,float[] w,float[] d,float wrinkle=0,float roundness=.9f)
        {return PlayerMeshPart(name,parent,position,color,PlayerLoftMesh(name,y,w,d,wrinkle,roundness));}

        // Cubic interpolated cross-sections, directional folds and flattened panels give an authored continuous contour.
        static Mesh PlayerLoftMesh(string name,float[] y,float[] w,float[] d,float wrinkle,float roundness)
        {
            const int around=48,subdiv=5;int rings=(y.Length-1)*subdiv+1;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            float Smooth(float[] a,int i,float t)
            {
                float p=a[Mathf.Max(0,i-1)],q=a[i],r=a[Mathf.Min(a.Length-1,i+1)],s=a[Mathf.Min(a.Length-1,i+2)];
                return .5f*((2*q)+(-p+r)*t+(2*p-5*q+4*r-s)*t*t+(-p+3*q-3*r+s)*t*t*t);
            }
            for(int ring=0;ring<rings;ring++)
            {
                int section=Mathf.Min(y.Length-2,ring/subdiv);float t=ring==rings-1?1:(ring%subdiv)/(float)subdiv;
                float h=Mathf.Lerp(y[section],y[section+1],t),width=Mathf.Max(.001f,Smooth(w,section,t)),depth=Mathf.Max(.001f,Smooth(d,section,t));
                float height01=(h-y[0])/(y[y.Length-1]-y[0]);
                for(int j=0;j<=around;j++)
                {
                    float a=j*Mathf.PI*2/around;float sin=Mathf.Sin(a),cos=Mathf.Cos(a);
                    float fold=wrinkle*(Mathf.Sin(height01*34f+sin*5f)*.55f+Mathf.Sin(height01*63f-cos*9f)*.28f+Mathf.Cos(a*7+height01*12)*.17f)*Mathf.Sin(height01*Mathf.PI);
                    float x=Mathf.Sign(sin)*Mathf.Pow(Mathf.Abs(sin),roundness)*(width+fold);
                    float z=Mathf.Sign(cos)*Mathf.Pow(Mathf.Abs(cos),roundness)*(depth+fold*.75f);
                    // The back and front are independently shaped instead of rotationally symmetric cylinders.
                    z+=wrinkle*.5f*Mathf.Sin(height01*19f)*Mathf.Cos(a*2);
                    v.Add(new Vector3(x,h,z));uv.Add(new Vector2(j/(float)around,height01));
                }
            }
            for(int r=0;r<rings-1;r++)for(int j=0;j<around;j++)
            {int a=r*(around+1)+j,b=a+around+1;tri.AddRange(new[]{a,b+1,b,a,a+1,b+1});}
            for(int end=0;end<2;end++)
            {
                int center=v.Count;float h=end==0?y[0]:y[y.Length-1];v.Add(new Vector3(0,h,0));uv.Add(new Vector2(.5f,.5f));
                int first=end==0?0:(rings-1)*(around+1);
                for(int j=0;j<around;j++){if(end==0)tri.AddRange(new[]{center,first+j+1,first+j});else tri.AddRange(new[]{center,first+j,first+j+1});}
            }
            return PlayerFinishMesh(name,v,uv,tri);
        }

        Transform PlayerPlate(string name,Transform parent,Vector3 position,Color color,float width,float height,float bulge)
        {
            const int nx=16,ny=20;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            float thickness=Mathf.Min(.009f,Mathf.Min(width,height)*.045f);
            for(int side=0;side<2;side++)for(int iy=0;iy<=ny;iy++)for(int ix=0;ix<=nx;ix++)
            {
                float u=ix/(float)nx,q=iy/(float)ny,x=(u-.5f)*2,h=(q-.5f)*2;
                float taper=1-.20f*Mathf.Pow(Mathf.Abs(h),5)-.10f*Mathf.Max(0,-h);
                float z=bulge*(1-x*x)*(.6f+.4f*(1-h*h));
                // Bevel the edge into a visible rolled, solid silhouette.
                float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(q,1-q));z-=thickness*Mathf.Clamp01((.10f-edge)/.10f);
                v.Add(new Vector3(x*width*.5f*taper,h*height*.5f,z-side*thickness));uv.Add(new Vector2(u,q));
            }
            int count=(nx+1)*(ny+1);
            for(int side=0;side<2;side++)for(int iy=0;iy<ny;iy++)for(int ix=0;ix<nx;ix++)
            {
                int a=side*count+iy*(nx+1)+ix,b=a+nx+1;
                if(side==0)tri.AddRange(new[]{a,b+1,b,a,a+1,b+1});else tri.AddRange(new[]{a,b,b+1,a,b+1,a+1});
            }
            void Edge(int a,int b){tri.AddRange(new[]{a,b,a+count,b,b+count,a+count});}
            for(int x=0;x<nx;x++){Edge(x+1,x);int a=ny*(nx+1)+x;Edge(a,a+1);}
            for(int y=0;y<ny;y++){Edge(y*(nx+1),(y+1)*(nx+1));Edge((y+1)*(nx+1)+nx,y*(nx+1)+nx);}
            return PlayerMeshPart(name,parent,position,color,PlayerFinishMesh(name,v,uv,tri));
        }

        Transform PlayerMeshPart(string name,Transform parent,Vector3 position,Color color,Mesh mesh)
        {
            var t=Group(name,position,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=WastelandMaterial(name,color);TrackMesh(t,mesh);return t;
        }
        static Mesh PlayerFinishMesh(string name,List<Vector3> v,List<Vector2> uv,List<int> tri)
        {
            var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();
            // Average duplicated seam normals without smoothing deliberate front/back thickness edges.
            var normals=m.normals;var sums=new Dictionary<Vector3,Vector3>();
            for(int i=0;i<v.Count;i++){sums.TryGetValue(v[i],out var n);sums[v[i]]=n+normals[i];}
            for(int i=0;i<v.Count;i++)normals[i]=sums[v[i]].normalized;
            m.normals=normals;m.RecalculateBounds();return m;
        }
        void PlayerStitch(string name,Transform parent,Color color,float radius,params Vector3[] path)
        {
            Filigree(name,parent,color,radius,path);
        }
        void PlayerRibbon(string name,Transform parent,Color color,Vector3[] path,float width,float thickness)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int p=0;p<path.Length;p++)
            {
                var forward=(path[Mathf.Min(p+1,path.Length-1)]-path[Mathf.Max(0,p-1)]).normalized;
                var sideways=Vector3.Cross(forward,Vector3.forward).normalized;
                if(sideways.sqrMagnitude<.2f)sideways=Vector3.right;
                if(Vector3.Dot(sideways,Vector3.right)<0)sideways=-sideways;
                for(int s=0;s<4;s++){v.Add(path[p]+sideways*((s%2==0?-1:1)*width*.5f)+Vector3.forward*((s<2?1:-1)*thickness*.5f));uv.Add(new Vector2(s%2,p/(float)(path.Length-1)));}
            }
            for(int p=0;p<path.Length-1;p++)for(int s=0;s<4;s++){int[] loop={0,1,3,2};int a=p*4+loop[s],b=p*4+loop[(s+1)%4];tri.AddRange(new[]{a,b,b+4,a,b+4,a+4});}
            PlayerMeshPart(name,parent,Vector3.zero,color,PlayerFinishMesh(name,v,uv,tri));
        }
        void PlayerRing(string name,Transform parent,Vector3 position,Color color,float outer,float inner,float depth)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();const int n=48,tube=8;
            for(int i=0;i<=n;i++)for(int j=0;j<=tube;j++)
            {float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/tube;float r=(outer+inner)*.5f+Mathf.Cos(b)*(outer-inner)*.5f;v.Add(new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,Mathf.Sin(b)*depth));uv.Add(new Vector2(i/(float)n,j/(float)tube));}
            for(int i=0;i<n;i++)for(int j=0;j<tube;j++){int a=i*(tube+1)+j,b=a+tube+1;tri.AddRange(new[]{a,b+1,b,a,a+1,b+1});}
            PlayerMeshPart(name,parent,position,color,PlayerFinishMesh(name,v,uv,tri));
        }
        void PlayerDisc(string name,Transform parent,Vector3 position,Color color,float width,float height,float length)
        {
            var t=PlayerLoft(name,parent,position,color,new[]{0f,length*.12f,length*.9f,length},new[]{width*.91f,width,width,width*.9f},new[]{height*.91f,height,height,height*.9f},0,1);
            t.localRotation=Quaternion.Euler(90,0,0);
        }
        void PlayerRivet(Transform parent,Vector3 position,Color color,float diameter)
        {PlayerDisc("Steel round head rivet",parent,position,color,diameter*.5f,diameter*.5f,diameter*.22f);}
        void PlayerBuckle(Transform parent,Vector3 position,Color color,float width,float height)
        {
            PlayerStitch("Steel rectangular strap buckle",parent,color,Mathf.Min(width,height)*.07f,
                position+new Vector3(-width*.5f,-height*.5f,0),position+new Vector3(-width*.5f,height*.5f,0),
                position+new Vector3(width*.5f,height*.5f,0),position+new Vector3(width*.5f,-height*.5f,0),position+new Vector3(-width*.5f,-height*.5f,0));
            PlayerStitch("Steel buckle tongue",parent,color,Mathf.Min(width,height)*.05f,position+new Vector3(0,-height*.5f,0),position+new Vector3(0,height*.36f,.004f));
        }
    }
}
