using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // A single persistent director owns the music channels. AudioListener.volume,
    // set by the existing options slider, continues to control music and effects.
    public sealed class CyberwareMusicDirector : MonoBehaviour
    {
        enum Music { None, Menu, Explore, Dungeon, Combat }
        const float MasterMusicLevel = .34f;
        const float FadeSeconds = 1.5f;
        const float CombatRadius = 18f;
        const float CombatReleaseSeconds = 7f;
        static CyberwareMusicDirector instance;
        readonly Dictionary<Music, AudioClip> clips = new Dictionary<Music, AudioClip>();
        AudioSource[] sources;
        VaultGame game;
        Music selected;
        int active;
        float combatRemaining;
        float nextEnemyScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance) return;
            new GameObject("Cyberware music director").AddComponent<CyberwareMusicDirector>();
        }
        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            sources = new AudioSource[2];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].loop = true;
                sources[i].spatialBlend = 0;
                sources[i].ignoreListenerPause = true;
                sources[i].volume = 0;
            }
        }
        void OnDestroy() { if (instance == this) instance = null; }
        AudioClip Clip(Music music)
        {
            if (clips.TryGetValue(music, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>("CyberwareBGM/" + music);
            clips[music] = clip;
            if (!clip) Debug.LogWarning("Cyberware music clip missing: " + music);
            return clip;
        }
        void Select(Music desired)
        {
            if (selected == desired) return;
            AudioClip clip = Clip(desired);
            if (!clip) return;
            int next = 1 - active;
            sources[next].Stop();
            sources[next].clip = clip;
            sources[next].volume = 0;
            sources[next].Play();
            active = next;
            selected = desired;
        }
        void Update()
        {
            if (!game) game = FindAnyObjectByType<VaultGame>();
            if (!game) return;
            Music desired = selected;
            if (game.screen == VaultGame.Screen.Menu) { desired = Music.Menu; combatRemaining = 0; }
            else if (game.screen == VaultGame.Screen.Hub || game.screen == VaultGame.Screen.Shop)
            { desired = Music.Explore; combatRemaining = 0; }
            else if (game.screen == VaultGame.Screen.Playing && !game.InventoryOpen)
            {
                if (Time.unscaledTime >= nextEnemyScan)
                {
                    nextEnemyScan = Time.unscaledTime + .25f;
                    bool near = false;
                    var p = game.PlayerPosition;
                    foreach (var foe in game.Enemies)
                    {
                        if (!foe || foe.dead) continue;
                        Vector3 d = foe.transform.position - p; d.y = 0;
                        if (d.sqrMagnitude <= CombatRadius * CombatRadius) { near = true; break; }
                    }
                    if (near) combatRemaining = CombatReleaseSeconds;
                }
                combatRemaining = Mathf.Max(0, combatRemaining - Time.unscaledDeltaTime);
                desired = combatRemaining > 0 ? Music.Combat : game.IsOverworld ? Music.Explore : Music.Dungeon;
            }
            // Pause, inventory, map, reward and dialogue retain the current track.
            if (desired != Music.None) Select(desired);
            for (int i = 0; i < sources.Length; i++)
            {
                float target = i == active && selected != Music.None ? MasterMusicLevel : 0;
                sources[i].volume = Mathf.MoveTowards(sources[i].volume, target, MasterMusicLevel * Time.unscaledDeltaTime / FadeSeconds);
                if (target == 0 && sources[i].volume <= 0 && sources[i].isPlaying) sources[i].Stop();
            }
        }
    }
}
