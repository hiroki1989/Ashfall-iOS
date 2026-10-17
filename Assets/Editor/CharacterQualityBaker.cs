using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NeonVault;

public static class CharacterQualityBaker
{
    public static void Bake()
    {
        const string output="D:/Unity/AshfallArtValidation/QuarterStridePlayer/quality";
        Directory.CreateDirectory(output);var oldMain=Camera.main;if(oldMain!=null)oldMain.tag="Untagged";
        var cameraObject=new GameObject("Character quality comparison");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();
        camera.transform.rotation=Quaternion.Euler(35.264f,45,0);camera.orthographic=true;camera.orthographicSize=1.4f;camera.nearClipPlane=.01f;camera.farClipPlane=80;camera.backgroundColor=new Color(.08f,.09f,.1f);
        var rt=new RenderTexture(512,512,24);var frame=new Texture2D(512,512,TextureFormat.RGBA32,false);camera.targetTexture=rt;
        var scene=new GameObject("Existing NPC style reference");var materials=new List<Material>();var art=new VaultArt(scene.transform,materials,new System.Random(314));
        var sunlight=new GameObject("Reference daylight");var light=sunlight.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.95f,.84f);light.intensity=1.2f;sunlight.transform.rotation=Quaternion.Euler(48,-25,0);
        try
        {
            foreach(int kind in new[]{0,1,9})
            {
                var actor=art.Character(new Vector3(5000,.65f,5000),true,kind);actor.rotation=Quaternion.Euler(0,90,0);
                foreach(var motion in actor.GetComponentsInChildren<ActorMotion>())motion.enabled=false;
                var player=actor.GetComponent<LayeredPlayer2D>();
                if(player!=null){var gear=new ArmorItem[5];for(int slot=0;slot<5;slot++)gear[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=0};player.ApplyArmor(gear);player.SetWeaponModel(12,Color.white);player.SetReviewPose(3,LayeredPlayer2D.Motion.Idle,0,false);}
                camera.transform.position=actor.position+camera.transform.up*.70f-camera.transform.forward*20;camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,512,512),0,0);frame.Apply(false);RenderTexture.active=active;
                File.WriteAllBytes(Path.Combine(output,kind==0?"player-quarter.png":"npc-"+kind+"-quarter.png"),frame.EncodeToPNG());Object.DestroyImmediate(actor.gameObject);
            }
        }
        finally{camera.targetTexture=null;rt.Release();Object.DestroyImmediate(frame);Object.DestroyImmediate(rt);Object.DestroyImmediate(scene);Object.DestroyImmediate(sunlight);Object.DestroyImmediate(cameraObject);foreach(var material in materials)if(material!=null)Object.DestroyImmediate(material);if(oldMain!=null)oldMain.tag="MainCamera";}
    }
}
