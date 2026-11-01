using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NeonVault
{
    // A disabled, never-awakened game supplies the real UI without loading or writing saves.
    public sealed class ActionTreeVisualReview : MonoBehaviour
    {
        const string Output="D:/Unity/BuildTemp/ActionTreeReview";
        const BindingFlags Instance=BindingFlags.NonPublic|BindingFlags.Instance;
        VaultGame game; MethodInfo styles,draw,description; bool ready; string failure; bool originalLanguage;
        bool syntheticDescription; string testDescription; int scrollPhase; bool sawScrollAtTop,sawScrollAtBottom;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--action-tree-review")>=0)
                new GameObject("Action tree visual review").AddComponent<ActionTreeVisualReview>();
        }
        static void Set(object target,string name,object value){typeof(VaultGame).GetField(name,Instance).SetValue(target,value);}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);originalLanguage=VaultLanguage.Japanese;
            var holder=new GameObject("Inactive UI review game");holder.SetActive(false);
            game=holder.AddComponent<VaultGame>();
            Set(game,"font",VaultFont.Preferred());Set(game,"englishHeadingFont",VaultFont.Preferred());
            var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();Set(game,"pixel",pixel);
            Set(game,"smoke",true);Set(game,"actionSkillLoaded",true);
            // Show examples of unlocked paths without touching persistent state.
            var ranks=(int[])typeof(VaultGame).GetField("actionSkillRanks",Instance).GetValue(game);
            var extras=(int[])typeof(VaultGame).GetField("actionSkillExtraRanks",Instance).GetValue(game);
            for(int k=0;k<3;k++)for(int b=0;b<3;b++){ranks[k*15+b*5]=3;extras[((k*3+b)*5)*4]=1;extras[((k*3+b)*5)*4+1]=1;}
            styles=typeof(VaultGame).GetMethod("Styles",Instance);draw=typeof(VaultGame).GetMethod("DrawActionSkillTree",Instance);description=typeof(VaultGame).GetMethod("ActionTreeDescription",Instance);ready=true;
            for(int i=0;i<20;i++)yield return null;
            foreach(bool ja in new[]{true,false})
            {
                VaultLanguage.Set(ja,false);
                for(int k=0;k<3;k++)for(int b=0;b<3;b++)
                {
                    Set(game,"actionTreeKind",k);Set(game,"actionTreeBranch",b);Set(game,"actionTreeTier",0);Set(game,"actionTreeSlot",1);
                    yield return null;yield return new WaitForEndOfFrame();
                    if(failure!=null)break;
                    var image=ScreenCapture.CaptureScreenshotAsTexture();
                    if(!image){failure="screenshot unavailable";break;}
                    File.WriteAllBytes(Output+"/tree-"+(ja?"ja":"en")+"-"+k+"-"+b+".png",image.EncodeToPNG());Destroy(image);
                }
                // Capture the true bomber capstone copy, where DPS rather than
                // weapon single-shot damage determines the described bonus.
                Set(game,"actionTreeKind",1);Set(game,"actionTreeBranch",2);Set(game,"actionTreeTier",4);Set(game,"actionTreeSlot",-1);
                string bomber=game.ActionSkillNodeDescription(ActionSkillKind.Bomber,2,4);
                if(!bomber.Contains("DPS")||!bomber.Contains("3/7.7")){failure="bomber capstone DPS description missing / "+(ja?"ja":"en");break;}
                yield return null;yield return new WaitForEndOfFrame();
                var capstone=ScreenCapture.CaptureScreenshotAsTexture();
                if(!capstone){failure="bomber screenshot unavailable";break;}
                File.WriteAllBytes(Output+"/bomber-capstone-"+(ja?"ja":"en")+".png",capstone.EncodeToPNG());Destroy(capstone);
                // Stress the production description renderer with enough text
                // to require scrolling in either language. Set the scrollbar's
                // offset directly: this verifies rendering and bounds, not a
                // physical touch/drag gesture.
                testDescription=ja?"長文検証の先頭。\n各行はスクロール可能な説明領域です。\n追加効果の説明を確認します。\n第４行。\n第５行。\n第６行。\n第７行。\n第８行。\n長文検証の末尾。":"Long description start.\nEach line occupies the scrollable description area.\nVerify the additional effects.\nFourth line.\nFifth line.\nSixth line.\nSeventh line.\nEighth line.\nLong description end.";
                syntheticDescription=true;scrollPhase=0;sawScrollAtTop=false;sawScrollAtBottom=false;
                yield return null;yield return new WaitForEndOfFrame();
                var top=ScreenCapture.CaptureScreenshotAsTexture();if(!top){failure="scroll top screenshot unavailable";break;}
                File.WriteAllBytes(Output+"/scroll-top-"+(ja?"ja":"en")+".png",top.EncodeToPNG());Destroy(top);
                scrollPhase=1;yield return null;yield return new WaitForEndOfFrame();
                var bottom=ScreenCapture.CaptureScreenshotAsTexture();if(!bottom){failure="scroll bottom screenshot unavailable";break;}
                File.WriteAllBytes(Output+"/scroll-bottom-"+(ja?"ja":"en")+".png",bottom.EncodeToPNG());Destroy(bottom);
                if(!sawScrollAtTop||!sawScrollAtBottom){failure="description scroll offset did not advance / "+(ja?"ja":"en");break;}
                syntheticDescription=false;
            }
            ready=false;VaultLanguage.Set(originalLanguage,false);
            File.WriteAllText(Output+"/result.txt",failure==null?"PASS 24 screenshots: 18 tree, 2 bomber capstone, 4 long-description scroll; visual inspection and physical touch verification required":"FAIL "+failure);
        }
        void OnGUI()
        {
            if(!ready||failure!=null)return;
            try
            {
                GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
                styles.Invoke(game,null);draw.Invoke(game,null);
                if(syntheticDescription)
                {
                    // The first render establishes the text and resets its
                    // scroll. Subsequent renders deliberately request bottom.
                    Set(game,"actionTreeLastDescription",testDescription);
                    Set(game,"actionTreeDescriptionScroll",new Vector2(0,scrollPhase==1?120:0));
                    var pixel=(Texture2D)typeof(VaultGame).GetField("pixel",Instance).GetValue(game);
                    GUI.color=new Color(.035f,.045f,.05f,1);GUI.DrawTexture(new Rect(978,330,385,235),pixel);GUI.color=Color.white;
                    description.Invoke(game,new object[]{new Rect(978,330,385,235),testDescription,Color.white,22});
                    var offset=(Vector2)typeof(VaultGame).GetField("actionTreeDescriptionScroll",Instance).GetValue(game);
                    if(scrollPhase==0&&offset.y==0)sawScrollAtTop=true;
                    if(scrollPhase==1&&offset.y>0)sawScrollAtBottom=true;
                }
            }
            catch(Exception e){failure=e.ToString();}
        }
    }
}
