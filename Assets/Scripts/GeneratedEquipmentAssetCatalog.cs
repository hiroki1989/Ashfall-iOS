using System;
using UnityEngine;

namespace NeonVault
{
    // A visual entry is valid only when both imported assets are present. Gameplay sockets
    // and item statistics remain owned by ManufacturerEquipment3D and the inventory.
    public static class GeneratedEquipmentAssetCatalog
    {
        public enum Kind { Gun, Armor, Melee, Module, WardenBody, WardenMask }
        public enum Socket { Weapon, Head, Chest, LeftForearm, RightForearm,
            LeftShin, RightShin, LeftFoot, RightFoot, RightHand, Module }

        public struct Entry
        {
            public readonly Kind kind;
            public readonly string catalogId, modelPath, atlasPath;
            public readonly Socket socket;
            public readonly ArmorSlot armorSlot;
            public readonly bool combinedWardenArmor;

            public Entry(Kind kind, string catalogId, string modelPath, string atlasPath,
                Socket socket, ArmorSlot armorSlot = ArmorSlot.Head, bool combinedWardenArmor = false)
            {
                this.kind = kind; this.catalogId = catalogId;
                this.modelPath = modelPath; this.atlasPath = atlasPath;
                this.socket = socket; this.armorSlot = armorSlot;
                this.combinedWardenArmor = combinedWardenArmor;
            }
        }

        const string Warden = "Art/GeneratedWarden/";
        const string Products = "Art/GeneratedEquipment/";

        static bool Available(Entry entry) =>
            Resources.Load<GameObject>(entry.modelPath) != null &&
            Resources.Load<Texture2D>(entry.atlasPath) != null;

        static bool Resolve(Entry candidate, out Entry entry)
        {
            entry = candidate;
            if (Available(candidate)) return true;
            entry = default(Entry);
            return false;
        }

        // Product-specific paths are deliberately exact. A missing product never borrows
        // another product's silhouette merely because the items share a family or maker.
        static Entry Product(Kind kind, string catalogId, Socket socket,
            ArmorSlot slot = ArmorSlot.Head)
        {
            string folder = Products + kind.ToString().ToLowerInvariant() + "/" + catalogId + "/";
            return new Entry(kind, catalogId, folder + "mesh", folder + "atlas", socket, slot);
        }

