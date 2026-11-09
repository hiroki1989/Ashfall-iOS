using PlayerPrefs = NeonVault.SaveSlotPrefs;
using System;
using UnityEngine;

namespace NeonVault
{
    // Each point comes from a donated legendary item.  State is independent of the player's action tree.
    public static class PetSkillState
    {
        const string Prefix="Vault.PetSkills.v1.";
        public static int Points => Mathf.Max(0,PlayerPrefs.GetInt(Prefix+"Points",0));
        // -1 means no skill equipped; 0/1/2 are rescue, supply and treasure.
        public static int Equipped { get { int v=PlayerPrefs.GetInt(Prefix+"Equipped",-1);return v>=0&&v<3&&Rank(v,0)>0?v:-1; } }
        public static int MaxRank(int node)=>node==0?1:node<5?5:3;
        static bool Valid(int branch,int node)=>branch>=0&&branch<3&&node>=0&&node<25;
        static string RankKey(int branch,int node)=>Prefix+"Rank."+branch+"."+node;
        public static int Rank(int branch,int node)=>Valid(branch,node)?Mathf.Clamp(PlayerPrefs.GetInt(RankKey(branch,node),0),0,MaxRank(node)):0;
        public static bool IsUnlocked(int branch,int node)
        {
            if(!Valid(branch,node))return false;
            if(node==0)return true;
            // 5 Tiers of 5 nodes: t*5 is root.
            // Branching without convergence:
            // Left Path:  t*5+1 -> t*5+3
            // Right Path: t*5+2 -> t*5+4
            int rem = node % 5;
            int root = (node / 5) * 5;
            if(rem == 0)
            {
                // Tier root unlocks if previous tier's root is invested
                return Rank(branch, root - 5) > 0;
            }
            if(rem == 1 || rem == 2) return Rank(branch, root) > 0;
            if(rem == 3) return Rank(branch, root + 1) > 0; // Pure Left branch
            if(rem == 4) return Rank(branch, root + 2) > 0; // Pure Right branch
            return false;
        }
        public static bool CanUpgrade(int branch,int node)=>Valid(branch,node)&&Points>0&&Rank(branch,node)<MaxRank(node)&&IsUnlocked(branch,node);
        public static bool TryUpgrade(int branch,int node)
        {
            if(!CanUpgrade(branch,node))return false;
            PlayerPrefs.SetInt(RankKey(branch,node),Rank(branch,node)+1);
            PlayerPrefs.SetInt(Prefix+"Points",Points-1);PlayerPrefs.Save();return true;
        }
        public static bool Equip(int branch)
        {
            if(branch<0||branch>=3||Rank(branch,0)==0)return false;
            PlayerPrefs.SetInt(Prefix+"Equipped",branch);PlayerPrefs.Save();return true;
        }
        public static void GrantPoint(){PlayerPrefs.SetInt(Prefix+"Points",Mathf.Min(1000000,Points+1));PlayerPrefs.Save();}
        public static void Reset(){PlayerPrefs.DeleteKey(Prefix+"Points");PlayerPrefs.DeleteKey(Prefix+"Equipped");for(int b=0;b<3;b++)for(int n=0;n<25;n++)PlayerPrefs.DeleteKey(RankKey(b,n));PlayerPrefs.Save();}
    }

