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
        public static int MaxRank(int node)=>node==0?1:5;
        static bool Valid(int branch,int node)=>branch>=0&&branch<3&&node>=0&&node<5;
        static string RankKey(int branch,int node)=>Prefix+"Rank."+branch+"."+node;
        public static int Rank(int branch,int node)=>Valid(branch,node)?Mathf.Clamp(PlayerPrefs.GetInt(RankKey(branch,node),0),0,MaxRank(node)):0;
        public static bool CanUpgrade(int branch,int node)=>Valid(branch,node)&&Points>0&&Rank(branch,node)<MaxRank(node)&&(node==0||Rank(branch,node-1)>0);
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
        public static void Reset(){PlayerPrefs.DeleteKey(Prefix+"Points");PlayerPrefs.DeleteKey(Prefix+"Equipped");for(int b=0;b<3;b++)for(int n=0;n<5;n++)PlayerPrefs.DeleteKey(RankKey(b,n));PlayerPrefs.Save();}
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
        float petSkillNextDropAt,petSkillBuffUntil,petSkillAuraAt;
        const string PetOverworldCooldownKey="Vault.PetSkills.v1.OverworldCooldown";
        float PetCooldownRemaining=>Mathf.Max(0,petSkillNextDropAt-Time.time);
        void RestorePetCooldown(float remaining){petSkillNextDropAt=Time.time+Mathf.Clamp(remaining,0,60);}
        void SavePetOverworldCooldown(){PlayerPrefs.SetFloat(PetOverworldCooldownKey,PetCooldownRemaining);PlayerPrefs.Save();}
        void RestorePetOverworldCooldown(){if(!PlayerPrefs.HasKey(PetOverworldCooldownKey))return;RestorePetCooldown(PlayerPrefs.GetFloat(PetOverworldCooldownKey));PlayerPrefs.DeleteKey(PetOverworldCooldownKey);PlayerPrefs.Save();}
        float PetTreeDamageBonus=>ActivePetRank(1)*.15f;
        float PetTreeSpeedBonus=>ActivePetRank(2)*.10f;
        float PetPlayerDamageBonus=>ActivePetBranch>=0&&Time.time<petSkillBuffUntil?ActivePetRank(4)*.03f:0;
        float PetGearDropBonus=>ActivePetBranch==2?.05f+ActivePetRank(3)*.01f:0;
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
            if(buff>0){if(Time.time>=petSkillBuffUntil)PlayPetSkillBark();petSkillBuffUntil=Time.time+10f;petSkillAuraAt=0;}
            if(Time.time<petSkillNextDropAt)return;
            int rank=ActivePetRank(3);
            if(b==0){AddHealthPackDrop(p+Vector3.right*.9f,.20f+rank*.03f);petSkillNextDropAt=Time.time+Mathf.Max(20f,60f-rank*5f);PlayPetSkillBark();}
            else if(b==1)
            {
                AmmoType type=ActiveAmmoTypeAt(rng.Next(ActiveAmmoTypeCount));
                if(rank>=3){float lowest=float.MaxValue;for(int i=0;i<ActiveAmmoTypeCount;i++){var candidate=ActiveAmmoTypeAt(i);float fraction=AmmoReserve(candidate)/(float)Mathf.Max(1,AmmoCapacity(candidate));if(fraction<lowest){lowest=fraction;type=candidate;}}}
                AddAmmoDrop(p+Vector3.right*.9f,type,Mathf.Max(1,Mathf.RoundToInt(AmmoPackSize(type)*(1+rank*.2f))));
                petSkillNextDropAt=Time.time+Mathf.Max(12f,30f-rank*3f);
                PlayPetSkillBark();
            }
        }
        void TickPetSkillAura()
        {
            if(PetPlayerDamageBonus<=0||!player||!world||Time.time<petSkillAuraAt)return;
            petSkillAuraAt=Time.time+.55f;Burst(PlayerPosition+Vector3.up*.8f,new Color(.32f,.85f,.68f,.7f),4);
        }
        Rarity PetImproveEnemyRarity(System.Random random,Rarity rarity,bool boss)
        {
            if(petEnemyLootRoll&&ActivePetBranch==2&&random.NextDouble()<ActivePetRank(3)*.05)
            {
                var improved=(Rarity)Mathf.Max((int)rarity,(int)DungeonCatalog.RollRarity(random,boss));
                if(improved>rarity)PlayPetSkillBark();
                return improved;
            }
            return rarity;
        }
    }
}
