using System;
using UnityEngine;

namespace NeonVault
{
    // Builds only catalogued equipment parts. The original weapon/socket rig remains the
    // source of truth for aiming, hit detection and grip placement.
    public static class GeneratedEquipmentAssetFactory
    {
        public sealed class Visual : IDisposable
        {
            public readonly GameObject[] instances;
            readonly Material material;
            bool disposed;

            internal Visual(GameObject[] instances, Material material)
            { this.instances = instances; this.material = material; }

            public void SetVisible(bool visible)
            {
                if (disposed) return;
                foreach (var instance in instances) if (instance) instance.SetActive(visible);
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                foreach (var instance in instances)
                    if (instance) DestroySafe(instance);
                if (material) DestroySafe(material);
            }
        }

        static void DestroySafe(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        public static Visual Create(GeneratedEquipmentAssetCatalog.Entry entry, DetailedHero3D hero)
        {
            if (!hero || string.IsNullOrEmpty(entry.modelPath) ||
                string.IsNullOrEmpty(entry.atlasPath)) return null;
            // A gun needs authored trigger/support/muzzle landmarks. The current entry has
            // no such contract, so falling back to ManufacturerEquipment3D is required.
            // The existing Warden rifle/sidearm use their dedicated placement code.
            if (entry.kind == GeneratedEquipmentAssetCatalog.Kind.Gun ||
                entry.kind == GeneratedEquipmentAssetCatalog.Kind.WardenBody ||
                entry.kind == GeneratedEquipmentAssetCatalog.Kind.WardenMask ||
                entry.combinedWardenArmor) return null;

            var prefab = Resources.Load<GameObject>(entry.modelPath);
            var atlas = Resources.Load<Texture2D>(entry.atlasPath);
            var shader = Resources.Load<Shader>("GeneratedWardenTexture");
            if (!shader) shader = Resources.Load<Shader>("VaultSurface");
            if (!prefab || !atlas || !shader) return null;

            var first = SocketFor(hero, entry.socket);
            if (!first) return null;
            Transform second = null;
            if (entry.kind == GeneratedEquipmentAssetCatalog.Kind.Armor)
                second = OtherSide(hero, entry.armorSlot);
            if (NeedsPair(entry) && !second) return null;

            Material material = null;
            GameObject a = null, b = null;
            try
            {
                material = new Material(shader) { name = "Generated equipment / " + entry.catalogId,
                    mainTexture = atlas };
                material.SetTexture("_MainTex", atlas);
                a = Instance(prefab, first, material, entry.catalogId);
                if (second) b = Instance(prefab, second, material, entry.catalogId);
                if (!a || (second && !b)) throw new InvalidOperationException("Equipment model has no renderers");
                return new Visual(b ? new[] { a, b } : new[] { a }, material);
            }
            catch (Exception error)
            {
                if (a) DestroySafe(a);
                if (b) DestroySafe(b);
                if (material) DestroySafe(material);
                Debug.LogError("Generated equipment visual failed for " + entry.catalogId + ": " + error);
                return null;
            }
        }

        static GameObject Instance(GameObject prefab, Transform socket, Material material, string id)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, socket, false);
            instance.name = "Generated equipment / " + id;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { DestroySafe(instance); return null; }
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
            return instance;
        }

        static bool NeedsPair(GeneratedEquipmentAssetCatalog.Entry entry) =>
            entry.kind == GeneratedEquipmentAssetCatalog.Kind.Armor &&
            (entry.armorSlot == ArmorSlot.Arms || entry.armorSlot == ArmorSlot.Legs ||
             entry.armorSlot == ArmorSlot.Feet);

        static Transform OtherSide(DetailedHero3D hero, ArmorSlot slot)
        {
            switch (slot)
            {
                case ArmorSlot.Arms: return hero.rightForearm;
                case ArmorSlot.Legs: return hero.rightShin;
                case ArmorSlot.Feet: return hero.rightFoot;
                default: return null;
            }
        }

        static Transform SocketFor(DetailedHero3D hero, GeneratedEquipmentAssetCatalog.Socket socket)
        {
            switch (socket)
            {
                case GeneratedEquipmentAssetCatalog.Socket.Head: return hero.head;
                case GeneratedEquipmentAssetCatalog.Socket.Chest: return hero.chest;
                case GeneratedEquipmentAssetCatalog.Socket.LeftForearm: return hero.leftForearm;
                case GeneratedEquipmentAssetCatalog.Socket.RightForearm: return hero.rightForearm;
                case GeneratedEquipmentAssetCatalog.Socket.LeftShin: return hero.leftShin;
                case GeneratedEquipmentAssetCatalog.Socket.RightShin: return hero.rightShin;
                case GeneratedEquipmentAssetCatalog.Socket.LeftFoot: return hero.leftFoot;
                case GeneratedEquipmentAssetCatalog.Socket.RightFoot: return hero.rightFoot;
                case GeneratedEquipmentAssetCatalog.Socket.RightHand: return hero.rightHand;
                case GeneratedEquipmentAssetCatalog.Socket.Module: return hero.chest;
                default: return null;
            }
        }
    }
}
