using UnityEngine;

namespace NeonVault
{
    // Shared identity and presentation. Infer methods deliberately never modify rolled stats.
    public static class ManufacturerCatalog
    {
        public const int Count=6;
        static readonly string[] ids={"kestrel","ironwake","voltarc","mireline","cinder","aegis"};
        static readonly string[] english={"Kestrel","Ironwake","Voltarc","Mireline","Cinder","Aegis"};
        static readonly string[] japanese={"ケストレル","アイアンウェイク","ヴォルトアーク","マイアライン","シンダー","イージス"};
        static readonly string[] descriptions={"Precise action, quick reload; reduced damage.","Heavy impact; slower fire and handling.","Rapid fire, large magazines; reduced impact and accuracy.","Medical recovery systems restore health; reduced direct damage.","Reclaimed scatter systems; broad spread and longer reload.","Reliable protection; lower output and faster reload."};
        static readonly string[] descriptionsJa={"高精度・高速装填。単発威力は低い。","高威力の重機関。射撃・装填は遅い。","高速連射・大容量。単発威力と精度は低い。","医療・防疫機構で体力を回復。直接威力は低い。","再生散弾機構。拡散が広く装填が遅い。","安定した防護と高速装填。攻撃出力は低い。"};
        public static string Id(int index)=>ids[(index%Count+Count)%Count];
        public static int Index(string id){for(int i=0;i<Count;i++)if(ids[i]==id)return i;return -1;}
        public static string Name(string id,bool japanese=false){int i=Index(id);return i<0?(japanese?"不明メーカー":"Unknown manufacturer"):(japanese?ManufacturerCatalog.japanese:english)[i];}
        public static string Description(string id,bool japanese=false){int i=Index(id);return i<0?"":(japanese?descriptionsJa:descriptions)[i];}
        public static Color Color(string id){switch(Index(id)){case 0:return new Color(.35f,.8f,.92f);case 1:return new Color(.66f,.35f,.2f);case 2:return new Color(.9f,.8f,.2f);case 3:return new Color(.4f,.8f,.4f);case 4:return new Color(.96f,.4f,.16f);default:return new Color(.4f,.55f,.9f);}}
        public static string ForDesign(int model,int design)=>Id(model+design);
        public static void InferGunIdentity(Gun gun)
        {
            if(gun==null)return;
            // Resolve the legacy scatter presentation before adding its new identity.
            if(string.IsNullOrEmpty(gun.catalogId)&&gun.effect==GunEffect.Scatter&&gun.modelVariant==0)gun.modelVariant=3;
            if(DistinctEquipmentDesigns.Find(gun.catalogId)==null)gun.designVariant=Mathf.Clamp(gun.designVariant,0,Arsenal.DesignsPerModel-1);
            if(Index(gun.manufacturerId)<0)gun.manufacturerId=ForDesign(gun.modelVariant,gun.designVariant);
            if(string.IsNullOrEmpty(gun.catalogId))gun.catalogId="gun-"+Mathf.Clamp(gun.modelVariant,0,Arsenal.ModelCount-1).ToString("00")+"-"+gun.designVariant.ToString("00");
        }
        // Creation only: callers must not invoke this while restoring saves.
        internal static void Apply(Gun gun)
        {
            switch(Index(gun.manufacturerId))
            {
                case 0:gun.accuracy=Mathf.Min(100,gun.accuracy+7);gun.reloadSeconds*=.84f;gun.damage*=.94f;break;
                case 1:gun.damage*=1.22f;gun.interval*=1.2f;gun.reloadSeconds*=1.15f;gun.movementBonus-=.025f;break;
                case 2:gun.interval*=.8f;gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*1.3f));gun.damage*=.86f;gun.accuracy=Mathf.Max(1,gun.accuracy-5);break;
                case 3:gun.bonusHealth+=6;gun.regenBonus+=.25f;gun.damage*=.92f;break;
                case 4:gun.pellets++;gun.damage*=.72f;gun.accuracy=Mathf.Max(1,gun.accuracy-9);gun.reloadSeconds*=1.18f;if(gun.effect==GunEffect.Normal)gun.effect=GunEffect.Scatter;break;
                case 5:gun.damageReductionBonus+=.025f;gun.reloadSeconds*=.9f;gun.damage*=.93f;break;
            }
        }
    }
}
