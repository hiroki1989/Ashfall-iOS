using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool actionSkillInventoryOpen, actionSkillInventoryTree;
        Texture2D actionSkillArt, actionSkillDisc, actionTreeRing;
        int actionTreeKind, actionTreeBranch, actionTreeTier;
        int actionTreeSlot = -1;
        Vector2 actionTreeDescriptionScroll;
        string actionTreeLastDescription;
        static readonly Color[] ActionTreeColors = {
            new Color(.38f,.70f,.76f), new Color(.91f,.53f,.27f), new Color(.44f,.76f,.53f)
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
        static readonly Color[] ActionBranchHighlights = {new Color(.31f,.70f,.83f),new Color(.92f,.61f,.28f),new Color(.65f,.46f,.82f)};
        Color ActionBranchColor(int kind,int branch)
        {
            return Color.Lerp(ActionTreeColors[kind],ActionBranchHighlights[branch],.58f);
        }
        bool DrawActionTreeNode(Rect r, string title, int rank, int max, bool available, bool selected, Color accent, bool diamond=false)
        {
            Color edge=selected?UiPaper:rank>0?accent:available?new Color(.58f,.64f,.64f):new Color(.28f,.32f,.33f);
            float cx=r.x+19,cy=r.center.y;
            if(rank>0)Diamond(cx,cy,20,new Color(accent.r,accent.g,accent.b,.12f));
            if(diamond)
            {
                Diamond(cx,cy,17,edge);
                Diamond(cx,cy,13,new Color(.065f,.078f,.085f,.98f));
                Diamond(cx,cy,rank>0?7:4,rank>0?accent:edge);
            }
            else
            {
                Color old=GUI.color;GUI.color=edge;
                GUI.DrawTexture(new Rect(cx-18,cy-18,36,36),ActionTreeRing());GUI.color=old;
                Diamond(cx,cy,rank>0?7:4,rank>0?accent:edge);
            }
            if(selected)Box(new Rect(r.x+41,r.yMax-2,r.width-44,2),accent);
            ActionLabel(new Rect(r.x+42,r.y,r.width-44,r.height),title+"  "+rank+"/"+max,rank>0?accent:available?UiPaper:Mist,20,TextAnchor.MiddleLeft);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
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

        void DrawActionSkillTree()
        {
            int kind=Mathf.Clamp(actionTreeKind,0,2), branch=Mathf.Clamp(actionTreeBranch,0,2), tier=Mathf.Clamp(actionTreeTier,0,4);
            var selected=(ActionSkillKind)kind; Color accent=ActionTreeColors[kind];
            Box(new Rect(0,0,1440,900),new Color(.045f,.053f,.059f,.99f));
            ActionLabel(new Rect(46,20,760,48),T("ACTION SKILL TREE","アクションスキルツリー"),UiPaper,32);
            ActionLabel(new Rect(48,67,700,32),T("AVAILABLE POINTS  ","残りポイント  ")+ActionSkillAvailablePoints,accent,22);
            if(ActionButton(new Rect(1195,20,200,65),T("BACK","戻る"),false,accent)) {if(actionSkillInventoryTree)actionSkillInventoryTree=false;else debtSkillTreePage=false;return;}
            for(int i=0;i<3;i++)
                if(ActionButton(new Rect(48+i*300,105,285,55),ActionSkillName((ActionSkillKind)i),kind==i,ActionTreeColors[i],22))
                {actionTreeKind=i;actionTreeBranch=0;actionTreeTier=0;actionTreeSlot=-1;kind=i;selected=(ActionSkillKind)i;accent=ActionTreeColors[i];branch=tier=0;}
            for(int b=0;b<3;b++)
                if(ActionButton(new Rect(48+b*300,172,285,55),ActionBranchName(kind,b),branch==b,ActionBranchColor(kind,b),20))
                {actionTreeBranch=b;actionTreeTier=0;actionTreeSlot=-1;branch=b;tier=0;}
            accent=ActionBranchColor(kind,branch);
            Box(new Rect(48,240,870,580),new Color(.065f,.074f,.078f,.96f));
            ActionLabel(new Rect(68,245,800,27),T("BASE  →  BRANCH  →  SPECIALIZATION","基礎  →  分岐  →  特化"),accent,16);
            for(int t=0;t<5;t++)
            {
                float cy=305+t*101;
                Rect root=new Rect(68,cy-23,145,46), first=new Rect(245,cy-23,145,46);
                Rect upper=new Rect(430,cy-47,145,44), lower=new Rect(430,cy+3,145,44), end=new Rect(635,cy-23,145,46);
                Color line=new Color(accent.r,accent.g,accent.b,.42f);
                if(t<4)Line(new Vector2(root.center.x,cy+23),new Vector2(root.center.x,cy+78),line,2);
                Line(new Vector2(root.xMax,cy),new Vector2(first.x,cy),line,2);
                Line(new Vector2(first.xMax,cy),new Vector2(upper.x,upper.center.y),line,2);
                Line(new Vector2(first.xMax,cy),new Vector2(lower.x,lower.center.y),line,2);
                Line(new Vector2(upper.xMax,upper.center.y),new Vector2(end.x,cy),line,2);
                Line(new Vector2(lower.xMax,lower.center.y),new Vector2(end.x,cy),line,2);
                if(DrawActionTreeNode(root,(t+1)+"-0",ActionSkillRank(selected,branch,t),ActionSkillMaxRank(t),ActionSkillTierAvailable(selected,branch,t),tier==t&&actionTreeSlot<0,accent)){actionTreeTier=t;actionTreeSlot=-1;tier=t;}
                Rect[] nodes={first,upper,lower,end};
                for(int slot=0;slot<4;slot++)
                    if(DrawActionTreeNode(nodes[slot],(t+1)+"-"+((char)('A'+slot)),ActionSkillExtraRank(selected,branch,t,slot),1,ActionSkillExtraAvailable(selected,branch,t,slot),tier==t&&actionTreeSlot==slot,accent,slot==3)){actionTreeTier=t;actionTreeSlot=slot;tier=t;}
            }
            Rect panel=new Rect(955,172,438,648);FieldUIArt.Plate(panel,false);
            int chosenSlot=actionTreeSlot;
            bool extra=chosenSlot>=0;
            int rank=extra?ActionSkillExtraRank(selected,branch,tier,chosenSlot):ActionSkillRank(selected,branch,tier);
            int max=extra?1:ActionSkillMaxRank(tier);
            bool available=extra?ActionSkillExtraAvailable(selected,branch,tier,chosenSlot):ActionSkillTierAvailable(selected,branch,tier);
            bool canUnlock=available&&rank<max&&ActionSkillAvailablePoints>0;
            string title=extra?ActionSkillExtraTitle(selected,branch,tier,chosenSlot):ActionSkillNodeTitle(selected,branch,tier);
            string desc=extra?ActionSkillExtraDescription(selected,branch,tier,chosenSlot):ActionSkillNodeDescription(selected,branch,tier);
            string nodeId=(tier+1)+"-"+(extra?((char)('A'+chosenSlot)).ToString():"0");
            ActionLabel(new Rect(978,198,385,38),ActionBranchName(kind,branch)+" / "+nodeId,accent,22);
            ActionLabel(new Rect(978,245,385,76),title,UiPaper,25,TextAnchor.MiddleLeft,true);
            ActionTreeDescription(new Rect(978,330,385,235),desc,UiPaper,22);
            ActionLabel(new Rect(978,580,385,42),T("RANK ","ランク ")+rank+" / "+max,accent,22);
            string button=rank>=max?T("MAX RANK","最大ランク"):T("UPGRADE / 1 POINT","強化 / 1ポイント");
            if(ActionButton(new Rect(978,660,390,72),button,canUnlock,accent,21,canUnlock))
            {if(extra)TryUpgradeActionSkillExtra(selected,branch,tier,chosenSlot);else TryUpgradeActionSkill(selected,branch,tier);}
            if(!canUnlock&&rank<max)
            {
                string reason=ActionSkillAvailablePoints<=0?T("Not enough points","ポイントが足りません"):
                    extra?(chosenSlot==0?T("Acquire this tier's base skill","同段階の基礎スキルが必要"):chosenSlot==3?T("Acquire either preceding branch","手前の分岐をいずれか取得"):T("Acquire the branch origin","分岐の起点を取得")):
                    tier==4?T("Previous tier: rank 6 required","前段階をランク6にする"):T("Previous tier: rank 3 required","前段階をランク3にする");
                ActionLabel(new Rect(978,740,390,48),reason,Mist,17,TextAnchor.MiddleLeft,true);
            }
        }

        void DrawActionSkillInventory()
        {
            if(actionSkillInventoryTree){DrawActionSkillTree();return;}
            Box(new Rect(0,0,1440,900),new Color(.045f,.053f,.059f,.99f));
            ActionLabel(new Rect(50,28,930,60),T("ACTION SKILL","アクションスキル"),UiPaper,36);
            ActionLabel(new Rect(52,95,1000,40),T("Changing skills resets your action gauge.","スキルを切り替えるとゲージは0になります。"),Mist,22);
            if(ActionButton(new Rect(1195,26,200,70),T("BACK","戻る"),false,EqAccent)){actionSkillInventoryOpen=false;return;}
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
