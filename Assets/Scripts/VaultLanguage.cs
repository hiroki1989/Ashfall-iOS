using UnityEngine;
namespace NeonVault
{
    public static class VaultLanguage
    {
        public static bool Japanese {get;private set;}=PlayerPrefs.GetInt("Vault.Language",Application.systemLanguage==SystemLanguage.Japanese?0:1)==0;
        public static string T(string english,string japanese)=>Japanese?japanese:english;
        public static void Set(bool japanese,bool persist=true){Japanese=japanese;if(persist){PlayerPrefs.SetInt("Vault.Language",japanese?0:1);PlayerPrefs.Save();}}
        public static string RarityName(Rarity rarity)
        {return Japanese?new[]{"コモン","アンコモン","レア","エピック","レジェンダリー"}[(int)rarity]:rarity.ToString().ToUpper();}
        public static string GunName(Gun gun,bool? japanese=null)
        {
            if(LegendaryCatalog.Index(gun.legendaryKey)>=0)return LegendaryCatalog.GunName(gun,japanese??Japanese);
            var recipe=DistinctEquipmentDesigns.For(gun);if(recipe!=null)return (japanese??Japanese)?recipe.japanese:recipe.name;
            int variant=DisplayModel(gun);
            if(gun.visualSet==Arsenal.TrailWardenVisualSet)return (japanese??Japanese)?(variant==15?"ウォーデン・サイドアーム":"ウォーデン・ウッドストック"):(variant==15?"Warden Sidearm":"Warden Woodstock");
            return ((japanese??Japanese)?Arsenal.ModelJapanese[variant]:Arsenal.ModelNames[variant])+(gun.designVariant>0?" / "+Arsenal.DesignName(gun.designVariant,japanese??Japanese):"");
        }
        public static string GunDescription(Gun gun,bool? japanese=null)
        {
            bool ja=japanese??Japanese;
            if(!string.IsNullOrEmpty(gun.legendaryKey))return LegendaryCatalog.Description(gun,ja)+"\n"+ManufacturerBranding.GunDescription(gun,ja);
            return ManufacturerBranding.GunDescription(gun,ja)+"\n"+GunMechanismDescription(gun,ja);
        }
        public static string GunClassName(Gun gun,bool? japanese=null)
        {
            bool ja=japanese??Japanese;
            if(gun==null)return ja?"不明":"Unknown";
            var recipe=DistinctEquipmentDesigns.Find(gun.catalogId);
            if(recipe!=null&&!recipe.legendary&&gun.catalogId.StartsWith("distinct.gun."))return GunSeriesDesigns.ClassName(recipe.gunClass,ja);
            int model=DisplayModel(gun);
            switch(Arsenal.Category(model))
            {
                case WeaponClass.Handgun:return ja?"ハンドガン":"Handgun";
                case WeaponClass.Shotgun:return ja?"ショットガン":"Shotgun";
                case WeaponClass.SniperRifle:return ja?"スナイパーライフル":"Sniper Rifle";
                case WeaponClass.HeavyMachineGun:return ja?"ヘビーマシンガン":"Heavy Machine Gun";
                default:return ja?"アサルトライフル":"Assault Rifle";
            }
        }
        static string GunMechanismDescription(Gun gun,bool ja)
        {
            if(gun.visualSet==Arsenal.TrailWardenVisualSet)return ja?(DisplayModel(gun)==15?"トレイルウォーデンの自動拳銃 / 素早い鉛弾連射":"トレイルウォーデンの木製銃床ライフル / 制御しやすい鉛弾連射"):(DisplayModel(gun)==15?"Trail Warden automatic sidearm / rapid lead fire":"Trail Warden wood-stock rifle / controlled automatic fire");
            var recipe=DistinctEquipmentDesigns.For(gun);if(recipe!=null&&!recipe.legendary)return GunSeriesDesigns.Description(recipe,ja);
            if(!ja)return Arsenal.ModelDescription(DisplayModel(gun));
            if(DisplayModel(gun)==14)return "六発式リボルバー / 狙いを定めた単発の鉛弾";
            if(DisplayModel(gun)==15)return "ブローバック式オートピストル / 素早い連射";
            if(DisplayModel(gun)==16)return "大口径ハンドキャノン / 重い単発鉛弾";
            if(DisplayModel(gun)==17)return "小型消音ピストル / 精密な射撃";
            if(DisplayModel(gun)==12)return "現代式カービン / 制御しやすい鉛弾連射";
            if(DisplayModel(gun)==13)return "ポンプ式ショットガン / 接近戦向けの強力な散弾";
            switch(gun.effect){case GunEffect.Ricochet:return "鉛弾が跳弾 / 命中時に破片を放つ";case GunEffect.Orbit:return "同位体ドローンから照準へ弾を放つ";case GunEffect.Singularity:return "真空管駆動の吸引 / 時間差で圧壊";case GunEffect.Scatter:return "鉛の散弾 / 接近戦向けの広角射撃";default:return "機械式作動 / 鉛弾またはコイル射出";}

        }
        public static int DisplayModel(Gun gun)
        {
            int bespoke=BespokeLegendaryDesigns.Index(gun);if(bespoke>=0)return BespokeLegendaryDesigns.Model(bespoke);
            var recipe=DistinctEquipmentDesigns.Find(gun.catalogId);if(recipe!=null&&!recipe.legendary&&gun.catalogId.StartsWith("distinct.gun."))return recipe.family;
            // Legacy saves keep their IDs, statistics and stored names; presentation migrates only.
            if(gun.effect==GunEffect.Ricochet)return 9;
            if(gun.effect==GunEffect.Orbit)return 10;
            if(gun.effect==GunEffect.Singularity)return 11;
            if(gun.effect==GunEffect.Scatter&&gun.modelVariant==0&&string.IsNullOrEmpty(gun.catalogId))return 3;
            return Mathf.Clamp(gun.modelVariant,0,Arsenal.ModelCount-1);
        }
    }
    [DefaultExecutionOrder(100)]
    public class AimedWeapon : MonoBehaviour
    {
        public Transform muzzle,grip,supportGrip;
        public Vector3 ShoulderMount=>new Vector3(.25f,1.25f,.185f);
        public Vector3 StockContactPosition=>transform.TransformPoint(new Vector3(0,0,-.285f));
        public Vector3 GripPosition=>grip!=null?grip.position:transform.position;
        public Vector3 SupportGripPosition=>supportGrip!=null?supportGrip.position:transform.position;
        public Vector3 Direction=>transform.forward;
        public Vector3 MuzzlePosition=>muzzle!=null?muzzle.position:transform.position;
        public void Aim(Vector3 direction){direction.y=0;if(direction.sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(direction);SynchronizeMount();SynchronizeHands();}
        public void Recolor(Color color){var block=new MaterialPropertyBlock();block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*1.4f);foreach(var r in GetComponentsInChildren<Renderer>())if(r.name.Contains("energy")||r.name.Contains("Muzzle"))r.SetPropertyBlock(block);GetComponent<PaintedWeapon>()?.SetAccent(color);}
        float recoil;
        public Vector3 RecoilDisplacement=>Direction*recoil*transform.parent.lossyScale.x;
        public void Kick(){transform.parent.GetComponent<DetailedHeroPlayer3D>()?.NotifyFire();recoil=.13f;SynchronizeMount();transform.parent.GetComponent<LayeredPlayer2D>()?.Kick();SynchronizeHands();}
        void SynchronizeHands(){transform.parent.GetComponent<ActorEquipmentVisual>()?.SynchronizePose();}
        void SynchronizeMount(){transform.position=transform.parent.TransformPoint(ShoulderMount)-Direction*recoil*transform.parent.lossyScale.x;GetComponent<PaintedWeapon>()?.SynchronizeGripPosition();}
        void LateUpdate(){recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*1.3f);SynchronizeMount();transform.parent.GetComponent<LayeredPlayer2D>()?.RefreshPose();}
    }
}
