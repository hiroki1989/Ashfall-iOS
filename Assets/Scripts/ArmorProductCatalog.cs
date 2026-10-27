using UnityEngine;

namespace NeonVault
{
    public sealed class ArmorProduct
    {
        public readonly string id,manufacturerId;
        public readonly ArmorSlot slot;
        public readonly int visualVariant,designVariant;
        internal ArmorProduct(EquipmentRecipe r){slot=(ArmorSlot)r.slot;visualVariant=r.body%5;designVariant=r.index;id=r.id;manufacturerId=ManufacturerCatalog.Id(r.maker);}
        internal ArmorProduct(ArmorSlot slot,int visual,int design)
        {
            this.slot=slot;visualVariant=visual;designVariant=design;
            id="armor-"+slot.ToString().ToLowerInvariant()+"-"+visual+"-"+design;
            manufacturerId=ArmorProductCatalog.Manufacturers[(visual+design+(int)slot)%6];
        }
    }
    // Fixed product identity is independent of item level, rarity and random affixes.
    public static class ArmorProductCatalog
    {
        internal static readonly string[] Manufacturers={"kestrel","ironwake","voltarc","mireline","cinder","aegis"};
        static readonly ArmorProduct[] products=Build();
        public static int Count=>products.Length;
        public static System.Collections.Generic.IReadOnlyList<ArmorProduct> All=>System.Array.AsReadOnly(products);
        static ArmorProduct[] Build()
        {
            var result=new System.Collections.Generic.List<ArmorProduct>();
            foreach(var r in DistinctEquipmentDesigns.Armor)result.Add(new ArmorProduct(r));
            return result.ToArray();
        }

