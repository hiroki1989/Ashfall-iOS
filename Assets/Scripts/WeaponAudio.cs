using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        readonly Dictionary<string, AudioClip> weaponClips = new Dictionary<string, AudioClip>();
        AudioSource reloadAudioSource;

        void StopReloadSound() { if (reloadAudioSource != null) reloadAudioSource.Stop(); }

        void PlayWeaponSound(Gun weapon, string action, float volume)
        {
            if (weapon == null || audioSource == null) return;
            var recipe = DistinctEquipmentDesigns.Find(weapon.catalogId);
            int kind;
            if (recipe != null && !recipe.legendary && weapon.catalogId.StartsWith("distinct.gun.")) kind = recipe.gunClass;
            else
            {
                switch (Arsenal.Category(VaultLanguage.DisplayModel(weapon)))
                {
                    case WeaponClass.Handgun: kind = 1; break;
                    case WeaponClass.Shotgun: kind = 2; break;
                    case WeaponClass.HeavyMachineGun: kind = 3; break;
                    case WeaponClass.SniperRifle: kind = 5; break;
                    default: kind = 0; break;
                }
            }
            string family = kind == 1 ? "Handgun" : kind == 2 ? "Shotgun" : kind == 3 || kind == 4 ? "HeavyMachineGun" : kind == 5 ? "SniperRifle" : "AssaultRifle";
            string key = family + "_" + action;
            if (!weaponClips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>("WeaponSFX/" + key);
                weaponClips[key] = clip;
            }
            if (clip != null && action == "reload")
            {
                if (reloadAudioSource == null) reloadAudioSource = gameObject.AddComponent<AudioSource>();
                reloadAudioSource.Stop();
                reloadAudioSource.volume = audioSource.volume;
                reloadAudioSource.mute = audioSource.mute;
                reloadAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                reloadAudioSource.PlayOneShot(clip, volume);
            }
            else if (clip != null) audioSource.PlayOneShot(clip, volume);
            else if (action == "fire") audioSource.PlayOneShot(kind == 2 ? scatterSound : fireSound, volume);
        }
    }
}
