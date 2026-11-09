using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool petSkillInventoryOpen;
        int petSkillBranch,petSkillNode;
        Vector2 petSkillScroll;
        bool petSkillAutoScroll;
        Texture2D petBoneIcon;
        PetSkillPortrait petSkillPortrait;
        static readonly Color[] PetSkillColors={new Color(.34f,.84f,.58f),new Color(.36f,.74f,.92f),new Color(.96f,.62f,.24f)};

        string PetBranchName(int branch)=>branch==0?T("RESCUE DOG","レスキュードッグ"):branch==1?T("SUPPLY HOUND","サプライハウンド"):T("TREASURE SCOUT","トレジャースカウト");
        const int PetNodeCount=25;
        string PetNodeName(int node)
        {
            if(node==0)return T("PARTNER CORE","相棒の核");
            if(node==1)return T("STRONG BITE","強いかみつき"); if(node==2)return T("QUICK BITE","すばやいかみつき");
            if(node==3)return T("SPECIAL TRAINING","特別訓練"); if(node==4)return T("RALLY","奮起");
            string[] en={"Scent Trail","Guard Step","Field Medic","Second Wind","Pack Triage","Last Stand","Rapid Fetch","Ammo Satchel","Smart Scavenge","Reserve Cache","Battle Courier","Deep Pockets","Keen Nose","Rare Find","Master Scavenger","Trail Sense","Swift Paws","Steady Nerves","Pack Bond","Veteran Companion"};
            string[] ja={"追跡の嗅覚","護衛の歩み","野戦救護","再起の合図","救急手当","最後の盾","素早い回収","弾薬袋","弾種選別","予備弾薬","戦場配達","大容量袋","鋭い嗅覚","希少品発見","熟練スカウト","獣道の勘","俊足","冷静な判断","相棒の絆","歴戦の相棒"};
            return T(en[node-5],ja[node-5]);
        }
        string PetNodeDescription(int branch,int node)
        {
            if(node<5){if(node==0)return branch==0?T("On a kill, drop a health pack restoring 20% HP. Cooldown: 60 seconds.","敵撃破時、HPを20%回復するパックを落とす。再発動60秒。"):branch==1?T("On a kill, drop ammunition for a random gun type. Cooldown: 30 seconds.","敵撃破時、ランダムな銃種の弾薬を落とす。再発動30秒。"):T("Increase enemy equipment drop chance by 1.25 percentage points.","敵の装備ドロップ率を1.25ポイント上げる。");if(node==1)return T("Companion bite damage +15% per rank.","ペットのかみつき威力+15%／ランク。");if(node==2)return T("Companion attack speed +10% per rank.","ペットの攻撃速度+10%／ランク。");if(node==3)return branch==0?T("Pack healing +3 points and cooldown -5s per rank.","回復量+3ポイント、再発動-5秒／ランク。"):branch==1?T("Ammo amount +20% and cooldown -3s per rank; rank 3 picks the least-stocked type.","弾薬量+20%、再発動-3秒／ランク。ランク3で残量最少銃種を選択。"):T("Equipment chance +0.25 points and rarity upgrade chance +5% per rank.","装備率+0.25ポイント、レア度上昇抽選+5%／ランク。");return T("Kill rally: companion bite damage +3% per rank for 10 seconds.","撃破時の奮起：10秒間ペットのかみつき威力+3%／ランク。");}
            int i=node-5;
            if(branch==0)return T(new[]{"Rescue pack healing +2% per rank.","Rescue cooldown -2s per rank.","Below 35% HP, rescue healing +8%.","Companion bite damage +4% per rank.","Rescue pack cooldown -3s per rank.","On rescue drop, restore 2% HP immediately.","Rescue healing +3% per rank.","Below 50% HP, rescue cooldown -4s.","Companion attack speed +4% per rank.","Rescue healing +4% per rank.","Below 25% HP, rescue healing +8%.","Companion bite damage +5% per rank.","On kill, rally lasts +2s per rank.","Rescue cooldown -4s per rank.","Rescue healing +5% per rank.","Below 35% HP, companion damage +10%.","On rescue, restore an additional 3% HP.","Rescue healing +6% per rank.","Rescue cooldown -5s per rank.","Final rescue pack heals an additional 12%."}[i],new[]{"救急パック回復量+2%／ランク。","救助再発動-2秒／ランク。","HP35%未満なら救急回復+8%。","ペットかみつき威力+4%／ランク。","救助再発動-3秒／ランク。","救急ドロップ時HPを即時2%回復。","救急回復量+3%／ランク。","HP50%未満なら救助再発動-4秒。","ペット攻撃速度+4%／ランク。","救急回復量+4%／ランク。","HP25%未満なら救急回復+8%。","ペットかみつき威力+5%／ランク。","撃破奮起の持続+2秒／ランク。","救助再発動-4秒／ランク。","救急回復量+5%／ランク。","HP35%未満ならペット威力+10%。","救助発動時さらにHP3%即時回復。","救急回復量+6%／ランク。","救助再発動-5秒／ランク。","最終救急パックの回復量+12%。"}[i]);
            if(branch==1)return T(new[]{"Ammo supply quantity +8% per rank.","Supply cooldown -2s per rank.","Below 20% reserve, ammo amount +15%.","Companion bite damage +4% per rank.","Supply cooldown -3s per rank.","Favor equipped weapon ammo unless least-stocked training is active.","Ammo quantity +10% per rank.","Ammo supply cooldown -4s per rank.","Companion attack speed +4% per rank.","Ammo quantity +12% per rank.","At empty reserve, supply gives +20%.","Companion bite damage +5% per rank.","Kill rally lasts +2s per rank.","Supply cooldown -4s per rank.","Ammo quantity +15% per rank.","Below 20% reserve, companion damage +10%.","Supply chooses least-stocked ammo type.","Ammo quantity +18% per rank.","Supply cooldown -5s per rank.","Supply drop gives an extra ammo stack."}[i],new[]{"弾薬供給量+8%／ランク。","補給再発動-2秒／ランク。","予備弾薬20%未満なら量+15%。","ペットかみつき威力+4%／ランク。","補給再発動-3秒／ランク。","残量最少訓練がなければ装備中の銃種弾薬を優先。","弾薬量+10%／ランク。","補給再発動-4秒／ランク。","ペット攻撃速度+4%／ランク。","弾薬量+12%／ランク。","弾切れ時の供給量+20%。","ペットかみつき威力+5%／ランク。","撃破奮起の持続+2秒／ランク。","補給再発動-4秒／ランク。","弾薬量+15%／ランク。","予備弾薬20%未満ならペット威力+10%。","最も残量の少ない銃種を選ぶ。","弾薬量+18%／ランク。","補給再発動-5秒／ランク。","弾薬ドロップに追加スタック。"}[i]);
            return T(new[]{"Equipment chance +0.125 points per rank.","Rarity upgrade chance +2% per rank.","Boss loot rarity chance +3%.","Companion bite damage +4% per rank.","Loot rarity chance +2% per rank.","Rarity upgrade chance +1% per rank.","Equipment chance +0.1875 points per rank.","Rarity upgrade chance +3% per rank.","Companion attack speed +4% per rank.","Final boss equipment chance +0.5 points per rank.","Rarity upgrade chance +2% per rank.","Companion bite damage +5% per rank.","Kill rally lasts +2s per rank.","Rarity upgrade chance +4% per rank.","Equipment chance +0.25 points per rank.","Loot rarity upgrade chance +5% per rank.","Boss loot rarity upgrade chance +5% per rank.","Rarity upgrade chance +5% per rank.","Equipment chance +0.375 points per rank.","Final boss loot rarity chance +8%."}[i],new[]{"装備ドロップ率+0.125ポイント／ランク。","レア度上昇抽選+2%／ランク。","ボス戦のレア度+3%。","ペットかみつき威力+4%／ランク。","戦利品レア度+2%。","レア度上昇抽選+1%／ランク。","装備率+0.1875ポイント／ランク。","レア度上昇抽選+3%／ランク。","ペット攻撃速度+4%／ランク。","最終ボス装備率+0.5ポイント／ランク。","レア度上昇抽選+2%／ランク。","ペットかみつき威力+5%／ランク。","撃破奮起の持続+2秒／ランク。","レア度上昇抽選+4%／ランク。","装備率+0.25ポイント／ランク。","戦利品のレア度上昇抽選+5%。","ボス戦利品のレア度上昇率+5%。","レア度上昇抽選+5%／ランク。","装備率+0.375ポイント／ランク。","最終ボス戦利品レア度+8%。"}[i]);
        }
        int GetPetNodeIconIndex(int branch,int node)=>node<5?(node==0?(branch==0?20:branch==1?21:22):node==1?19:node==2?14:node==3?(branch==0?11:branch==1?2:4):23):new[]{20,11,19,14,11,23,21,2,4,21,2,23,22,4,23}[(node-5)%15];
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
            // LB/RB and horizontal navigation share one branch selector and never double-step.
            bool lb=Input.GetKeyDown(KeyCode.JoystickButton4)||Input.GetKeyDown(KeyCode.Q);
            bool rb=Input.GetKeyDown(KeyCode.JoystickButton5);
            bool branchSwitched=lb||rb;
            if(lb!=rb)
            {
                petSkillBranch=(petSkillBranch+(rb?1:2))%3;petSkillAutoScroll=true;
                PlayUserSound("skill",.1f);
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

            if(Mathf.Abs(vy)>=Mathf.Abs(hx)) petSkillNode=Mathf.Clamp(petSkillNode+(vy<0?1:-1),0,PetNodeCount-1);
            else if(!branchSwitched) petSkillBranch=hx>0?(petSkillBranch+1)%3:(petSkillBranch+2)%3;
            petSkillAutoScroll=true;
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
                { petSkillBranch=b; petSkillNode=0; petSkillAutoScroll=true; }
            }

            Color accent=PetSkillColors[petSkillBranch];

            // Left All-3-Branches Top-Down Visual Skill Tree Panel (Scrollable)
            Rect treeFrame = new Rect(42, 136, 904, 704);
            FieldUIArt.Plate(treeFrame, false);
            Box(new Rect(treeFrame.x+2, treeFrame.y+2, treeFrame.width-4, 30), new Color(.055f,.068f,.078f,.95f));
            ActionLabel(new Rect(treeFrame.x+16, treeFrame.y+3, 860, 26),
                T("COMPANION EVOLUTION TREE  ·  FIVE STAGES  ·  CHOOSE YOUR PATH",
                  "相棒進化ツリー  ·  5段階  ·  進む道を選択"),
                accent, 14);

            Rect scrollViewRect = new Rect(treeFrame.x+4, treeFrame.y+34, treeFrame.width-8, treeFrame.height-38);
            const float contentW = 870f;
            const float contentH = 80f + (PetNodeCount-1)*96f + 60f;
            if(Event.current.type==EventType.ScrollWheel&&scrollViewRect.Contains(Event.current.mousePosition)){petSkillScroll.y=Mathf.Clamp(petSkillScroll.y+Event.current.delta.y*24f,0,contentH-scrollViewRect.height);Event.current.Use();}
            if(Event.current.type==EventType.Layout&&petSkillAutoScroll){float selectedY=petSkillNode==0?55f:80f+petSkillNode*96f;if(selectedY<petSkillScroll.y+60f)petSkillScroll.y=Mathf.Max(0,selectedY-60f);else if(selectedY>petSkillScroll.y+scrollViewRect.height-60f)petSkillScroll.y=Mathf.Min(contentH-scrollViewRect.height,selectedY-scrollViewRect.height+60f);petSkillAutoScroll=false;}
            petSkillScroll = GUI.BeginScrollView(scrollViewRect, petSkillScroll, new Rect(0, 0, contentW, contentH), false, false);

            // Each branch is a 25-stage vertical path; connectors and nodes share content coordinates.
            for(int b=0;b<3;b++)
            {
                Color c=PetSkillColors[b];float x=148f+b*287f;
                Rect col=new Rect(x-134f,8f,268f,contentH-16f);
                Box(col,b==petSkillBranch?new Color(c.r*.12f,c.g*.12f,c.b*.12f,.65f):new Color(.03f,.038f,.044f,.5f));
                ActionLabel(new Rect(col.x+8,12,252,24),PetBranchName(b)+(PetEquippedSkill==b+1?" ★":""),b==petSkillBranch?c:Mist,15,TextAnchor.MiddleCenter);
                for(int n=0;n<PetNodeCount;n++)
                {
                    Vector2 pos=new Vector2(x,n==0?55f:80f+n*96f);
                    if(n>0){Vector2 parent=new Vector2(x,n==1?55f:80f+(n-1)*96f);DrawSkillConnectorPipe(parent,pos,PetSkillRank(b,n)>0,PetSkillState.IsUnlocked(b,n),c);}
                    int rank=PetSkillRank(b,n);bool available=PetSkillState.IsUnlocked(b,n);
                    if(DrawBorderlandsSkillNode(pos,n==0?46f:44f,GetPetNodeIconIndex(b,n),n==0||n>=20?2:0,PetNodeName(n),rank,PetSkillState.MaxRank(n),available,petSkillBranch==b&&petSkillNode==n,c))
                    {petSkillBranch=b;petSkillNode=n;petSkillAutoScroll=true;PlayUserSound("skill",.08f);}
                }
            }            GUI.EndScrollView();

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
            int selectedRank=PetSkillRank(petSkillBranch,petSkillNode),selectedMax=PetSkillState.MaxRank(petSkillNode);
            bool unlocked=PetSkillState.IsUnlocked(petSkillBranch,petSkillNode);
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
