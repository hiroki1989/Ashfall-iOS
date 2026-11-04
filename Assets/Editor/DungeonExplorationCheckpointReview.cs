using System;
using System.IO;
using NeonVault;
using UnityEditor;
using UnityEngine;

public static class DungeonExplorationCheckpointReview
{
    [Serializable] sealed class Snapshot { public int columns, rows; public string bits; }
    public static void Run()
    {
        string output = "D:/Unity/BuildTemp/DungeonExplorationCheckpointReview.txt";
        int checks=0;
        try
        {
            Action<bool,string> check=(ok,msg)=>{checks++;if(!ok)throw new Exception(msg);};
            for(int seed=1;seed<=30;seed++) foreach(bool compact in new[]{false,true})
            {
                var before=new SectorLayout(new System.Random(seed*7919),seed%3,compact);
                before.Discover(before.Entry);
                before.Discover(before.Exit);
                var json=JsonUtility.ToJson(new Snapshot{columns=before.Columns,rows=before.Rows,bits=before.SaveExploration()});
                var saved=JsonUtility.FromJson<Snapshot>(json);
                var after=new SectorLayout(new System.Random(seed*7919),seed%3,compact);
                check(after.RestoreExploration(saved.bits,saved.columns,saved.rows),"snapshot did not restore");
                for(int x=0;x<before.Columns;x++) for(int y=0;y<before.Rows;y++)
                    check(before.IsExplored(x,y)==after.IsExplored(x,y),"discovered cell lost");
                check(!after.RestoreExploration(saved.bits,saved.columns+1,saved.rows),"wrong dimensions accepted");
                check(!after.RestoreExploration("!!",saved.columns,saved.rows),"malformed data accepted");
                check(!after.RestoreExploration(null,0,0),"legacy absent snapshot accepted");
                after.Discover(after.Exit);
                var exit=after.ToCell(after.Exit);
                check(after.IsVisible(exit.x,exit.y),"visibility not recomputed");
            }
            File.WriteAllText(output,"PASS checks="+checks+" layouts=60; snapshots, old-save fallback, malformed data, current visibility");
            Debug.Log("DUNGEON_EXPLORATION_CHECKPOINT_PASS "+checks);EditorApplication.Exit(0);
        }
        catch(Exception error){File.WriteAllText(output,"FAIL checks="+checks+" "+error);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
