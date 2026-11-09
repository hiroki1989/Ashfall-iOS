using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool actionSkillInventoryOpen, actionSkillInventoryTree;
        Texture2D actionSkillArt, actionSkillDisc, actionTreeRing;
        Texture2D skillTreeIconsTex, skillNodeFramesTex;
        int actionTreeKind, actionTreeBranch, actionTreeTier;
        int actionTreeSlot = -1;
        Vector2 actionTreeScroll, actionTreeDescriptionScroll;
        Vector2 actionTreeMaxScroll = new Vector2(0f, 5000f), petSkillMaxScroll = new Vector2(0f, 2000f); // set each frame by the tree draw; used by touch drag
        string actionTreeLastDescription;
        float skillTreeNavCooldown;
        const float ActionTreeTierPitch = 1100f;
        const float ActionTreeContentHeight = 58f + 4f * ActionTreeTierPitch + 1030f;
        const int ActionTreeNodeRows = 14;

        static readonly Color[] ActionTreeColors = {
            new Color(.32f,.76f,.86f), new Color(.96f,.52f,.22f), new Color(.38f,.84f,.54f)
        };

        string ActionSkillName(ActionSkillKind kind)
        {
            switch(kind)
            {
                case ActionSkillKind.Mechanic: return T("MECHANIC", "メカニック");
                case ActionSkillKind.Bomber: return T("BOMBER", "ボマー");
                default: return T("MEDIC", "メディック");
            }
        }
        string ActionSkillSummary(ActionSkillKind kind)
        {
            switch(kind)
            {
                case ActionSkillKind.Mechanic: return T("Deploy a turret to fight alongside you.", "タレットを呼び、共に戦う。");
                case ActionSkillKind.Bomber: return T("Throw grenades into the fight.", "グレネードを投げ、敵を攻撃する。");
                default: return T("Create a field that restores health.", "回復エリアを作り、HPを回復する。");
            }
        }
        void DrawActionSkillArt(Rect rect,int kind)
        {
            if(!actionSkillArt)actionSkillArt=Resources.Load<Texture2D>("Art/action-skills-v1");
            if(actionSkillArt){GUI.color=Color.white;GUI.DrawTextureWithTexCoords(rect,actionSkillArt,new Rect(Mathf.Clamp(kind,0,2)/3f,0,1f/3f,1),true);}
        }
        string ActionBranchName(int kind,int branch)
        {
            string[,] en={{"POWER","MOBILITY","CONTROL"},{"BARRAGE","RADIUS","FORCE"},{"HEALING","REACH","SUPPORT"}};
            string[,] ja={{"威力","機動","制圧"},{"弾数","範囲","威力"},{"回復","範囲","支援"}};
            return T(en[kind,branch],ja[kind,branch]);
        }
        void ActionLabel(Rect r,string value,Color color,int size,TextAnchor alignment=TextAnchor.MiddleLeft,bool wrap=false)
        {
            var style=new GUIStyle(label){fontSize=size,fontStyle=FontStyle.Normal,alignment=alignment,wordWrap=wrap,clipping=TextClipping.Clip};
            style.normal.textColor=color;GUI.Label(r,value,style);
        }
        bool ActionButton(Rect r,string value,bool chosen,Color accent,int size=22,bool enabled=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            FieldUIArt.Plate(r,chosen);
            if(chosen)Box(new Rect(r.x+4,r.yMax-4,r.width-8,3),accent);
            var style=new GUIStyle(label){fontSize=size,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal,wordWrap=true,clipping=TextClipping.Clip};
            style.normal.textColor=enabled?UiPaper:Mist;style.hover.textColor=enabled?accent:Mist;style.active.textColor=style.hover.textColor;
            bool clicked=GUI.Button(r,value,style);GUI.enabled=previous;return clicked;
        }

        Texture2D ActionTreeRing()
        {
            if(actionTreeRing)return actionTreeRing;
            const int size=64; var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
            tex.name="ActionTreeRing";tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+0.5f-size*.5f)/(size*.5f),dy=(y+0.5f-size*.5f)/(size*.5f);
                float d=Mathf.Sqrt(dx*dx+dy*dy);
                float alpha=Mathf.Clamp01((1f-d)*size*.5f)*Mathf.Clamp01((d-.74f)*size*.5f);
                pixels[y*size+x]=new Color(1,1,1,alpha);
            }
            tex.SetPixels32(pixels);tex.Apply(false,true);actionTreeRing=tex;return tex;
        }

        static readonly Color[] ActionBranchHighlights = {new Color(.28f,.78f,.92f),new Color(.96f,.58f,.24f),new Color(.72f,.46f,.90f)};
        Color ActionBranchColor(int kind,int branch)
        {
            return Color.Lerp(ActionTreeColors[kind],ActionBranchHighlights[branch],.58f);
        }

        void EnsureSkillTreeTextures()
        {
            if(!skillTreeIconsTex)skillTreeIconsTex=Resources.Load<Texture2D>("Art/UI/skill_tree_icons");
            if(!skillNodeFramesTex)skillNodeFramesTex=Resources.Load<Texture2D>("Art/UI/skill_node_frames");
        }

        public void DrawSkillAtlasIcon(Rect r, int iconIndex, Color tint)
        {
            EnsureSkillTreeTextures();
            if(!skillTreeIconsTex)return;
            int idx=Mathf.Clamp(iconIndex,0,31);
            int col=idx%8, row=idx/8;
            Rect uv=new Rect(col/8f, 1f-(row+1)/4f, 1f/8f, 1f/4f);
            Color old=GUI.color; GUI.color=tint;
            GUI.DrawTextureWithTexCoords(r, skillTreeIconsTex, uv, true);
            GUI.color=old;
        }

        public void DrawSkillNodeFrame(Rect r, int frameKind, Color tint)
        {
            EnsureSkillTreeTextures();
            if(!skillNodeFramesTex)return;
            int k=Mathf.Clamp(frameKind,0,3);
            Rect uv=new Rect(k/4f, 0f, 1f/4f, 1f);
            Color old=GUI.color; GUI.color=tint;
            GUI.DrawTextureWithTexCoords(r, skillNodeFramesTex, uv, true);
            GUI.color=old;
        }

        int GetActionNodeIconIndex(ActionSkillKind kind, int branch, int tier, int slot)
        {
            if(slot < 0)
            {
                if(tier == 4) return 24; // Capstone Crown
                if(tier == 1)
                {
                    if(kind == ActionSkillKind.Mechanic) return branch==0?0:branch==1?6:2;
                    if(kind == ActionSkillKind.Bomber) return branch==0?0:branch==1?9:4;
                    return branch==0?11:branch==1?3:13;
                }
                if(tier == 3 && branch == 1) return kind==ActionSkillKind.Medic?11:1;
                if(kind == ActionSkillKind.Mechanic) return branch==0?5:branch==1?6:1;
                if(kind == ActionSkillKind.Bomber) return branch==0?10:branch==1?9:8;
                return branch==0?11:branch==1?12:13;
            }
            string id = ExtraEffect(kind, branch, tier, slot);
            switch(id){case "handgun_damage":return 0;case "shotgun_damage":return 8;case "assault_damage":return 0;case "heavy_damage":return 1;case "turret_overclock":return 1;case "grenade_chain":return 8;case "medic_purge":return 11;}
            switch(id)
            {
                case "handgun_damage": return 0;
                case "shotgun_damage": return 8;
                case "assault_damage": return 0;
                case "heavy_damage": return 1;
            }
            switch(id)
            {
                case "turret_damage": case "turret_near_damage": return 5;
                case "turret_rate": case "fire_rate": return 1;
                case "turret_range": case "accuracy": case "sniper_damage": return 3;
                case "turret_duration": case "medic_duration": return 6;
                case "turret_double": return 7;
                case "turret_focus": case "critical_chance": case "critical_damage": case "turret_near_critical": return 4;
                case "turret_follow": case "medic_follow": case "move_speed": return 14;
                case "turret_element": case "electric_damage": case "action_charge": case "turret_hit_charge": case "grenade_hit_charge": case "medic_field_charge": case "turret_near_element": return 15;
                case "grenade_damage": case "explosive_damage": case "grenade_element": return 8;
                case "grenade_radius": case "grenade_suppress": case "grenade_slow": return 9;
                case "grenade_count": return 10;
                case "turret_overclock":return 1;
                case "grenade_chain":return 8;
                case "medic_purge":return 11;
                case "grenade_heal": case "grenade_hit_heal": case "medic_heal": case "max_health": return 11;
                case "medic_radius": case "medic_field_damage": return 12;
                case "medic_guard": case "damage_reduction": case "turret_near_guard": return 13;
                case "medic_damage": case "medic_debuff": case "poison_damage": return 17;
                case "fire_damage": return 16;
                case "reload_speed": case "turret_near_reload": case "medic_field_reload": return 2;
                case "pet_damage": case "pet_speed": return 19;
                default: return 0;
            }
        }

        public void DrawSkillConnectorPipe(Vector2 from, Vector2 to, bool unlocked, bool available, Color accent)
        {
            Color outer = unlocked ? new Color(accent.r*.45f, accent.g*.45f, accent.b*.45f, .95f) :
                          available ? new Color(.25f, .30f, .33f, .85f) : new Color(.14f, .16f, .18f, .75f);
            Color core = unlocked ? Color.Lerp(accent, Color.white, .32f) :
                         available ? new Color(.48f, .54f, .56f, .75f) : new Color(.22f, .25f, .27f, .55f);
            // Orthogonal elbow (down / across / down) drawn with axis-aligned boxes only.
            // Rotated GUI.matrix lines break inside GUI.BeginScrollView clipping and showed up as
            // stray diagonal streaks that slid against the scrolling nodes, so no rotation is used here.
            float midY = Mathf.Abs(from.x - to.x) < .5f ? to.y : (from.y + to.y) * .5f;
            Vector2 a = from, b = new Vector2(from.x, midY), c = new Vector2(to.x, midY), d = to;
            float ow = unlocked ? 7f : 5f, cw = unlocked ? 3.2f : 2f;
            OrthoSeg(a, b, outer, ow); OrthoSeg(b, c, outer, ow); OrthoSeg(c, d, outer, ow);
            OrthoSeg(a, b, core, cw); OrthoSeg(b, c, core, cw); OrthoSeg(c, d, core, cw);
            if(unlocked)
            {
                float l1 = Mathf.Abs(b.y - a.y), l2 = Mathf.Abs(c.x - b.x), l3 = Mathf.Abs(d.y - c.y);
                float total = Mathf.Max(1f, l1 + l2 + l3);
                float s = Mathf.Repeat(Time.unscaledTime * 1.15f, 1f) * total;
                Vector2 dot = s < l1 ? Vector2.Lerp(a, b, s / Mathf.Max(1f, l1)) :
                              s < l1 + l2 ? Vector2.Lerp(b, c, (s - l1) / Mathf.Max(1f, l2)) :
                              Vector2.Lerp(c, d, (s - l1 - l2) / Mathf.Max(1f, l3));
                Box(new Rect(dot.x - 3.5f, dot.y - 3.5f, 7f, 7f), Color.white);
            }
        }

        void OrthoSeg(Vector2 p, Vector2 q, Color color, float w)
        {
            float x0 = Mathf.Min(p.x, q.x), x1 = Mathf.Max(p.x, q.x), y0 = Mathf.Min(p.y, q.y), y1 = Mathf.Max(p.y, q.y);
            if(x1 - x0 < .5f && y1 - y0 < .5f) return;
            Box(new Rect(x0 - w * .5f, y0 - w * .5f, x1 - x0 + w, y1 - y0 + w), color);
        }

        bool DrawBorderlandsSkillNode(Vector2 center, float size, int iconIdx, int frameKind, string title, int rank, int max, bool available, bool selected, Color accent)
        {
            Rect r = new Rect(center.x - size*.5f, center.y - size*.5f, size, size);
            bool hover = r.Contains(Event.current.mousePosition);
            bool unlocked = rank > 0;
            bool maxed = rank >= max;

            // Outer glow for selected or unlocked nodes
            if(selected || unlocked || hover)
            {
                float glowSize = size * (selected ? 1.48f : 1.28f);
                Color glowCol = selected ? new Color(1f, .92f, .65f, .82f) :
                                maxed ? new Color(accent.r, accent.g, accent.b, .62f) :
                                unlocked ? new Color(accent.r, accent.g, accent.b, .36f) :
                                new Color(1f, 1f, 1f, .22f);
                DrawSkillNodeFrame(new Rect(center.x - glowSize*.5f, center.y - glowSize*.5f, glowSize, glowSize), 3, glowCol);
            }

            // Inner background tint
            Color bgCol = unlocked ? Color.Lerp(new Color(.08f,.10f,.12f,.96f), accent, maxed ? .38f : .22f) :
                          available ? new Color(.11f,.13f,.15f,.95f) : new Color(.05f,.06f,.07f,.92f);
            Box(new Rect(r.x + size*.14f, r.y + size*.14f, size*.72f, size*.72f), bgCol);

            // Frame
            Color frameCol = selected ? new Color(1f, .95f, .78f) :
                             maxed ? Color.Lerp(accent, new Color(1f,.88f,.45f), .45f) :
                             unlocked ? accent :
                             available ? new Color(.64f, .70f, .72f) : new Color(.28f, .31f, .33f);
            DrawSkillNodeFrame(r, frameKind, frameCol);

            // Icon
            float iconSize = size * .58f;
            Color iconCol = unlocked ? Color.white : available ? new Color(.84f, .88f, .90f, .92f) : new Color(.42f, .45f, .48f, .55f);
            DrawSkillAtlasIcon(new Rect(center.x - iconSize*.5f, center.y - iconSize*.5f - 2f, iconSize, iconSize), iconIdx, iconCol);

            // Selection bracket corners
            if(selected)
            {
                Color selCol = new Color(1f, .88f, .42f);
                float b = 9f;
                Box(new Rect(r.x-3, r.y-3, b, 2), selCol); Box(new Rect(r.x-3, r.y-3, 2, b), selCol);
                Box(new Rect(r.xMax+3-b, r.y-3, b, 2), selCol); Box(new Rect(r.xMax+1, r.y-3, 2, b), selCol);
                Box(new Rect(r.x-3, r.yMax+1, b, 2), selCol); Box(new Rect(r.x-3, r.yMax+3-b, 2, b), selCol);
                Box(new Rect(r.xMax+3-b, r.yMax+1, b, 2), selCol); Box(new Rect(r.xMax+1, r.yMax+3-b, 2, b), selCol);
            }

            // Rank Pill Badge at bottom center of node
            Rect badge = new Rect(center.x - 24f, r.yMax - 11f, 48f, 18f);
            Box(badge, new Color(.03f, .04f, .05f, .96f));
            Box(new Rect(badge.x, badge.y, badge.width, 1), frameCol);
            Box(new Rect(badge.x, badge.yMax-1, badge.width, 1), frameCol);
            Color rankCol = maxed ? new Color(1f, .86f, .36f) : unlocked ? accent : available ? UiPaper : Mist;
            ActionLabel(badge, rank + "/" + max, rankCol, 12, TextAnchor.MiddleCenter);

            // Short label beneath badge
            if(!string.IsNullOrEmpty(title))
            {
                Rect lbl = new Rect(center.x - 84f, badge.yMax + 1f, 168f, 20f);
                ActionLabel(lbl, title, selected ? Color.white : unlocked ? UiPaper : Mist, 13, TextAnchor.UpperCenter, false);
            }

            return GUI.Button(new Rect(r.x-4, r.y-4, r.width+8, r.height+22), GUIContent.none, GUIStyle.none);
        }

        void ActionTreeDescription(Rect area,string value,Color color,int size)
        {
            var style=new GUIStyle(label){fontSize=size,fontStyle=FontStyle.Normal,alignment=TextAnchor.UpperLeft,wordWrap=false,clipping=TextClipping.Clip};
            style.normal.textColor=color;
            if(value!=actionTreeLastDescription){actionTreeDescriptionScroll=Vector2.zero;actionTreeLastDescription=value;}
            var lines=new List<string>();string line="";
            foreach(char c in value??"")
            {
                if(c=='\r')continue;
                if(c=='\n'||style.CalcSize(new GUIContent(line+c)).x>area.width-18)
                {
                    lines.Add(line);line="";
                    if(c=='\n')continue;
                }
                line+=c;
            }
            if(line.Length>0)lines.Add(line);
            float height=Mathf.Max(area.height,lines.Count*35f);
            actionTreeDescriptionScroll=GUI.BeginScrollView(area,actionTreeDescriptionScroll,new Rect(0,0,area.width-18,height),false,height>area.height);
            for(int i=0;i<lines.Count;i++)GUI.Label(new Rect(0,i*35f,area.width-18,31),lines[i],style);
            GUI.EndScrollView();
        }

        void HandleActionTreeControllerInput(ActionSkillKind selected, int kind, int branch)
        {
            if(Event.current.type != EventType.Layout) return;
            float now = Time.unscaledTime;

            // Cancel / Back
            if(Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                if(actionSkillInventoryTree) actionSkillInventoryTree = false;
                else debtSkillTreePage = false;
                PlayUserSound("skill", .12f);
                return;
            }
            // Upgrade selected node (A / Cross / Return / Space)
            if(Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                int t = Mathf.Clamp(actionTreeTier, 0, 4);
                if(actionTreeSlot >= 0) TryUpgradeActionSkillExtra(selected, branch, t, actionTreeSlot);
                else TryUpgradeActionSkill(selected, branch, t);
            }
            // Switch Branch (LB / RB or Q / E)
            if(Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.Q))
            {
                actionTreeBranch = (branch + 2) % 3;
                PlayUserSound("skill", .1f);
            }
            if(Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.E))
            {
                actionTreeBranch = (branch + 1) % 3;
                PlayUserSound("skill", .1f);
            }
            // Switch Class (Y / Triangle or Tab)
            if(Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.Tab))
            {
                actionTreeKind = (kind + 1) % 3;
                PlayUserSound("skill", .12f);
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

            int curTier = Mathf.Clamp(actionTreeTier, 0, 4);
            int curSlot = actionTreeSlot;

            int subRow = curSlot < 0 ? 0 : Mathf.Clamp(curSlot + 1,1,ActionTreeNodeRows);

            if(Mathf.Abs(vy) >= Mathf.Abs(hx))
            {
                int dir = vy < 0 ? 1 : -1;
                subRow += dir;
                if(subRow > ActionTreeNodeRows)
                {
                    if(curTier < 4) { curTier++; subRow = 0; }
                    else subRow = 3;
                }
                else if(subRow < 0)
                {
                    if(curTier > 0) { curTier--; subRow = 3; }
                    else subRow = 0;
                }
                actionTreeTier = curTier;
                actionTreeSlot = subRow == 0 ? -1 : subRow - 1;
            }
            else
            {
                if(curSlot>=0)actionTreeSlot=Mathf.Clamp(curSlot+(hx>0?1:-1),0,13);
                else actionTreeBranch = hx > 0 ? (branch + 1) % 3 : (branch + 2) % 3;
            }
            EnsureActionNodeScrollVisible(actionTreeTier, actionTreeSlot);
            PlayUserSound("skill", .08f);
        }

        void EnsureActionNodeScrollVisible(int tier, int slot)
        {
            int[] rows={0,1,2,2,3,4,5,5,6,7,8,8,9,10};
            float subY = slot < 0 ? 0f : 65f + rows[slot+1]*84f;
            float nodeY = 58f + tier * ActionTreeTierPitch + subY;
            float viewH = 610f;
            if(nodeY - 80f < actionTreeScroll.y) actionTreeScroll.y = Mathf.Max(0f, nodeY - 80f);
            else if(nodeY + 95f > actionTreeScroll.y + viewH) actionTreeScroll.y = Mathf.Min(ActionTreeContentHeight - viewH, nodeY + 95f - viewH);
        }

        void DrawActionSkillTree()
        {
            int kind=Mathf.Clamp(actionTreeKind,0,2), branch=Mathf.Clamp(actionTreeBranch,0,2), tier=Mathf.Clamp(actionTreeTier,0,4);
            var selected=(ActionSkillKind)kind; Color classAccent=ActionTreeColors[kind];
            HandleActionTreeControllerInput(selected, kind, branch);
            kind=Mathf.Clamp(actionTreeKind,0,2); branch=Mathf.Clamp(actionTreeBranch,0,2); tier=Mathf.Clamp(actionTreeTier,0,4);
            selected=(ActionSkillKind)kind; classAccent=ActionTreeColors[kind];

            Box(new Rect(0,0,1440,900),new Color(.035f,.042f,.048f,.99f));
            ActionLabel(new Rect(46,16,580,44),T("ACTION SKILL TREE","アクションスキルツリー"),UiPaper,32);

            Rect ptsBox = new Rect(610, 18, 298, 44);
            FieldUIArt.Plate(ptsBox, true);
            ActionLabel(ptsBox, T("SKILL POINTS: ","残りポイント: ")+ActionSkillAvailablePoints, classAccent, 21, TextAnchor.MiddleCenter);

            if(ActionButton(new Rect(1195,16,200,56),T("BACK","戻る"),false,classAccent))
            {
                if(actionSkillInventoryTree)actionSkillInventoryTree=false;
                else debtSkillTreePage=false;
                return;
            }

            for(int i=0;i<3;i++)
            {
                Rect tab = new Rect(46+i*296, 72, 284, 52);
                if(ActionButton(tab, ActionSkillName((ActionSkillKind)i), kind==i, ActionTreeColors[i], 21))
                {
                    actionTreeKind=i; actionTreeBranch=0; actionTreeTier=0; actionTreeSlot=-1;
                    kind=i; selected=(ActionSkillKind)i; classAccent=ActionTreeColors[i]; branch=tier=0;
                    actionTreeScroll=Vector2.zero;
                }
            }

            for(int b=0;b<3;b++)
            {
                Color bCol = ActionBranchColor(kind,b);
                Rect bTab = new Rect(46+b*296, 132, 284, 48);
                if(ActionButton(bTab, ActionBranchName(kind,b), branch==b, bCol, 19))
                {
                    actionTreeBranch=b; actionTreeTier=0; actionTreeSlot=-1; branch=b; tier=0;
                    actionTreeScroll=Vector2.zero;
                }
            }

            Color accent=ActionBranchColor(kind,branch);

            Rect treeFrame = new Rect(46, 190, 882, 650);
            FieldUIArt.Plate(treeFrame, false);
            Box(new Rect(treeFrame.x+2, treeFrame.y+2, treeFrame.width-4, 32), new Color(.06f,.075f,.085f,.95f));
            ActionLabel(new Rect(treeFrame.x+18, treeFrame.y+4, 840, 28),
                T("TOP-DOWN SKILL TREE  ·  CORE  ▼  ORIGIN  ▼  SPLIT BRANCH  ▼  SYNERGY","上から下へ分岐  ·  基礎スキル  ▼  派生起点  ▼  左右分岐  ▼  特化統合"),
                accent, 15, TextAnchor.MiddleLeft);

            Rect scrollArea = new Rect(treeFrame.x+4, treeFrame.y+36, treeFrame.width-8, treeFrame.height-40);
            const float contentW = 846f;
            const float tierPitch = ActionTreeTierPitch;
            float contentH = ActionTreeContentHeight;
            float cx = contentW * 0.5f;

            if(Event.current.type == EventType.ScrollWheel && scrollArea.Contains(Event.current.mousePosition))
            {
                actionTreeScroll.y = Mathf.Clamp(actionTreeScroll.y + Event.current.delta.y * 28f, 0f, contentH - scrollArea.height);
                Event.current.Use();
            }

            actionTreeMaxScroll = new Vector2(Mathf.Max(0f, contentW - scrollArea.width), Mathf.Max(0f, contentH - scrollArea.height));
            actionTreeScroll = GUI.BeginScrollView(scrollArea, actionTreeScroll, new Rect(0, 0, contentW, contentH), false, true);

            for(int t=0;t<5;t++)
            {
                float topY = 58f + t * tierPitch;

                Rect tierBand = new Rect(16f, topY - 40f, contentW - 32f, tierPitch - 14f);
                Box(tierBand, t % 2 == 0 ? new Color(.055f,.066f,.074f,.55f) : new Color(.042f,.050f,.058f,.55f));
                Box(new Rect(tierBand.x, tierBand.y, 4f, tierBand.height), new Color(accent.r, accent.g, accent.b, .45f));
                ActionLabel(new Rect(tierBand.x + 14f, tierBand.y + 8f, 220f, 24f),
                    t == 4 ? T("TIER 5 // CAPSTONE","第5段階 // 最終奥義") : (T("TIER ","第") + (t+1) + T("", "段階")),
                    new Color(accent.r, accent.g, accent.b, .85f), 16, TextAnchor.MiddleLeft);

                Vector2 pRoot  = new Vector2(cx, topY);
                int rRoot = ActionSkillRank(selected, branch, t);
                int mRoot = ActionSkillMaxRank(t);
                bool avRoot = ActionSkillTierAvailable(selected, branch, t);

                for(int s=0;s<14;s++)
                {
                    int[] row={1,2,2,3,4,5,5,6,7,8,8,9,10,10};
                    int[] lane={0,0,-1,1,0,0,-1,1,0,0,-1,1,0,1};
                    float y=topY+65f+row[s]*84f;Vector2 p=new Vector2(cx+lane[s]*150f,y);
                    int nodeRank=ActionSkillExtraRank(selected,branch,t,s);bool nodeAvailable=ActionSkillExtraAvailable(selected,branch,t,s);
                    int parentSlot=s==0? -1:s==1||s==2?0:s==3?(ActionSkillExtraRank(selected,branch,t,1)>0?1:2):s==4?3:s==5||s==6?4:s==7?(ActionSkillExtraRank(selected,branch,t,5)>0?5:6):s==8?7:s==9||s==10?8:s==11?(ActionSkillExtraRank(selected,branch,t,9)>0?9:10):11;
                    Vector2 parent=parentSlot<0?pRoot:new Vector2(cx+lane[parentSlot]*150f,topY+65f+row[parentSlot]*84f);
                    DrawSkillConnectorPipe(parent,p,nodeRank>0,nodeAvailable,accent);
                    int icon=GetActionNodeIconIndex(selected,branch,t,s);
                    if(DrawBorderlandsSkillNode(p,48f,icon,s%4==3?2:1,ActionSkillExtraTitle(selected,branch,t,s),nodeRank,1,nodeAvailable,tier==t&&actionTreeSlot==s,accent))
                    {actionTreeTier=t;actionTreeSlot=s;PlayUserSound("skill",.08f);}
                }

                int icRoot = GetActionNodeIconIndex(selected, branch, t, -1);
                if(DrawBorderlandsSkillNode(pRoot, t==4 ? 68f : 62f, icRoot, t==4 ? 2 : 0, ActionSkillNodeTitle(selected, branch, t), rRoot, mRoot, avRoot, tier==t && actionTreeSlot<0, accent))
                { actionTreeTier=t; actionTreeSlot=-1; tier=t; PlayUserSound("skill",.08f); }

                if(t<4)
                {
                    bool nextUnl=ActionSkillRank(selected,branch,t+1)>0,nextAv=ActionSkillTierAvailable(selected,branch,t+1);
                    int[] rows={1,2,2,3,4,5,5,6,7,8,8,9,10,10};int[] lanes={0,0,-1,1,0,0,-1,1,0,0,-1,1,0,1};
                    Vector2 next=new Vector2(cx,topY+tierPitch);
                    for(int terminal=12;terminal<=13;terminal++)
                    {Vector2 end=new Vector2(cx+lanes[terminal]*150f,topY+65f+rows[terminal]*84f);DrawSkillConnectorPipe(end,next,ActionSkillExtraRank(selected,branch,t,terminal)>0,nextAv,accent);}
                }

            }

            GUI.EndScrollView();

            Rect panel=new Rect(948,132,446,708);FieldUIArt.Plate(panel,false);
            int chosenSlot=actionTreeSlot;
            bool extra=chosenSlot>=0;
            int rank=extra?ActionSkillExtraRank(selected,branch,tier,chosenSlot):ActionSkillRank(selected,branch,tier);
            int max=extra?1:ActionSkillMaxRank(tier);
            bool available=extra?ActionSkillExtraAvailable(selected,branch,tier,chosenSlot):ActionSkillTierAvailable(selected,branch,tier);
            bool canUnlock=available&&rank<max&&ActionSkillAvailablePoints>0;
            string title=extra?ActionSkillExtraTitle(selected,branch,tier,chosenSlot):ActionSkillNodeTitle(selected,branch,tier);
            string desc=extra?ActionSkillExtraDescription(selected,branch,tier,chosenSlot):ActionSkillNodeDescription(selected,branch,tier);
            string nodeId=(tier+1)+"-"+(extra?(chosenSlot+1).ToString():"0");

            int inspectIcon = GetActionNodeIconIndex(selected, branch, tier, chosenSlot);
            int inspectFrame = extra ? (chosenSlot%4==3 ? 2 : 1) : (tier==4 ? 2 : 0);
            Rect inspectIconRect = new Rect(972, 152, 74, 74);
            DrawSkillNodeFrame(inspectIconRect, inspectFrame, rank>0 ? accent : UiPaper);
            DrawSkillAtlasIcon(new Rect(986, 166, 46, 46), inspectIcon, Color.white);

            ActionLabel(new Rect(1058,154,315,28),ActionBranchName(kind,branch)+" // NODE "+nodeId,accent,19);
            ActionLabel(new Rect(1058,184,315,58),title,UiPaper,24,TextAnchor.MiddleLeft,true);

            Box(new Rect(972, 246, 398, 2), new Color(accent.r, accent.g, accent.b, .35f));

            ActionLabel(new Rect(978,262,385,34),T("SKILL EFFECT","スキル効果"),Mist,17);
            Box(new Rect(972,324,396,246),new Color(.03f,.038f,.044f,.92f));
            ActionTreeDescription(new Rect(978,330,385,235),desc,UiPaper,21);

            ActionLabel(new Rect(978,582,385,38),T("INVESTED RANK:  ","習得ランク:  ")+rank+" / "+max, rank>=max ? Gold : accent, 22);

            string button=rank>=max?T("MAX RANK","最大ランク"):T("UPGRADE / 1 POINT","強化 / 1ポイント");
            if(ActionButton(new Rect(976,632,390,68),button,canUnlock,accent,21,canUnlock))
            {if(extra)TryUpgradeActionSkillExtra(selected,branch,tier,chosenSlot);else TryUpgradeActionSkill(selected,branch,tier);}

            if(!canUnlock&&rank<max)
            {
                string reason=ActionSkillAvailablePoints<=0?T("Not enough points","ポイントが足りません"):
                    extra?(chosenSlot==0?T("Acquire this tier's base skill","同段階の基礎スキルが必要"):T("Acquire the previous node","直前のノードを取得")):
                    tier==4?T("Previous tier: rank 6 required","前段階をランク6にする"):T("Previous tier: rank 3 required","前段階をランク3にする");
                ActionLabel(new Rect(978,708,390,44),reason,new Color(.95f,.52f,.42f),17,TextAnchor.MiddleLeft,true);
            }

            ActionLabel(new Rect(46, 848, 1348, 36),
                T("[D-Pad / Stick] Select Node   ·   [A / Enter] Upgrade   ·   [LB / RB] Switch Branch   ·   [Y / Tab] Switch Class   ·   [B / Esc] Back",
                  "[十字キー / スティック] ノード選択   ·   [A / Enter] スキル強化   ·   [LB / RB] 分岐切替   ·   [Y / Tab] クラス切替   ·   [B / Esc] 戻る"),
                Mist, 16, TextAnchor.MiddleCenter);
        }

        void DrawActionSkillInventory()
        {
            if(actionSkillInventoryTree){DrawActionSkillTree();return;}
            Box(new Rect(0,0,1440,900),new Color(.045f,.053f,.059f,.99f));
            ActionLabel(new Rect(50,28,930,60),T("ACTION SKILL","アクションスキル"),UiPaper,36);
            ActionLabel(new Rect(52,95,1000,40),T("Changing skills resets your action gauge.","スキルを切り替えるとゲージは0になります。"),Mist,22);
            if(ActionButton(new Rect(1195,26,200,70),T("BACK","戻る"),false,EqAccent)){actionSkillInventoryOpen=false;return;}

            if(Event.current.type == EventType.Layout)
            {
                if(Input.GetKeyDown(KeyCode.JoystickButton1)) { actionSkillInventoryOpen=false; return; }
                if(Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.Return))
                {
                    actionTreeKind = (int)ActionSkillSelected;
                    actionTreeBranch = 0; actionTreeTier = 0; actionTreeSlot = -1;
                    actionSkillInventoryTree = true;
                }
            }

            for(int i=0;i<3;i++)
            {
                var kind=(ActionSkillKind)i;Color accent=ActionTreeColors[i];bool equipped=ActionSkillSelected==kind;
                Rect card=new Rect(52+i*458,185,434,530);FieldUIArt.Plate(card,equipped);
                Box(new Rect(card.x+20,card.y+20,4,55),accent);
                DrawActionSkillArt(new Rect(card.x+36,card.y+92,116,116),i);
                ActionLabel(new Rect(card.x+38,card.y+28,360,54),ActionSkillName(kind),UiPaper,29);
                ActionLabel(new Rect(card.x+36,card.y+220,360,115),ActionSkillSummary(kind),UiPaper,24,TextAnchor.UpperLeft,true);
                ActionLabel(new Rect(card.x+36,card.y+356,360,32),equipped?T("EQUIPPED","装備中"):T("AVAILABLE","選択可能"),equipped?accent:Mist,19);
                if(ActionButton(new Rect(card.x+28,card.y+400,183,78),equipped?T("EQUIPPED","装備中"):T("EQUIP","装備する"),equipped,accent,20,!equipped))SelectActionSkill(kind);
                if(ActionButton(new Rect(card.x+220,card.y+400,186,78),T("SKILL TREE","スキルツリー"),false,accent,19)){actionTreeKind=i;actionTreeBranch=0;actionTreeTier=0;actionTreeSlot=-1;actionSkillInventoryTree=true;}
            }
        }

        void DrawMobileActionSkill()
        {
            Rect use=MobileTouchControls.GetControlRect(MobileAction.Skill);
            if(use.width<=0)return;
            Matrix4x4 previousMatrix=GUI.matrix;
            GUI.matrix=Matrix4x4.identity;
            try
            {
                Rect r=use;
                if(!actionSkillDisc)
                {
                    const int size=64;
                    actionSkillDisc=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Action skill button disc",hideFlags=HideFlags.HideAndDontSave};
                    var pixels=new Color32[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {
                        float dx=(x+.5f-size*.5f)/(size*.5f),dy=(y+.5f-size*.5f)/(size*.5f);
                        byte alpha=(byte)(Mathf.Clamp01((1-Mathf.Sqrt(dx*dx+dy*dy))*size*.5f)*230);
                        pixels[y*size+x]=new Color32(17,27,32,alpha);
                    }
                    actionSkillDisc.SetPixels32(pixels);actionSkillDisc.Apply();
                }
                bool ready=ActionSkillPoints>=ActionSkillChargeMax;
                float charge=Mathf.Clamp01(ActionSkillPoints/(float)Mathf.Max(1,ActionSkillChargeMax));
                Color accent=ActionTreeColors[Mathf.Clamp((int)ActionSkillSelected,0,2)];
                if(ready)DrawActionReadyPulse(r,accent);
                GUI.color=Color.white;GUI.DrawTexture(r,actionSkillDisc);
                Vector2 center=r.center;float radius=r.width*.46f;
                const int segments=48;
                for(int i=0;i<segments;i++)
                {
                    float a=-Mathf.PI*.5f+i*Mathf.PI*2/segments,b=-Mathf.PI*.5f+(i+1)*Mathf.PI*2/segments;
                    Color color=(i+.5f)/segments<=charge?accent:new Color(.35f,.40f,.42f,.8f);
                    if(ready)color=Color.Lerp(color,Color.white,.45f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Time.unscaledTime*2.62f)),10));
                    Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color,Mathf.Max(2,r.width*.045f));
                }
                int fontSize=Mathf.Max(12,Mathf.RoundToInt(r.width*.15f));
                ActionLabel(new Rect(r.x+r.width*.1f,r.y+r.height*.28f,r.width*.8f,r.height*.25f),T("SKILL","スキル"),ready?UiPaper:Mist,fontSize,TextAnchor.MiddleCenter);
                ActionLabel(new Rect(r.x+r.width*.1f,r.y+r.height*.53f,r.width*.8f,r.height*.21f),ready?T("READY","発動可能"):Mathf.FloorToInt(ActionSkillPoints)+" / "+ActionSkillChargeMax,ready?accent:Mist,Mathf.Max(10,Mathf.RoundToInt(r.width*.12f)),TextAnchor.MiddleCenter);

            }
            finally{GUI.matrix=previousMatrix;GUI.color=Color.white;}
        }
        void DrawActionSkillHUD()
        {
            if(!Active||InventoryOpen)return;
            bool mobile=MobileTouchControls.Enabled;
            if(mobile){DrawMobileActionSkill();return;}
            Rect r=new Rect(650,810,235,64);
            bool ready=ActionSkillPoints>=ActionSkillChargeMax;
            Color accent=ActionTreeColors[Mathf.Clamp((int)ActionSkillSelected,0,2)];
            if(ready)DrawActionReadyPulse(r,accent);
            FieldUIArt.Plate(r,ready);
            Box(new Rect(r.x+9,r.yMax-12,r.width-18,4),new Color(.18f,.20f,.21f));
            Box(new Rect(r.x+9,r.yMax-12,(r.width-18)*Mathf.Clamp01(ActionSkillPoints/(float)Mathf.Max(1,ActionSkillChargeMax)),4),accent);
            var title=ActionSkillName(ActionSkillSelected);
            ActionLabel(new Rect(r.x+5,r.y+4,r.width-10,28),title,ready?UiPaper:Mist,mobile?16:14,TextAnchor.MiddleCenter);
            ActionLabel(new Rect(r.x+5,r.y+30,r.width-10,25),ready?(mobile?T("READY","発動可能"):T("READY / V","発動 / V")):Mathf.FloorToInt(ActionSkillPoints)+" / "+ActionSkillChargeMax,ready?accent:Mist,16,TextAnchor.MiddleCenter);
            bool previous=GUI.enabled;GUI.enabled=previous&&ready;
            if(GUI.Button(r,GUIContent.none,GUIStyle.none))TryUseActionSkill();
            GUI.enabled=previous;
        }
        void DrawActionReadyPulse(Rect rect,Color accent)
        {
            float phase=Mathf.Repeat(Time.unscaledTime,2.4f)/2.4f;
            float fade=(1-phase)*(1-phase);
            Vector2 center=rect.center;
            float radius=Mathf.Min(rect.width,rect.height)*(.48f+phase*.5f);
            Color wave=new Color(accent.r,accent.g,accent.b,.65f*fade);
            const int segments=40;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,wave,Mathf.Max(1.5f,rect.height*.025f));
            }
        }
    }
}
