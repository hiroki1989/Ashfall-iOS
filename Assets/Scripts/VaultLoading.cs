using System;using System.Collections;using UnityEngine;
namespace NeonVault {public partial class VaultGame {
 bool worldTransitionPending,insideWorldTransition;
 bool QueueWorldTransition(Action action){if((smoke&&Array.IndexOf(Environment.GetCommandLineArgs(),"--transition-review")<0)||insideWorldTransition)return false;if(worldTransitionPending)return true;worldTransitionPending=true;StartCoroutine(WorldTransition(action));return true;}
 IEnumerator WorldTransition(Action action){yield return null;yield return new WaitForEndOfFrame();yield return null;insideWorldTransition=true;try{action();}catch{worldTransitionPending=false;throw;}finally{insideWorldTransition=false;}while(world&&world.GetComponent<OverworldStream>() is OverworldStream stream&&!stream.Ready)yield return null;worldTransitionPending=false;}
 void DrawWorldLoading(){var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;var tint=GUI.color;GUI.color=new Color(.035f,.05f,.07f);GUI.DrawTexture(new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height),Texture2D.whiteTexture);GUI.color=tint;GUI.matrix=matrix;Text(100,650,"ASHFALL",heading,Color.white,1100);Text(100,720,T("LOADING / Preparing the next area","ロード中 / 次のエリアを準備しています"),label,Mist,1100);float phase=Mathf.Repeat(Time.realtimeSinceStartup*.35f,1);Box(new Rect(100,790,1240,3),new Color(.2f,.25f,.28f));Box(new Rect(100+phase*1000,790,240,3),Cyan);}
}}
