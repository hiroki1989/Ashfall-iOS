using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        AudioClip meleeSwingSound;
        bool meleeSwingSoundLoaded;

        public void PlayMeleeSwingSound()
        {
            if (audioSource == null) return;
            if (!meleeSwingSoundLoaded)
            {
                meleeSwingSound = Resources.Load<AudioClip>("WeaponSFX/MeleeSwing");
                meleeSwingSoundLoaded = true;
            }
            if (meleeSwingSound != null) audioSource.PlayOneShot(meleeSwingSound, .7f);
        }
    }
}