    public partial class VaultGame
    {
        public int PetPoints=>PetSkillState.Points;
        public int PetEquippedSkill=>PetSkillState.Equipped+1;
        public int PetSkillRank(int branch,int node)=>PetSkillState.Rank(branch,node);
        public bool SpendPetSkillPoint(int branch,int node)=>PetAdopted&&PetSkillState.TryUpgrade(branch,node);
        public void EquipPetSkill(int branch){if(PetAdopted)PetSkillState.Equip(branch);}
        public void GrantPetPoint()=>PetSkillState.GrantPoint();
        int ActivePetBranch=>PetAdopted?PetSkillState.Equipped:-1;
        int ActivePetRank(int node){int b=ActivePetBranch;return b<0?0:PetSkillState.Rank(b,node);}
        int PetRank(int branch,int node)=>branch<0?0:PetSkillState.Rank(branch,node);
        float petSkillNextDropAt,petSkillBuffUntil,petSkillAuraAt;
        const string PetOverworldCooldownKey="Vault.PetSkills.v1.OverworldCooldown";
        float PetCooldownRemaining=>Mathf.Max(0,petSkillNextDropAt-Time.time);
        void RestorePetCooldown(float remaining){petSkillNextDropAt=Time.time+Mathf.Clamp(remaining,0,60);}
        void SavePetOverworldCooldown(){PlayerPrefs.SetFloat(PetOverworldCooldownKey,PetCooldownRemaining);PlayerPrefs.Save();}
        void RestorePetOverworldCooldown(){if(!PlayerPrefs.HasKey(PetOverworldCooldownKey))return;RestorePetCooldown(PlayerPrefs.GetFloat(PetOverworldCooldownKey));PlayerPrefs.DeleteKey(PetOverworldCooldownKey);PlayerPrefs.Save();}
        float PetTreeDamageBonus=>ActivePetRank(1)*.15f+(Time.time<petSkillBuffUntil?ActivePetRank(4)*.03f:0)+(ActivePetBranch==0?ActivePetRank(8)*.04f+ActivePetRank(16)*.05f+(hp/Mathf.Max(1f,maxHp)<.35f?ActivePetRank(20)*.10f:0):ActivePetBranch==1?ActivePetRank(8)*.04f+ActivePetRank(16)*.05f+(gun!=null&&AmmoReserve(AmmoForGun(gun))/(float)Mathf.Max(1,AmmoCapacity(AmmoForGun(gun)))<.2f?ActivePetRank(20)*.10f:0):ActivePetRank(8)*.04f+ActivePetRank(16)*.05f);
        float PetTreeSpeedBonus=>ActivePetRank(2)*.10f+(ActivePetRank(13)*.04f);
        float PetPlayerDamageBonus=>0;
        float PetGearDropBonus=>ActivePetBranch==2?.05f+ActivePetRank(3)*.01f+PetRank(2,5)*.005f+PetRank(2,11)*.0075f+PetRank(2,19)*.01f+PetRank(2,23)*.015f+(CurrentDungeon!=null&&room==CurrentDungeon.AreaCount-1?PetRank(2,14)*.02f:0):0;
        bool petEnemyLootRoll;
        float petSkillBarkAt;
        void PlayPetSkillBark()
        {
            if(Time.time<petSkillBarkAt)return;
            if(!petBarkClip)petBarkClip=Resources.Load<AudioClip>("UserSFX/pet-bark");
            if(petBarkClip&&audioSource){audioSource.PlayOneShot(petBarkClip,.5f);petSkillBarkAt=Time.time+2f;}
        }
        void PetSkillOnEnemyKilled(Vector3 p)
        {
            int b=ActivePetBranch;if(b<0)return;
            int buff=ActivePetRank(4);
            if(buff>0){if(Time.time>=petSkillBuffUntil)PlayPetSkillBark();petSkillBuffUntil=Time.time+10f+ActivePetRank(17)*2f;petSkillAuraAt=0;}
            if(Time.time<petSkillNextDropAt)return;
            int rank=ActivePetRank(3);
            if(b==0){float hpFraction=hp/Mathf.Max(1f,maxHp);float heal=.20f+rank*.03f+PetRank(0,5)*.02f+PetRank(0,11)*.03f+PetRank(0,14)*.04f+PetRank(0,22)*.06f+PetRank(0,19)*.05f+PetRank(0,24)*.12f;if(hpFraction<.35f)heal+=PetRank(0,7)*.08f+PetRank(0,20)*.12f;if(hpFraction<.25f)heal+=PetRank(0,15)*.08f;AddHealthPackDrop(p+Vector3.right*.9f,heal);if(PetRank(0,10)>0)HealPlayer(maxHp*.02f*PetRank(0,10));if(PetRank(0,21)>0)HealPlayer(maxHp*.03f*PetRank(0,21));petSkillNextDropAt=Time.time+Mathf.Max(15f,60f-rank*5f-PetRank(0,6)*2f-PetRank(0,9)*3f-(hp/Mathf.Max(1f,maxHp)<.5f?PetRank(0,12)*4f:0)-PetRank(0,18)*4f-PetRank(0,23)*5f);PlayPetSkillBark();}
            else if(b==1)
            {
                AmmoType type=ActiveAmmoTypeAt(rng.Next(ActiveAmmoTypeCount));
                if(PetRank(1,21)>0||(rank>=3&&PetRank(1,10)==0)){float lowest=float.MaxValue;for(int i=0;i<ActiveAmmoTypeCount;i++){var candidate=ActiveAmmoTypeAt(i);float fraction=AmmoReserve(candidate)/(float)Mathf.Max(1,AmmoCapacity(candidate));if(fraction<lowest){lowest=fraction;type=candidate;}}}else if(PetRank(1,10)>0&&gun!=null)type=AmmoForGun(gun);
                int supplyAmount=Mathf.Max(1,Mathf.RoundToInt(AmmoPackSize(type)*(1+rank*.2f+PetRank(1,5)*.08f+PetRank(1,11)*.10f+PetRank(1,14)*.12f+PetRank(1,19)*.15f+PetRank(1,22)*.18f+(AmmoReserve(type)/(float)Mathf.Max(1,AmmoCapacity(type))<.2f?PetRank(1,7)*.15f:0)+(AmmoReserve(type)<=0?PetRank(1,15)*.20f:0))));AddAmmoDrop(p+Vector3.right*.9f,type,supplyAmount);if(PetRank(1,24)>0)AddAmmoDrop(p+Vector3.left*.9f,type,supplyAmount/2);
                petSkillNextDropAt=Time.time+Mathf.Max(10f,30f-rank*3f-PetRank(1,6)*2f-PetRank(1,9)*3f-PetRank(1,12)*4f-PetRank(1,18)*4f-PetRank(1,23)*5f);
                PlayPetSkillBark();
            }
        }
        void TickPetSkillAura()
        {
            if(ActivePetBranch<0||Time.time>=petSkillBuffUntil||!player||!world||Time.time<petSkillAuraAt)return;
            petSkillAuraAt=Time.time+.55f;Burst(PlayerPosition+Vector3.up*.8f,new Color(.32f,.85f,.68f,.7f),4);
        }
        Rarity PetImproveEnemyRarity(System.Random random,Rarity rarity,bool boss)
        {
            if(petEnemyLootRoll&&ActivePetBranch==2&&random.NextDouble()<ActivePetRank(3)*.05+PetRank(2,6)*.02+PetRank(2,9)*.02+PetRank(2,10)*.01+PetRank(2,12)*.03+PetRank(2,15)*.02+PetRank(2,18)*.04+PetRank(2,20)*.05+PetRank(2,22)*.05+(boss?(PetRank(2,7)*.03+PetRank(2,21)*.05+(CurrentDungeon!=null&&room==CurrentDungeon.AreaCount-1?PetRank(2,24)*.08:0)):0))
            {
                var improved=(Rarity)Mathf.Max((int)rarity,(int)DungeonCatalog.RollRarity(random,boss));
                if(improved>rarity)PlayPetSkillBark();
                return improved;
            }
            return rarity;
        }
    }
}
