using UnityEngine;

namespace NeonVault
{
    // Each faction uses two complete product sets from its own manufacturer.
    // The shared armor system owns the atlas materials and releases them with the actor.
    public sealed class HumanFactionArmorTextures : MonoBehaviour
    {
        // Slots are chest, arms, legs and feet. IDs are catalog products, not
        // sequential images: each row has the same manufacturer across all slots.
        static readonly string[,,] Sets = {
            { { "distinct.armor.041", "distinct.armor.047", "distinct.armor.053", "distinct.armor.059" },
              { "distinct.armor.071", "distinct.armor.077", "distinct.armor.083", "distinct.armor.089" } }, // Aegis
            { { "distinct.armor.040", "distinct.armor.046", "distinct.armor.052", "distinct.armor.058" },
              { "distinct.armor.070", "distinct.armor.076", "distinct.armor.082", "distinct.armor.088" } }, // Cinder
            { { "distinct.armor.038", "distinct.armor.044", "distinct.armor.050", "distinct.armor.056" },
              { "distinct.armor.068", "distinct.armor.074", "distinct.armor.080", "distinct.armor.086" } }  // Voltarc
        };

        readonly SharedArmorTextures textures = new SharedArmorTextures();

        public void Apply(SkinnedMeshRenderer[] parts, int faction, int variant)
        {
            faction = Mathf.Clamp(faction, 0, 2);
            variant = Mathf.Clamp(variant, 0, 1);
            var items = new ArmorItem[5];
            for (int slot = 1; slot <= 4; slot++)
                items[slot] = new ArmorItem { slot = (ArmorSlot)slot, catalogId = Sets[faction, variant, slot - 1] };
            textures.Apply(parts, items);
        }

        void OnDestroy() { textures.Dispose(); }
    }
}
