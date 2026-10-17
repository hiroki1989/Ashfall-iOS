using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

// Offline evidence renders actual articulated 2D assets. These are rendered poses,
// not 1,536 independently AI-painted animation drawings.
public static class LayeredPlayerBaker
{
    static void ValidateHandling(LayeredPlayer2D player,Camera camera,string context)
    {
        if(!player.WeaponReady)throw new Exception("Missing painted weapon / "+context);
        if(Vector3.Distance(player.RightHandPosition,player.VisualGripPosition)>.001f)throw new Exception("Trigger hand misses grip / "+context);
        if(Vector3.Distance(player.RightElbowPosition,player.RightShoulderPosition)<.05f||Vector3.Distance(player.RightElbowPosition,player.RightHandPosition)<.02f)throw new Exception("Collapsed projected trigger arm / "+context);
        if(Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftHandPosition,camera.transform.forward).magnitude<.02f||Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftShoulderPosition,camera.transform.forward).magnitude<.05f)throw new Exception("Collapsed projected left arm / "+context);
        float supportGap=Vector3.Distance(player.LeftHandPosition,player.VisualSupportGripPosition);
        float stockGap=Vector3.ProjectOnPlane(player.VisualStockPosition-player.RightShoulderPosition,camera.transform.forward).magnitude;
        if(player.IsOneHanded)
        {
            if(supportGap<.04f)throw new Exception("Free hand incorrectly attached to gun / "+context);
            if(player.StockOcclusionActive||stockGap<.025f)throw new Exception("One-handed gun incorrectly shoulder mounted / "+context);
        }
        else
        {
            if(supportGap>.001f)throw new Exception("Two-handed weapon misses support grip / "+context);
            if(player.RequiresShoulderStock&&player.StockContactError>.001f)throw new Exception("Aimed weapon misses shoulder stock / "+context);
            if(player.WeaponAimBlend<.001f&&Vector3.ProjectOnPlane(player.VisualGripPosition-player.CarryGripTarget,camera.transform.forward).magnitude>.001f)throw new Exception("Low ready misses front-body carry grip / "+context);
        }
        foreach(var hand in player.ArmorHandRenderers)if(hand.enabled&&hand.sprite==null)throw new Exception("Missing rendered glove / "+context);
    }
    static void ReadFrame(Camera camera,RenderTexture rt,Texture2D frame)
    {
        camera.Render();var previous=RenderTexture.active;
        try{RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);frame.Apply(false);}
        finally{RenderTexture.active=previous;}
    }
    static void BakeArmAudit(LayeredPlayer2D player,Camera camera,RenderTexture rt,Texture2D frame,ArmorItem[] gear,string output)
    {
        string audit=Path.Combine(output,"arm-audit");Directory.CreateDirectory(audit);
        int captures=0;var manifest=new System.Text.StringBuilder("Actual 320px renderer isolation. Full, no chest, no bracers, no body upper arms. Priority model0/style2 carry also includes six isolated body-arm tiles with torso/head. Disabled renderer states restored after every capture.\n");
        // First two complete branches are the reported model0/style2 carry views.
        foreach(int model in new[]{0,12,14})foreach(int style in new[]{2,0})for(int aimed=0;aimed<2;aimed++)foreach(int direction in new[]{1,7})
        {
            for(int slot=0;slot<gear.Length;slot++)gear[slot].visualVariant=style;
            player.ApplyArmor(gear);player.SetWeaponModel(model,Color.white);
            player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,aimed==1);
            string name="model"+model+"-armor"+style+"-d"+direction+(aimed==1?"-aim":"-carry");
            string folder=Path.Combine(audit,name);Directory.CreateDirectory(folder);
            var renderers=player.GetComponentsInChildren<SpriteRenderer>(true);var enabled=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++)enabled[i]=renderers[i].enabled;
            Action restore=()=>{for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];};
            try
            {
                for(int variant=0;variant<4;variant++)
                {
                    restore();
                    if(variant==1){player.ArmorRenderers[1].enabled=false;foreach(var upper in player.ArmorUpperArmRenderers)upper.enabled=false;}
                    if(variant==2){player.ArmorRenderers[2].enabled=false;player.ArmorRenderers[3].enabled=false;}
                    if(variant==3){player.BodyRenderers[3].enabled=false;player.BodyRenderers[6].enabled=false;}
                    ReadFrame(camera,rt,frame);
                    string file=new[]{"full","without-chest","without-bracers","without-upper-arms"}[variant]+".png";
                    File.WriteAllBytes(Path.Combine(folder,file),frame.EncodeToPNG());captures++;
                }
                if(model==0&&style==2&&aimed==0)
                {
                    var tiles=new Texture2D(frame.width*3,frame.height*2,TextureFormat.RGBA32,false);
                    try
                    {
                        for(int part=3;part<=8;part++)
                        {
                            foreach(var renderer in renderers)renderer.enabled=false;
                            player.BodyRenderers[0].enabled=true;player.BodyRenderers[1].enabled=true;
                            player.BodyRenderers[part].enabled=true;
                            ReadFrame(camera,rt,frame);int tile=part-3;
                            tiles.SetPixels(tile%3*frame.width,(1-tile/3)*frame.height,frame.width,frame.height,frame.GetPixels());
                        }
                        tiles.Apply(false);File.WriteAllBytes(Path.Combine(folder,"isolated-body-arms.png"),tiles.EncodeToPNG());
                    }
                    finally{UnityEngine.Object.DestroyImmediate(tiles);restore();}
                }
                manifest.AppendLine(name+": four comparisons; bracer exclusion retains separate armor gloves; upper-arm exclusion retains shoulder bridge.");
            }
            finally{restore();}
        }
        manifest.AppendLine("96 individual 320x320 PNGs plus two 3x2 isolation sheets. Isolation tiles: top R upper/forearm/hand; bottom L upper/forearm/hand. Each tile retains body torso/head only.");
        File.WriteAllText(Path.Combine(audit,"manifest.txt"),manifest.ToString());
        Debug.Log("ASHFALL_ARM_AUDIT_OK / "+captures+" comparisons / "+audit);
    }
    static void BakeMotionFocus(LayeredPlayer2D player,Camera camera,RenderTexture rt,Texture2D frame,ArmorItem[] gear,string output)
    {
        const int count=32;bool poseOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"--pose-check")>=0,efficient=Array.IndexOf(Environment.GetCommandLineArgs(),"--motion-efficient")>=0;int size=frame.width;float speed=7f,cycle=LayeredPlayer2D.GaitCycleDistance(true);int pairedFrames=0;
        var csv=new System.Text.StringBuilder("image,model,direction,aim,motion,sample,phase,cycleDistance,distanceFromCycleStart,referenceSpeed,timeFromCycleStart,secondsPerSample,rightKneeX,rightKneeY,leftKneeX,leftKneeY,rightFootX,rightFootY,leftFootX,leftFootY\n");
        var comparison=new Texture2D(size*2,size,TextureFormat.RGBA32,false);
        try
        {
            foreach(int model in new[]{12,14,5,15})foreach(int direction in (poseOnly?new[]{0,4,7}:new[]{0,1,2,3,4,5,6,7}))for(int aimed=0;aimed<2;aimed++)
            {
                player.SetWeaponModel(model,Color.white);
                int motionFrames=poseOnly||efficient&&(!(model==12||model==14)||!(direction==0||direction==4||direction==7))?0:count;
                string name=model+"-"+direction+(aimed==1?"-aim":"-carry"),directory=Path.Combine(output,"motion-focus",name);Directory.CreateDirectory(directory);
                var strip=new Texture2D(size*2*8,size*4,TextureFormat.RGBA32,false);
                try
                {
                    // Each frame pairs armor (left) with the same bare-body pose (right).
                    // Both retain the weapon so exposed arms can be compared at identical grips.
                    for(int sample=-1;sample<motionFrames;sample++)
                    {
                        bool idle=sample<0;float phase=idle?0:sample/(float)count;var motion=idle?LayeredPlayer2D.Motion.Idle:LayeredPlayer2D.Motion.Run;
                        player.ApplyArmor(gear);player.SetReviewPose(direction,motion,phase,aimed==1);ValidateHandling(player,camera,name+" / "+sample);
                        ReadFrame(camera,rt,frame);comparison.SetPixels(0,0,size,size,frame.GetPixels());
                        player.ApplyArmor(null);player.SetReviewPose(direction,motion,phase,aimed==1);ValidateHandling(player,camera,name+" bare / "+sample);
                        ReadFrame(camera,rt,frame);comparison.SetPixels(size,0,size,size,frame.GetPixels());comparison.Apply(false);
                        string imageName=idle?"idle.png":"frame-"+sample.ToString("D3")+".png";File.WriteAllBytes(Path.Combine(directory,imageName),comparison.EncodeToPNG());pairedFrames++;
                        if(!idle)strip.SetPixels(sample%8*size*2,(3-sample/8)*size,size*2,size,comparison.GetPixels());
                        Vector3 origin=player.transform.position;Vector2 rk=Projected(player.RightKneePosition-origin,camera),lk=Projected(player.LeftKneePosition-origin,camera),rf=Projected(player.RightFootPosition-origin,camera),lf=Projected(player.LeftFootPosition-origin,camera);
                        csv.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6},{12:F6},{13:F6},{14:F6},{15:F6},{16:F6},{17:F6},{18:F6},{19:F6}",name+"/"+imageName,model,direction,aimed,motion,sample,phase,cycle,idle?0:phase*cycle,speed,idle?0:phase*cycle/speed,idle?0:cycle/speed/count,rk.x,rk.y,lk.x,lk.y,rf.x,rf.y,lf.x,lf.y));
                    }
                    if(motionFrames>0){strip.Apply(false);File.WriteAllBytes(Path.Combine(output,"motion-focus",name+"-run-32.png"),strip.EncodeToPNG());}
                }
                finally{UnityEngine.Object.DestroyImmediate(strip);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(comparison);player.ApplyArmor(gear);}
        File.WriteAllText(Path.Combine(output,"motion-focus-samples.csv"),csv.ToString());
        File.WriteAllText(Path.Combine(output,"motion-focus-manifest.txt"),string.Format(System.Globalization.CultureInfo.InvariantCulture,"Models 12/14/5/15; {5} actual paired images. Efficient mode limits run sequences to models12/14 directions0/4/7; idle still covers all8 facings. Left: armor. Right: bare body with identical weapon and pose. Actual articulated renders require visual QA; coordinate checks alone do not establish natural motion. Run cycle distance {0:F3}; walk cycle distance {1:F3}; reference speed {2:F3}. Run cycle duration {3:F6}s, sample duration {4:F6}s, two alternating steps per cycle. CSV phase/distance/time use runtime GaitCycleDistance; sampled poses use SetReviewPose, not simulated wall-clock movement.",cycle,LayeredPlayer2D.GaitCycleDistance(false),speed,cycle/speed,cycle/speed/count,pairedFrames));
        Debug.Log("ASHFALL_LAYERED_MOTION_FOCUS_BAKE_OK / "+output);
    }
    static Vector2 Projected(Vector3 vector,Camera camera)=>new Vector2(Vector3.Dot(vector,camera.transform.right),Vector3.Dot(vector,camera.transform.up));
    [MenuItem("ASHFALL/Render layered player motion evidence")]
    public static void Bake()
    {
        string[] args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--layered-output");
        string output=flag>=0&&flag+1<args.Length?args[flag+1]:"D:/Unity/AshfallLayeredPlayerReview";
        Directory.CreateDirectory(output);
        var currentMain=Camera.main;
        var root=new GameObject("Offline layered player review");var cameraObject=new GameObject("Layered review camera");
        var camera=cameraObject.AddComponent<Camera>();cameraObject.tag="MainCamera";
        if(currentMain!=null&&currentMain!=camera)currentMain.gameObject.tag="Untagged";
        camera.transform.rotation=Quaternion.Euler(35.264f,45,0);camera.orthographic=true;camera.orthographicSize=1.40f;
        camera.backgroundColor=new Color(.095f,.10f,.095f,1);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.01f;camera.farClipPlane=80;
        root.transform.position=new Vector3(3000,.65f,3000);
        camera.transform.position=root.transform.position+camera.transform.up*.40f-camera.transform.forward*20;
        var player=root.AddComponent<LayeredPlayer2D>();
        const int size=320;var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
        try
        {
            player.Init();if(!player.Ready)throw new InvalidOperationException("All five Body_* directional source sheets are required before baking.");
            var gear=new ArmorItem[5];for(int i=0;i<5;i++)gear[i]=new ArmorItem{slot=(ArmorSlot)i,visualVariant=0,color=Color.white};player.ApplyArmor(gear);player.SetWeaponModel(0,Color.white);
            camera.targetTexture=rt;
            if(Array.IndexOf(args,"--arm-audit")>=0){BakeArmAudit(player,camera,rt,frame,gear,output);return;}
            if(Array.IndexOf(args,"--motion-focus")>=0){BakeMotionFocus(player,camera,rt,frame,gear,output);return;}
            for(int direction=0;direction<8;direction++)for(int state=0;state<4;state++)
            {
                int count=state==1||state==3?64:32;int columns=16,rows=count/columns;
                string sequence=Path.Combine(output,"frames",direction+"-"+((LayeredPlayer2D.Motion)state).ToString().ToLowerInvariant());Directory.CreateDirectory(sequence);
                var sheet=new Texture2D(size*columns,size*rows,TextureFormat.RGBA32,false);
                for(int sample=0;sample<count;sample++)
                {
                    player.SetReviewPose(direction,(LayeredPlayer2D.Motion)state,sample/(float)count,state==2);camera.Render();
                    var old=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=old;
                    sheet.SetPixels(sample%columns*size,(rows-1-sample/columns)*size,size,size,frame.GetPixels());
                    File.WriteAllBytes(Path.Combine(sequence,"frame-"+sample.ToString("D3")+".png"),frame.EncodeToPNG());
                }
                sheet.Apply(false);File.WriteAllBytes(Path.Combine(output,"direction-"+direction+"-"+((LayeredPlayer2D.Motion)state).ToString().ToLowerInvariant()+"-"+count+".png"),sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
            }
            File.WriteAllText(Path.Combine(output,"motion-manifest.txt"),"8 directions; per direction64 walk,64 run,32 idle,32 shoot. Total1536 articulated compositions and PNG sequence evidence on D. Five authored neutral body/armor sheets mirrored into8 facings. Independent body/armor/weapon sprites with joint interpolation; these are rendered compositions, not1536 independently AI-painted drawings.");
            var weapons=Path.Combine(output,"weapons");Directory.CreateDirectory(weapons);
            for(int model=0;model<Arsenal.ModelCount;model++)for(int direction=0;direction<8;direction++)
            {
                player.SetWeaponModel(model,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);
                float carryHandHeight=Vector3.Dot(player.RightHandPosition,camera.transform.up);
                player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true);
                if(player.IsOneHanded&&Vector3.Dot(player.RightHandPosition,camera.transform.up)<carryHandHeight+.15f)throw new Exception("One-handed aim drops trigger arm below carry / "+model+" / "+direction);
                ValidateHandling(player,camera,model+" / "+direction);
                float roll=Mathf.Abs(Mathf.DeltaAngle(0,(Quaternion.Inverse(camera.transform.rotation)*player.WeaponRenderer.transform.rotation).eulerAngles.z));
                if(roll>70.1f)throw new Exception("Weapon rolls upside down / "+model+" / "+direction);
                camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=old;
                File.WriteAllBytes(Path.Combine(weapons,model+"-"+direction+".png"),frame.EncodeToPNG());
                if(model==8&&direction==0)for(int phase=0;phase<4;phase++)
                {
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,phase/4f,true);ValidateHandling(player,camera,"coil front run / "+phase);
                    camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=previous;
                    File.WriteAllBytes(Path.Combine(output,"coil-front-run-"+phase+".png"),frame.EncodeToPNG());
                }
            }
            int compactFrames=0;var compactOutput=Path.Combine(output,"compact-guns-two-hand-grip");Directory.CreateDirectory(compactOutput);
            for(int model=0;model<Arsenal.ModelCount;model++)if(Arsenal.IsOneHanded(model))for(int direction=0;direction<8;direction++)for(int aimed=0;aimed<2;aimed++)
            {
                player.SetWeaponModel(model,Color.white);string sequence=Path.Combine(compactOutput,model+"-"+direction+"-run"+(aimed==1?"-aim":"-carry"));Directory.CreateDirectory(sequence);
                for(int sample=0;sample<16;sample++)
                {
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,sample/16f,aimed==1);ValidateHandling(player,camera,model+" / "+direction+" / "+sample+" / "+aimed);
                    camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=old;
                    File.WriteAllBytes(Path.Combine(sequence,"frame-"+sample.ToString("D3")+".png"),frame.EncodeToPNG());compactFrames++;
                }
            }
            File.WriteAllText(Path.Combine(output,"handling-manifest.txt"),Arsenal.ModelCount+" weapons x8 aimed directions; "+compactFrames+" compact-gun run/carry + run/aim frames. Every gun uses both hands; compact pistols and short shotguns do not brace a stock on the shoulder. Long-gun stock contact and masking are checked separately. / "+DateTime.UtcNow.ToString("O"));
            player.SetWeaponModel(12,Color.white);
            player.SetReviewPose(7,LayeredPlayer2D.Motion.Shoot,.05f,true);camera.Render();
            var active=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=active;
            var occluded=frame.GetPixels32();File.WriteAllBytes(Path.Combine(output,"stock-occluded.png"),frame.EncodeToPNG());
            var stockBlock=new MaterialPropertyBlock();player.WeaponRenderer.GetPropertyBlock(stockBlock);stockBlock.SetFloat("_StockOcclusionEnabled",0);player.WeaponRenderer.SetPropertyBlock(stockBlock);camera.Render();
            active=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=active;
            var exposed=frame.GetPixels32();int stockPixels=0;for(int pixel=0;pixel<exposed.Length;pixel++)if(exposed[pixel].r!=occluded[pixel].r||exposed[pixel].g!=occluded[pixel].g||exposed[pixel].b!=occluded[pixel].b)stockPixels++;
            File.WriteAllBytes(Path.Combine(output,"stock-exposed-control.png"),frame.EncodeToPNG());if(stockPixels<4)throw new Exception("Stock torso shader fails to hide actual rendered pixels / "+stockPixels);
            File.WriteAllText(Path.Combine(output,"stock-occlusion-result.txt"),"PASS / "+stockPixels+" actual front-left stock pixels hidden by torso / "+DateTime.UtcNow.ToString("O"));
            for(int direction=0;direction<8;direction++)
            {
                string sequence=Path.Combine(output,"frames",direction+"-run-aim");Directory.CreateDirectory(sequence);
                for(int sample=0;sample<64;sample++)
                {
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,sample/64f,true);
                    if(player.StockContactError>.001f)throw new Exception("Moving stock misses shoulder / "+direction+" / "+sample);
                    if(direction==7&&!player.StockOcclusionActive)throw new Exception("Moving front-left stock lacks torso occlusion");
                    camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=old;
                    File.WriteAllBytes(Path.Combine(sequence,"frame-"+sample.ToString("D3")+".png"),frame.EncodeToPNG());
                }
            }
            Debug.Log("ASHFALL_LAYERED_MOTION_BAKE_OK / "+output);
            var armorOutput=Path.Combine(output,"armor-styles");Directory.CreateDirectory(armorOutput);
            for(int style=0;style<LayeredPlayer2D.ArmorStyleCount;style++)for(int direction=0;direction<8;direction++)
            {
                for(int slot=0;slot<5;slot++)gear[slot].visualVariant=style;player.ApplyArmor(gear);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);RenderTexture.active=old;
                File.WriteAllBytes(Path.Combine(armorOutput,style+"-"+direction+".png"),frame.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);
            if(currentMain!=null&&currentMain!=camera)currentMain.gameObject.tag="MainCamera";
        }
    }
}

