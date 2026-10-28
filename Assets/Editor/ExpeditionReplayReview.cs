using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor
{
    // Opt-in, in-memory review. It never reads or changes the player's save.
    public static class ExpeditionReplayReview
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        public static void Run()
        {
            var root=new GameObject("Expedition replay verification");
            var game=root.AddComponent<VaultGame>();var type=typeof(VaultGame);
            var stage=type.GetProperty("SelectedStageId",Flags);var room=type.GetField("room",Flags);
            var cleared=type.GetField("cleared",Flags);var kills=type.GetField("missionKills",Flags);
            var completed=type.GetField("missionCompleted",Flags);var claims=(List<int>)type.GetField("clearedExpeditionStages",Flags).GetValue(game);
            var exit=type.GetProperty("ExpeditionExitOpen",Flags);var reset=type.GetMethod("ResetExpeditionMission",Flags);
            int checks=0;var failures=new List<string>();
            void Check(bool condition,string text){checks++;if(!condition)failures.Add(text);}
            for(int mission=0;mission<6;mission++)
            {
                int id=-1;for(int i=0;i<ExpeditionMap.StageCount;i++)if((int)ExpeditionMap.Get(i).mission==mission){id=i;break;}
                Check(id>=0,"Mission exists "+mission);if(id<0)continue;
                stage.SetValue(game,id);claims.Add(id);room.SetValue(game,0);cleared.SetValue(game,false);
                kills.SetValue(game,0x7fffff);completed.SetValue(game,true);reset.Invoke(game,null);
                Check(!(bool)completed.GetValue(game)&&(int)kills.GetValue(game)==0,"Replay resets mission "+mission);
                Check(!(bool)exit.GetValue(game),"Replay starts with exit closed "+mission);
                cleared.SetValue(game,true);Check((bool)exit.GetValue(game),"Cleared floor exit opens "+mission);
                room.SetValue(game,3);cleared.SetValue(game,false);Check(!(bool)exit.GetValue(game),"Boss floor starts closed "+mission);
                cleared.SetValue(game,true);Check((bool)exit.GetValue(game),"Boss defeat allows safe exit "+mission);
                room.SetValue(game,0);cleared.SetValue(game,false);
                if((bool)type.GetProperty("ExpeditionAllowsBypass",Flags).GetValue(game))
                {kills.SetValue(game,1<<16);Check((bool)exit.GetValue(game),"Investigated floor opens exit "+mission);}
                claims.Remove(id);
            }
            UnityEngine.Object.DestroyImmediate(root);
            string report="Checks="+checks+" Failures="+failures.Count+"\n"+string.Join("\n",failures);
            Directory.CreateDirectory("D:/Unity/QA-expedition-replay");File.WriteAllText("D:/Unity/QA-expedition-replay/result.txt",report);
            Debug.Log("EXPEDITION_REPLAY_REVIEW "+report);
            if(failures.Count>0)throw new Exception("Expedition replay verification failed");
        }
    }
}
