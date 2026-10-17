using System;
using System.IO;
using UnityEngine;
using NeonVault;

public static class EnemyVisualBaker
{
    public static void Bake()
    {
        var args=Environment.GetCommandLineArgs();int outputFlag=Array.IndexOf(args,"--enemy-output"),kindFlag=Array.IndexOf(args,"--enemy-kind");
        string output=outputFlag>=0&&outputFlag+1<args.Length?args[outputFlag+1]:"D:/Unity/AshfallArtValidation/Enemy2DReview";
        int selectedKind=kindFlag>=0&&kindFlag+1<args.Length?int.Parse(args[kindFlag+1]):-1;
        Directory.CreateDirectory(output);
        var oldMain=Camera.main;if(oldMain!=null)oldMain.tag="Untagged";
        var cameraObject=new GameObject("Enemy review camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();
        camera.transform.rotation=Quaternion.Euler(35.264f,45,0);camera.orthographic=true;camera.orthographicSize=2.3f;camera.nearClipPlane=.01f;camera.farClipPlane=80;
        camera.backgroundColor=new Color(.08f,.09f,.11f);camera.clearFlags=CameraClearFlags.SolidColor;
        const int size=256;var rt=new RenderTexture(size,size,24);var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);camera.targetTexture=rt;
        int checks=0;
        try
        {
            for(int kind=0;kind<10;kind++)
            {
                if(selectedKind>=0&&selectedKind!=kind)continue;
                var actor=new GameObject("Enemy review / "+kind);actor.transform.position=new Vector3(4000,.65f,4000);var foe=actor.AddComponent<Foe>();foe.kind=kind;foe.enabled=false;
                var visual=actor.AddComponent<EnemyPainted2D>();visual.Init(foe);if(!visual.Ready)throw new Exception("Missing enemy painted views / "+kind);
                foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled)throw new Exception("Visible enemy mesh / "+kind);checks++;
                float[] heights={2.05f,2.02f,2.16f,3.5f,1.9f,2.22f,2.08f,2.5f,2.26f,2.65f};
                camera.orthographicSize=heights[kind]*.62f;
                camera.transform.position=actor.transform.position+camera.transform.up*(heights[kind]*.5f-.53f)-camera.transform.forward*30;
                try
                {
                    for(int direction=0;direction<8;direction++)
                    {
                        visual.SetReviewPose(direction,EnemyPainted2D.Motion.Idle,0);
                        int source=direction<=4?direction:8-direction;bool mirrored=direction>4;
                        if(visual.LeftLegSourceCell/3!=source||visual.RightLegSourceCell/3!=source||visual.LeftLegSourceCell==visual.RightLegSourceCell)throw new Exception("Enemy anatomical sources lack a distinct directional pair / "+kind+" / "+direction);
                        if(direction==4&&(visual.BodyRenderers[1].sprite.texture.name!="BackLegsV2"||visual.BodyRenderers[2].sprite.texture.name!="BackLegsV2"))throw new Exception("Enemy rear view must use actual rear-leg artwork / "+kind);
                        float separation=Vector3.Dot(visual.BodyRenderers[1].transform.position-visual.BodyRenderers[2].transform.position,camera.transform.right);
                        float expected=Mathf.Cos(direction*Mathf.PI/4);if(Mathf.Abs(expected)>.1f&&separation*expected<=0)throw new Exception("Enemy left/right hips reversed / "+kind+" / "+direction);checks++;
                        foreach(EnemyPainted2D.Motion motion in Enum.GetValues(typeof(EnemyPainted2D.Motion)))
                        {
                            int count=motion==EnemyPainted2D.Motion.Walk?64:motion==EnemyPainted2D.Motion.Hit?16:32;
                            string directory=Path.Combine(output,"frames",kind+"-"+direction+"-"+motion.ToString().ToLowerInvariant());Directory.CreateDirectory(directory);
                            var strip=new Texture2D(size*16,size*(count/16),TextureFormat.RGBA32,false);
                            var fingerprints=new System.Collections.Generic.HashSet<ulong>();
                            for(int sample=0;sample<count;sample++)
                            {
                                visual.SetReviewPose(direction,motion,sample/(float)count);
                                if(motion!=EnemyPainted2D.Motion.Death)foreach(var part in visual.BodyRenderers)if(part.color.a<.99f)throw new Exception("Living enemy must be opaque / "+kind+" / "+direction+" / "+motion);
                                camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
                                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=active;
                                ulong hash=1469598103934665603UL;int detailed=0;foreach(var pixel in frame.GetPixels32()){hash^=(ulong)(pixel.r|(pixel.g<<8)|(pixel.b<<16));hash*=1099511628211UL;if(pixel.r+pixel.g+pixel.b>120)detailed++;}
                                fingerprints.Add(hash);if(sample==0&&motion!=EnemyPainted2D.Motion.Death){if(detailed<200)throw new Exception("Missing visible enemy pixels / "+kind+" / "+direction);checks++;}
                                strip.SetPixels(sample%16*size,(count/16-1-sample/16)*size,size,size,frame.GetPixels());
                                if(motion==EnemyPainted2D.Motion.Walk||motion==EnemyPainted2D.Motion.Death||sample==0)File.WriteAllBytes(Path.Combine(directory,"frame-"+sample.ToString("D3")+".png"),frame.EncodeToPNG());
                            }
                            strip.Apply(false);File.WriteAllBytes(Path.Combine(output,kind+"-"+direction+"-"+motion.ToString().ToLowerInvariant()+".png"),strip.EncodeToPNG());UnityEngine.Object.DestroyImmediate(strip);
                            if(motion==EnemyPainted2D.Motion.Walk){if(fingerprints.Count<50)throw new Exception("Enemy walk lacks distinct actual frames / "+kind+" / "+direction+" / "+fingerprints.Count);checks++;}
                        }
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(actor);}
            }
            Debug.Log("ASHFALL_ENEMY_2D_BAKE_OK");
            File.WriteAllText(Path.Combine(output,"enemy-2d-review-result.txt"),"PASS / "+checks+" checks / "+DateTime.UtcNow.ToString("O")+" / Actual painted images, eight directions, moving walk pixels. Three-part compositions; not individually redrawn animation cels.");
        }
        finally{camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(cameraObject);if(oldMain!=null)oldMain.tag="MainCamera";}
    }
}
