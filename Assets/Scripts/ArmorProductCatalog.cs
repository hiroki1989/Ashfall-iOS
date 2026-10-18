using UnityEngine;

namespace NeonVault
{
    public sealed class ArmorProduct
    {
        public readonly string id,manufacturerId;
        public readonly ArmorSlot slot;
        public readonly int visualVariant,designVariant;
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
            var result=new System.Collections.Generic.List<ArmorProduct>();var counts=new int[6];
            for(int slot=0;slot<5;slot++)for(int visual=0;visual<5;visual++)for(int design=0;design<10;design++)Add(new ArmorProduct((ArmorSlot)slot,visual,design));
            for(int design=10;result.Count<2520;design++)for(int slot=0;slot<5;slot++)for(int visual=0;visual<5;visual++)
            {var p=new ArmorProduct((ArmorSlot)slot,visual,design);if(counts[ManufacturerCatalog.Index(p.manufacturerId)]<420)Add(p);}
            return result.ToArray();
            void Add(ArmorProduct p){result.Add(p);counts[ManufacturerCatalog.Index(p.manufacturerId)]++;}
        }
        public static ArmorProduct Get(ArmorSlot slot,int visual,int design)
        {
            var p=Find("armor-"+slot.ToString().ToLowerInvariant()+"-"+visual+"-"+design);
            if(p==null)throw new System.ArgumentOutOfRangeException();return p;
        }
        public static ArmorProduct Roll(System.Random random,ArmorSlot slot)
        {
            // Rejection samples the complete pool uniformly within this body slot.
            ArmorProduct p;do{p=products[random.Next(products.Length)];}while(p.slot!=slot);return p;
        }
        public static ArmorProduct Find(string id)
        {if(string.IsNullOrEmpty(id))return null;foreach(var product in products)if(product.id==id)return product;return null;}
        public static string Name(ArmorProduct p,bool ja)
        {
            string[] en={"SALVAGER","BREACHER","RANGER","SENTINEL","TRAIL WARDEN"};
            string[] jp={"回収兵","突破兵","遊撃兵","守備兵","トレイルウォーデン"};
            string[] designEn={"Patrol","Ballistic","Marksman","Assault","Courier","Survivor","Gunner","Pathfinder","Medic","Bulwark"};
            string[] designJa={"巡回","防弾","狙撃","突撃","伝令","生存","銃手","先導","衛生","防塁"};
            return ManufacturerCatalog.Name(p.manufacturerId,ja)+" / "+(ja?jp[p.visualVariant]+"・"+designJa[p.designVariant%10]+ProductGeneration.Name(p.designVariant,true):en[p.visualVariant]+" "+designEn[p.designVariant%10]+ProductGeneration.Name(p.designVariant,false))+" / "+Armory.SlotName(p.slot,ja);
        }
        public static string Description(ArmorItem item,bool ja)
        {
            if(Find(item.catalogId)==null)return "";
            string en,jp;
            switch(item.manufacturerId)
            {
                case "kestrel":en="KESTREL: precision / mobility";jp="KESTREL：精度・機動";break;
                case "ironwake":en="IRONWAKE: health / protection; slower movement";jp="IRONWAKE：生命・防護、移動低下";break;
                case "voltarc":en="VOLTARC: fire rate / reload";jp="VOLTARC：連射・装填";break;
                case "mireline":en="MIRELINE: health / regeneration";jp="MIRELINE：生命・再生";break;
                case "cinder":en="CINDER: damage at the cost of protection";jp="CINDER：威力強化、防護低下";break;
                default:en="AEGIS: reliable protection / vitality";jp="AEGIS：安定防護・生命";break;
            }
            return (ja?jp:en)+" · ";
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
