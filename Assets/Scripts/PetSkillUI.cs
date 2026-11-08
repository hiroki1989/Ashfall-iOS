using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool petSkillInventoryOpen;
        int petSkillBranch,petSkillNode;
        Vector2 petSkillScroll;
        Texture2D petBoneIcon;
        PetSkillPortrait petSkillPortrait;
        static readonly Color[] PetSkillColors={new Color(.34f,.84f,.58f),new Color(.36f,.74f,.92f),new Color(.96f,.62f,.24f)};

        string PetBranchName(int branch)=>branch==0?T("RESCUE DOG","レスキュードッグ"):branch==1?T("SUPPLY HOUND","サプライハウンド"):T("TREASURE SCOUT","トレジャースカウト");
        string PetNodeName(int node)=>node==0?T("PARTNER SKILL","相棒スキル"):node==1?T("STRONG BITE","強いかみつき"):node==2?T("QUICK BITE","すばやいかみつき"):node==3?T("SPECIAL TRAINING","特別訓練"):T("RALLY","奮起");
        string PetNodeDescription(int branch,int node)
        {
            if(node==0)return branch==0?T("On a kill, drop a health pack restoring 20% HP. Cooldown: 60 seconds.","敵撃破時、HPを20%回復するパックを落とす。クールダウン60秒。"):
                branch==1?T("On a kill, drop ammunition of a random gun type. Cooldown: 30 seconds.","敵撃破時、ランダムな銃種の弾薬を落とす。クールダウン30秒。"):
                T("Increase enemy equipment drop chance by 5 percentage points.","敵からの装備ドロップ確率を5ポイント上げる。");
            if(node==1)return T("Increase the dog's bite damage by 15% per rank.","犬のかみつき威力を1ランクごとに15%上げる。");
            if(node==2)return T("Increase the dog's attack speed by 10% per rank.","犬の攻撃速度を1ランクごとに10%上げる。");
            if(node==3)return branch==0?T("Per rank: pack healing +3 percentage points; cooldown -5 seconds.","1ランクごとに回復量を3ポイント増加、クールダウンを5秒短縮。"):
                branch==1?T("Per rank: ammunition +20%, cooldown -3 seconds. At rank 3, choose the least stocked ammo type.","1ランクごとに弾薬量20%増加、クールダウン3秒短縮。ランク3から残量割合が最も少ない銃種を選ぶ。"):
                T("Per rank: equipment drop chance +1 percentage point and a 5% chance to improve rarity.","1ランクごとに装備ドロップ確率を1ポイント増加し、レア度上昇の抽選を5%追加。");
            return T("After a kill, increase player damage by 3% per rank for 10 seconds.","敵撃破後10秒間、プレイヤーの威力を1ランクごとに3%上げる。");
        }

        int GetPetNodeIconIndex(int branch, int node)
        {
            if(node == 0) return branch == 0 ? 20 : branch == 1 ? 21 : 22;
            if(node == 1) return 19; // Fierce Bite / Paw
            if(node == 2) return 14; // Quick Speed Chevrons
            if(node == 3) return branch == 0 ? 11 : branch == 1 ? 2 : 4;
            return 23; // Rally Flag / Howl
        }

        Texture2D PetBone()
        {
            if(petBoneIcon)return petBoneIcon;
            const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Pet points bone",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+y-63)*.7071068f,v=(y-x)*.7071068f;
                bool shape=(Mathf.Abs(u)<20&&Mathf.Abs(v)<5);
                for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2){float a=u-s*20,b=v-t*6;shape|=a*a+b*b<42;}
                pixels[y*n+x]=shape?new Color32(244,231,200,255):new Color32(0,0,0,0);
            }
            tex.SetPixels32(pixels);tex.Apply(false,true);petBoneIcon=tex;return tex;
        }

        void HandlePetTreeControllerInput()
        {
            if(Event.current.type != EventType.Layout) return;
            float now = Time.unscaledTime;

            if(Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                petSkillInventoryOpen = false;
                PlayUserSound("skill", .12f);
                return;
            }
            if(!PetAdopted) return;

            // Upgrade (A / Enter / Space)
            if(Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if(SpendPetSkillPoint(petSkillBranch, petSkillNode)) PlayUserSound("skill", .35f);
            }
            // Equip (X / Y / E)
            if(Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.E))
            {
                if(PetSkillRank(petSkillBranch, 0) > 0 && PetEquippedSkill != petSkillBranch + 1)
                {
                    EquipPetSkill(petSkillBranch);
                    PlayUserSound("skill", .25f);
                }
            }
            // Switch branch via LB / RB
            if(Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.Q))
            {
                petSkillBranch = (petSkillBranch + 2) % 3;
                PlayUserSound("skill", .1f);
            }
            if(Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                petSkillBranch = (petSkillBranch + 1) % 3;
                PlayUserSound("skill", .1f);
            }

            if(now < skillTreeNavCooldown) return;
            float hx = Input.GetAxisRaw("Horizontal");
            float vy = Input.GetAxisRaw("Vertical");
            if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) hx = -1f;
            if(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) hx = 1f;
            if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) vy = 1f;
            if(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) vy = -1f;

            if(Mathf.Abs(hx) < 0.45f && Mathf.Abs(vy) < 0.45f) return;
            skillTreeNavCooldown = now + 0.19f;

            if(Mathf.Abs(vy) >= Mathf.Abs(hx))
            {
                if(vy < 0)
                {
                    // Down in diamond tree: 0 -> 1 -> 2 -> 3 -> 4
                    if(petSkillNode == 0) petSkillNode = 1;
                    else if(petSkillNode == 1 || petSkillNode == 2) petSkillNode = 3;
                    else if(petSkillNode == 3) petSkillNode = 4;
                }
                else
                {
                    // Up in diamond tree
                    if(petSkillNode == 4) petSkillNode = 3;
                    else if(petSkillNode == 3) petSkillNode = 1;
                    else if(petSkillNode == 1 || petSkillNode == 2) petSkillNode = 0;
                }
            }
            else
            {
                if(petSkillNode == 1 || petSkillNode == 2)
                {
                    if(hx > 0 && petSkillNode == 1) petSkillNode = 2;
                    else if(hx < 0 && petSkillNode == 2) petSkillNode = 1;
                    else petSkillBranch = hx > 0 ? (petSkillBranch + 1) % 3 : (petSkillBranch + 2) % 3;
                }
                else
                {
                    petSkillBranch = hx > 0 ? (petSkillBranch + 1) % 3 : (petSkillBranch + 2) % 3;
                }
            }
            PlayUserSound("skill", .08f);
        }

        void DrawPetSkillInventory()
        {
            HandlePetTreeControllerInput();
            Box(new Rect(0,0,1440,900),new Color(.035f,.045f,.05f,.99f));
            ActionLabel(new Rect(42,16,650,48),T("ROOK // COMPANION SKILL TREE","ルーク // ペットスキルツリー"),UiPaper,32);

            Rect ptsBox = new Rect(680, 18, 250, 44);
            FieldUIArt.Plate(ptsBox, true);
            GUI.DrawTexture(new Rect(ptsBox.x+16, ptsBox.y+6, 32, 32), PetBone(), ScaleMode.ScaleToFit, true);
            ActionLabel(new Rect(ptsBox.x+56, ptsBox.y+4, 180, 36), T("POINTS: ","ポイント: ") + PetPoints, UiPaper, 21);

            if(ActionButton(new Rect(1195,16,200,56),T("BACK","戻る"),false,EqAccent)){petSkillInventoryOpen=false;return;}
            if(!PetAdopted){ActionLabel(new Rect(55,110,1140,65),T("Find your companion to unlock pet skills.","犬を仲間にするとペットスキルを使えるようになります。"),Mist,24,TextAnchor.MiddleLeft,true);return;}

            // 3 Branch Columns Selector Tabs at top
            for(int b=0;b<3;b++)
            {
                bool isEq = PetEquippedSkill == b + 1;
                string tabCaption = PetBranchName(b) + (isEq ? T("  [EQUIPPED]","  [装備中]") : "");
                if(ActionButton(new Rect(42+b*302, 74, 290, 52), tabCaption, petSkillBranch==b, PetSkillColors[b], 18))
                { petSkillBranch=b; petSkillNode=0; }
            }

            Color accent=PetSkillColors[petSkillBranch];

            // Left All-3-Branches Top-Down Visual Skill Tree Panel (Scrollable)
            Rect treeFrame = new Rect(42, 136, 904, 704);
            FieldUIArt.Plate(treeFrame, false);
            Box(new Rect(treeFrame.x+2, treeFrame.y+2, treeFrame.width-4, 30), new Color(.055f,.068f,.078f,.95f));
            ActionLabel(new Rect(treeFrame.x+16, treeFrame.y+3, 860, 26),
                T("COMPANION EVOLUTION TREE  ·  PARTNER CORE  ▼  COMBAT FANGS  ▼  SPECIALIZATION  ▼  APEX RALLY",
                  "相棒進化ツリー  ·  相棒スキル  ▼  噛みつき・俊敏  ▼  特別訓練  ▼  最終奮起"),
                accent, 14);

            Rect scrollViewRect = new Rect(treeFrame.x+4, treeFrame.y+34, treeFrame.width-8, treeFrame.height-38);
            const float contentW = 870f;
            const float contentH = 660f;
            petSkillScroll = GUI.BeginScrollView(scrollViewRect, petSkillScroll, new Rect(0, 0, contentW, contentH), false, false);

            // Draw all 3 branches side-by-side in a Borderlands 3-column tree layout!
            for(int b=0;b<3;b++)
            {
                Color bCol = PetSkillColors[b];
                bool activeBranch = (b == petSkillBranch);
                float colX = 148f + b * 287f;

                Rect colBg = new Rect(colX - 134f, 10f, 268f, contentH - 20f);
                Box(colBg, activeBranch ? new Color(bCol.r*.12f, bCol.g*.12f, bCol.b*.12f, .65f) : new Color(.03f,.038f,.044f,.5f));
                Box(new Rect(colBg.x, colBg.y, colBg.width, 3f), activeBranch ? bCol : new Color(bCol.r, bCol.g, bCol.b, .35f));

                bool branchEquipped = PetEquippedSkill == b + 1;
                ActionLabel(new Rect(colBg.x+8, colBg.y+8, colBg.width-16, 24),
                    PetBranchName(b) + (branchEquipped ? " ★" : ""),
                    activeBranch ? bCol : Mist, 15, TextAnchor.MiddleCenter);

                // Top-To-Bottom Diamond Tree Coordinates inside column:
                // Row 0: Node 0 (Partner Skill Root)
                // Row 1: Node 1 (Strong Bite - Left) & Node 2 (Quick Bite - Right)
                // Row 2: Node 3 (Special Training - Center)
                // Row 3: Node 4 (Rally Capstone - Center)
                Vector2 p0 = new Vector2(colX, 88f);
                Vector2 p1 = new Vector2(colX - 66f, 224f);
                Vector2 p2 = new Vector2(colX + 66f, 224f);
                Vector2 p3 = new Vector2(colX, 368f);
                Vector2 p4 = new Vector2(colX, 512f);

                int r0 = PetSkillRank(b, 0), r1 = PetSkillRank(b, 1), r2 = PetSkillRank(b, 2), r3 = PetSkillRank(b, 3), r4 = PetSkillRank(b, 4);
                bool av0 = true;
                bool av1 = r0 > 0;
                bool av2 = r1 > 0;
                bool av3 = r2 > 0;
                bool av4 = r3 > 0;

                // Connectors showing prerequisite flow and visual branching
                DrawSkillConnectorPipe(p0, p1, r1 > 0, av1, bCol);
                DrawSkillConnectorPipe(p0, p2, r2 > 0, av2, bCol);
                DrawSkillConnectorPipe(p1, p2, r2 > 0, av2, bCol);
                DrawSkillConnectorPipe(p1, p3, r3 > 0, av3, bCol);
                DrawSkillConnectorPipe(p2, p3, r3 > 0, av3, bCol);
                DrawSkillConnectorPipe(p3, p4, r4 > 0, av4, bCol);

                Vector2[] pts = { p0, p1, p2, p3, p4 };
                int[] ranks = { r0, r1, r2, r3, r4 };
                bool[] avs = { av0, av1, av2, av3, av4 };

                for(int n=0;n<5;n++)
                {
                    int maxR = n == 0 ? 1 : 5;
                    int frameKind = n == 0 ? 2 : n == 4 ? 2 : (n == 3 ? 0 : 1);
                    float nSize = (n == 0 || n == 4) ? 64f : 56f;
                    int ic = GetPetNodeIconIndex(b, n);
                    bool sel = (petSkillBranch == b && petSkillNode == n);
                    if(DrawBorderlandsSkillNode(pts[n], nSize, ic, frameKind, PetNodeName(n), ranks[n], maxR, avs[n], sel, bCol))
                    {
                        petSkillBranch = b;
                        petSkillNode = n;
                        PlayUserSound("skill", .08f);
                    }
                }
            }

            GUI.EndScrollView();

            // Right Panel: 3D Sitting Companion Preview + Selected Skill Inspector
            Rect rightPanel = new Rect(962, 74, 436, 766);
            FieldUIArt.Plate(rightPanel, false);

            // Top Half of Right Panel: Sitting Dog 3D Portrait
            Rect portraitBox = new Rect(978, 88, 404, 310);
            Box(portraitBox, new Color(.025f,.032f,.038f,.95f));
            Box(new Rect(portraitBox.x, portraitBox.yMax-2, portraitBox.width, 2), new Color(accent.r, accent.g, accent.b, .45f));
            if(!petSkillPortrait)petSkillPortrait=gameObject.AddComponent<PetSkillPortrait>();
            if(Event.current.type==EventType.Repaint)
            {
                var portrait=petSkillPortrait.Render();
                if(portrait)GUI.DrawTexture(new Rect(986, 92, 388, 300), portrait, ScaleMode.ScaleToFit, true);
            }
            ActionLabel(new Rect(988, 94, 380, 26), T("COMPANION // ROOK","相棒 // ルーク"), accent, 16);

            // Selected Skill Inspector in Bottom Half of Right Panel
            int selectedRank=PetSkillRank(petSkillBranch,petSkillNode),selectedMax=petSkillNode==0?1:5;
            bool unlocked=petSkillNode==0||PetSkillRank(petSkillBranch,petSkillNode-1)>0;
            bool canUpgrade=unlocked&&selectedRank<selectedMax&&PetPoints>0;

            int selIcon = GetPetNodeIconIndex(petSkillBranch, petSkillNode);
            Rect iconPreview = new Rect(980, 412, 64, 64);
            DrawSkillNodeFrame(iconPreview, (petSkillNode==0||petSkillNode==4)?2:0, selectedRank>0?accent:UiPaper);
            DrawSkillAtlasIcon(new Rect(992, 424, 40, 40), selIcon, Color.white);

            ActionLabel(new Rect(1056, 412, 320, 26), PetBranchName(petSkillBranch), accent, 18);
            ActionLabel(new Rect(1056, 438, 320, 38), PetNodeName(petSkillNode), UiPaper, 24);

            Rect descBox = new Rect(980, 488, 400, 128);
            Box(descBox, new Color(.03f,.038f,.044f,.92f));
            ActionLabel(new Rect(990, 496, 380, 112), PetNodeDescription(petSkillBranch,petSkillNode), UiPaper, 19, TextAnchor.UpperLeft, true);

            ActionLabel(new Rect(980, 624, 396, 32), T("RANK:  ","習得ランク:  ") + selectedRank + " / " + selectedMax, selectedRank>=selectedMax?Gold:accent, 21);

            if(ActionButton(new Rect(980, 664, 400, 56), selectedRank>=selectedMax?T("MAX RANK","最大ランク"):T("UPGRADE / 1 POINT","強化 / 1ポイント"), canUpgrade, accent, 20, canUpgrade))
                SpendPetSkillPoint(petSkillBranch,petSkillNode);

            bool equipped=PetEquippedSkill==petSkillBranch+1;
            if(ActionButton(new Rect(980, 728, 400, 56), equipped?T("EQUIPPED","装備中"):T("EQUIP BRANCH SKILL","この系統のスキルを装備"), equipped, accent, 20, PetSkillRank(petSkillBranch,0)>0&&!equipped))
                EquipPetSkill(petSkillBranch);

            ActionLabel(new Rect(980, 792, 400, 42), T("Give Iona unwanted legendary gear for Pet Points.","不要なレジェンダリ装備をイオナに渡すとポイント獲得。"), Mist, 15, TextAnchor.MiddleCenter, true);

            // Controller Legend Footer
            ActionLabel(new Rect(42, 848, 1356, 36),
                T("[D-Pad / Stick] Select Node   ·   [A / Enter] Upgrade   ·   [X / E] Equip Skill   ·   [LB / RB] Switch Branch   ·   [B / Esc] Back",
                  "[十字キー / スティック] ノード選択   ·   [A / Enter] スキル強化   ·   [X / E] スキル装備   ·   [LB / RB] 系統切替   ·   [B / Esc] 戻る"),
                Mist, 16, TextAnchor.MiddleCenter);
        }

        void DrawPetSkillHUD()
        {
            if(!PetAdopted||PetEquippedSkill<1||PetEquippedSkill>3)return;
            int b=PetEquippedSkill-1;Rect badge=new Rect(1025,120,128,48);
            Box(badge,new Color(.025f,.035f,.04f,.75f));Box(new Rect(badge.x,badge.yMax-4,badge.width,4),PetSkillColors[b]);
            DrawSkillAtlasIcon(new Rect(badge.x+5,badge.y+5,36,36), GetPetNodeIconIndex(b,0), PetSkillColors[b]);
            string shortName=b==0?T("HEAL","回復"):b==1?T("AMMO","弾薬"):T("LOOT","ドロップ");
            ActionLabel(new Rect(badge.x+43,badge.y+3,82,38),shortName,UiPaper,14,TextAnchor.MiddleLeft,false);
        }
    }

    // Offscreen model uses the same dog prefab and an isolated camera; the main game actor is untouched.
    public sealed class PetSkillPortrait : MonoBehaviour
    {
        const int Layer=31;
        GameObject stage;Camera view;RenderTexture image;Material skin;
        public Texture Render()
        {
            if(!stage)Initialize();
            if(!view||!image)return null;
            view.Render();return image;
        }
        void Initialize()
        {
            var prefab=Resources.Load<GameObject>("PetDog/GermanShepherd");if(!prefab)return;
            stage=new GameObject("Pet skill portrait stage");stage.transform.SetParent(transform,false);stage.transform.position=new Vector3(24000,0,0);
            var dog=Instantiate(prefab,stage.transform);dog.name="Rook sitting portrait";
            foreach(var col in dog.GetComponentsInChildren<Collider>(true))col.enabled=false;
            foreach(var a in dog.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;a.Update(0);a.enabled=false;}
            foreach(var t in dog.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer=Layer;
            }
            Transform pelvis=null,shoulder=null;
            foreach(var t in dog.GetComponentsInChildren<Transform>(true)){if(t.name=="DEF-spine.004")pelvis=t;else if(t.name=="DEF-spine.008")shoulder=t;}
            if(pelvis&&shoulder)pelvis.RotateAround(shoulder.position,dog.transform.right,-32f);
            foreach(var t in dog.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="DEF-thigh.L"||t.name=="DEF-thigh.R")t.Rotate(dog.transform.right,-55f,Space.World);
                if(t.name=="DEF-shin.L"||t.name=="DEF-shin.R")t.Rotate(dog.transform.right,95f,Space.World);
            }
            var texture=Resources.Load<Texture2D>("PetDog/GermanShepherdBase");var shader=Shader.Find("Standard");
            if(shader){skin=new Material(shader){mainTexture=texture};skin.SetFloat("_Glossiness",.12f);foreach(var r in dog.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=skin;r.sharedMaterials=mats;}}
            var renderers=dog.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var cam=new GameObject("Pet portrait camera");cam.transform.SetParent(stage.transform,false);view=cam.AddComponent<Camera>();view.enabled=false;view.orthographic=true;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.clear;view.cullingMask=1<<Layer;view.nearClipPlane=.01f;view.farClipPlane=1000;
            float extent=Mathf.Max(.1f,Mathf.Max(bounds.extents.y,Mathf.Max(bounds.extents.x,bounds.extents.z)));view.orthographicSize=extent*1.12f;
            view.transform.position=bounds.center+new Vector3(-1.6f,1.0f,2.5f)*extent;view.transform.LookAt(bounds.center);
            var lamp=new GameObject("Pet portrait light");lamp.transform.SetParent(stage.transform,false);lamp.transform.position=bounds.center+new Vector3(0,2,-2)*extent;var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=extent*10;light.intensity=2;light.cullingMask=1<<Layer;
            image=new RenderTexture(512,640,24,RenderTextureFormat.ARGB32){name="Pet skill portrait"};image.Create();view.targetTexture=image;
        }
        void OnDestroy(){if(view)view.targetTexture=null;if(image){image.Release();Destroy(image);}if(skin)Destroy(skin);if(stage)Destroy(stage);}
    }
}
