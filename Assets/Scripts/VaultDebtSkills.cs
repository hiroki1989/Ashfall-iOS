using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const int InitialDebt = 1000000000;
        const int FirstDebtLevelRequirement = 1000000;
        const float DebtLevelRequirementGrowth = .12f;
        const int DebtLevelCap = 123;
        const string DebtSaveKey = "Vault.DebtSkills.v1";

        enum DebtSkillKind { Damage, FireRate, Accuracy, Reload, Ammo, Health, Recovery, WeaponSlot, DamageReduction, HealOnKill, AmmoPickup, ShopDiscount, KillChain, BossDamage, CriticalChance, CriticalDamage, AmmoCapacity }
        [Serializable] class DebtSkillSave { public int version, paid; public bool introSeen; public List<string> unlocked; }
        class DebtSkillNode
        {
            public string id, track, en, ja; public DebtSkillKind kind; public float value; public int modelFamily; public string[] requires;
            public DebtSkillNode(string id,string track,string en,string ja,DebtSkillKind kind,float value,int family=-1,params string[] requires)
            {this.id=id;this.track=track;this.en=en;this.ja=ja;this.kind=kind;this.value=value;modelFamily=family;this.requires=requires;}
        }

        readonly HashSet<string> unlockedDebtSkills = new HashSet<string>();
        readonly List<DebtSkillNode> debtSkillNodes = BuildDebtSkillNodes();
        int debtPaid; bool debtIntroSeen=true, debtPage, debtSkillTreePage; int debtTrack;
        int debtPaymentAmount;
        int DebtPaymentLimit=>Mathf.Max(0,Mathf.Min(Credits,DebtRemaining));
        void SetDebtPaymentFraction(float fraction)
        {
            int limit=DebtPaymentLimit;
            debtPaymentAmount=fraction>=.9999f?limit:Mathf.Clamp(Mathf.RoundToInt(limit*Mathf.Clamp01(fraction)/1000f)*1000,0,limit);
        }
        bool RepaySelectedDebt()
        {
            bool paid=RepayDebt(Mathf.Clamp(debtPaymentAmount,0,DebtPaymentLimit));
            debtPaymentAmount=Mathf.Min(debtPaymentAmount,DebtPaymentLimit);
            return paid;
        }
        Vector2 debtSkillScroll;
        readonly Vector3 debtCollectorPosition = new Vector3(-3,.65f,8);

        public int DebtLevel
        {
            get
            {
                int level=1;
                while(level<DebtLevelCap&&debtPaid>=DebtTotalForLevel(level+1))level++;
                return level;
            }
        }
        // Legacy expedition token saves now contribute to the same spendable pool.
        public int DebtSkillPoints => ActionSkillAvailablePoints;
        public int DebtRemaining => Mathf.Max(0,InitialDebt-debtPaid);
        public int DebtNextLevelRemaining => DebtLevel>=DebtLevelCap?0:Mathf.Max(0,DebtTotalForLevel(DebtLevel+1)-debtPaid);
        public int DebtNextLevelRequirement => DebtLevel>=DebtLevelCap?0:DebtTotalForLevel(DebtLevel+1)-DebtTotalForLevel(DebtLevel);
        // At the cap use the final level's full cost, since there is no next level.
        public int ActionSkillRespecCost => Mathf.Max(1,Mathf.RoundToInt((DebtLevel>=DebtLevelCap?DebtTotalForLevel(DebtLevelCap)-DebtTotalForLevel(DebtLevelCap-1):DebtNextLevelRequirement)*.1f));
        public bool TryRespecActionSkills()
        {
            if(ActionSkillSpentPoints<=0){Notify("NO SKILLS TO RESET","振り直すスキルがありません");return false;}
            if(Credits<ActionSkillRespecCost){Notify("NOT ENOUGH SCRAP","スクラップが足りません");return false;}
            if(!TrySpendCredits(ActionSkillRespecCost))return false;
            ResetActionSkills();RefreshPermanentStats();
            Notify("SKILLS RESET","スキルを振り直しました");return true;
        }
        public float DebtProgressRatio
        {
            get
            {
                int level=DebtLevel;
                if(level>=DebtLevelCap)return 1f;
                int levelStart=DebtTotalForLevel(level);
                int nextLevel=DebtTotalForLevel(level+1);
                return Mathf.Clamp01((debtPaid-levelStart)/(float)Mathf.Max(1,nextLevel-levelStart));
            }
        }
        int DebtRequirementForLevel(int level)=>Mathf.Max(1,Mathf.RoundToInt(FirstDebtLevelRequirement*(1f+DebtLevelRequirementGrowth*(level-2))));
        int DebtTotalForLevel(int level)
        {
            int total=0;
            for(int reached=2;reached<=Mathf.Min(level,DebtLevelCap);reached++)total+=DebtRequirementForLevel(reached);
            return Mathf.Min(InitialDebt,total);
        }
        // Preserve weapon slots already unlocked in a pre-migration save. The old
        // tree no longer grants combat bonuses or consumes new tree points.
        public int DebtExtraWeaponSlots => (unlockedDebtSkills.Contains("survival_slot1")?1:0)+(unlockedDebtSkills.Contains("survival_slot2")?1:0);
        bool DebtIntroReview => Array.IndexOf(Environment.GetCommandLineArgs(),"--debt-intro-review")>=0;
        public bool DebtIntroVisible => !debtIntroSeen && DebtIntroReview;
        public int DebtTotalPaid => debtPaid;

        void DebtSkillsInitialize()
        {
            debtPaid=0;debtIntroSeen=true;unlockedDebtSkills.Clear();
            if(smoke){debtIntroSeen=!DebtIntroReview;return;}
            if(!PlayerPrefs.HasKey(DebtSaveKey)){debtIntroSeen=false;SaveDebtSkills();return;}
            debtIntroSeen=false;
            try
            {
                var saved=JsonUtility.FromJson<DebtSkillSave>(PlayerPrefs.GetString(DebtSaveKey));
                if(saved==null||saved.version!=1){SaveDebtSkills();return;}
                debtPaid=Mathf.Clamp(saved.paid,0,InitialDebt);debtIntroSeen=saved.introSeen;
                if(saved.unlocked!=null)foreach(var id in saved.unlocked)if(FindDebtSkill(id)!=null)unlockedDebtSkills.Add(id);
                // Invalid old/corrupt selections cannot grant more points than earned milestones.
                while(unlockedDebtSkills.Count>DebtLevel-1){string last=null;foreach(var id in unlockedDebtSkills)last=id;if(last==null)break;unlockedDebtSkills.Remove(last);}
            }
            catch(Exception e){Debug.LogWarning("Debt skill load failed: "+e.GetType().Name);SaveDebtSkills();}
        }
        void SaveDebtSkills()
        {
            if(smoke)return;
            var data=new DebtSkillSave{version=1,paid=debtPaid,introSeen=debtIntroSeen,unlocked=new List<string>(unlockedDebtSkills)};
            PlayerPrefs.SetString(DebtSaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();
        }
        void MarkDebtIntroSeen(){debtIntroSeen=true;SaveDebtSkills();}
        public bool RepayDebt(int requested)
        {
            if(requested<=0||Credits<=0||DebtRemaining<=0)return false;
            int amount=Mathf.Min(Mathf.Min(requested,Credits),DebtRemaining);
            int before=DebtLevel;Credits-=amount;debtPaid+=amount;SaveDebtSkills();SaveProgression();PlayUserSound("transaction",.35f);
            if(DebtRemaining==0)Notify("Briggs: Balance zero. Talk to Vale about the collar.","ブリッグス：残高、ゼロ。首輪の話はヴェイルにしろ。");
            else if(DebtLevel>before)Notify("Briggs: Check your training points.","ブリッグス：次の段階に達した。訓練に使えるポイントを確認しておけ。");
            else Notify("Briggs: Received "+amount.ToString("N0")+". Balance "+DebtRemaining.ToString("N0"),"ブリッグス："+amount.ToString("N0")+"スクラップ、受領。残りは"+DebtRemaining.ToString("N0")+"だ。");return amount>0;
        }
        public bool TryUnlockDebtSkill(string id)
        {
            // Retained for old callers; legacy skills no longer spend points or grant effects.
            return false;
        }
        DebtSkillNode FindDebtSkill(string id)=>debtSkillNodes.Find(n=>n.id==id);
        bool DebtSkillAvailable(DebtSkillNode node)
        {if(node==null||unlockedDebtSkills.Contains(node.id))return false;foreach(var id in node.requires)if(!unlockedDebtSkills.Contains(id))return false;return true;}
        bool HasDebtSkill(DebtSkillKind kind,int family=-1)
        {return false;}
        float SumDebtSkill(DebtSkillKind kind,int family=-1)
        {return ActionSkillPassive(kind.ToString(),family);}

        int DebtWeaponFamily(int model)
        {
            if(model==14||model==15||model==16||model==17)return 0; // handguns
            if(model==3||model==4||model==5||model==13)return 1; // shotguns
            if(model==0||model==9||model==10||model==12)return 2; // assault rifles
            if(model==2||model==7||model==8||model==11)return 3; // precision / long-range
            if(model==1||model==6)return 4; // heavy machine guns
            return -1;
        }
        public float DebtDamageBonus(int weaponModel)=>SumDebtSkill(DebtSkillKind.Damage,DebtWeaponFamily(weaponModel));
        public float DebtFireRateBonus(int weaponModel)=>SumDebtSkill(DebtSkillKind.FireRate,DebtWeaponFamily(weaponModel));
        public float DebtAccuracyBonus(int weaponModel)=>SumDebtSkill(DebtSkillKind.Accuracy,DebtWeaponFamily(weaponModel));
        public float DebtReloadBonus(int weaponModel)=>SumDebtSkill(DebtSkillKind.Reload,DebtWeaponFamily(weaponModel));
        public float DebtAmmoBonus(int weaponModel)=>SumDebtSkill(DebtSkillKind.Ammo,DebtWeaponFamily(weaponModel));
        public float DebtCriticalChanceBonus(int model)=>SumDebtSkill(DebtSkillKind.CriticalChance,DebtWeaponFamily(model));
        public float DebtCriticalDamageBonus(int model)=>SumDebtSkill(DebtSkillKind.CriticalDamage,DebtWeaponFamily(model));
        public float DebtAmmoCapacityBonus(int model)=>SumDebtSkill(DebtSkillKind.AmmoCapacity,DebtWeaponFamily(model));
        public float DebtHealthBonus=>SumDebtSkill(DebtSkillKind.Health);
        public float DebtRecoveryBonus=>SumDebtSkill(DebtSkillKind.Recovery);
        public float DebtDamageReductionBonus=>SumDebtSkill(DebtSkillKind.DamageReduction);
        public float DebtHealOnKill=>SumDebtSkill(DebtSkillKind.HealOnKill);
        public float DebtAmmoPickupBonus=>SumDebtSkill(DebtSkillKind.AmmoPickup);
        public float DebtShopDiscount=>SumDebtSkill(DebtSkillKind.ShopDiscount);
        public float DebtKillChainBonus=>SumDebtSkill(DebtSkillKind.KillChain);
        public float DebtBossDamageBonus=>SumDebtSkill(DebtSkillKind.BossDamage);

        public float ArtifactAffixBonus(string type)
        {
            if(string.IsNullOrEmpty(type)||EquippedArtifacts==null)return 0;
            float total=0;foreach(var item in EquippedArtifacts)if(item!=null&&item.affixes!=null)total+=GearScoring.AffixTotal(item.affixes,type);
            return total;
        }
        public float EquippedWeaponAffixBonus(string type)
        {
            if(string.IsNullOrEmpty(type))return 0;float total=0;
            for(int i=0;i<AvailableWeaponSlotCount;i++){var weapon=WeaponInSlot(i);if(weapon==null)continue;
                if(type=="health")total+=weapon.bonusHealth;
                else if(type=="mitigation")total+=weapon.damageReductionBonus;
                else if(type=="moveSpeed")total+=weapon.movementBonus;
                else if(type=="regen")total+=weapon.regenBonus;
                else if(type=="ammo"&&weapon.affixes!=null)total+=GearScoring.AffixTotal(weapon.affixes,type);
            }
            return total;
        }

        void DrawDebtOffice()
        {
            Box(new Rect(0,0,1440,900),PanelInk);
            Text(65,54,T("BRIGGS / DEBT OFFICE","ブリッグス / 借金返済窓口"),heading,new Color(1,.39f,.28f),1000);
            Text(68,116,T("COLLAR BALANCE","首輪の残債")+"   "+DebtRemaining.ToString("N0")+" / 1,000,000,000",label,Mist,1100);
            float progress=1-DebtRemaining/(float)InitialDebt;Box(new Rect(68,157,1070,14),new Color(.12f,.16f,.17f));Box(new Rect(68,157,1070*Mathf.Clamp01(progress),14),new Color(1,.34f,.23f));
            Text(68,187,T("LEVEL ","レベル ")+DebtLevel+" / "+DebtLevelCap+"    ·    "+T("SKILL POINTS ","スキルポイント ")+DebtSkillPoints,small,Gold,900);
            Text(68,217,T("PAID ","返済累計 ")+DebtTotalPaid.ToString("N0")+"    ·    "+(DebtLevel>=DebtLevelCap?T("LEVEL CAP REACHED","レベル上限到達"):T("NEXT LEVEL IN ","次のレベルまで ")+DebtNextLevelRemaining.ToString("N0")),tiny,Cyan,1000);
            Box(new Rect(68,244,1070,22),new Color(.09f,.13f,.15f));
            Box(new Rect(68,244,1070*DebtProgressRatio,22),new Color(.12f,.82f,.73f));
            Text(1150,244,DebtLevel>=DebtLevelCap?"100%":(DebtProgressRatio*100f).ToString("0")+"%",tiny,Cyan,110);
            if(debtSkillTreePage){DrawActionSkillTree();return;}
            else
            {
                Text(68,276,T("REPAYMENTS ARE PERMANENT. EACH LEVEL COSTS MORE THAN THE LAST.","返済は取り消せません。レベルが上がるほど次の必要額が増えます。"),small,Mist,1200);
                int[] payments={1000000,5000000,25000000};string[] labels={"1,000,000","5,000,000","25,000,000"};
                for(int i=0;i<payments.Length;i++)if(Button(new Rect(68+i*260,330,230,62),T("PAY ","返済 ")+labels[i]+T(" SCRAP"," スクラップ"),i==2))
                {int before=DebtLevel;if(RepayDebt(payments[i]))Notify("Debt payment recorded. Level "+before+" → "+DebtLevel,"返済を記録。レベル "+before+" → "+DebtLevel);}
                int paymentLimit=DebtPaymentLimit;
                debtPaymentAmount=Mathf.Clamp(debtPaymentAmount,0,paymentLimit);
                Text(68,409,T("PAYMENT AMOUNT  /  ","返済額  /  ")+debtPaymentAmount.ToString("N0")+" / "+paymentLimit.ToString("N0"),label,Cyan,800);
                if(paymentLimit>0)SetDebtPaymentFraction(GUI.HorizontalSlider(new Rect(68,457,750,25),debtPaymentAmount/(float)paymentLimit,0f,1f));
                if(Button(new Rect(850,409,270,58),T("PAY SELECTED","指定額を返済"),true)&&debtPaymentAmount>0)RepaySelectedDebt();
                if(Button(new Rect(850,476,270,50),T("SELECT ALL","全額を指定")))debtPaymentAmount=paymentLimit;
                Text(68,556,T("WALLET ","所持金 ")+Credits.ToString("N0")+T(" SCRAP"," スクラップ"),label,Cyan,850);
                Text(68,602,T("The collar detonator stays with the Malcolm Vale. Clearing your balance is the only way out.","起爆装置はマルコム・ヴェイルが握っている。首輪から解放されるには、負債を完済するしかない。"),new GUIStyle(small){wordWrap=true},Mist,1060);
                if(Button(new Rect(68,694,380,54),T("OPEN SKILL TREE  /  ","スキルツリーを開く  /  ")+DebtSkillPoints+T(" POINTS"," ポイント"),true))debtSkillTreePage=true;
                if(Button(new Rect(480,694,550,54),T("RESET SKILLS / ","スキル振り直し / ")+ActionSkillRespecCost.ToString("N0"),ActionSkillSpentPoints>0&&Credits>=ActionSkillRespecCost))TryRespecActionSkills();
            }
            if(Button(new Rect(68,790,300,52),T("RETURN TO SAFEHOUSE","拠点へ戻る")))screen=Screen.Hub;
        }

        void DrawDebtSkillTree()
        {
            string[] tracks={"HANDGUN","SHOTGUN","ASSAULT","PRECISION","HEAVY MG"};string[] ja={"ハンドガン","ショットガン","アサルトライフル","スナイパーライフル","ヘビーマシンガン"};
            for(int i=0;i<tracks.Length;i++)if(Button(new Rect(68+i*244,266,232,46),T(tracks[i],ja[i]),i==debtTrack))debtTrack=i;
            var nodes=debtSkillNodes.FindAll(n=>n.track==tracks[debtTrack]);
            debtSkillScroll=GUI.BeginScrollView(new Rect(68,323,1280,425),debtSkillScroll,new Rect(0,0,1235,Mathf.Max(425,nodes.Count*82)));
            for(int i=0;i<nodes.Count;i++)
            {
                var n=nodes[i];float y=i*82;bool unlocked=unlockedDebtSkills.Contains(n.id),available=DebtSkillAvailable(n);Panel(new Rect(0,y,1220,72),unlocked?Cyan:available?Gold*.8f:new Color(.24f,.31f,.32f));
                string detail=DebtSkillDescription(n);Text(18,y+8,(VaultLanguage.Japanese?n.ja:n.en)+"   ·   "+detail,label,unlocked?Cyan:Color.white,900);
                string prereq=n.requires.Length==0?T("ROOT","起点"):string.Join(" + ",n.requires);
                Text(20,y+40,T("Requires: ","前提: ")+prereq,tiny,Mist,880);
                if(unlocked)Text(1020,y+21,T("UNLOCKED","取得済み"),small,Cyan,180);
                else if(available)
                {
                    bool hasPoints=DebtSkillPoints>0;
                    if(Button(new Rect(1000,y+14,195,44),hasPoints?T("UNLOCK  /  1 PT","取得  /  1 pt"):T("NO POINTS","ポイント不足"),hasPoints)&&hasPoints)TryUnlockDebtSkill(n.id);
                }
                else Text(1020,y+21,T("LOCKED","未解放"),small,Mist,180);
            }
            GUI.EndScrollView();
            Text(68,759,T("Four weapon paths · 13 nodes each · choose damage, cadence, precision, ammo, survival and slot synergies.","4系統・各13ノード。威力、連射、精度、弾薬、生存力、武器枠の相乗効果を選択。"),tiny,Mist,1220);
            if(Button(new Rect(1025,790,320,52),T("BACK TO REPAYMENTS","返済画面へ戻る")))debtSkillTreePage=false;
        }
        string DebtSkillDescription(DebtSkillNode node)
        {
            string en=node.kind==DebtSkillKind.CriticalChance?"critical chance":node.kind==DebtSkillKind.CriticalDamage?"critical damage":node.kind==DebtSkillKind.AmmoCapacity?"reserve capacity":node.kind==DebtSkillKind.Damage?"damage":node.kind==DebtSkillKind.FireRate?"fire rate":node.kind==DebtSkillKind.Reload?"reload speed":node.kind==DebtSkillKind.DamageReduction?"damage reduction":node.kind==DebtSkillKind.AmmoPickup?"ammo pickup":node.kind==DebtSkillKind.ShopDiscount?"shop discount":node.kind==DebtSkillKind.KillChain?"kill chain":node.kind==DebtSkillKind.BossDamage?"boss damage":"bonus";
            string ja=node.kind==DebtSkillKind.CriticalChance?"クリティカル率":node.kind==DebtSkillKind.CriticalDamage?"クリティカル倍率":node.kind==DebtSkillKind.AmmoCapacity?"予備弾上限":node.kind==DebtSkillKind.Damage?"威力":node.kind==DebtSkillKind.FireRate?"連射速度":node.kind==DebtSkillKind.Reload?"リロード速度":node.kind==DebtSkillKind.DamageReduction?"ダメージ軽減":node.kind==DebtSkillKind.AmmoPickup?"弾薬取得量":node.kind==DebtSkillKind.ShopDiscount?"購入割引":node.kind==DebtSkillKind.KillChain?"撃破連鎖":node.kind==DebtSkillKind.BossDamage?"ボス特効":"強化";
            string value=node.kind==DebtSkillKind.Accuracy?"+"+(node.value*100).ToString("0")+T(" accuracy"," 命中率"):node.kind==DebtSkillKind.WeaponSlot?T("+1 weapon slot","武器枠 +1"):node.kind==DebtSkillKind.Health?"+"+node.value.ToString("0")+T(" max HP"," 最大HP"):node.kind==DebtSkillKind.HealOnKill?"+"+node.value.ToString("0")+T(" HP per kill"," 撃破HP回復"):node.kind==DebtSkillKind.Recovery?"+"+node.value.ToString("0.0")+T(" HP/s"," HP/秒"):node.kind==DebtSkillKind.Ammo?"+"+node.value.ToString("0")+T(" magazine rounds"," 装弾数"):"+"+(node.value*100).ToString("0")+"% "+T(en,ja);
            return value;
        }

        static List<DebtSkillNode> BuildDebtSkillNodes()
        {
            var n=new List<DebtSkillNode>();string[] tracks={"HANDGUN","SHOTGUN","ASSAULT","PRECISION","HEAVY MG"};
            string[][] titlesEn={
                new[]{"Caliber I","Quick Draw I","Sight Picture I","Fresh Magazine I","Deep Pockets I","Caliber II","Reinforced Lining I","Impact Padding I","Emergency Recovery I","Last Stand Heal"},
                new[]{"Heavy Payload I","Pump Rhythm I","Choke I","Shell Practice I","Shell Carrier I","Heavy Payload II","Reinforced Lining II","Impact Padding II","Emergency Recovery II","Scavenger's Reserve"},
                new[]{"Pressure I","Sustained Fire I","Combat Sight I","Mag Drill I","Belt Carrier I","Pressure II","Ammo Scavenging I","Quartermaster I","Kill Chain I","Ammo Scavenging II"},
                new[]{"Penetrator I","Bolt Timing I","Rangefinder I","Field Strip I","Long Case I","Penetrator II","Boss Hunter I","Third Weapon Slot","Fourth Weapon Slot","Boss Hunter II"},
                new[]{"Suppressive Fire I","Sustained Fire I","Heavy Sight I","Feed Drill I","Belt Carrier I","Suppressive Fire II","Ammo Scavenging I","Quartermaster I","Kill Chain I","Ammo Scavenging II"}};
            string[][] titlesJa={
                new[]{"口径強化 I","速射 I","照準 I","素早いリロード I","深い弾倉 I","口径強化 II","防護ライニング I","衝撃緩和 I","応急回復 I","起死回生"},
                new[]{"重装弾 I","ポンプ操作 I","集弾 I","リロード訓練 I","弾薬携行 I","重装弾 II","防護ライニング II","衝撃緩和 II","応急回復 II","回収弾薬"},
                new[]{"制圧火力 I","継続射撃 I","戦闘照準 I","高速リロード I","ベルト給弾 I","制圧火力 II","弾薬回収 I","仕入れの達人 I","撃破連鎖 I","弾薬回収 II"},
                new[]{"貫通弾 I","操作短縮 I","測距 I","分解整備 I","長尺弾倉 I","貫通弾 II","対ボス戦 I","武器枠 第3","武器枠 第4","対ボス戦 II"},
                new[]{"制圧火力 I","継続射撃 I","重機関照準 I","給弾訓練 I","ベルト携行 I","制圧火力 II","弾薬回収 I","仕入れの達人 I","撃破連鎖 I","弾薬回収 II"}};
            for(int f=0;f<tracks.Length;f++)
            {
                string prefix="weapon"+f+"_";
                n.Add(new DebtSkillNode(prefix+"damage1",tracks[f],titlesEn[f][0],titlesJa[f][0],DebtSkillKind.Damage,.12f,f));
                n.Add(new DebtSkillNode(prefix+"fire1",tracks[f],titlesEn[f][1],titlesJa[f][1],DebtSkillKind.FireRate,.06f,f));
                n.Add(new DebtSkillNode(prefix+"accuracy1",tracks[f],titlesEn[f][2],titlesJa[f][2],DebtSkillKind.Accuracy,.04f,f));
                n.Add(new DebtSkillNode(prefix+"reload1",tracks[f],titlesEn[f][3],titlesJa[f][3],DebtSkillKind.Reload,.08f,f));
                n.Add(new DebtSkillNode(prefix+"ammo1",tracks[f],titlesEn[f][4],titlesJa[f][4],DebtSkillKind.Ammo,2,f));
                n.Add(new DebtSkillNode(prefix+"damage2",tracks[f],titlesEn[f][5],titlesJa[f][5],DebtSkillKind.Damage,.25f,f,prefix+"damage1",prefix+"fire1"));
                n.Add(new DebtSkillNode(prefix+"critical1",tracks[f],"Weak Point I","弱点射撃 I",DebtSkillKind.CriticalChance,.03f,f,prefix+"accuracy1"));
                n.Add(new DebtSkillNode(prefix+"critical2",tracks[f],"Lethal Precision I","致命精度 I",DebtSkillKind.CriticalDamage,.20f,f,prefix+"critical1",prefix+"damage2"));
                n.Add(new DebtSkillNode(prefix+"reserve1",tracks[f],"Reserve Carrier I","予備弾携行 I",DebtSkillKind.AmmoCapacity,.50f,f,prefix+"ammo1"));
                DebtSkillKind[] utilityKinds=f==0?new[]{DebtSkillKind.Health,DebtSkillKind.DamageReduction,DebtSkillKind.Recovery,DebtSkillKind.HealOnKill}:
                    f==1?new[]{DebtSkillKind.Health,DebtSkillKind.DamageReduction,DebtSkillKind.Recovery,DebtSkillKind.Ammo}:
                    f==2||f==4?new[]{DebtSkillKind.AmmoPickup,DebtSkillKind.ShopDiscount,DebtSkillKind.KillChain,DebtSkillKind.AmmoPickup}:
                    new[]{DebtSkillKind.BossDamage,DebtSkillKind.WeaponSlot,DebtSkillKind.WeaponSlot,DebtSkillKind.BossDamage};
                float[] values=f==0?new[]{20f,.05f,.5f,3f}:f==1?new[]{25f,.06f,.6f,3f}:f==2||f==4?new[]{.2f,.05f,.12f,.25f}:new[]{.12f,1f,1f,.18f};
                for(int j=0;j<4;j++)
                {
                    string id=f==3&&j==1?"survival_slot1":f==3&&j==2?"survival_slot2":prefix+"utility"+(j+1);
                    string[] req=j==0?new[]{prefix+"accuracy1",prefix+"ammo1"}:j==1?new[]{prefix+"reload1",prefix+"damage2"}:j==2?new[]{prefix+"utility1",prefix+"damage2"}:new[]{prefix+"utility2",prefix+"utility3"};
                    if(f==3&&j==2)req=new[]{"survival_slot1",prefix+"utility1"};
                    var en=titlesEn[f][6+j];var ja=titlesJa[f][6+j];
                    n.Add(new DebtSkillNode(id,tracks[f],en,ja,utilityKinds[j],values[j],-1,req));
                }
            }
            return n;
        }
    }
}