        static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "." || id == "..") return false;
            foreach (char c in id)
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')) return false;
            return true;
        }

        public static bool TryExactGun(Gun item, out Entry entry)
        {
            entry = default(Entry);
            if (item == null || !ValidId(item.catalogId)) return false;
            var candidate = Product(Kind.Gun, item.catalogId, Socket.Weapon);
            if (!Resolve(candidate, out entry)) return false;
            if (GeneratedProductWeapon.HasMarkers(Resources.Load<GameObject>(entry.modelPath))) return true;
            entry = default(Entry);
            return false;
        }

        public static bool TryGun(Gun item, out Entry entry)
        {
            entry = default(Entry);
            if (item == null || !ValidId(item.catalogId)) return false;
            if (TryExactGun(item, out entry)) return true;
            // Only the two authored Trail Warden models may use the shared fieldkit.
            if (item.visualSet != Arsenal.TrailWardenVisualSet) return false;
            int model = VaultLanguage.DisplayModel(item);
            string name = model == 12 ? "rifle" : model == 15 ? "sidearm" : null;
            if (name == null) return false;
            return Resolve(new Entry(Kind.Gun, item.catalogId,
                Warden + name + "-mobile", Warden + name + "-mobile-atlas", Socket.Weapon), out entry);
        }

        static Socket ArmorSocket(ArmorSlot slot)
        {
            switch (slot)
            {
                case ArmorSlot.Head: return Socket.Head;
                case ArmorSlot.Chest: return Socket.Chest;
                case ArmorSlot.Arms: return Socket.LeftForearm;
                case ArmorSlot.Legs: return Socket.LeftShin;
                case ArmorSlot.Feet: return Socket.LeftFoot;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static bool TryArmor(ArmorItem item, out Entry entry)
        {
            entry = default(Entry);
            if (item == null || !ValidId(item.catalogId) || (int)item.slot < 0 || (int)item.slot > 4)
                return false;
            // The scan is shared source art, split into slot meshes at runtime. It is
            // never registered as a product-specific generated model.
            return Resolve(Product(Kind.Armor, item.catalogId, ArmorSocket(item.slot), item.slot), out entry);
        }

        public static bool[] CoveredBodySlots(ArmorItem[] equipped)
        {
            var slots=new bool[5];
            if(equipped!=null)foreach(var item in equipped)
                if(item!=null&&(int)item.slot>0&&(int)item.slot<5)slots[(int)item.slot]=true;
            return slots;
        }

        public static bool[] WardenSlots(ArmorItem[] equipped)
        {
            var slots=new bool[5];slots[0]=true; // Fixed cosmetic, independent of equipment.
            if(equipped==null)return slots;
            foreach(var item in equipped)
            {
                if(item==null||(int)item.slot<0||(int)item.slot>=slots.Length)continue;
                // Approved shared armor geometry covers every non-head product.
                // Head accessories retain their existing exact-product routing.
                if(item.slot!=ArmorSlot.Head)slots[(int)item.slot]=true;
            }
            return slots;
        }

        public static bool ShowWardenScanFeet(ArmorItem[] equipped,bool[] slots)
        {
            // Trousers never equip or display a second pair of boots implicitly.
            return slots!=null&&slots.Length>4&&slots[4];
        }

        public static bool HasWardenSlot(bool[] slots)
        {
            if(slots==null)return false;
            foreach(bool active in slots)if(active)return true;
            return false;
        }

        public static bool HasFullWardenSet(bool[] slots)
        {
            if(slots==null||slots.Length<5)return false;
            foreach(bool active in slots)if(!active)return false;
            return true;
        }

        public static ArmorItem[] WithoutHeadArmor(ArmorItem[] equipped)
        {
            if(equipped==null)return null;
            var visible=(ArmorItem[])equipped.Clone();
            for(int i=0;i<visible.Length;i++)if(visible[i]!=null&&visible[i].slot==ArmorSlot.Head)visible[i]=null;
            return visible;
        }
        public static ArmorItem[] WithoutWardenSlots(ArmorItem[] equipped,bool[] slots)
        {
            if(equipped==null)return null;
            var visible=(ArmorItem[])equipped.Clone();
            for(int i=0;i<visible.Length;i++)if(visible[i]!=null&&(int)visible[i].slot>=0
                &&(int)visible[i].slot<slots.Length&&slots[(int)visible[i].slot])visible[i]=null;
            return visible;
        }

        public static bool TryMelee(MeleeWeaponItem item, out Entry entry)
        {
            entry = default(Entry);
            if (item == null || !ValidId(item.catalogId)) return false;
            return Resolve(Product(Kind.Melee, item.catalogId, Socket.RightHand), out entry);
        }

        public static bool TryModule(ArtifactItem item, out Entry entry)
        {
            entry = default(Entry);
            if (item == null || !ValidId(item.catalogId)) return false;
            return Resolve(Product(Kind.Module, item.catalogId, Socket.Module), out entry);
        }

        // The existing Warden mesh/atlas remains a complete outfit. Require all five
        // starter identities so changing or removing one slot cannot leave stale armor.
        public static bool TryWardenSet(ArmorItem[] equipped, out Entry body,
            out Entry armor, out Entry mask)
        {
            body = armor = mask = default(Entry);
            if (equipped == null || equipped.Length < 5) return false;
            for (int i = 1; i < 5; i++)
            {
                var item = equipped[i];
                if (item == null || item.slot != (ArmorSlot)i ||
                    item.catalogId != ArmorProductCatalog.Get((ArmorSlot)i, 4, 0).id) return false;
            }
            var b = new Entry(Kind.WardenBody, "trail-warden", Warden + "head-rebuilt",
                Warden + "head-rebuilt-atlas", Socket.Chest);
            var a = new Entry(Kind.Armor, "trail-warden", Warden + "armor-rebuilt",
                Warden + "armor-rebuilt-atlas", Socket.Chest, ArmorSlot.Chest, true);
            var m = new Entry(Kind.WardenMask, "trail-warden", Warden + "mask-mobile",
                Warden + "mask-mobile-atlas", Socket.Head);
            if (!Available(b) || !Available(a) || !Available(m)) return false;
            body = b; armor = a; mask = m;
            return true;
        }
    }
}


