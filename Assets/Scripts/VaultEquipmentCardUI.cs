using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        static Texture2D eqCardIconsTex;
        static Texture2D eqHeaderGradTex;
        static Texture2D eqBodyGradTex;

        // Icon indices in Resources/Art/UI/equipment_card_icons (6 cols x 4 rows):
        //  0: Damage        1: Accuracy      2: Reload        3: Fire Rate     4: Magazine      5: Critical
        //  6: Range         7: Speed         8: Armor/Shield  9: Max HP       10: Regen        11: Move Speed
        // 12: Dodge        13: Melee Blade  14: Melee Arc    15: Module       16: Cooldown     17: Legendary Star
        // 18: Affix Mod    19: Maker Gear   20: Scrap Value  21: Pet Paw      22: Elemental    23: Score Crest

        struct EqStatCell
        {
            public int icon;
            public string value;
            public string label;
            public Color tint;
            public EqStatCell(int icon, string value, string label, Color tint)
            {
                this.icon = icon;
                this.value = value;
                this.label = label;
                this.tint = tint;
            }
        }

        struct EqFeatureRow
        {
            public int icon;
            public GunElement element; // if != None, uses vibrant elemental icon from gun-element-icons
            public Color badgeBg;
            public Color iconTint;
            public Color borderTint;
            public string richText;
        }

        static void EnsureEquipmentCardTextures()
        {
            if (!eqCardIconsTex)
                eqCardIconsTex = Resources.Load<Texture2D>("Art/UI/equipment_card_icons");
            if (!eqHeaderGradTex)
            {
                eqHeaderGradTex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 32; y++)
                {
                    float t = y / 31f;
                    float alpha = Mathf.Lerp(0.18f, 0.58f, t * t);
                    eqHeaderGradTex.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
                }
                eqHeaderGradTex.Apply(false, true);
            }
            if (!eqBodyGradTex)
            {
                eqBodyGradTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 64; y++)
                {
                    float t = y / 63f;
                    float edge = Mathf.Pow(t, 1.8f) * 0.28f + Mathf.Pow(1f - t, 2.4f) * 0.14f;
                    eqBodyGradTex.SetPixel(0, y, new Color(1f, 1f, 1f, edge));
                }
                eqBodyGradTex.Apply(false, true);
            }
        }

        public static void DrawEqCardIcon(Rect r, int iconIndex, Color tint)
        {
            EnsureEquipmentCardTextures();
            if (!eqCardIconsTex) return;
            int idx = Mathf.Clamp(iconIndex, 0, 23);
            int col = idx % 6, row = idx / 6;
            float insetX = 0.5f / eqCardIconsTex.width;
            float insetY = 0.5f / eqCardIconsTex.height;
            Rect uv = new Rect(col / 6f + insetX, 1f - (row + 1) / 4f + insetY, 1f / 6f - 2f * insetX, 1f / 4f - 2f * insetY);
            Color prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(r, eqCardIconsTex, uv, true);
            GUI.color = prev;
        }

        static void DrawEqElementSymbol(Rect r, GunElement element)
        {
            int cell = (int)element - 1;
            if (cell < 0 || cell >= 4) return;
            var tex = Resources.Load<Texture2D>("Art/UI/gun-element-icons");
            if (!tex) { DrawEqCardIcon(r, 22, GunElements.ColorFor(element)); return; }
            float inset = 0.5f / tex.width;
            Rect uv = new Rect(cell * 0.25f + inset, 0f, 0.25f - 2f * inset, 1f);
            float aspect = 0.75f;
            float h = r.height;
            float w = h * aspect;
            if (w > r.width) { w = r.width; h = w / aspect; }
            Rect fit = new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(fit, tex, uv, true);
            GUI.color = prev;
        }

        public static Color RarityThemeColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:    return new Color(0.66f, 0.73f, 0.78f);
                case Rarity.Uncommon:  return new Color(0.24f, 0.90f, 0.50f);
                case Rarity.Rare:      return new Color(0.24f, 0.64f, 1.00f);
                case Rarity.Epic:      return new Color(0.76f, 0.34f, 1.00f);
                case Rarity.Legendary: return new Color(1.00f, 0.66f, 0.14f);
                default:               return new Color(0.66f, 0.73f, 0.78f);
            }
        }

        public static Color RarityPanelBaseColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:    return new Color(0.085f, 0.102f, 0.118f, 0.97f);
                case Rarity.Uncommon:  return new Color(0.052f, 0.128f, 0.092f, 0.97f);
                case Rarity.Rare:      return new Color(0.050f, 0.106f, 0.175f, 0.97f);
                case Rarity.Epic:      return new Color(0.118f, 0.064f, 0.172f, 0.97f);
                case Rarity.Legendary: return new Color(0.165f, 0.112f, 0.042f, 0.98f);
                default:               return new Color(0.085f, 0.102f, 0.118f, 0.97f);
            }
        }

        public static Color RarityHeaderBaseColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:    return new Color(0.15f, 0.18f, 0.20f, 0.96f);
                case Rarity.Uncommon:  return new Color(0.08f, 0.24f, 0.15f, 0.96f);
                case Rarity.Rare:      return new Color(0.08f, 0.19f, 0.31f, 0.96f);
                case Rarity.Epic:      return new Color(0.21f, 0.10f, 0.30f, 0.96f);
                case Rarity.Legendary: return new Color(0.31f, 0.20f, 0.06f, 0.97f);
                default:               return new Color(0.15f, 0.18f, 0.20f, 0.96f);
            }
        }

        public void DrawRarityBackdrop(Rect r, Rarity rarity, bool selected = false, bool drawHeaderBand = false, float headerHeight = 0f)
        {
            EnsureEquipmentCardTextures();
            Color theme = RarityThemeColor(rarity);
            Color baseBg = RarityPanelBaseColor(rarity);

            // Drop shadow
            Box(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));

            // Full rarity-tinted dark slate panel
            Box(r, selected ? Color.Lerp(baseBg, theme, 0.12f) : baseBg);

            // Soft rarity gradient wash across entire panel
            if (eqBodyGradTex)
            {
                Color prev = GUI.color;
                GUI.color = new Color(theme.r, theme.g, theme.b, selected ? 0.62f : 0.42f);
                GUI.DrawTexture(r, eqBodyGradTex, ScaleMode.StretchToFill, true);
                GUI.color = prev;
            }

            // Optional Borderlands-style distinct rarity header band
            if (drawHeaderBand && headerHeight > 4f)
            {
                float hh = Mathf.Min(headerHeight, r.height);
                Rect hr = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, hh - 2f);
                Box(hr, RarityHeaderBaseColor(rarity));
                if (eqHeaderGradTex)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(theme.r, theme.g, theme.b, 0.65f);
                    GUI.DrawTexture(hr, eqHeaderGradTex, ScaleMode.StretchToFill, true);
                    GUI.color = prev;
                }
                Box(new Rect(hr.x, hr.yMax - 2f, hr.width, 2f), new Color(theme.r, theme.g, theme.b, 0.80f));
            }

            // Outer rarity frame + corner brackets
            Color border = selected ? Color.Lerp(theme, Color.white, 0.42f) : new Color(theme.r, theme.g, theme.b, 0.82f);
            float bw = selected ? 2.5f : 2f;
            Box(new Rect(r.x, r.y, r.width, bw), border);
            Box(new Rect(r.x, r.yMax - bw, r.width, bw), border);
            Box(new Rect(r.x, r.y, bw, r.height), border);
            Box(new Rect(r.xMax - bw, r.y, bw, r.height), border);

            // Inner subtle bevel line
            Color inner = new Color(theme.r, theme.g, theme.b, 0.18f);
            Box(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, 1f), inner);
            Box(new Rect(r.x + 4f, r.yMax - 5f, r.width - 8f, 1f), inner);

            // Tactical corner notches
            float cLen = Mathf.Min(16f, Mathf.Min(r.width, r.height) * 0.16f);
            Color cornerCol = Color.Lerp(theme, Color.white, selected ? 0.6f : 0.28f);
            Box(new Rect(r.x, r.y, cLen, 3f), cornerCol);
            Box(new Rect(r.x, r.y, 3f, cLen), cornerCol);
            Box(new Rect(r.xMax - cLen, r.y, cLen, 3f), cornerCol);
            Box(new Rect(r.xMax - 3f, r.y, 3f, cLen), cornerCol);
            Box(new Rect(r.x, r.yMax - 3f, cLen, 3f), cornerCol);
            Box(new Rect(r.x, r.yMax - cLen, 3f, cLen), cornerCol);
            Box(new Rect(r.xMax - cLen, r.yMax - 3f, cLen, 3f), cornerCol);
            Box(new Rect(r.xMax - 3f, r.yMax - cLen, 3f, cLen), cornerCol);
        }

        static Rarity ItemRarityOf(object item)
        {
            if (item is Gun g) return g.rarity;
            if (item is ArmorItem a) return a.rarity;
            if (item is ArtifactItem m) return m.rarity;
            if (item is MeleeWeaponItem w) return w.rarity;
            return Rarity.Common;
        }

        static int ItemLevelOf(object item)
        {
            if (item is Gun g) return g.level;
            if (item is ArmorItem a) return a.level;
            if (item is ArtifactItem m) return m.level;
            if (item is MeleeWeaponItem w) return w.level;
            return 1;
        }

        int ItemScoreOf(object item)
        {
            if (item is Gun g) return GearScoring.Score(g);
            if (item is ArmorItem a) return GearScoring.Score(a);
            if (item is ArtifactItem m) return GearScoring.Score(m);
            if (item is MeleeWeaponItem w) return MeleeWeaponCatalog.Score(w);
            return 0;
        }

        int ItemSaleValueOf(object item)
        {
            if (item is Gun g) return SaleValue(g);
            if (item is ArmorItem a) return SaleValue(a);
            if (item is ArtifactItem m) return SaleValue(m);
            if (item is MeleeWeaponItem w) return SaleValue(w);
            return 0;
        }

        static string ItemMakerOf(object item)
        {
            if (item is Gun g) return ManufacturerBranding.Maker(g);
            if (item is ArmorItem a) return ManufacturerBranding.Maker(a);
            if (item is ArtifactItem m) return ManufacturerBranding.Maker(m);
            if (item is MeleeWeaponItem w) return ManufacturerBranding.Maker(w);
            return null;
        }

        string ItemDisplayNameOf(object item)
        {
            if (item is Gun g) return VaultLanguage.GunName(g);
            if (item is ArmorItem a) return Armory.Name(a);
            if (item is ArtifactItem m) return ArtifactCatalog.DisplayName(m);
            if (item is MeleeWeaponItem w) return MeleeWeaponCatalog.Name(w);
            return "";
        }

        string ItemTypeSubtitleOf(object item)
        {
            if (item is Gun g) return VaultLanguage.GunClassName(g);
            if (item is ArmorItem a) return Armory.SlotName(a.slot) + T(" ARMOR", " アーマー");
            if (item is ArtifactItem m)
            {
                switch (m.effect)
                {
                    case ArtifactEffect.KillShield: return T("SHIELD MODULE", "シールドモジュール");
                    case ArtifactEffect.DashPulse: return T("PULSE MODULE", "パルスモジュール");
                    case ArtifactEffect.ReloadSurge: return T("SURGE MODULE", "サージモジュール");
                    default: return T("CONVERSION MODULE", "変換モジュール");
                }
            }
            if (item is MeleeWeaponItem w) return MeleeWeaponCatalog.Name(w.family);
            return "";
        }

        static int AffixIconForType(string affixType)
        {
            switch (affixType)
            {
                case "damage": case "handgunDamage": case "shotgunDamage": case "rifleDamage": case "sniperDamage": case "heavyDamage": return 0;
                case "health": return 9;
                case "fireRate": return 3;
                case "reload": return 2;
                case "accuracy": return 1;
                case "mitigation": return 8;
                case "moveSpeed": return 11;
                case "regen": return 10;
                case "ammo": return 4;
                case "critChance": case "critDamage": return 5;
                case "elementDamage": return 22;
                case "petDamage": case "petSpeed": return 21;
                case "rangeRetention": return 6;
                default: return 18;
            }
        }

        static string FormatAffixHighlight(GearAffix a)
        {
            if (a == null) return null;
            string[] types = { "damage", "health", "fireRate", "reload", "accuracy", "mitigation", "moveSpeed", "regen", "ammo", "critChance", "critDamage", "elementDamage", "handgunDamage", "shotgunDamage", "rifleDamage", "sniperDamage", "heavyDamage", "petDamage", "petSpeed", "rangeRetention" };
            string[] en = { "Damage", "Max HP", "Fire Rate", "Reload Speed", "Accuracy", "Damage Reduction", "Move Speed", "Health Regen", "Magazine Size", "Critical Chance", "Critical Damage", "Elemental Damage", "Handgun Damage", "Shotgun Damage", "Assault Rifle Damage", "Sniper Damage", "Heavy MG Damage", "Companion Damage", "Companion Speed", "30m Range Retention" };
            string[] ja = { "武器ダメージ", "最大HP", "連射速度", "リロード速度", "命中精度", "被ダメージ軽減", "移動速度", "HP自動回復", "マガジン装弾数", "クリティカル率", "クリティカル倍率", "属性ダメージ", "ハンドガン威力", "ショットガン威力", "アサルトライフル威力", "スナイパー威力", "ヘビーMG威力", "ペット攻撃力", "ペット攻撃速度", "30m距離減衰耐性" };
            int i = Array.IndexOf(types, a.type);
            if (i < 0) return null;
            bool jaLang = VaultLanguage.Japanese;
            string label = jaLang ? ja[i] : en[i];
            bool pct = i == 0 || i == 2 || i == 3 || i == 4 || i == 5 || i == 6 || i == 8 || i == 9 || i == 10 || i >= 11;
            string num = "+" + (pct ? a.value * 100f : a.value).ToString(pct ? "0.#" : "0.0") + (pct ? "%" : i == 7 ? (jaLang ? " HP/秒" : " HP/s") : "");
            return "<color=#5CE1FF>" + num + "</color> " + label;
        }

        static readonly Regex RxHighlightNums = new Regex(@"([+\-]?\d+(?:\.\d+)?(?:\s*%|秒|s|m|体|発|回)?)", RegexOptions.Compiled);

        static string HighlightNumbersInText(string raw, string numHex = "#5CE1FF")
        {
            if (string.IsNullOrEmpty(raw)) return "";
            if (raw.Contains("<color=")) return raw;
            return RxHighlightNums.Replace(raw, "<color=" + numHex + ">$1</color>");
        }

        void BuildItemCardData(object item, out List<EqStatCell> primaryStats, out List<EqStatCell> subStats, out List<EqFeatureRow> features)
        {
            primaryStats = new List<EqStatCell>();
            subStats = new List<EqStatCell>();
            features = new List<EqFeatureRow>();
            bool ja = VaultLanguage.Japanese;

            Color cWhite = new Color(0.95f, 0.96f, 0.98f);
            Color cGold  = new Color(1.00f, 0.84f, 0.32f);
            Color cCyan  = new Color(0.42f, 0.88f, 1.00f);
            Color cGreen = new Color(0.40f, 0.95f, 0.62f);

            if (item is Gun g)
            {
                GearBalance.Migrate(g);
                float avgDmg = g.damage;
                string dmgStr = avgDmg >= 100f ? Mathf.RoundToInt(avgDmg).ToString("N0") : avgDmg.ToString("0.0");
                if (g.pellets > 1) dmgStr += "×" + g.pellets;
                float rate = 1f / Mathf.Max(0.05f, g.interval);
                primaryStats.Add(new EqStatCell(0, dmgStr, T("DMG", "威力"), cWhite));
                primaryStats.Add(new EqStatCell(1, Arsenal.Accuracy(g).ToString("0") + "%", T("ACC", "精度"), cWhite));
                primaryStats.Add(new EqStatCell(2, g.reloadSeconds.ToString("0.0") + T("s", "秒"), T("RELOAD", "装填"), cWhite));
                primaryStats.Add(new EqStatCell(3, rate.ToString("0.0") + T("/s", "/秒"), T("RATE", "連射"), cWhite));
                primaryStats.Add(new EqStatCell(4, g.magazine.ToString(), T("MAG", "弾数"), cWhite));

                // Sub-banner stats: Crit + 30m Range Retention
                float critPct = GunCriticalChance(g) * 100f;
                float critMul = GunCriticalMultiplier(g);
                subStats.Add(new EqStatCell(5, critPct.ToString("0.#") + "% (×" + critMul.ToString("0.0#") + ")", T("CRIT", "会心"), cGold));
                subStats.Add(new EqStatCell(6, (Arsenal.RangeRetention(g) * 100f).ToString("0") + "%", T("30m RNG", "30m残存"), cCyan));

                // 1) Legendary / Boss Relic Unique Effect FIRST with Gold Star
                string legDesc = null;
                if (BossRelicFoundation.Decode(g.bossRelicId, out _, out _))
                    legDesc = BossRelicFoundation.Description(g.bossRelicId, ja);
                else if (!string.IsNullOrEmpty(g.legendaryKey))
                    legDesc = LegendaryCatalog.Description(g, ja);
                if (!string.IsNullOrEmpty(legDesc))
                {
                    features.Add(new EqFeatureRow
                    {
                        icon = 17,
                        badgeBg = new Color(0.28f, 0.19f, 0.04f, 0.95f),
                        iconTint = new Color(1f, 0.82f, 0.18f),
                        borderTint = new Color(1f, 0.72f, 0.16f, 0.85f),
                        richText = "<color=#FFD447>" + T("UNIQUE", "固有効果") + "</color> - " + HighlightNumbersInText(legDesc, "#5CE1FF")
                    });
                }

                // 2) Elemental Property Row (if present)
                if (g.element != GunElement.None)
                {
                    Color elCol = GunElements.ColorFor(g.element);
                    string elHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(elCol, Color.white, 0.25f));
                    string elText = GunElements.Description(g, ja);
                    int colon = elText.IndexOf(':');
                    if (colon < 0) colon = elText.IndexOf('：');
                    string formattedEl = colon >= 0
                        ? "<color=#" + elHex + ">" + elText.Substring(0, colon) + "</color> - " + HighlightNumbersInText(elText.Substring(colon + 1).Trim(), "#5CE1FF")
                        : HighlightNumbersInText(elText, "#5CE1FF");
                    features.Add(new EqFeatureRow
                    {
                        icon = 22,
                        element = g.element,
                        badgeBg = new Color(elCol.r * 0.25f, elCol.g * 0.25f, elCol.b * 0.25f, 0.92f),
                        iconTint = elCol,
                        borderTint = new Color(elCol.r, elCol.g, elCol.b, 0.75f),
                        richText = formattedEl
                    });
                }

                // 3) Shotgun close-range trait
                if (Arsenal.Category(VaultLanguage.DisplayModel(g)) == WeaponClass.Shotgun)
                {
                    features.Add(new EqFeatureRow
                    {
                        icon = 0,
                        badgeBg = new Color(0.10f, 0.18f, 0.24f, 0.90f),
                        iconTint = cCyan,
                        borderTint = new Color(0.30f, 0.65f, 0.85f, 0.55f),
                        richText = "<color=#9AD8FF>" + T("CLOSE QUARTERS", "近距離補正") + "</color> - " + HighlightNumbersInText(T("+25% damage within 2m (fades by 8m)", "2m以内で威力+25%（8mで消失）"))
                    });
                }

                // 4) Rolled Affixes (each with dedicated stat icon)
                if (g.affixes != null)
                {
                    foreach (var af in g.affixes)
                    {
                        string text = FormatAffixHighlight(af);
                        if (string.IsNullOrEmpty(text)) continue;
                        features.Add(new EqFeatureRow
                        {
                            icon = AffixIconForType(af.type),
                            badgeBg = new Color(0.08f, 0.16f, 0.22f, 0.92f),
                            iconTint = new Color(0.58f, 0.88f, 1.00f),
                            borderTint = new Color(0.32f, 0.62f, 0.82f, 0.55f),
                            richText = "<color=#FFD166>❖</color> " + text
                        });
                    }
                }
            }
            else if (item is ArmorItem a)
            {
                GearBalance.MigrateFireRate(a);
                primaryStats.Add(new EqStatCell(8, (a.damageReduction * 100f).ToString("0.#") + "%", T("GUARD", "軽減"), cCyan));
                primaryStats.Add(new EqStatCell(9, "+" + a.maxHealth.ToString("0.#"), T("MAX HP", "最大HP"), cGreen));

                var extras = new List<EqStatCell>();
                if (Mathf.Abs(a.damageBonus) > 0.0001f) extras.Add(new EqStatCell(0, "+" + (a.damageBonus * 100f).ToString("0.#") + "%", T("DMG", "威力"), cWhite));
                if (Mathf.Abs(a.moveBonus) > 0.0001f) extras.Add(new EqStatCell(11, (a.moveBonus >= 0 ? "+" : "") + (a.moveBonus * 100f).ToString("0.#") + "%", T("MOVE", "移動"), cWhite));
                if (Mathf.Abs(a.fireBonus) > 0.0001f) extras.Add(new EqStatCell(3, "+" + (a.fireBonus * 100f).ToString("0.#") + "%", T("RATE", "連射"), cWhite));
                if (Mathf.Abs(a.reloadBonus) > 0.0001f) extras.Add(new EqStatCell(2, "+" + (a.reloadBonus * 100f).ToString("0.#") + "%", T("RELOAD", "装填"), cWhite));
                if (Mathf.Abs(a.dodgeBonus) > 0.0001f) extras.Add(new EqStatCell(12, "+" + (a.dodgeBonus * 100f).ToString("0.#") + "%", T("DODGE", "回避"), cWhite));
                if (Mathf.Abs(a.accuracyBonus) > 0.0001f) extras.Add(new EqStatCell(1, "+" + (a.accuracyBonus * 100f).ToString("0.#") + "%", T("ACC", "精度"), cWhite));
                if (Mathf.Abs(a.regenBonus) > 0.0001f) extras.Add(new EqStatCell(10, "+" + a.regenBonus.ToString("0.0") + "/s", T("REGEN", "回復"), cGreen));
                if (Mathf.Abs(a.ammoBonus) > 0.0001f) extras.Add(new EqStatCell(4, "+" + (a.ammoBonus * 100f).ToString("0.#") + "%", T("AMMO", "弾薬"), cWhite));
                for (int i = 0; i < extras.Count; i++)
                {
                    if (primaryStats.Count < 4) primaryStats.Add(extras[i]);
                    else subStats.Add(extras[i]);
                }

                // Legendary / Unique Armor Effect
                string legArmor = null;
                if (BossRelicFoundation.Decode(a.bossRelicId, out _, out _))
                    legArmor = BossRelicFoundation.Description(a.bossRelicId, ja);
                else if (!string.IsNullOrEmpty(a.legendaryKey))
                    legArmor = LegendaryCatalog.Description(a, ja);
                else if (a.uniqueEffect > 0)
                    legArmor = ja
                        ? new[] { "撃破時：シールド+4（クールダウン4秒）", "ダッシュ時：HP+4回復（クールダウン5秒）", "シールド持続中：武器ダメージ+15%" }[Mathf.Clamp(a.dungeon, 0, 2)]
                        : new[] { "On kill: +4 shield (4s cooldown)", "On dash: restore 4 HP (5s cooldown)", "While shield active: +15% weapon damage" }[Mathf.Clamp(a.dungeon, 0, 2)];

                if (!string.IsNullOrEmpty(legArmor))
                {
                    features.Add(new EqFeatureRow
                    {
                        icon = 17,
                        badgeBg = new Color(0.28f, 0.19f, 0.04f, 0.95f),
                        iconTint = new Color(1f, 0.82f, 0.18f),
                        borderTint = new Color(1f, 0.72f, 0.16f, 0.85f),
                        richText = "<color=#FFD447>" + T("UNIQUE", "固有効果") + "</color> - " + HighlightNumbersInText(legArmor, "#5CE1FF")
                    });
                }

                if (a.affixes != null)
                {
                    foreach (var af in a.affixes)
                    {
                        string text = FormatAffixHighlight(af);
                        if (string.IsNullOrEmpty(text)) continue;
                        features.Add(new EqFeatureRow
                        {
                            icon = AffixIconForType(af.type),
                            badgeBg = new Color(0.08f, 0.16f, 0.22f, 0.92f),
                            iconTint = new Color(0.58f, 0.88f, 1.00f),
                            borderTint = new Color(0.32f, 0.62f, 0.82f, 0.55f),
                            richText = "<color=#FFD166>❖</color> " + text
                        });
                    }
                }
            }
            else if (item is ArtifactItem m)
            {
                GearBalance.MigrateFireRate(m);
                string outVal = m.effect == ArtifactEffect.KillShield ? "+" + (8f * m.power).ToString("0.0")
                              : m.effect == ArtifactEffect.DashPulse ? (12f * m.power).ToString("0.0")
                              : m.effect == ArtifactEffect.ReloadSurge ? "+" + (15f * m.power).ToString("0") + "%"
                              : "+" + (12f * m.power).ToString("0") + "%";
                string outLbl = m.effect == ArtifactEffect.KillShield ? T("SHIELD", "シールド")
                              : m.effect == ArtifactEffect.DashPulse ? T("PULSE", "波動威力")
                              : T("SURGE", "威力増幅");
                int outIcon = m.effect == ArtifactEffect.KillShield ? 8 : m.effect == ArtifactEffect.DashPulse ? 22 : 0;

                primaryStats.Add(new EqStatCell(outIcon, outVal, outLbl, cCyan));
                primaryStats.Add(new EqStatCell(16, m.cooldown > 0.01f ? m.cooldown.ToString("0.0") + T("s", "秒") : T("PASSIVE", "常時"), T("COOLDOWN", "再発動"), cWhite));
                primaryStats.Add(new EqStatCell(15, "×" + m.power.ToString("0.00"), T("OUTPUT", "出力倍率"), cGold));

                // Core Trigger Circuit
                string baseEffect;
                switch (m.effect)
                {
                    case ArtifactEffect.KillShield:
                        baseEffect = ja ? "敵撃破時：シールド +" + (8f * m.power).ToString("0.0") + "（上限30 / 毎秒2減衰 / 被ダメ軽減25%+）"
                                        : "On kill: gain +" + (8f * m.power).ToString("0.0") + " shield (cap 30, decays 2/s, 25%+ dmg reduction)";
                        break;
                    case ArtifactEffect.DashPulse:
                        baseEffect = ja ? "ダッシュ時：半径3.5mに " + (12f * m.power).ToString("0.0") + " ダメージの衝撃波を放つ"
                                        : "On dash: emit a 3.5m shockwave dealing " + (12f * m.power).ToString("0.0") + " damage";
                        break;
                    case ArtifactEffect.ReloadSurge:
                        baseEffect = ja ? "リロード完了時：3秒間、武器ダメージ +" + (15f * m.power).ToString("0") + "%"
                                        : "On reload: +" + (15f * m.power).ToString("0") + "% weapon damage for 3s";
                        break;
                    default:
                        baseEffect = ja ? "シールド持続中：武器ダメージ +" + (12f * m.power).ToString("0") + "%（被ダメ軽減25%+）"
                                        : "While shield active: +" + (12f * m.power).ToString("0") + "% weapon damage (25%+ dmg reduction)";
                        break;
                }
                features.Add(new EqFeatureRow
                {
                    icon = 15,
                    badgeBg = new Color(0.08f, 0.20f, 0.24f, 0.92f),
                    iconTint = cCyan,
                    borderTint = new Color(0.28f, 0.78f, 0.95f, 0.65f),
                    richText = "<color=#7CE8FF>" + T("CIRCUIT", "基本回路") + "</color> - " + HighlightNumbersInText(baseEffect, "#5CE1FF")
                });

                // Legendary / Boss Relic Module Effect
                string legMod = null;
                if (BossRelicFoundation.Decode(m.bossRelicId, out _, out _))
                    legMod = BossRelicFoundation.Description(m.bossRelicId, ja);
                else if (ArtifactCatalog.LegendaryModuleBonus(m, out _, out _, out _))
                {
                    string cond = ArtifactCatalog.LegendaryModuleMode(m) == 17 ? ""
                                : m.effect == ArtifactEffect.ShieldConversion ? T("While shield active (every 5s): ", "シールド有効中（5秒毎）：")
                                : T("When module triggers: ", "モジュール発動時：");
                    string sig = ArtifactCatalog.LegendaryModuleSignature(m);
                    if (!string.IsNullOrEmpty(sig)) legMod = cond + sig;
                }
                if (!string.IsNullOrEmpty(legMod))
                {
                    features.Add(new EqFeatureRow
                    {
                        icon = 17,
                        badgeBg = new Color(0.28f, 0.19f, 0.04f, 0.95f),
                        iconTint = new Color(1f, 0.82f, 0.18f),
                        borderTint = new Color(1f, 0.72f, 0.16f, 0.85f),
                        richText = "<color=#FFD447>" + T("OVERCLOCK", "固有回路") + "</color> - " + HighlightNumbersInText(legMod, "#5CE1FF")
                    });
                }

                if (m.affixes != null)
                {
                    foreach (var af in m.affixes)
                    {
                        string text = FormatAffixHighlight(af);
                        if (string.IsNullOrEmpty(text)) continue;
                        features.Add(new EqFeatureRow
                        {
                            icon = AffixIconForType(af.type),
                            badgeBg = new Color(0.08f, 0.16f, 0.22f, 0.92f),
                            iconTint = new Color(0.58f, 0.88f, 1.00f),
                            borderTint = new Color(0.32f, 0.62f, 0.82f, 0.55f),
                            richText = "<color=#FFD166>❖</color> " + text
                        });
                    }
                }
            }
            else if (item is MeleeWeaponItem w)
            {
                MeleeWeaponCatalog.EnsureAffixes(w);
                GearBalance.MigrateFireRate(w);
                float rate = 1f / Mathf.Max(0.05f, w.interval);
                primaryStats.Add(new EqStatCell(13, w.damage.ToString("N0"), T("DMG", "威力"), cWhite));
                primaryStats.Add(new EqStatCell(3, rate.ToString("0.0") + T("/s", "/秒"), T("SPEED", "速度"), cWhite));
                primaryStats.Add(new EqStatCell(6, w.effectiveRange.ToString("0.0") + "m", T("REACH", "射程"), cCyan));
                primaryStats.Add(new EqStatCell(14, w.arcDegrees.ToString("0") + "°", T("ARC", "範囲角"), cGold));

                string legMelee = MeleeWeaponCatalog.LegendaryDescription(w);
                if (!string.IsNullOrEmpty(legMelee))
                {
                    features.Add(new EqFeatureRow
                    {
                        icon = 17,
                        badgeBg = new Color(0.28f, 0.19f, 0.04f, 0.95f),
                        iconTint = new Color(1f, 0.82f, 0.18f),
                        borderTint = new Color(1f, 0.72f, 0.16f, 0.85f),
                        richText = "<color=#FFD447>" + T("UNIQUE", "固有効果") + "</color> - " + HighlightNumbersInText(legMelee, "#5CE1FF")
                    });
                }

                if (w.affixes != null)
                {
                    foreach (var af in w.affixes)
                    {
                        string text = FormatAffixHighlight(af);
                        if (string.IsNullOrEmpty(text)) continue;
                        features.Add(new EqFeatureRow
                        {
                            icon = AffixIconForType(af.type),
                            badgeBg = new Color(0.08f, 0.16f, 0.22f, 0.92f),
                            iconTint = new Color(0.58f, 0.88f, 1.00f),
                            borderTint = new Color(0.32f, 0.62f, 0.82f, 0.55f),
                            richText = "<color=#FFD166>❖</color> " + text
                        });
                    }
                }
            }
        }

        void DrawItemSilhouetteOrPortrait(Rect r, object item)
        {
            bool prevBadge = ManufacturerBranding.PortraitBadgeEnabled;
            ManufacturerBranding.PortraitBadgeEnabled = false;
            try
            {
                if (item is Gun g) ItemPortraits.Draw(r, g);
                else if (item is ArmorItem a) ItemPortraits.Draw(r, a);
                else if (item is ArtifactItem m) ManufacturerBranding.ModulePortrait(r, m);
                else if (item is MeleeWeaponItem w)
                {
                    var atlas = Resources.Load<Texture2D>("Art/MeleeWeaponIcons-portrait-v2") ?? Resources.Load<Texture2D>("Art/MeleeWeaponIcons");
                    Rect pr = r;
                    if (atlas)
                    {
                        float aspect = atlas.width / (5f * atlas.height);
                        float pw = Mathf.Min(r.width, r.height * aspect);
                        float ph = Mathf.Min(r.height, r.width / aspect);
                        pr = new Rect(r.center.x - pw * 0.5f, r.center.y - ph * 0.5f, pw, ph);
                    }
                    ManufacturerPortraits.Melee(pr, w);
                }
            }
            finally
            {
                ManufacturerBranding.PortraitBadgeEnabled = prevBadge;
            }
        }

        static Font eqNumericFont;
        static Font EqNumericFont()
        {
            if (!eqNumericFont)
                eqNumericFont = Resources.Load<Font>("Fonts/GenShinGothic-P-Bold") ?? Resources.Load<Font>("Fonts/NotoSansJP");
            return eqNumericFont;
        }

        GUIStyle EqCleanStyle(Font fontOverride, int fontSize, FontStyle fontStyle = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false, bool rich = false, TextClipping clip = TextClipping.Overflow)
        {
            var s = new GUIStyle(label)
            {
                font = fontOverride ?? this.font,
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = align,
                wordWrap = wrap,
                richText = rich,
                clipping = clip,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            s.normal.textColor = Color.white;
            return s;
        }

        bool eqCompareEquipped;
        bool eqExpandedCardOpen;
        object eqExpandedCardItem;
        bool eqExpandedCardEquipped;
        int eqExpandedCardRefScore;
        bool eqExpandedCardHasCmp;
        int eqExpandedCardPrice;
        bool eqExpandedCardJunk;
        Vector2 eqExpandedCardScroll;
        float eqExpandedCardExtent;

        bool eqTapDown;
        bool eqTapDragged;
        Vector2 eqTapStart;
        int eqTapTargetId;

        bool eqModalTapDown;
        bool eqModalTapDragged;
        Vector2 eqModalTapStart;

        public void CloseExpandedEquipmentCard()
        {
            eqExpandedCardOpen = false;
            eqExpandedCardItem = null;
            eqTapDown = false;
            eqModalTapDown = false;
        }

        public void HandleEquipmentCardTapToExpand(Rect cardRect, object item, bool equipped, int refScore, bool hasCmp, int price, bool showJunk, int targetId = 1)
        {
            if (item == null || eqExpandedCardOpen || !GUI.enabled) return;
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.MouseDown && e.button == 0 && cardRect.Contains(e.mousePosition))
            {
                eqTapDown = true;
                eqTapDragged = false;
                eqTapStart = e.mousePosition;
                eqTapTargetId = targetId;
            }
            else if (e.type == EventType.MouseDrag && eqTapDown && eqTapTargetId == targetId)
            {
                if (Vector2.Distance(e.mousePosition, eqTapStart) >= 14f)
                    eqTapDragged = true;
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && eqTapDown && eqTapTargetId == targetId)
            {
                bool open = !eqTapDragged && cardRect.Contains(e.mousePosition);
                eqTapDown = false;
                eqTapDragged = false;
                if (open)
                {
                    eqExpandedCardOpen = true;
                    eqExpandedCardItem = item;
                    eqExpandedCardEquipped = equipped;
                    eqExpandedCardRefScore = refScore;
                    eqExpandedCardHasCmp = hasCmp;
                    eqExpandedCardPrice = price;
                    eqExpandedCardJunk = showJunk;
                    eqExpandedCardScroll = Vector2.zero;
                    GUIUtility.hotControl = 0;
                    PlayUserSound("tab", 0.1f);
                    e.Use();
                }
            }
        }

        public void DrawExpandedEquipmentCardModal()
        {
            if (!eqExpandedCardOpen || eqExpandedCardItem == null) return;
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Backspace))
            {
                CloseExpandedEquipmentCard();
                PlayUserSound("tab", 0.08f);
                e.Use();
                return;
            }

            Box(new Rect(0f, 0f, 1440f, 900f), new Color(0.008f, 0.015f, 0.022f, 0.93f));
            Rect expRect = new Rect(64f, 32f, 1312f, 836f);

            DrawBorderlandsItemCard(expRect, eqExpandedCardItem, ref eqExpandedCardScroll, out eqExpandedCardExtent,
                eqExpandedCardEquipped, eqExpandedCardRefScore, eqExpandedCardHasCmp, eqExpandedCardPrice, eqExpandedCardJunk, expanded: true, showExpandHint: false);

            // Top-right Close button inside the expanded card (uses ClickableButton so it works in Stash even before Inventory initializes eqButtonStyle)
            Rect closeRect = new Rect(expRect.xMax - 186f, expRect.y + 12f, 170f, 48f);
            bool hoverClose = e != null && closeRect.Contains(e.mousePosition);
            Box(closeRect, hoverClose ? new Color(0.28f, 0.14f, 0.14f, 0.96f) : new Color(0.10f, 0.14f, 0.18f, 0.95f));
            Color closeBorder = hoverClose ? new Color(1f, 0.55f, 0.45f, 0.95f) : new Color(0.55f, 0.72f, 0.82f, 0.80f);
            Box(new Rect(closeRect.x, closeRect.y, closeRect.width, 2f), closeBorder);
            Box(new Rect(closeRect.x, closeRect.yMax - 2f, closeRect.width, 2f), closeBorder);
            Box(new Rect(closeRect.x, closeRect.y, 2f, closeRect.height), closeBorder);
            Box(new Rect(closeRect.xMax - 2f, closeRect.y, 2f, closeRect.height), closeBorder);
            var closeStyle = EqCleanStyle(this.font, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            EqCardLabel(closeRect, T("× CLOSE", "× 閉じる"), Color.white, closeStyle);
            if (ClickableButton(closeRect, GUIContent.none, GUIStyle.none))
            {
                CloseExpandedEquipmentCard();
                PlayUserSound("tab", 0.09f);
                return;
            }

            // Tap anywhere (inside or outside card) without dragging to close expanded view
            if (e != null)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    eqModalTapDown = true;
                    eqModalTapDragged = false;
                    eqModalTapStart = e.mousePosition;
                }
                else if (e.type == EventType.MouseDrag && eqModalTapDown)
                {
                    if (Vector2.Distance(e.mousePosition, eqModalTapStart) >= 14f)
                        eqModalTapDragged = true;
                }
                else if (e.type == EventType.MouseUp && e.button == 0 && eqModalTapDown)
                {
                    bool shouldClose = !eqModalTapDragged;
                    eqModalTapDown = false;
                    eqModalTapDragged = false;
                    if (shouldClose)
                    {
                        CloseExpandedEquipmentCard();
                        GUIUtility.hotControl = 0;
                        PlayUserSound("tab", 0.08f);
                        e.Use();
                    }
                }
            }
        }

        // Draws a full Borderlands-style inspector card inside cardRect, with large portrait showcase, non-clipping stat labels, scrollable feature body, and Maker/Price footer.
        public void DrawBorderlandsItemCard(Rect cardRect, object item, ref Vector2 detailScroll, out float computedDetailExtent,
            bool equipped = false, int compareReferenceScore = 0, bool hasComparison = false, int priceOverride = -1, bool showJunkBadge = false, bool expanded = false, bool showExpandHint = true)
        {
            computedDetailExtent = 0f;
            if (item == null) return;
            EnsureEquipmentCardTextures();

            Rarity rarity = ItemRarityOf(item);
            Color theme = RarityThemeColor(rarity);
            int level = ItemLevelOf(item);
            int score = ItemScoreOf(item);
            string maker = ItemMakerOf(item);
            string itemName = ItemDisplayNameOf(item);
            string typeSubtitle = ItemTypeSubtitleOf(item);
            int price = priceOverride >= 0 ? priceOverride : ItemSaleValueOf(item);
            string cleanName = (itemName ?? "").Trim().Replace("\n ", "\n").Replace("\n　", "\n");
            int chevCount = Mathf.Clamp((int)rarity + 1, 1, 5);
            string catLine = new string('▸', chevCount) + " " + typeSubtitle + "  ·  " + VaultLanguage.RarityName(rarity);

            float heroValue = item is Gun dpsGun
                ? (dpsGun.damage * Mathf.Max(1, dpsGun.pellets) / Mathf.Max(0.05f, dpsGun.interval))
                : item is MeleeWeaponItem dpsMelee
                ? (dpsMelee.damage / Mathf.Max(0.05f, dpsMelee.interval))
                : score;
            string heroNum = item is Gun || item is MeleeWeaponItem
                ? (heroValue >= 100f ? Mathf.RoundToInt(heroValue).ToString("N0") : heroValue.ToString("0.0"))
                : score.ToString("N0");
            string heroUnit = (item is Gun || item is MeleeWeaponItem) ? "DPS" : T("SCORE", "評価");

            float cursorY;
            float footerH = expanded ? 62f : 38f;

            if (expanded)
            {
                float headerH = 232f;
                DrawRarityBackdrop(cardRect, rarity, false, true, headerH);

                float padX = 24f;
                // Right side showcase portrait (leaves top-right 54px for Close button)
                Rect portRect = new Rect(cardRect.xMax - 526f, cardRect.y + 68f, 504f, 152f);
                Box(new Rect(portRect.x - 1f, portRect.y - 1f, portRect.width + 2f, portRect.height + 2f), new Color(theme.r, theme.g, theme.b, 0.55f));
                Box(portRect, new Color(0.015f, 0.025f, 0.036f, 0.78f));
                DrawItemSilhouetteOrPortrait(portRect, item);
                if (item is Gun ehg)
                {
                    Rect silRect = new Rect(portRect.x + 6f, portRect.y + 6f, 54f, 32f);
                    Box(silRect, new Color(0.02f, 0.03f, 0.04f, 0.84f));
                    FieldUIArt.GunClassIcon(new Rect(silRect.x + 3f, silRect.y + 3f, silRect.width - 6f, silRect.height - 6f), ehg, Color.Lerp(theme, Color.white, 0.65f));
                }
                if (showJunkBadge)
                {
                    if (!junkBinIcon) junkBinIcon = Resources.Load<Texture2D>("Art/UI/junk-bin");
                    if (junkBinIcon) GUI.DrawTexture(new Rect(portRect.xMax - 42f, portRect.y + 6f, 36f, 36f), junkBinIcon, ScaleMode.ScaleToFit, true);
                }

                // Level badge next to Close button
                var lvStyle = EqCleanStyle(this.font, 30, FontStyle.Bold, TextAnchor.MiddleRight);
                EqCardLabel(new Rect(portRect.x, cardRect.y + 14f, portRect.width - 184f, 44f), "Lv " + level, Color.white, lvStyle);

                // Left side: Large Item Name + Category + Hero DPS/Score
                float leftW = portRect.x - (cardRect.x + padX) - 18f;
                var nameStyle = EqCleanStyle(this.font, 36, FontStyle.Bold, TextAnchor.UpperLeft, wrap: true, clip: TextClipping.Clip);
                while (nameStyle.fontSize > 22 && nameStyle.CalcHeight(new GUIContent(cleanName), leftW) > 90f)
                    nameStyle.fontSize--;
                Color nameColor = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.80f : 0.54f);
                EqCardLabel(new Rect(cardRect.x + padX + 2f, cardRect.y + 18f, leftW, 92f), cleanName, new Color(0f, 0f, 0f, 0.9f), nameStyle);
                EqCardLabel(new Rect(cardRect.x + padX, cardRect.y + 16f, leftW, 92f), cleanName, nameColor, nameStyle);

                var typeStyle = EqCleanStyle(this.font, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
                EqCardLabel(new Rect(cardRect.x + padX, cardRect.y + 114f, leftW, 30f), catLine, Color.Lerp(theme, new Color(1f, 0.92f, 0.55f), 0.35f), typeStyle);

                var heroNumStyle = EqCleanStyle(EqNumericFont() ?? this.font, 40, FontStyle.Bold, TextAnchor.MiddleLeft);
                var heroUnitStyle = EqCleanStyle(this.font, 21, FontStyle.Bold, TextAnchor.LowerLeft);
                float numW = heroNumStyle.CalcSize(new GUIContent(heroNum)).x;
                float unitW = heroUnitStyle.CalcSize(new GUIContent(heroUnit)).x;
                Rect heroPill = new Rect(cardRect.x + padX, cardRect.y + 156f, Mathf.Min(leftW, numW + unitW + 34f), 56f);
                Box(heroPill, new Color(0.02f, 0.03f, 0.04f, 0.82f));
                Box(new Rect(heroPill.x, heroPill.y, 4f, heroPill.height), theme);
                EqCardLabel(new Rect(heroPill.x + 12f, heroPill.y, numW + 8f, heroPill.height), heroNum, Color.white, heroNumStyle);
                EqCardLabel(new Rect(heroPill.x + 18f + numW, heroPill.y, unitW + 10f, heroPill.height - 8f), heroUnit, new Color(0.78f, 0.86f, 0.92f), heroUnitStyle);

                float compX = heroPill.xMax + 16f;
                var compStyle = EqCleanStyle(this.font, 23, FontStyle.Bold, TextAnchor.MiddleLeft, rich: true);
                if (equipped)
                {
                    Rect eqBadge = new Rect(compX, heroPill.y + 8f, 168f, 40f);
                    Box(eqBadge, new Color(0.10f, 0.28f, 0.18f, 0.92f));
                    Box(new Rect(eqBadge.x, eqBadge.y, eqBadge.width, 2f), new Color(0.35f, 0.95f, 0.60f, 0.85f));
                    EqCardLabel(new Rect(eqBadge.x + 12f, eqBadge.y, eqBadge.width - 16f, eqBadge.height), T("✓ EQUIPPED", "✓ 装備中"), new Color(0.48f, 1f, 0.70f), compStyle);
                }
                else if (hasComparison)
                {
                    int delta = score - compareReferenceScore;
                    string cmpStr = delta > 0 ? "<color=#4DFF88>▲+" + delta + "</color>"
                                  : delta < 0 ? "<color=#FF5E5E>▼" + delta + "</color>"
                                  : "<color=#B0BEC5>＝0</color>";
                    EqCardLabel(new Rect(compX, heroPill.y, leftW - (compX - (cardRect.x + padX)), heroPill.height), cmpStr, Color.white, compStyle);
                }

                cursorY = cardRect.y + headerH + 8f;
            }
            else
            {
                bool compact = cardRect.height < 440f;
                bool veryShort = cardRect.height < 395f;
                float titleBandH = compact ? 60f : 66f;
                // Restore full-width large equipment illustration (450x124 in Inventory, 450x104 in Shop)
                float portH = veryShort ? 90f : compact ? 104f : 124f;
                float headerH = titleBandH + portH + 8f;

                DrawRarityBackdrop(cardRect, rarity, false, true, headerH);

                float padX = 11f;
                // Row 1: Level top-right + Item Name top-left
                var lvStyle = EqCleanStyle(this.font, compact ? 18 : 20, FontStyle.Bold, TextAnchor.UpperRight);
                string lvText = "Lv " + level;
                float lvW = Mathf.Max(66f, lvStyle.CalcSize(new GUIContent(lvText)).x + 6f);
                EqCardLabel(new Rect(cardRect.xMax - padX - lvW + 1f, cardRect.y + 7f, lvW, 24f), lvText, new Color(0f, 0f, 0f, 0.88f), lvStyle);
                EqCardLabel(new Rect(cardRect.xMax - padX - lvW, cardRect.y + 6f, lvW, 24f), lvText, Color.white, lvStyle);

                float nameMaxW = cardRect.width - padX * 2f - lvW - 6f;
                var nameStyle = EqCleanStyle(this.font, compact ? 18 : 21, FontStyle.Bold, TextAnchor.UpperLeft, wrap: true, clip: TextClipping.Clip);
                while (nameStyle.fontSize > 13 && nameStyle.CalcHeight(new GUIContent(cleanName), nameMaxW) > (compact ? 30f : 34f))
                    nameStyle.fontSize--;
                Color nameColor = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.52f);
                EqCardLabel(new Rect(cardRect.x + padX + 1f, cardRect.y + 6f, nameMaxW, 34f), cleanName, new Color(0f, 0f, 0f, 0.9f), nameStyle);
                EqCardLabel(new Rect(cardRect.x + padX, cardRect.y + 5f, nameMaxW, 34f), cleanName, nameColor, nameStyle);

                // Row 2: Hero DPS/Score Pill + Comparison on Left, Rarity/Type on Right
                float heroY = cardRect.y + titleBandH - (compact ? 25f : 27f);
                float heroH = compact ? 23f : 25f;
                var heroNumStyle = EqCleanStyle(EqNumericFont() ?? this.font, compact ? 18 : 21, FontStyle.Bold, TextAnchor.MiddleLeft);
                var heroUnitStyle = EqCleanStyle(this.font, compact ? 11 : 12, FontStyle.Bold, TextAnchor.LowerLeft);
                float numW = heroNumStyle.CalcSize(new GUIContent(heroNum)).x;
                float unitW = heroUnitStyle.CalcSize(new GUIContent(heroUnit)).x;
                float pillW = numW + unitW + 18f;
                Rect heroPill = new Rect(cardRect.x + padX, heroY, pillW, heroH);
                Box(heroPill, new Color(0.02f, 0.03f, 0.04f, 0.78f));
                Box(new Rect(heroPill.x, heroPill.y, 3f, heroPill.height), theme);
                EqCardLabel(new Rect(heroPill.x + 7f, heroPill.y, numW + 6f, heroPill.height), heroNum, Color.white, heroNumStyle);
                EqCardLabel(new Rect(heroPill.x + 9f + numW, heroPill.y, unitW + 8f, heroPill.height - 2f), heroUnit, new Color(0.75f, 0.82f, 0.88f), heroUnitStyle);

                float compX = heroPill.xMax + 6f;
                var compStyle = EqCleanStyle(this.font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, rich: true);
                float compUsedW = 0f;
                if (equipped)
                {
                    Rect eqBadge = new Rect(compX, heroY + 2f, 84f, heroH - 4f);
                    Box(eqBadge, new Color(0.10f, 0.28f, 0.18f, 0.90f));
                    EqCardLabel(new Rect(eqBadge.x + 5f, eqBadge.y, eqBadge.width - 6f, eqBadge.height), T("✓ EQUIPPED", "✓ 装備中"), new Color(0.48f, 1f, 0.70f), compStyle);
                    compUsedW = 90f;
                }
                else if (hasComparison)
                {
                    int delta = score - compareReferenceScore;
                    string cmpStr = delta > 0 ? "<color=#4DFF88>▲+" + delta + "</color>"
                                  : delta < 0 ? "<color=#FF5E5E>▼" + delta + "</color>"
                                  : "<color=#B0BEC5>＝0</color>";
                    EqCardLabel(new Rect(compX, heroY, 88f, heroH), cmpStr, Color.white, compStyle);
                    compUsedW = 76f;
                }

                float catLeft = compX + compUsedW + 4f;
                float catW = Mathf.Max(80f, (cardRect.xMax - padX) - catLeft);
                var typeStyle = EqCleanStyle(this.font, compact ? 12 : 13, FontStyle.Normal, TextAnchor.MiddleRight);
                while (typeStyle.fontSize > 10 && typeStyle.CalcSize(new GUIContent(catLine)).x > catW)
                    typeStyle.fontSize--;
                EqCardLabel(new Rect(catLeft, heroY, catW, heroH), catLine, Color.Lerp(theme, new Color(1f, 0.92f, 0.55f), 0.35f), typeStyle);

                // Dedicated Full-Width Large Equipment Illustration Box (restored to ~450x124!)
                Rect portRect = new Rect(cardRect.x + 10f, cardRect.y + titleBandH + 2f, cardRect.width - 20f, portH);
                Box(new Rect(portRect.x - 1f, portRect.y - 1f, portRect.width + 2f, portRect.height + 2f), new Color(theme.r, theme.g, theme.b, 0.50f));
                Box(portRect, new Color(0.015f, 0.024f, 0.035f, 0.76f));
                DrawItemSilhouetteOrPortrait(portRect, item);

                if (item is Gun hg)
                {
                    Rect silRect = new Rect(portRect.x + 5f, portRect.y + 5f, 42f, 24f);
                    Box(silRect, new Color(0.02f, 0.03f, 0.04f, 0.84f));
                    FieldUIArt.GunClassIcon(new Rect(silRect.x + 3f, silRect.y + 2f, silRect.width - 6f, silRect.height - 4f), hg, Color.Lerp(theme, Color.white, 0.65f));
                }
                if (showJunkBadge)
                {
                    if (!junkBinIcon) junkBinIcon = Resources.Load<Texture2D>("Art/UI/junk-bin");
                    if (junkBinIcon) GUI.DrawTexture(new Rect(portRect.xMax - 32f, portRect.y + 4f, 28f, 28f), junkBinIcon, ScaleMode.ScaleToFit, true);
                }
                if (showExpandHint)
                {
                    string hintTxt = MobileTouchControls.Enabled ? T("TAP TO ZOOM", "タップで拡大") : T("CLICK TO ZOOM", "クリックで拡大");
                    var hintStyle = EqCleanStyle(this.font, 11, FontStyle.Normal, TextAnchor.MiddleCenter);
                    float hw = hintStyle.CalcSize(new GUIContent(hintTxt)).x + 14f;
                    Rect hintBox = new Rect(portRect.xMax - hw - 4f, portRect.yMax - 21f, hw, 18f);
                    Box(hintBox, new Color(0.02f, 0.035f, 0.05f, 0.84f));
                    Box(new Rect(hintBox.x, hintBox.y, hintBox.width, 1f), new Color(theme.r, theme.g, theme.b, 0.45f));
                    EqCardLabel(hintBox, hintTxt, new Color(0.76f, 0.88f, 0.96f), hintStyle);
                }

                cursorY = cardRect.y + headerH + 4f;
            }

            // 3. BUILD STAT CELLS & FEATURE ROWS
            BuildItemCardData(item, out var primaryStats, out var subStats, out var features);

            bool compactBody = !expanded && cardRect.height < 440f;

            // 4. PRIMARY STAT ICON TILE STRIP (Labels ALWAYS visible and NEVER clipped!)
            if (primaryStats.Count > 0)
            {
                float stripH = expanded ? 106f : (compactBody ? 58f : 64f);
                Rect stripRect = new Rect(cardRect.x + (expanded ? 16f : 8f), cursorY, cardRect.width - (expanded ? 32f : 16f), stripH);
                Box(stripRect, new Color(0.03f, 0.055f, 0.078f, 0.88f));
                Box(new Rect(stripRect.x, stripRect.y, stripRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.38f));
                Box(new Rect(stripRect.x, stripRect.yMax - 1f, stripRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.38f));

                int cols = primaryStats.Count;
                float cellW = stripRect.width / cols;
                int baseValFont = expanded ? (cols >= 5 ? 28 : 31) : (cols >= 5 ? (compactBody ? 15 : 17) : (compactBody ? 17 : 19));
                int baseLblFont = expanded ? 21 : (compactBody ? 12 : 14);
                var valStyle = EqCleanStyle(EqNumericFont() ?? this.font, baseValFont, FontStyle.Bold, TextAnchor.MiddleCenter);
                var lblStyle = EqCleanStyle(this.font, baseLblFont, FontStyle.Bold, TextAnchor.MiddleCenter);

                for (int i = 0; i < cols; i++)
                {
                    var st = primaryStats[i];
                    float cx = stripRect.x + i * cellW;
                    if (i > 0)
                        Box(new Rect(cx, stripRect.y + 8f, 1f, stripRect.height - 16f), new Color(0.26f, 0.44f, 0.54f, 0.55f));

                    float iconSz = expanded ? 32f : (compactBody ? 17f : 19f);
                    DrawEqCardIcon(new Rect(cx + (cellW - iconSz) * 0.5f, stripRect.y + (expanded ? 6f : 3f), iconSz, iconSz), st.icon, Color.Lerp(st.tint, theme, 0.22f));

                    GUIStyle vStyle = valStyle;
                    if (vStyle.CalcSize(new GUIContent(st.value)).x > cellW - 6f)
                    {
                        vStyle = EqCleanStyle(valStyle.font, baseValFont, FontStyle.Bold, TextAnchor.MiddleCenter);
                        while (vStyle.fontSize > 11 && vStyle.CalcSize(new GUIContent(st.value)).x > cellW - 6f)
                            vStyle.fontSize--;
                    }
                    float valY = stripRect.y + (expanded ? 40f : (compactBody ? 20f : 22f));
                    float valH = expanded ? 34f : 21f;
                    EqCardLabel(new Rect(cx + 2f, valY, cellW - 4f, valH), st.value, st.tint, vStyle);

                    GUIStyle lStyle = lblStyle;
                    if (lStyle.CalcSize(new GUIContent(st.label)).x > cellW - 4f)
                    {
                        lStyle = EqCleanStyle(this.font, baseLblFont, FontStyle.Bold, TextAnchor.MiddleCenter);
                        while (lStyle.fontSize > 10 && lStyle.CalcSize(new GUIContent(st.label)).x > cellW - 4f)
                            lStyle.fontSize--;
                    }
                    float lblY = stripRect.y + (expanded ? 73f : (compactBody ? 39f : 42f));
                    float lblH = expanded ? 30f : 20f;
                    EqCardLabel(new Rect(cx + 1f, lblY, cellW - 2f, lblH), st.label, new Color(0.78f, 0.88f, 0.95f), lStyle);
                }
                cursorY += stripH + (expanded ? 6f : 3f);
            }

            // 5. SECONDARY SUB-STAT BANNER
            if (subStats.Count > 0)
            {
                float subH = expanded ? 46f : (compactBody ? 24f : 27f);
                Rect subRect = new Rect(cardRect.x + (expanded ? 16f : 8f), cursorY, cardRect.width - (expanded ? 32f : 16f), subH);
                Box(subRect, new Color(0.045f, 0.08f, 0.11f, 0.84f));
                Box(new Rect(subRect.x, subRect.yMax - 1f, subRect.width, 1f), new Color(0.24f, 0.42f, 0.52f, 0.45f));

                int sCols = Mathf.Min(3, subStats.Count);
                float sCellW = subRect.width / sCols;
                int subFont = expanded ? 22 : (compactBody ? 13 : 14);
                for (int i = 0; i < sCols; i++)
                {
                    var st = subStats[i];
                    float sx = subRect.x + i * sCellW;
                    if (i > 0)
                        Box(new Rect(sx, subRect.y + 5f, 1f, subRect.height - 10f), new Color(0.26f, 0.44f, 0.54f, 0.45f));

                    var sValStyle = EqCleanStyle(EqNumericFont() ?? this.font, subFont, FontStyle.Bold, TextAnchor.MiddleLeft);
                    string combined = st.label + " " + st.value;
                    float icSz = expanded ? 26f : 17f;
                    while (sValStyle.fontSize > 10 && icSz + 6f + sValStyle.CalcSize(new GUIContent(combined)).x > sCellW - 8f)
                        sValStyle.fontSize--;
                    float textW = sValStyle.CalcSize(new GUIContent(combined)).x;
                    float totalW = Mathf.Min(sCellW - 8f, icSz + 6f + textW);
                    float startX = sx + (sCellW - totalW) * 0.5f;
                    DrawEqCardIcon(new Rect(startX, subRect.center.y - icSz * 0.5f, icSz, icSz), st.icon, st.tint);
                    EqCardLabel(new Rect(startX + icSz + 5f, subRect.y, sCellW - (startX - sx) - icSz - 6f, subRect.height), combined, st.tint, sValStyle);
                }
                cursorY += subH + (expanded ? 8f : 4f);
            }

            // 6. SCROLLABLE FEATURE ROWS (Unique, Element, Affixes)
            float bodyBottom = cardRect.yMax - footerH - (expanded ? 6f : 3f);
            float bodyH = Mathf.Max(36f, bodyBottom - cursorY);
            Rect scrollViewport = new Rect(cardRect.x + (expanded ? 16f : 8f), cursorY, cardRect.width - (expanded ? 32f : 16f), bodyH);

            int featFont = expanded ? (MobileTouchControls.Enabled ? 27 : 25) : (MobileTouchControls.Enabled ? 18 : (compactBody ? 15 : 16));
            var featStyle = EqCleanStyle(this.font, featFont, FontStyle.Normal, TextAnchor.UpperLeft, wrap: true, rich: true, clip: TextClipping.Clip);
            featStyle.normal.textColor = new Color(0.92f, 0.95f, 0.98f);

            float contentW = scrollViewport.width - 20f;
            float iconBoxSz = expanded ? 42f : (compactBody ? 26f : 28f);
            float textColW = contentW - iconBoxSz - (expanded ? 18f : 14f);

            float totalH = 2f;
            var rowHeights = new float[features.Count];
            for (int i = 0; i < features.Count; i++)
            {
                float th = featStyle.CalcHeight(new GUIContent(features[i].richText), textColW);
                rowHeights[i] = Mathf.Max(iconBoxSz + (expanded ? 10f : 6f), th + (expanded ? 10f : 6f));
                totalH += rowHeights[i] + (expanded ? 5f : 3f);
            }
            computedDetailExtent = Mathf.Max(bodyH, totalH + 2f);

            if (Event.current != null && Event.current.type == EventType.ScrollWheel && scrollViewport.Contains(Event.current.mousePosition) && computedDetailExtent > bodyH)
            {
                detailScroll.y = Mathf.Clamp(detailScroll.y + Event.current.delta.y * 24f, 0f, computedDetailExtent - bodyH);
                Event.current.Use();
            }

            detailScroll = GUI.BeginScrollView(scrollViewport, detailScroll, new Rect(0f, 0f, contentW, computedDetailExtent), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            float ry = 1f;
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                float rh = rowHeights[i];
                Rect rowRect = new Rect(0f, ry, contentW, rh);
                Box(rowRect, new Color(0.025f, 0.045f, 0.065f, 0.72f));
                Box(new Rect(rowRect.x, rowRect.y, expanded ? 3f : 2f, rowRect.height), f.borderTint);

                Rect badgeRect = new Rect(rowRect.x + (expanded ? 8f : 5f), rowRect.y + (rh - iconBoxSz) * 0.5f, iconBoxSz, iconBoxSz);
                Box(badgeRect, f.badgeBg);
                Box(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1f), f.borderTint);
                Box(new Rect(badgeRect.x, badgeRect.yMax - 1f, badgeRect.width, 1f), f.borderTint);
                Box(new Rect(badgeRect.x, badgeRect.y, 1f, badgeRect.height), f.borderTint);
                Box(new Rect(badgeRect.xMax - 1f, badgeRect.y, 1f, badgeRect.height), f.borderTint);

                float padIc = expanded ? 5f : 3f;
                Rect icRect = new Rect(badgeRect.x + padIc, badgeRect.y + padIc, badgeRect.width - padIc * 2f, badgeRect.height - padIc * 2f);
                if (f.element != GunElement.None)
                    DrawEqElementSymbol(icRect, f.element);
                else
                    DrawEqCardIcon(icRect, f.icon, f.iconTint);

                float th = featStyle.CalcHeight(new GUIContent(f.richText), textColW);
                float ty = rowRect.y + Mathf.Max(2f, (rh - th) * 0.5f);
                GUI.Label(new Rect(badgeRect.xMax + (expanded ? 12f : 7f), ty, textColW, th + 4f), f.richText, featStyle);

                ry += rh + (expanded ? 6f : 3f);
            }
            GUI.EndScrollView();

            // 7. FOOTER BAR: Manufacturer Logo & Name ONLY (Left) + Green Bracketed Scrap Value Box (Right)
            Rect footerRect = new Rect(cardRect.x + 4f, cardRect.yMax - footerH - 2f, cardRect.width - 8f, footerH);
            Box(footerRect, new Color(0.02f, 0.032f, 0.045f, 0.94f));
            Box(new Rect(footerRect.x, footerRect.y, footerRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.42f));

            if (!string.IsNullOrEmpty(maker))
            {
                Color mkCol = ManufacturerCatalog.Color(maker);
                string mkName = ManufacturerCatalog.Name(maker, VaultLanguage.Japanese);
                var mkStyle = EqCleanStyle(this.font, expanded ? 23 : 14, FontStyle.Bold, TextAnchor.MiddleLeft);
                float badgeW = expanded ? 104f : 66f;
                float mkTextW = mkStyle.CalcSize(new GUIContent(mkName)).x + 16f;
                float mkBoxW = Mathf.Min(footerRect.width * 0.52f, badgeW + mkTextW + 10f);
                Box(new Rect(footerRect.x + 6f, footerRect.y + (expanded ? 7f : 5f), mkBoxW, footerRect.height - (expanded ? 14f : 10f)), new Color(mkCol.r * 0.16f, mkCol.g * 0.16f, mkCol.b * 0.16f, 0.68f));
                ManufacturerBranding.Badge(new Rect(footerRect.x + 8f, footerRect.y + (expanded ? 5f : 3f), badgeW, footerRect.height - (expanded ? 10f : 6f)), maker);
                EqCardLabel(new Rect(footerRect.x + 12f + badgeW, footerRect.y, mkBoxW - badgeW - 10f, footerRect.height), mkName, Color.Lerp(mkCol, Color.white, 0.35f), mkStyle);
            }

            if (expanded)
            {
                var closeHintStyle = EqCleanStyle(this.font, 17, FontStyle.Normal, TextAnchor.MiddleCenter);
                EqCardLabel(new Rect(footerRect.center.x - 210f, footerRect.y, 420f, footerRect.height),
                    MobileTouchControls.Enabled ? T("TAP ANYWHERE TO CLOSE", "画面をタップで閉じる") : T("CLICK ANYWHERE OR PRESS ESC TO CLOSE", "クリック または ESCキーで閉じる"),
                    new Color(0.62f, 0.74f, 0.82f), closeHintStyle);
            }

            if (price > 0)
            {
                float valW = expanded ? 300f : Mathf.Min(195f, footerRect.width * 0.44f);
                Rect priceBox = new Rect(footerRect.xMax - valW - (expanded ? 12f : 8f), footerRect.y + (expanded ? 8f : 6f), valW, footerRect.height - (expanded ? 16f : 12f));
                Color cashGreen = new Color(0.28f, 0.92f, 0.48f, 0.95f);
                Box(priceBox, new Color(0.04f, 0.16f, 0.09f, 0.90f));
                Box(new Rect(priceBox.x, priceBox.y, expanded ? 4f : 3f, priceBox.height), cashGreen);
                Box(new Rect(priceBox.xMax - (expanded ? 4f : 3f), priceBox.y, expanded ? 4f : 3f, priceBox.height), cashGreen);
                float cashIc = expanded ? 28f : 20f;
                DrawEqCardIcon(new Rect(priceBox.x + (expanded ? 10f : 7f), priceBox.center.y - cashIc * 0.5f, cashIc, cashIc), 20, cashGreen);

                var priceStyle = EqCleanStyle(EqNumericFont() ?? this.font, expanded ? 28 : 17, FontStyle.Bold, TextAnchor.MiddleRight);
                string priceText = price.ToString("N0");
                float textPadLeft = expanded ? 44f : 28f;
                while (priceStyle.fontSize > 12 && priceStyle.CalcSize(new GUIContent(priceText)).x > priceBox.width - textPadLeft - 10f)
                    priceStyle.fontSize--;
                EqCardLabel(new Rect(priceBox.x + textPadLeft, priceBox.y, priceBox.width - textPadLeft - (expanded ? 12f : 8f), priceBox.height), priceText, new Color(0.85f, 1f, 0.90f), priceStyle);
            }
        }

        // Compact Borderlands-styled list card for Left Inventory Grid, Shop List, Stash List, Catalog Grid, and World Drop Preview
        public void DrawBorderlandsListRow(Rect r, object item, bool selected, bool equipped, string comparisonRich, bool showJunk = false, int priceBadge = -1)
        {
            if (item == null) return;
            EnsureEquipmentCardTextures();
            Rarity rarity = ItemRarityOf(item);
            Color theme = RarityThemeColor(rarity);
            int level = ItemLevelOf(item);
            int score = ItemScoreOf(item);
            string maker = ItemMakerOf(item);
            string name = ItemDisplayNameOf(item);
            string subType = ItemTypeSubtitleOf(item);

            DrawRarityBackdrop(r, rarity, selected, true, 54f);

            float portSz = Mathf.Min(r.height - 24f, 114f);
            Rect portRect = new Rect(r.x + 10f, r.y + 10f, portSz, portSz);
            Box(new Rect(portRect.x - 1f, portRect.y - 1f, portRect.width + 2f, portRect.height + 2f), new Color(theme.r, theme.g, theme.b, 0.45f));
            DrawItemSilhouetteOrPortrait(portRect, item);

            if (item is Gun lg)
            {
                Rect sil = new Rect(portRect.x + 3f, portRect.y + 3f, 32f, 20f);
                Box(sil, new Color(0.02f, 0.03f, 0.04f, 0.82f));
                FieldUIArt.GunClassIcon(new Rect(sil.x + 2f, sil.y + 2f, sil.width - 4f, sil.height - 4f), lg, Color.Lerp(theme, Color.white, 0.65f));
            }
            if (showJunk)
            {
                if (!junkBinIcon) junkBinIcon = Resources.Load<Texture2D>("Art/UI/junk-bin");
                if (junkBinIcon) GUI.DrawTexture(new Rect(portRect.xMax - 28f, portRect.y + 2f, 26f, 26f), junkBinIcon, ScaleMode.ScaleToFit, true);
            }

            if (r.height >= 175f && !string.IsNullOrEmpty(maker))
            {
                ManufacturerBranding.Badge(new Rect(portRect.x, portRect.yMax + 4f, 58f, 28f), maker);
                var scoreMiniStyle = EqCleanStyle(this.font, 14, FontStyle.Bold, TextAnchor.MiddleRight);
                EqCardLabel(new Rect(portRect.x + 56f, portRect.yMax + 4f, portSz - 56f, 28f), score.ToString("N0"), new Color(0.86f, 0.92f, 0.96f), scoreMiniStyle);
            }

            float tx = portRect.xMax + 12f;
            float tw = r.xMax - tx - 10f;

            var lvStyle = EqCleanStyle(this.font, 15, FontStyle.Bold, TextAnchor.UpperRight);
            EqCardLabel(new Rect(r.xMax - 68f, r.y + 8f, 58f, 22f), "Lv " + level, Color.white, lvStyle);

            var nameStyle = EqCleanStyle(this.font, MobileTouchControls.Enabled ? 21 : 18, FontStyle.Bold, TextAnchor.UpperLeft, wrap: true, clip: TextClipping.Clip);
            string cleanName = (name ?? "").Trim().Replace("\n ", "\n").Replace("\n　", "\n");
            float nameW = tw - 54f;
            while (nameStyle.fontSize > 13 && nameStyle.CalcHeight(new GUIContent(cleanName), nameW) > 44f)
                nameStyle.fontSize--;
            Color nameCol = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.50f);
            EqCardLabel(new Rect(tx + 1f, r.y + 8f, nameW, 44f), cleanName, new Color(0f, 0f, 0f, 0.88f), nameStyle);
            EqCardLabel(new Rect(tx, r.y + 7f, nameW, 44f), cleanName, nameCol, nameStyle);

            int chev = Mathf.Clamp((int)rarity + 1, 1, 5);
            var subStyle = EqCleanStyle(this.font, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            EqCardLabel(new Rect(tx, r.y + 56f, tw, 20f), new string('▸', chev) + " " + subType + " · " + VaultLanguage.RarityName(rarity), Color.Lerp(theme, Color.white, 0.25f), subStyle);

            BuildItemCardData(item, out var pStats, out _, out _);
            int miniCount = Mathf.Min(3, pStats.Count);
            if (miniCount > 0 && r.height >= 145f)
            {
                Rect miniBar = new Rect(tx, r.y + 80f, tw, 42f);
                Box(miniBar, new Color(0.025f, 0.045f, 0.065f, 0.82f));
                float cw = miniBar.width / miniCount;
                var mvStyle = EqCleanStyle(EqNumericFont() ?? this.font, 13, FontStyle.Bold, TextAnchor.MiddleLeft);
                for (int i = 0; i < miniCount; i++)
                {
                    var st = pStats[i];
                    float cx = miniBar.x + i * cw;
                    if (i > 0) Box(new Rect(cx, miniBar.y + 6f, 1f, miniBar.height - 12f), new Color(0.25f, 0.42f, 0.52f, 0.45f));
                    DrawEqCardIcon(new Rect(cx + 5f, miniBar.center.y - 9f, 18f, 18f), st.icon, Color.Lerp(st.tint, theme, 0.2f));
                    EqCardLabel(new Rect(cx + 25f, miniBar.y, cw - 26f, miniBar.height), st.value, st.tint, mvStyle);
                }
            }

            float botY = r.yMax - 30f;
            var botStyle = EqCleanStyle(this.font, 14, FontStyle.Normal, TextAnchor.MiddleLeft, rich: true);
            if (equipped)
            {
                Rect eqChip = new Rect(tx, botY + 2f, 96f, 22f);
                Box(eqChip, new Color(0.10f, 0.28f, 0.18f, 0.90f));
                Box(new Rect(eqChip.x, eqChip.y, 3f, eqChip.height), new Color(0.38f, 0.98f, 0.64f));
                EqCardLabel(new Rect(eqChip.x + 7f, eqChip.y, eqChip.width - 8f, eqChip.height), T("✓ EQUIPPED", "✓ 装備中"), new Color(0.52f, 1f, 0.72f), botStyle);
            }
            else if (!string.IsNullOrEmpty(comparisonRich))
            {
                EqCardLabel(new Rect(tx, botY, tw, 24f), T("SCORE ", "評価 ") + score + "  " + comparisonRich, new Color(0.82f, 0.88f, 0.92f), botStyle);
            }

            if (priceBadge > 0)
            {
                var pStyle = EqCleanStyle(this.font, 14, FontStyle.Bold, TextAnchor.MiddleRight);
                EqCardLabel(new Rect(r.xMax - 128f, botY, 118f, 24f), "$ " + priceBadge.ToString("N0"), new Color(0.45f, 0.96f, 0.62f), pStyle);
            }
        }

        public object GetEquippedComparisonTarget(object selectedItem)
        {
            if (selectedItem == null) return null;
            if (selectedItem is Gun g)
            {
                if (gun != null && !ReferenceEquals(gun, g)) return gun;
                for (int i = 0; i < AvailableWeaponSlotCount; i++)
                {
                    var w = WeaponInSlot(i);
                    if (w != null && !ReferenceEquals(w, g)) return w;
                }
                return gun;
            }
            if (selectedItem is ArmorItem a)
                return EquippedArmor != null && (int)a.slot < EquippedArmor.Length ? EquippedArmor[(int)a.slot] : null;
            if (selectedItem is ArtifactItem m)
            {
                if (EquippedArtifacts != null)
                {
                    foreach (var cur in EquippedArtifacts) if (cur != null && cur.effect == m.effect && !ReferenceEquals(cur, m)) return cur;
                    foreach (var cur in EquippedArtifacts) if (cur != null && !ReferenceEquals(cur, m)) return cur;
                    return EquippedArtifacts.Length > 0 ? EquippedArtifacts[0] : null;
                }
                return null;
            }
            if (selectedItem is MeleeWeaponItem)
                return EquippedMeleeWeapon;
            return null;
        }

        Vector2 eqCompareDetailScroll;
        float eqCompareDetailExtent;

        // Compact World / Reward Card (when standing near loot drop in dungeon/overworld)
        public void DrawBorderlandsWorldLootCard(Rect card, object item, string promptLabel)
        {
            if (item == null) return;
            EnsureEquipmentCardTextures();
            Rarity rarity = ItemRarityOf(item);
            Color theme = RarityThemeColor(rarity);
            int level = ItemLevelOf(item);
            int score = ItemScoreOf(item);
            string name = ItemDisplayNameOf(item);
            string subType = ItemTypeSubtitleOf(item);

            DrawRarityBackdrop(card, rarity, true, true, 52f);

            Rect pic = new Rect(card.x + 10f, card.y + 10f, 88f, 88f);
            Box(new Rect(pic.x - 1f, pic.y - 1f, pic.width + 2f, pic.height + 2f), new Color(theme.r, theme.g, theme.b, 0.5f));
            DrawItemSilhouetteOrPortrait(pic, item);

            float tx = pic.xMax + 10f;
            float tw = card.xMax - tx - 10f;

            var lvStyle = EqCleanStyle(this.font, 15, FontStyle.Bold, TextAnchor.UpperRight);
            EqCardLabel(new Rect(card.xMax - 68f, card.y + 8f, 58f, 22f), "Lv " + level, Color.white, lvStyle);

            var nStyle = EqCleanStyle(this.font, 17, FontStyle.Bold, TextAnchor.UpperLeft, wrap: true, clip: TextClipping.Clip);
            Color nameCol = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.45f);
            EqCardLabel(new Rect(tx, card.y + 7f, tw - 52f, 26f), name, nameCol, nStyle);

            int chev = Mathf.Clamp((int)rarity + 1, 1, 5);
            var subStyle = EqCleanStyle(this.font, 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            EqCardLabel(new Rect(tx, card.y + 33f, tw, 18f), new string('▸', chev) + " " + subType + " · " + VaultLanguage.RarityName(rarity), Color.Lerp(theme, Color.white, 0.25f), subStyle);

            BuildItemCardData(item, out var pStats, out _, out _);
            int cnt = Mathf.Min(3, pStats.Count);
            if (cnt > 0)
            {
                Rect strip = new Rect(tx, card.y + 56f, tw, 36f);
                Box(strip, new Color(0.02f, 0.04f, 0.06f, 0.82f));
                float cw = strip.width / cnt;
                var mvStyle = EqCleanStyle(this.font, 13, FontStyle.Bold, TextAnchor.MiddleLeft);
                for (int i = 0; i < cnt; i++)
                {
                    float cx = strip.x + i * cw;
                    if (i > 0) Box(new Rect(cx, strip.y + 5f, 1f, strip.height - 10f), new Color(0.25f, 0.42f, 0.52f, 0.45f));
                    DrawEqCardIcon(new Rect(cx + 4f, strip.center.y - 8f, 16f, 16f), pStats[i].icon, Color.Lerp(pStats[i].tint, theme, 0.2f));
                    EqCardLabel(new Rect(cx + 22f, strip.y, cw - 23f, strip.height), pStats[i].value, pStats[i].tint, mvStyle);
                }
            }

            Rect promptBar = new Rect(card.x + 10f, card.yMax - 36f, card.width - 20f, 28f);
            Box(promptBar, new Color(0.03f, 0.05f, 0.07f, 0.88f));
            DrawEqCardIcon(new Rect(promptBar.x + 6f, promptBar.center.y - 9f, 18f, 18f), 23, Gold);
            var scrStyle = EqCleanStyle(this.font, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            EqCardLabel(new Rect(promptBar.x + 28f, promptBar.y, 140f, promptBar.height), T("SCORE ", "評価 ") + score, Color.white, scrStyle);
            var prStyle = EqCleanStyle(this.font, 15, FontStyle.Bold, TextAnchor.MiddleRight);
            EqCardLabel(new Rect(promptBar.xMax - 190f, promptBar.y, 182f, promptBar.height), promptLabel, Gold, prStyle);
        }

        static void EqCardLabel(Rect r, string text, Color col, GUIStyle style)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.Label(r, text, style);
            GUI.color = prev;
        }
    }
}
