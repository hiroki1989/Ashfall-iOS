using UnityEngine;

namespace NeonVault
{
    // Uses the same procedural zombie pose driver as the other imported infected.
    // The prefab retains its imported skin and materials; gameplay remains on Foe.
    public static class FemaleZombieVisual
    {
        public static bool TryAttach(GameObject actor)
        {
            if (!actor || !actor.GetComponent<Foe>()) return false;
            var prefab = Resources.Load<GameObject>("ImportedEnemies/FemaleZombie");
            if (!prefab) return false;
            var instance = Object.Instantiate(prefab, actor.transform, false);
            instance.name = "Female infected visual";
            if (!instance.GetComponentInChildren<SkinnedMeshRenderer>())
            {
                Object.Destroy(instance);
                return false;
            }
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var driver = instance.AddComponent<ImportedEnemyVisual>();
            driver.model = instance.transform;
            driver.useNamedBones = true;
            driver.targetHeight = 1.9f;
            driver.Bind(actor.transform, actor.GetComponent<Foe>(), -.09f);
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
                if (!renderer.transform.IsChildOf(instance.transform)) renderer.enabled = false;
            return true;
        }
    }
}
