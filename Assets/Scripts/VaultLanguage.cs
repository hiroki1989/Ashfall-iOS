using UnityEngine;
namespace NeonVault
{
    public static class VaultLanguage
    {
        public static bool Japanese {get;private set;}
        static VaultLanguage()
        {
            Japanese=PlayerPrefs.GetInt("Vault.Language",Application.systemLanguage==SystemLanguage.Japanese?0:1)==0;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            SyncSteamLanguage();
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int RegGetValueW(System.IntPtr hkey, string lpSubKey, string lpValue, int dwFlags, out int pdwType, System.Text.StringBuilder pvData, ref int pcbData);

        static string ReadSteamRegistryValue(string valueName)
        {
            try
            {
                var hklmOrHkcu=new System.IntPtr(unchecked((int)0x80000001));
                int size=1024;
                var sb=new System.Text.StringBuilder(size/2);
                int type;
                if(RegGetValueW(hklmOrHkcu, @"Software\Valve\Steam", valueName, 0x00000002, out type, sb, ref size)==0)
                    return sb.ToString();
            }
            catch {}
            return null;
        }

        static string DetectSteamAppLanguage()
        {
            string envLang=System.Environment.GetEnvironmentVariable("SteamAppLanguage")??System.Environment.GetEnvironmentVariable("STEAM_LANGUAGE");
            if(!string.IsNullOrEmpty(envLang))return envLang.Trim();

            var dirs=new System.Collections.Generic.List<string>();
            try
            {
                string gameDir=System.IO.Directory.GetParent(Application.dataPath)?.FullName;
                string steamApps=string.IsNullOrEmpty(gameDir)?null:System.IO.Path.GetFullPath(System.IO.Path.Combine(gameDir,"..",".."));
                if(!string.IsNullOrEmpty(steamApps)&&System.IO.Directory.Exists(steamApps))dirs.Add(steamApps);
            }
            catch {}

            string steamPath=ReadSteamRegistryValue("SteamPath");
            if(!string.IsNullOrEmpty(steamPath))
            {
                string mainApps=System.IO.Path.Combine(steamPath.Replace('/','\\'),"steamapps");
                if(System.IO.Directory.Exists(mainApps)&&!dirs.Contains(mainApps))dirs.Add(mainApps);
                string libVdf=System.IO.Path.Combine(mainApps,"libraryfolders.vdf");
                if(System.IO.File.Exists(libVdf))
                {
                    try
                    {
                        foreach(string line in System.IO.File.ReadAllLines(libVdf))
                        {
                            string p=ExtractVdfPair(line,"path");
                            if(!string.IsNullOrEmpty(p))
                            {
                                string candidate=System.IO.Path.Combine(p.Replace("\\\\","\\"),"steamapps");
                                if(System.IO.Directory.Exists(candidate)&&!dirs.Contains(candidate))dirs.Add(candidate);
                            }
                        }
                    }
                    catch {}
                }
            }

            string folderName=null;
            try { folderName=System.IO.Directory.GetParent(Application.dataPath)?.Name; } catch {}
            bool isDemo=!string.IsNullOrEmpty(folderName)&&folderName.IndexOf("Demo",System.StringComparison.OrdinalIgnoreCase)>=0;
            string envId=System.Environment.GetEnvironmentVariable("SteamAppId")??System.Environment.GetEnvironmentVariable("SteamGameId");
            var appIds=new System.Collections.Generic.List<string>();
            if(!string.IsNullOrEmpty(envId))appIds.Add(envId.Trim());
            appIds.Add(isDemo?"5620390":"5614640");
            appIds.Add(isDemo?"5614640":"5620390");

            foreach(string dir in dirs)
            {
                foreach(string appId in appIds)
                {
                    string acf=System.IO.Path.Combine(dir,"appmanifest_"+appId+".acf");
                    if(System.IO.File.Exists(acf))
                    {
                        string lang=ParseAcfLanguage(acf);
                        if(!string.IsNullOrEmpty(lang))return lang;
                    }
                }
                if(!string.IsNullOrEmpty(folderName))
                {
                    try
                    {
                        foreach(string acf in System.IO.Directory.GetFiles(dir,"appmanifest_*.acf"))
                        {
                            string text=System.IO.File.ReadAllText(acf);
                            if(text.IndexOf("\""+folderName+"\"",System.StringComparison.OrdinalIgnoreCase)>=0)
                            {
                                string lang=ParseAcfLanguageText(text);
                                if(!string.IsNullOrEmpty(lang))return lang;
                            }
                        }
                    }
                    catch {}
                }
            }
            return null;
        }

        static string ParseAcfLanguage(string path)
        {
            try { return ParseAcfLanguageText(System.IO.File.ReadAllText(path)); }
            catch { return null; }
        }

        static string ParseAcfLanguageText(string text)
        {
            if(string.IsNullOrEmpty(text))return null;
            string lang=ExtractAcfSectionKey(text,"UserConfig","language");
            if(!string.IsNullOrEmpty(lang))return lang;
            return ExtractAcfSectionKey(text,"MountedConfig","language");
        }

        static string ExtractAcfSectionKey(string text, string section, string key)
        {
            int idx=text.IndexOf("\""+section+"\"",System.StringComparison.OrdinalIgnoreCase);
            if(idx<0)return null;
            int open=text.IndexOf('{',idx);
            if(open<0)return null;
            int close=text.IndexOf('}',open+1);
            if(close<0)return null;
            string body=text.Substring(open+1,close-open-1);
            foreach(string line in body.Split('\n'))
            {
                string val=ExtractVdfPair(line,key);
                if(!string.IsNullOrEmpty(val))return val;
            }
            return null;
        }

        static string ExtractVdfPair(string line, string expectedKey)
        {
            if(string.IsNullOrEmpty(line))return null;
            int q1=line.IndexOf('"');if(q1<0)return null;
            int q2=line.IndexOf('"',q1+1);if(q2<0)return null;
            string key=line.Substring(q1+1,q2-q1-1);
            if(!string.Equals(key,expectedKey,System.StringComparison.OrdinalIgnoreCase))return null;
            int q3=line.IndexOf('"',q2+1);if(q3<0)return null;
            int q4=line.IndexOf('"',q3+1);if(q4<0)return null;
            return line.Substring(q3+1,q4-q3-1).Trim();
        }

        static void SyncSteamLanguage()
        {
            try
            {
                string appLang=DetectSteamAppLanguage();
                if(!string.IsNullOrEmpty(appLang))
                {
                    string prev=PlayerPrefs.GetString("Vault.SteamAppLanguage","");
                    if(!PlayerPrefs.HasKey("Vault.Language")||!string.Equals(appLang,prev,System.StringComparison.OrdinalIgnoreCase))
                    {
                        Japanese=string.Equals(appLang,"japanese",System.StringComparison.OrdinalIgnoreCase);
                        PlayerPrefs.SetInt("Vault.Language",Japanese?0:1);
                        PlayerPrefs.SetString("Vault.SteamAppLanguage",appLang);
                        PlayerPrefs.Save();
                    }
                }
                else if(!PlayerPrefs.HasKey("Vault.Language"))
                {
                    string regLang=ReadSteamRegistryValue("Language");
                    if(!string.IsNullOrEmpty(regLang))
                        Japanese=string.Equals(regLang.Trim(),"japanese",System.StringComparison.OrdinalIgnoreCase);
                }
            }
            catch {}
        }
#endif
        public static string T(string english,string japanese)=>Japanese?japanese:english;
        public static void Set(bool japanese,bool persist=true){Japanese=japanese;if(persist){PlayerPrefs.SetInt("Vault.Language",japanese?0:1);PlayerPrefs.Save();}}
        public static string RarityName(Rarity rarity)
        {return Japanese?new[]{"コモン","アンコモン","レア","エピック","レジェンダリー"}[(int)rarity]:rarity.ToString().ToUpper();}
        public static string GunName(Gun gun,bool? japanese=null)
        {
            string bossRelic=BossRelicFoundation.Name(gun?.bossRelicId,japanese??Japanese);if(!string.IsNullOrEmpty(bossRelic))return bossRelic;
            if(LegendaryCatalog.Index(gun.legendaryKey)>=0)return LegendaryCatalog.GunName(gun,japanese??Japanese);
            var recipe=DistinctEquipmentDesigns.For(gun);if(recipe!=null)return (japanese??Japanese)?recipe.japanese:recipe.name;
            int variant=DisplayModel(gun);
            if(gun.visualSet==Arsenal.TrailWardenVisualSet)return (japanese??Japanese)?(variant==15?"ウォーデン・サイドアーム":"ウォーデン・ウッドストック"):(variant==15?"Warden Sidearm":"Warden Woodstock");
            return ((japanese??Japanese)?Arsenal.ModelJapanese[variant]:Arsenal.ModelNames[variant])+(gun.designVariant>0?" / "+Arsenal.DesignName(gun.designVariant,japanese??Japanese):"");
        }
        public static string GunDescription(Gun gun,bool? japanese=null)
        {
            bool ja=japanese??Japanese;
            string element=GunElements.Description(gun,ja);
            string prefix=string.IsNullOrEmpty(element)?"":element+"\n";
            if(BossRelicFoundation.Decode(gun?.bossRelicId,out _,out _))return prefix+LegendaryCatalog.Description(gun,ja)+"\n"+ManufacturerBranding.GunDescription(gun,ja);
            if(!string.IsNullOrEmpty(gun.legendaryKey))return prefix+LegendaryCatalog.Description(gun,ja)+"\n"+ManufacturerBranding.GunDescription(gun,ja);
            return prefix+ManufacturerBranding.GunDescription(gun,ja)+"\n"+GunMechanismDescription(gun,ja);
        }
        public static string GunClassName(Gun gun,bool? japanese=null)
        {
            bool ja=japanese??Japanese;
            if(gun==null)return ja?"不明":"Unknown";
            if(GunClassCorrections.IsStrongholdBastion(gun))return ja?"アサルトライフル":"Assault Rifle";
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
            if(GunClassCorrections.IsStrongholdBastion(gun))return ja?"アサルトライフル / 安定した連射":"Assault rifle / controlled automatic fire";
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
            if(GunClassCorrections.IsStrongholdBastion(gun))return 12;
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