        public static ArmorProduct Default(ArmorSlot slot){foreach(var p in products)if(p.slot==slot&&!DistinctEquipmentDesigns.Find(p.id).legendary)return p;return null;}
        public static ArmorProduct Get(ArmorSlot slot,int visual,int design)
        {
            var p=Find("armor-"+slot.ToString().ToLowerInvariant()+"-"+visual+"-"+design);
            if(p==null)throw new System.ArgumentOutOfRangeException();return p;
        }
        public static ArmorProduct Roll(System.Random random,ArmorSlot slot)
        {
            if(!Armory.IsActiveSlot(slot))throw new System.ArgumentOutOfRangeException(nameof(slot));
            // Rejection samples the complete pool uniformly within this body slot.
            ArmorProduct p;do{p=products[random.Next(products.Length)];}while(p.slot!=slot||DistinctEquipmentDesigns.Find(p.id).legendary);return p;
        }
        public static ArmorProduct Find(string id)
        {if(string.IsNullOrEmpty(id))return null;foreach(var product in products)if(product.id==id)return product;
            var bits=id.Split('-');if(bits.Length==4&&bits[0]=="armor"&&System.Enum.TryParse(bits[1],true,out ArmorSlot slot)&&int.TryParse(bits[2],out int visual)&&int.TryParse(bits[3],out int design))return new ArmorProduct(slot,Mathf.Clamp(visual,0,4),Mathf.Max(0,design));return null;}
        public static string Name(ArmorProduct p,bool ja)
        {
            var recipe=DistinctEquipmentDesigns.Find(p.id);if(recipe!=null)return ja?recipe.japanese:recipe.name;
            string[] en={"SALVAGER","BREACHER","RANGER","SENTINEL","TRAIL WARDEN"};
            string[] jp={"回収兵","突破兵","遊撃兵","守備兵","トレイルウォーデン"};
            string[] designEn={"Patrol","Ballistic","Marksman","Assault","Courier","Survivor","Gunner","Pathfinder","Medic","Bulwark"};
            string[] designJa={"巡回","防弾","狙撃","突撃","伝令","生存","銃手","先導","衛生","防塁"};
            return DistinctEquipmentDesigns.ProductName("armor",p.visualVariant*50+p.designVariant*5+(int)p.slot,ja);
        }
        public static string Description(ArmorItem item,bool ja)
        {
            if(Find(item.catalogId)==null)return "";
            return ManufacturerCatalog.Name(item.manufacturerId,ja)+" · ";
        }
        // Called once during creation only. Saved effective stats are never modified on load.
        internal static void Apply(ArmorItem a,float power)
        {
            switch(a.manufacturerId)
            {
                case "kestrel":a.accuracyBonus+=.025f*power;a.moveBonus+=.018f*power;break;
                case "ironwake":a.maxHealth+=8*power;a.damageReduction+=.02f*power;a.moveBonus-=.025f*power;break;
                case "voltarc":a.fireBonus+=.025f*power;a.reloadBonus+=.035f*power;break;
                case "mireline":a.maxHealth+=5*power;a.regenBonus+=.22f*power;break;
                case "cinder":a.damageBonus+=.055f*power;a.damageReduction-=.012f*power;a.maxHealth-=2*power;break;
                case "aegis":a.damageReduction+=.018f*power;a.maxHealth+=4*power;break;
            }
            var recipe=DistinctEquipmentDesigns.For(a);
            if(recipe!=null){
                // Cover architecture changes its role rather than being a linear upgrade.
                switch(recipe.body){
                    case 0:a.moveBonus+=.018f*power;a.reloadBonus+=.018f*power;a.damageReduction-=.006f*power;break;
                    case 1:a.damageReduction+=.021f*power;a.maxHealth+=4*power;a.moveBonus-=.016f*power;break;
                    case 2:a.accuracyBonus+=.036f*power;a.reloadBonus+=.012f*power;a.dodgeBonus-=.008f*power;break;
                    case 3:a.maxHealth+=9*power;a.regenBonus+=.12f*power;a.reloadBonus-=.018f*power;break;
                    case 4:a.fireBonus+=.032f*power;a.moveBonus+=.016f*power;a.damageReduction-=.012f*power;break;
                    case 5:a.damageReduction+=.03f*power;a.maxHealth+=6*power;a.moveBonus-=.028f*power;break;
                }
                switch(recipe.attachment){
                    case 0:a.ammoBonus+=.09f*power;a.moveBonus-=.008f*power;break;
                    case 1:a.damageReduction+=.013f*power;a.reloadBonus-=.012f*power;break;
                    case 2:a.regenBonus+=.23f*power;a.maxHealth-=2*power;break;
                    case 3:a.reloadBonus+=.039f*power;a.damageBonus-=.008f*power;break;
                    case 4:a.ammoBonus+=.05f*power;a.dodgeBonus+=.012f*power;a.regenBonus-=.04f*power;break;
                    case 5:a.maxHealth+=7*power;a.damageReduction+=.01f*power;a.moveBonus-=.013f*power;break;
                }
                return;
            }
            ProductGeneration.Apply(a,power);
            switch(a.designVariant%10)
            {
                case 0:a.moveBonus+=.012f*power;a.accuracyBonus+=.01f*power;break;
                case 1:a.damageReduction+=.014f*power;a.maxHealth+=2*power;break;
                case 2:a.accuracyBonus+=.03f*power;a.damageBonus+=.01f*power;break;
                case 3:a.damageBonus+=.023f*power;a.fireBonus+=.01f*power;break;
                case 4:a.moveBonus+=.025f*power;a.reloadBonus+=.01f*power;break;
                case 5:a.maxHealth+=6*power;a.regenBonus+=.08f*power;break;
                case 6:a.fireBonus+=.024f*power;a.ammoBonus+=.04f*power;break;
                case 7:a.dodgeBonus+=.014f*power;a.moveBonus+=.012f*power;break;
                case 8:a.regenBonus+=.25f*power;a.maxHealth+=2*power;break;
                case 9:a.damageReduction+=.02f*power;a.moveBonus-=.01f*power;break;
            }
        }
    }
}
