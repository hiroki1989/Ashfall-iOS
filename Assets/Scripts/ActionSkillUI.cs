using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool actionSkillInventoryOpen;
        Texture2D actionSkillArt;
        int actionTreeKind, actionTreeBranch, actionTreeTier;
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

        void DrawActionSkillTree()
        {
            int kind=Mathf.Clamp(actionTreeKind,0,2),branch=Mathf.Clamp(actionTreeBranch,0,2),tier=Mathf.Clamp(actionTreeTier,0,4);
            var selected=(ActionSkillKind)kind;Color accent=ActionTreeColors[kind];
            Box(new Rect(0,0,1440,900),new Color(.045f,.053f,.059f,.99f));
            ActionLabel(new Rect(46,22,760,55),T("ACTION SKILL TREE","アクションスキルツリー"),UiPaper,34);
            ActionLabel(new Rect(46,77,700,34),T("AVAILABLE POINTS  ","残りポイント  ")+ActionSkillAvailablePoints,accent,23);
            if(ActionButton(new Rect(1195,24,200,70),T("BACK","戻る"),false,accent)) {debtSkillTreePage=false;return;}
            for(int i=0;i<3;i++)
            {
                var type=(ActionSkillKind)i;
                if(ActionButton(new Rect(48+i*310,125,294,68),ActionSkillName(type),kind==i,ActionTreeColors[i],24))
                {actionTreeKind=i;actionTreeBranch=0;actionTreeTier=0;kind=i;selected=type;accent=ActionTreeColors[i];branch=tier=0;}
            }
            ActionLabel(new Rect(955,129,430,55),T("SELECT A SKILL","スキルを選択"),Mist,19);
            DrawActionSkillArt(new Rect(1305,120,72,72),kind);
            for(int b=0;b<3;b++)
            {
                Rect column=new Rect(48+b*300,210,284,573);FieldUIArt.Plate(column,false);
                Box(new Rect(column.x+14,column.y+13,3,40),accent);
                ActionLabel(new Rect(column.x+27,column.y+10,column.width-42,44),ActionBranchName(kind,b),UiPaper,22);
                for(int t=0;t<5;t++)
                {
                    Rect node=new Rect(column.x+14,column.y+66+t*96,column.width-28,84);
                    int rank=ActionSkillRank(selected,b,t),max=ActionSkillMaxRank(t);
                    bool active=actionTreeBranch==b&&actionTreeTier==t;
                    bool available=ActionSkillTierAvailable(selected,b,t);
                    FieldUIArt.Plate(node,active);
                    if(rank>0)Box(new Rect(node.x+4,node.y+5,3,node.height-10),accent);
                    ActionLabel(new Rect(node.x+14,node.y+8,node.width-64,46),ActionSkillNodeTitle(selected,b,t),available?UiPaper:Mist,19,TextAnchor.MiddleLeft,true);
                    ActionLabel(new Rect(node.x+15,node.y+54,node.width-30,23),rank+" / "+max,rank>0?accent:Mist,17);
                    if(!available)ActionLabel(new Rect(node.xMax-36,node.y+9,25,30),"•",Mist,22,TextAnchor.MiddleCenter);
                    if(GUI.Button(node,GUIContent.none,GUIStyle.none)){actionTreeBranch=b;actionTreeTier=t;branch=b;tier=t;}
                }
            }
            var panel=new Rect(955,210,438,573);FieldUIArt.Plate(panel,false);
            int selectedRank=ActionSkillRank(selected,branch,tier),maxRank=ActionSkillMaxRank(tier);
            bool canUnlock=ActionSkillTierAvailable(selected,branch,tier)&&selectedRank<maxRank&&ActionSkillAvailablePoints>0;
            ActionLabel(new Rect(978,232,385,35),ActionBranchName(kind,branch)+" / "+(tier+1),accent,19);
            ActionLabel(new Rect(978,276,385,80),ActionSkillNodeTitle(selected,branch,tier),UiPaper,27,TextAnchor.MiddleLeft,true);
            ActionLabel(new Rect(978,362,385,205),ActionSkillNodeDescription(selected,branch,tier),UiPaper,22,TextAnchor.UpperLeft,true);
            ActionLabel(new Rect(978,595,385,48),T("RANK ","ランク ")+selectedRank+" / "+maxRank,accent,23);
            string button=selectedRank>=maxRank?T("MAX RANK","最大ランク"):T("UPGRADE / 1 POINT","強化 / 1ポイント");
            if(ActionButton(new Rect(978,670,390,78),button,canUnlock,accent,22,canUnlock))TryUpgradeActionSkill(selected,branch,tier);
            if(!canUnlock&&selectedRank<maxRank)ActionLabel(new Rect(978,751,390,28),ActionSkillAvailablePoints<=0?T("Not enough points","ポイントが足りません"):tier==4?T("Previous tier: rank 6 required","前段階をランク6にする"):T("Previous tier: rank 3 required","前段階をランク3にする"),Mist,17);
        }

        void DrawActionSkillInventory()
        {
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
                if(ActionButton(new Rect(card.x+28,card.y+420,378,78),equipped?T("EQUIPPED","装備中"):T("EQUIP","装備する"),equipped,accent,22,!equipped))SelectActionSkill(kind);
            }
        }

        void DrawActionSkillHUD()
        {
            if(!Active||InventoryOpen)return;
            bool mobile=MobileTouchControls.Enabled;
            Rect r=mobile?new Rect(975,375,165,80):new Rect(1035,800,145,70);
            bool ready=ActionSkillPoints>=ActionSkillChargeMax;
            Color accent=ActionTreeColors[Mathf.Clamp((int)ActionSkillSelected,0,2)];
            FieldUIArt.Plate(r,ready);
            Box(new Rect(r.x+9,r.yMax-12,r.width-18,4),new Color(.18f,.20f,.21f));
            Box(new Rect(r.x+9,r.yMax-12,(r.width-18)*Mathf.Clamp01(ActionSkillPoints/(float)Mathf.Max(1,ActionSkillChargeMax)),4),accent);
            var title=ActionSkillName(ActionSkillSelected);
            ActionLabel(new Rect(r.x+5,r.y+4,r.width-10,28),title,ready?UiPaper:Mist,mobile?16:14,TextAnchor.MiddleCenter);
            ActionLabel(new Rect(r.x+5,r.y+30,r.width-10,25),ready?(mobile?T("READY","発動可能"):T("READY / V","発動 / V")):ActionSkillPoints+" / "+ActionSkillChargeMax,ready?accent:Mist,16,TextAnchor.MiddleCenter);
            bool previous=GUI.enabled;GUI.enabled=previous&&ready;
            if(GUI.Button(r,GUIContent.none,GUIStyle.none))TryUseActionSkill();
            GUI.enabled=previous;
        }
    }
}
