using UnityEngine;

namespace NeonVault
{
    /// <summary>Uses the imported Renegade body and the shared boss combat rig.</summary>
    public static class InfectedRenegadeVisual
    {
        static Material bodyMaterial, headMaterial, detailMaterial;

        public static bool TryAttach(GameObject actor)
        {
            if (!actor || !actor.GetComponent<Foe>()) return false;
            var prefab = Resources.Load<GameObject>("ImportedEnemies/InfectedRenegade");
            if (!prefab) return false;
            var instance = Object.Instantiate(prefab, actor.transform, false);
            instance.name = "Infected Renegade";
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            EnsureMaterials();
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string part = renderer.gameObject.name.ToLowerInvariant();
                Material material = part.Contains("head") ? headMaterial : part.Contains("body") ? bodyMaterial : detailMaterial;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
            var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!skin)
            {
                Object.Destroy(instance);
                return false;
            }
            var driver = instance.GetComponent<ImportedEnemyVisual>() ?? instance.AddComponent<ImportedEnemyVisual>();
            driver.model = instance.transform;
            driver.useNamedBones = true;
            driver.suppressMask = true; // the Renegade's imported helmet is part of its design
            driver.targetHeight = 2.05f;
            driver.Bind(actor.transform, actor.GetComponent<Foe>(), -.09f);
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
                if (!renderer.transform.IsChildOf(instance.transform)) renderer.enabled = false;
            return true;
        }

        static void EnsureMaterials()
        {
            if (bodyMaterial) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            bodyMaterial = CreateMaterial(shader, "Renegade body", "ImportedEnemies/RenegadeBody");
            headMaterial = CreateMaterial(shader, "Renegade helmet", "ImportedEnemies/RenegadeHead");
            detailMaterial = CreateMaterial(shader, "Renegade field gear", "ImportedEnemies/RenegadeDetails");
        }

        static Material CreateMaterial(Shader shader, string materialName, string texturePath)
        {
            var material = new Material(shader) { name = materialName };
            var texture = Resources.Load<Texture2D>(texturePath);
            if (texture)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .18f);
            return material;
        }
    }
}
