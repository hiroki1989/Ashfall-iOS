using UnityEngine;

namespace NeonVault
{
    // Plays the supplied Fantastic Creature 1 animations against the existing Foe AI.
    public sealed class FantasticCreatureVisual : MonoBehaviour
    {
        Foe owner;
        Animator animator;
        Vector3 previous;
        bool wasCharging, dead;
        float attackUntil;
        string animationState;
        static readonly string[] Attacks = { "thc4_arma|st_attack1", "thc4_arma|st_attack2", "thc4_arma|st_attack3" };

        public static bool TryAttach(GameObject actor)
        {
            if (!actor) return false;
            var foe = actor.GetComponent<Foe>();
            var prefab = Resources.Load<GameObject>("ImportedEnemies/FantasticCreature1");
            if (!foe || !prefab) return false;
            var holder = new GameObject("Fantastic Creature 1");
            holder.transform.SetParent(actor.transform, false);
            holder.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var instance = Instantiate(prefab, holder.transform, false);
            instance.name = "Fantastic Creature 1 Mesh";
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (component.GetType().Name == "THC6_ctrl") component.enabled = false;
            var animator = instance.GetComponentInChildren<Animator>(true);
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (!animator || !animator.runtimeAnimatorController || renderers.Length == 0)
            { Destroy(holder); return false; }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.Update(0f);
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
                r.updateWhenOffscreen = !Application.isMobilePlatform;
                if (Application.isMobilePlatform) r.quality = SkinQuality.Bone2;
            }
            if (bounds.size.y > .001f)
            {
                float scale = Mathf.Clamp(2.7f / bounds.size.y, .02f, 8f);
                holder.transform.localScale *= scale;
                bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Vector3 offset = actor.transform.position - bounds.center;
                offset.y = actor.transform.position.y - .09f - bounds.min.y;
                holder.transform.position += offset;
            }
            var driver = holder.AddComponent<FantasticCreatureVisual>();
            driver.owner = foe;
            driver.animator = animator;
            driver.previous = actor.transform.position;
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
                if (!renderer.transform.IsChildOf(holder.transform)) renderer.enabled = false;
            return true;
        }

        void LateUpdate()
        {
            if (!owner || !animator || !owner.game || !owner.game.Active) return;
            Vector3 current = owner.transform.position;
            float speed = Time.deltaTime > 0 ? (current - previous).magnitude / Time.deltaTime : 0;
            previous = current;
            if (owner.dead)
            {
                if (!dead) { dead = true; Play("thc4_arma|st_death", .08f); }
                return;
            }
            // Start the supplied attack as the combat telegraph begins so its
            // visible strike approaches the moment the Foe resolves damage.
            if (!wasCharging && owner.IsTelegraphing)
            {
                int kind = owner.BossAttackStyle;
                Play(Attacks[(kind == 1 ? 1 : kind == 2 || kind == 7 ? 2 : 0)], .07f);
                attackUntil = Time.time + owner.TelegraphSeconds + .25f;
            }
            wasCharging = owner.IsTelegraphing;
            if (Time.time < attackUntil) return;
            Play(speed > .2f ? "thc4_arma|st_run" : "thc4_arma|st_idle_battle", .12f);
        }
        void Play(string state, float blend)
        {
            if (state == animationState) return;
            int hash = Animator.StringToHash(state);
            if (!animator.HasState(0, hash)) return;
            animator.CrossFade(hash, blend, 0, 0f);
            animationState = state;
        }
    }
}
