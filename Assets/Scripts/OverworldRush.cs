using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        OverworldActivitySite overworldRushSite;
        bool overworldRushRewardReady;
        float overworldRushRemaining, overworldRushSpawnTimer, overworldHoldProgress;
        int overworldChallengeTarget;
        int overworldChallengeMode; // 0: survival, 1: eliminate, 2: hold
        readonly List<WorldEnemySpawn> overworldRushEnemies = new List<WorldEnemySpawn>();
        public bool OverworldRushActive => overworldRushSite != null && !overworldRushRewardReady && overworldRushRemaining > 0;
        public int OverworldRushSeconds => Mathf.CeilToInt(Mathf.Max(0, overworldRushRemaining));

        public string OverworldRushObjective => overworldChallengeMode == 1
            ? VaultLanguage.T("ELIMINATE ", "殲滅 ") + OverworldChallengeKills + "/"+overworldChallengeTarget
            : overworldChallengeMode == 2
                ? VaultLanguage.T("HOLD SITE ", "地点防衛 ") + Mathf.FloorToInt(overworldHoldProgress) + "/30"
                : VaultLanguage.T("SURVIVE", "生存");
        int OverworldChallengeKills { get { int count = 0; foreach (var spawn in overworldRushEnemies) if (spawn.defeated) count++; return count; } }
        static int ActivityKillTarget(int siteId) => 15 + ((siteId * 17 ^ siteId >> 3) & int.MaxValue) % 5;

        void ReserveOverworldRushSlots()
        {
            int reserve=8;
            if(worldEnemyInstances.Count<=MaxActiveOverworldEnemies-reserve)return;
            var ordinary=new List<Foe>(worldEnemyInstances.Keys);
            ordinary.Sort((a,b)=>a==null?1:b==null?-1:(b.transform.position-PlayerPosition).sqrMagnitude.CompareTo((a.transform.position-PlayerPosition).sqrMagnitude));
            foreach(var foe in ordinary)
            {
                if(worldEnemyInstances.Count<=MaxActiveOverworldEnemies-reserve)break;
                if(foe==null||!worldEnemyInstances.TryGetValue(foe,out var spawn))continue;
                spawn.position=foe.transform.position;spawn.position.y=.65f;spawn.hp=foe.hp;spawn.active=null;
                worldEnemyInstances.Remove(foe);worldEnemyLevels.Remove(foe);Enemies.Remove(foe);
                foe.gameObject.SetActive(false);Destroy(foe.gameObject);
            }
        }

        void StartOverworldRush(OverworldActivitySite site) { StartOverworldChallenge(site, 0); }
        void StartOverworldChallenge(OverworldActivitySite site, int mode)
        {
            if (overworldRushSite != null)
            {
                Notify("Finish the current surge first", "進行中の敵襲を先に終えてください");
                return;
            }
            overworldRushSite = site; overworldRushRewardReady = false;
            overworldChallengeMode = mode; overworldHoldProgress = 0;
            overworldChallengeTarget=ActivityKillTarget(site.id);
            overworldRushRemaining = mode == 0 ? 45f : mode==1?100f:60f; overworldRushSpawnTimer = 0;
            ReserveOverworldRushSlots();
            RefreshOverworldActivities();
            if (mode == 0) Notify("HOSTILE SURGE / survive for 45 seconds", "敵襲 / 45秒間生き延びよう");
            else if (mode == 1) Notify("CLEAR / defeat "+overworldChallengeTarget+" attackers in 100 seconds", "殲滅 / 100秒以内に敵"+overworldChallengeTarget+"体を倒そう");
            else Notify("DEFEND / stay within 9m for 30 seconds", "防衛 / 地点から9m以内に30秒留まろう");
        }

        void TickOverworldRush()
        {
            if (!OverworldRushActive || !Active) return;
            if (player == null || hp <= 0 || (PlayerPosition - overworldRushSite.position).sqrMagnitude > 45f * 45f)
            {
                ResetOverworldRush(); RefreshOverworldActivities();
                Notify("CHALLENGE FAILED / left the area", "挑戦失敗 / エリアを離れました");
                return;
            }
            if (overworldChallengeMode == 2 && (PlayerPosition - overworldRushSite.position).sqrMagnitude <= 9f * 9f)
                overworldHoldProgress += Time.deltaTime;
            overworldRushRemaining -= Time.deltaTime;
            bool success = overworldChallengeMode == 0 ? overworldRushRemaining <= 0 && overworldRushEnemies.Count>=6 :
                overworldChallengeMode == 1 ? OverworldChallengeKills >= overworldChallengeTarget : overworldHoldProgress >= 30f && overworldRushEnemies.Count>=4;
            if (success)
            {
                ClearOverworldRushEnemies(); overworldRushRewardReady = true;
                RefreshOverworldActivities();
                Notify("CHALLENGE COMPLETE / open the reward chest", "挑戦成功 / 報酬箱を開けよう");
                return;
            }
            if (overworldRushRemaining <= 0)
            {
                ResetOverworldRush(); RefreshOverworldActivities();
                Notify("CHALLENGE FAILED / time expired", "挑戦失敗 / 時間切れ");
                return;
            }
            overworldRushSpawnTimer -= Time.deltaTime;
            if (overworldRushSpawnTimer > 0) return;
            overworldRushSpawnTimer = 1.5f;
            int alive = 0;
            foreach (var spawn in overworldRushEnemies) if (spawn.active != null && !spawn.active.dead) alive++;
            if (alive >= 8 || worldEnemyInstances.Count >= MaxActiveOverworldEnemies) return;
            for (int attempt = 0; attempt < 48; attempt++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2;
                float distance = 7f + (float)rng.NextDouble() * 11f;
                Vector3 origin=attempt<32?PlayerPosition:overworldRushSite.position;
                Vector3 p = origin + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
                p.y = .65f;
                if (!ChallengeSpawnClear(p)) continue;
                SpawnOverworldChallengeEnemy(p);
                return;
            }
            // Dense streets can block every random ring position. Reuse a validated
            // ordinary spawn point before giving up on this wave.
            foreach(var candidate in worldEnemySpawns)
            {
                float distance=(candidate.position-PlayerPosition).sqrMagnitude;
                if(distance<6f*6f||distance>40f*40f||!ChallengeSpawnClear(candidate.position))continue;
                SpawnOverworldChallengeEnemy(candidate.position);
                return;
            }
        }

        bool ChallengeSpawnClear(Vector3 position)
        {
            if(WorldGeography.Blocked(position,.8f)||WorldEntranceBuildings.Blocked(position,.8f,WorldGeography))return false;
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&(foe.transform.position-position).sqrMagnitude<2.5f*2.5f)return false;
            return true;
        }

        void SpawnOverworldChallengeEnemy(Vector3 position)
        {
            int[] roster={0,1,2,4,5,7,8,9};
            var spawn=new WorldEnemySpawn{position=position,kind=roster[rng.Next(roster.Length)],level=Mathf.Max(1,DebtLevel)};
            spawningWorldEnemy=spawn;
            try
            {
                SpawnFoe(position,spawn.kind);
                var foe=Enemies[Enemies.Count-1];spawn.active=foe;
                worldEnemyInstances.Add(foe,spawn);overworldRushEnemies.Add(spawn);
            }
            finally{spawningWorldEnemy=null;}
        }

        void ClearOverworldRushEnemies()
        {
            foreach (var spawn in overworldRushEnemies)
            {
                var foe = spawn.active;
                if (foe == null) continue;
                worldEnemyInstances.Remove(foe); worldEnemyLevels.Remove(foe); Enemies.Remove(foe);
                foe.gameObject.SetActive(false); Destroy(foe.gameObject); spawn.active = null;
            }
            overworldRushEnemies.Clear();
        }
        void ResetOverworldRush()
        {
            ClearOverworldRushEnemies(); overworldRushSite = null;
            overworldRushRemaining = 0; overworldRushRewardReady = false; overworldHoldProgress = 0; overworldChallengeMode = 0; overworldChallengeTarget=0;
        }
    }
}
