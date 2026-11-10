using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool saveSlotMenuOpen,saveSlotInitialized;
        int confirmDeleteSlot = -1;
        static int saveSlotStartAfterReload; // 1 = start, 2 = resume

        public void EnsureProgressionAndInventoryLoaded()
        {
            if(!saveSlotInitialized)
            {
                ProgressionInitialize();
                LoadInventory();
                saveSlotInitialized=true;
            }
            if(Weapons.Count==0)
            {
                LoadInventory();
            }
        }

        bool HandleSaveSlotMenu()
        {
            if (saveSlotStartAfterReload != 0)
            {
                int start=saveSlotStartAfterReload; saveSlotStartAfterReload=0;
                if(start==2 && HasSavedRun && ResumeSavedRun()) return true;
                ContinueOverworldGame();
                return true;
            }
            if (!saveSlotMenuOpen) return false;
            Box(new Rect(0,0,1440,900),new Color(.01f,.015f,.02f,.86f));
            Panel(new Rect(195,105,1050,700),Gold);
            Text(245,145,T("SELECT SAVE DATA","セーブデータを選択"),new GUIStyle(heading){fontSize=38},UiPaper,950);
            Text(250,208,T("Select a slot to continue or begin a new game.","続きから遊ぶか、新しいデータを選んで始めます。"),new GUIStyle(label){fontSize=22},Mist,940);
            for(int slot=1;slot<=3;slot++)
            {
                int selected=slot;
                bool exists=SaveSlotPrefs.Exists(slot);
                bool canResume=exists&&SaveSlotPrefs.SlotHasCheckpoint(slot);
                float y=265+(slot-1)*140;
                Panel(new Rect(245,y,950,120),exists?Cyan:Mist);
                Text(270,y+18,T("SAVE ","セーブ ")+slot,new GUIStyle(label){fontSize=27},UiPaper,350);
                Text(270,y+61,exists?T("SAVED DATA","セーブデータあり"):T("NEW GAME","新しく始める"),new GUIStyle(label){fontSize=20},exists?Cyan:Mist,350);
                
                float btnX = 640;
                if(exists && TitleMenuButton(new Rect(btnX,y+24,140,72),T("DELETE","削除"),false,MobileTouchControls.Enabled))
                    confirmDeleteSlot = selected;
                btnX += 155;
                if(TitleMenuButton(new Rect(btnX,y+20,180,80),exists?T("CONTINUE","続ける"):T("START","始める"),true,MobileTouchControls.Enabled))
                    StartSelectedSaveSlot(selected,false);
                btnX += 195;
                if(canResume&&TitleMenuButton(new Rect(btnX,y+20,190,80),T("RESUME","探索再開"),false,MobileTouchControls.Enabled))
                    StartSelectedSaveSlot(selected,true);
            }
            if(WorldButton(new Rect(965,720,230,60),T("BACK","戻る")))
            {
                saveSlotMenuOpen=false;
                confirmDeleteSlot=-1;
            }

            if(confirmDeleteSlot > 0)
            {
                Box(new Rect(0,0,1440,900),new Color(0,0,0,.75f));
                Panel(new Rect(370,280,700,340),new Color(1,.25f,.2f));
                Text(400,315,T("DELETE SAVE DATA ?","セーブデータを削除しますか？"),new GUIStyle(heading){fontSize=32},new Color(1,.35f,.3f),640);
                Text(400,380,T("Are you sure you want to delete SAVE " + confirmDeleteSlot + "?\nAll story, inventory, and equipment progress will be permanently erased.","セーブ " + confirmDeleteSlot + " のデータを完全に削除しますか？\n進行状況・所持装備・スキルなどの全データが消去されます。"),new GUIStyle(label){fontSize=20,wordWrap=true},UiPaper,640);
                
                if(WorldButton(new Rect(420,530,280,60),T("CANCEL","キャンセル")))
                    confirmDeleteSlot = -1;
                if(TitleMenuButton(new Rect(740,530,280,60),T("DELETE FOREVER","削除を実行"),true,MobileTouchControls.Enabled))
                {
                    SaveSlotPrefs.DeleteSlot(confirmDeleteSlot);
                    if(SaveSlotPrefs.Current == confirmDeleteSlot)
                    {
                        saveSlotInitialized = false;
                        cores = 0; rank = 0;
                    }
                    confirmDeleteSlot = -1;
                }
            }
            return true;
        }
        static void ClearArray<T>(T[] array) { if (array != null) System.Array.Clear(array, 0, array.Length); }
        void ResetRuntimeStateForSlot()
        {
            saveSlotInitialized=false;
            pausedFromHub=false;actionSkillTutorialVisible=false;legendaryWeaponTutorialVisible=false;
            actionSkillLoaded=false;
            overworldStateLoaded=false;
            debtPaid=0; debtIntroSeen=true; unlockedDebtSkills?.Clear();
            Weapons?.Clear(); ArmorInventory?.Clear(); MeleeWeaponInventory?.Clear();
            ArtifactInventory?.Clear(); ClearArray(EquippedArtifacts);
            ClearArray(EquippedArmor);
            clearedExpeditionStages?.Clear(); shopPurchased?.Clear();
            ClearArray(actionSkillRanks);
            ClearArray(actionSkillExtraRanks);
            actionSkillExtraValues?.Clear(); actionSkillExtraValuesDirty=true;
            actionSkillSelected=0; ActionSkillPoints=0;
            LastVisitedCamp=0; OverworldMissionClears=0; PendingWorldStage=-1; WorldStorySeen=false; SavedOutside=false;
            ClearArray(visitedOverworldCamps);
            ClearArray(discoveredOverworldDungeons);
            collectorIntroSeen=false; collectorClaims=0; ModuleCapacityTier=0; junkKeys=null; confirmJunkSale=false;
            ResetMissionRuntimeState();npcConversation=0; npcBeat=0;
            DungeonIndex=0; HighestUnlocked=0; Credits=0; ExpeditionTokens=0;
            SelectedStageId=-1;
        }

        void StartSelectedSaveSlot(int slot,bool resume)
        {
            if(resume&&!SaveSlotPrefs.SlotHasCheckpoint(slot))return;
            bool slotChanged=saveSlotInitialized&&slot!=SaveSlotPrefs.Current;
            SaveSlotPrefs.Select(slot);
            cores=SaveSlotPrefs.GetInt("Vault.Cores",0);rank=SaveSlotPrefs.GetInt("Vault.Rank",0);
            if(slotChanged)
            {
                saveSlotStartAfterReload=resume?2:1;
                SceneManager.sceneLoaded+=OnSaveSlotSceneLoaded;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            ResetRuntimeStateForSlot();
            ProgressionInitialize();LoadInventory();saveSlotInitialized=true;
            saveSlotMenuOpen=false;
            if(resume&&HasSavedRun&&ResumeSavedRun())return;
            ContinueOverworldGame();
        }
        static void OnSaveSlotSceneLoaded(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=OnSaveSlotSceneLoaded;
            var game=Object.FindFirstObjectByType<VaultGame>();
            if(!game)game=new GameObject("ASHFALL / runtime").AddComponent<VaultGame>();
            if(!game.saveSlotInitialized){game.ProgressionInitialize();game.LoadInventory();game.saveSlotInitialized=true;}
        }
    }
}
