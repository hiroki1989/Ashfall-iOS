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
                    // Unity Texture2D y=0 is bottom, y=31 is top
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
                    float t = y / 63f; // 0 = bottom, 1 = top
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
                // Diagonal / chevron rarity accent stripe under header
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

            // Tactical corner notches (Borderlands card frame flair)
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
            // Avoid double-wrapping if rich text tags already exist
            if (raw.Contains("<color=")) return raw;
            return RxHighlightNums.Replace(raw, "<color=" + numHex + ">$1</color>");
        }

        void BuildItemCardData(object item, out List<EqStatCell> primaryStats, out List<EqStatCell> subStats, out List<EqFeatureRow> features)
        {
            primaryStats = new List<EqStatCell>();
            subStats = new List<EqStatCell>();
            features = new List<EqFeatureRow>();
            bool ja = VaultLanguage.Japanese;
            string maker = ItemMakerOf(item);

            Color cWhite = new Color(0.95f, 0.96f, 0.98f);
            Color cGold  = new Color(1.00f, 0.84f, 0.32f);
            Color cCyan  = new Color(0.42f, 0.88f, 1.00f);
            Color cGreen = new Color(0.40f, 0.95f, 0.62f);

            if (item is Gun g)
            {
                float avgDmg = g.damage;
                string dmgStr = avgDmg >= 100f ? Mathf.RoundToInt(avgDmg).ToString("N0") : avgDmg.ToString("0.0");
                if (g.pellets > 1) dmgStr += "×" + g.pellets;
                float rate = 1f / Mathf.Max(0.001f, g.interval);
                primaryStats.Add(new EqStatCell(0, dmgStr, T("DMG", "威力"), cWhite));
                primaryStats.Add(new EqStatCell(1, Arsenal.Accuracy(g).ToString("0") + "%", T("ACC", "精度"), cWhite));
                primaryStats.Add(new EqStatCell(2, g.reloadSeconds.ToString("0.1") + T("s", "秒"), T("RELOAD", "装填"), cWhite));
                primaryStats.Add(new EqStatCell(3, rate.ToString("0.1") + T("/s", "/秒"), T("RATE", "連射"), cWhite));
                primaryStats.Add(new EqStatCell(4, g.magazine.ToString(), T("MAG", "弾数"), cWhite));

                // Sub-banner stats: Crit + 30m Range Retention + Bullet Speed
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

                // 4) Rolled Affixes (each with dedicated stat icon!)
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

                // 5) Manufacturer Trait Row
                if (!string.IsNullOrEmpty(maker))
                {
                    Color mkCol = ManufacturerCatalog.Color(maker);
                    string mkHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(mkCol, Color.white, 0.30f));
                    features.Add(new EqFeatureRow
                    {
                        icon = 19,
                        badgeBg = new Color(mkCol.r * 0.20f, mkCol.g * 0.20f, mkCol.b * 0.20f, 0.90f),
                        iconTint = Color.Lerp(mkCol, Color.white, 0.25f),
                        borderTint = new Color(mkCol.r, mkCol.g, mkCol.b, 0.55f),
                        richText = "<color=#" + mkHex + ">" + ManufacturerCatalog.Name(maker, ja) + "</color> - " + ManufacturerCatalog.Description(maker, ja)
                    });
                }
            }
            else if (item is ArmorItem a)
            {
                primaryStats.Add(new EqStatCell(8, (a.damageReduction * 100f).ToString("0.#") + "%", T("GUARD", "軽減"), cCyan));
                primaryStats.Add(new EqStatCell(9, "+" + a.maxHealth.ToString("0.#"), T("MAX HP", "最大HP"), cGreen));

                // Pick top 2 other non-zero stats on this armor piece for the stat tiles
                var extras = new List<EqStatCell>();
                if (Mathf.Abs(a.damageBonus) > 0.0001f) extras.Add(new EqStatCell(0, "+" + (a.damageBonus * 100f).ToString("0.#") + "%", T("DMG", "威力"), cWhite));
                if (Mathf.Abs(a.moveBonus) > 0.0001f) extras.Add(new EqStatCell(11, (a.moveBonus >= 0 ? "+" : "") + (a.moveBonus * 100f).ToString("0.#") + "%", T("MOVE", "移動"), cWhite));
                if (Mathf.Abs(a.fireBonus) > 0.0001f) extras.Add(new EqStatCell(3, "+" + (a.fireBonus * 100f).ToString("0.#") + "%", T("RATE", "連射"), cWhite));
                if (Mathf.Abs(a.reloadBonus) > 0.0001f) extras.Add(new EqStatCell(2, "+" + (a.reloadBonus * 100f).ToString("0.#") + "%", T("RELOAD", "装填"), cWhite));
                if (Mathf.Abs(a.dodgeBonus) > 0.0001f) extras.Add(new EqStatCell(12, "+" + (a.dodgeBonus * 100f).ToString("0.#") + "%", T("DODGE", "回避"), cWhite));
                if (Mathf.Abs(a.accuracyBonus) > 0.0001f) extras.Add(new EqStatCell(1, "+" + (a.accuracyBonus * 100f).ToString("0.#") + "%", T("ACC", "精度"), cWhite));
                if (Mathf.Abs(a.regenBonus) > 0.0001f) extras.Add(new EqStatCell(10, "+" + a.regenBonus.ToString("0.1") + "/s", T("REGEN", "回復"), cGreen));
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

                if (!string.IsNullOrEmpty(maker))
                {
                    Color mkCol = ManufacturerCatalog.Color(maker);
                    string mkHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(mkCol, Color.white, 0.30f));
                    string mkDesc = ManufacturerCatalog.Description(maker, ja);
                    features.Add(new EqFeatureRow
                    {
                        icon = 19,
                        badgeBg = new Color(mkCol.r * 0.20f, mkCol.g * 0.20f, mkCol.b * 0.20f, 0.90f),
                        iconTint = Color.Lerp(mkCol, Color.white, 0.25f),
                        borderTint = new Color(mkCol.r, mkCol.g, mkCol.b, 0.55f),
                        richText = "<color=#" + mkHex + ">" + ManufacturerCatalog.Name(maker, ja) + "</color> - " + mkDesc
                    });
                }
            }
            else if (item is ArtifactItem m)
            {
                string outVal = m.effect == ArtifactEffect.KillShield ? "+" + (8f * m.power).ToString("0.0")
                              : m.effect == ArtifactEffect.DashPulse ? (12f * m.power).ToString("0.0")
                              : m.effect == ArtifactEffect.ReloadSurge ? "+" + (15f * m.power).ToString("0") + "%"
                              : "+" + (12f * m.power).ToString("0") + "%";
                string outLbl = m.effect == ArtifactEffect.KillShield ? T("SHIELD", "シールド")
                              : m.effect == ArtifactEffect.DashPulse ? T("PULSE", "波動威力")
                              : T("SURGE", "威力増幅");
                int outIcon = m.effect == ArtifactEffect.KillShield ? 8 : m.effect == ArtifactEffect.DashPulse ? 22 : 0;

                primaryStats.Add(new EqStatCell(outIcon, outVal, outLbl, cCyan));
                primaryStats.Add(new EqStatCell(16, m.cooldown > 0.01f ? m.cooldown.ToString("0.1") + T("s", "秒") : T("PASSIVE", "常時"), T("COOLDOWN", "再発動"), cWhite));
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

                if (!string.IsNullOrEmpty(maker))
                {
                    Color mkCol = ManufacturerCatalog.Color(maker);
                    string mkHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(mkCol, Color.white, 0.30f));
                    features.Add(new EqFeatureRow
                    {
                        icon = 19,
                        badgeBg = new Color(mkCol.r * 0.20f, mkCol.g * 0.20f, mkCol.b * 0.20f, 0.90f),
                        iconTint = Color.Lerp(mkCol, Color.white, 0.25f),
                        borderTint = new Color(mkCol.r, mkCol.g, mkCol.b, 0.55f),
                        richText = "<color=#" + mkHex + ">" + ManufacturerCatalog.Name(maker, ja) + "</color> - " + ManufacturerCatalog.Description(maker, ja)
                    });
                }
            }
            else if (item is MeleeWeaponItem w)
            {
                MeleeWeaponCatalog.EnsureAffixes(w);
                float rate = 1f / Mathf.Max(0.01f, w.interval);
                primaryStats.Add(new EqStatCell(13, w.damage.ToString("N0"), T("DMG", "威力"), cWhite));
                primaryStats.Add(new EqStatCell(3, rate.ToString("0.1") + T("/s", "/秒"), T("SPEED", "速度"), cWhite));
                primaryStats.Add(new EqStatCell(6, w.effectiveRange.ToString("0.1") + "m", T("REACH", "射程"), cCyan));
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

                if (!string.IsNullOrEmpty(maker))
                {
                    Color mkCol = ManufacturerCatalog.Color(maker);
                    string mkHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(mkCol, Color.white, 0.30f));
                    features.Add(new EqFeatureRow
                    {
                        icon = 19,
                        badgeBg = new Color(mkCol.r * 0.20f, mkCol.g * 0.20f, mkCol.b * 0.20f, 0.90f),
                        iconTint = Color.Lerp(mkCol, Color.white, 0.25f),
                        borderTint = new Color(mkCol.r, mkCol.g, mkCol.b, 0.55f),
                        richText = "<color=#" + mkHex + ">" + ManufacturerCatalog.Name(maker, ja) + "</color> - " + ManufacturerCatalog.Description(maker, ja)
                    });
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

        bool eqCompareEquipped;

        // Draws a full Borderlands-style inspector card inside cardRect, with scrollable feature body and fixed Maker/Price footer.
        public void DrawBorderlandsItemCard(Rect cardRect, object item, ref Vector2 detailScroll, out float computedDetailExtent,
            bool equipped = false, int compareReferenceScore = 0, bool hasComparison = false, int priceOverride = -1, bool showJunkBadge = false)
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

            bool compact = cardRect.height < 420f;
            float headerH = compact ? 96f : 112f;
            float footerH = 38f;

            // 1. Full-panel rarity background + glowing Borderlands header band
            DrawRarityBackdrop(cardRect, rarity, false, true, headerH);

            // 2. HEADER: Item Name (top-left), Lv badge (top-right), Rarity/Type chevrons + Hero DPS/SCORE + Item Portrait/Class Silhouette
            float padX = 12f;

            // Level badge top-right (use this.font Bold so digits and '.' never drop glyphs)
            var lvStyle = new GUIStyle(label)
            {
                font = this.font,
                fontSize = compact ? 18 : 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                wordWrap = false
            };
            string lvText = "Lv " + level;
            float lvW = Mathf.Max(68f, lvStyle.CalcSize(new GUIContent(lvText)).x + 8f);
            EqCardLabel(new Rect(cardRect.xMax - padX - lvW + 1f, cardRect.y + 8f, lvW, 24f), lvText, new Color(0f, 0f, 0f, 0.88f), lvStyle);
            EqCardLabel(new Rect(cardRect.xMax - padX - lvW, cardRect.y + 7f, lvW, 24f), lvText, Color.white, lvStyle);

            // Item Portrait box on right side of header
            float portW = compact ? 108f : 132f;
            float portH = compact ? 60f : 72f;
            Rect portRect = new Rect(cardRect.xMax - padX - portW, cardRect.y + headerH - portH - 6f, portW, portH);
            Box(new Rect(portRect.x - 1f, portRect.y - 1f, portRect.width + 2f, portRect.height + 2f), new Color(theme.r, theme.g, theme.b, 0.48f));
            DrawItemSilhouetteOrPortrait(portRect, item);

            // Class silhouette mini-badge at top-left of portrait for guns
            if (item is Gun hg)
            {
                Rect silRect = new Rect(portRect.x + 3f, portRect.y + 3f, 34f, 20f);
                Box(silRect, new Color(0.02f, 0.03f, 0.04f, 0.80f));
                FieldUIArt.GunClassIcon(new Rect(silRect.x + 2f, silRect.y + 2f, silRect.width - 4f, silRect.height - 4f), hg, Color.Lerp(theme, Color.white, 0.65f));
            }
            if (showJunkBadge)
            {
                if (!junkBinIcon) junkBinIcon = Resources.Load<Texture2D>("Art/UI/junk-bin");
                if (junkBinIcon) GUI.DrawTexture(new Rect(portRect.xMax - 28f, portRect.y + 2f, 26f, 26f), junkBinIcon, ScaleMode.ScaleToFit, true);
            }

            // Item Name (auto-fitted, crisp shadow)
            float nameMaxW = portRect.x - (cardRect.x + padX) - 6f;
            var nameStyle = new GUIStyle(label)
            {
                font = this.font,
                fontSize = compact ? 18 : 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            string cleanName = (itemName ?? "").Trim().Replace("\n ", "\n").Replace("\n　", "\n");
            while (nameStyle.fontSize > 13 && nameStyle.CalcHeight(new GUIContent(cleanName), nameMaxW) > (compact ? 40f : 48f))
                nameStyle.fontSize--;
            Rect nameRect = new Rect(cardRect.x + padX, cardRect.y + 7f, nameMaxW, compact ? 42f : 50f);
            Color nameColor = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.52f);
            EqCardLabel(new Rect(nameRect.x + 1f, nameRect.y + 1f, nameRect.width, nameRect.height), cleanName, new Color(0f, 0f, 0f, 0.9f), nameStyle);
            EqCardLabel(nameRect, cleanName, nameColor, nameStyle);

            // Rarity Chevrons + Category Label
            float typeY = cardRect.y + (compact ? 47f : 56f);
            var typeStyle = new GUIStyle(label) { font = this.font, fontSize = compact ? 12 : 13, wordWrap = false };
            int chevCount = Mathf.Clamp((int)rarity + 1, 1, 5);
            string chevrons = new string('▸', chevCount);
            string catLine = chevrons + " " + typeSubtitle + "  ·  " + VaultLanguage.RarityName(rarity);
            EqCardLabel(new Rect(cardRect.x + padX, typeY, nameMaxW + 16f, 19f), catLine, Color.Lerp(theme, new Color(1f, 0.92f, 0.55f), 0.35f), typeStyle);

            // Hero DPS / SCORE Pill Box (bottom-left of header, just like Borderlands "30,824 DPS")
            float heroY = cardRect.y + headerH - (compact ? 29f : 33f);
            float heroH = compact ? 24f : 28f;
            float heroValue = item is Gun dpsGun
                ? (dpsGun.damage * Mathf.Max(1, dpsGun.pellets) / Mathf.Max(0.05f, dpsGun.interval))
                : item is MeleeWeaponItem dpsMelee
                ? (dpsMelee.damage / Mathf.Max(0.05f, dpsMelee.interval))
                : score;
            string heroNum = item is Gun || item is MeleeWeaponItem
                ? (heroValue >= 100f ? Mathf.RoundToInt(heroValue).ToString("N0") : heroValue.ToString("0.0"))
                : score.ToString("N0");
            string heroUnit = (item is Gun || item is MeleeWeaponItem) ? "DPS" : T("SCORE", "評価");

            var heroNumStyle = new GUIStyle(label)
            {
                font = EqNumericFont() ?? this.font,
                fontSize = compact ? 19 : 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            var heroUnitStyle = new GUIStyle(label) { font = this.font, fontSize = compact ? 11 : 13, alignment = TextAnchor.LowerLeft, wordWrap = false };
            float numW = heroNumStyle.CalcSize(new GUIContent(heroNum)).x;
            float unitW = heroUnitStyle.CalcSize(new GUIContent(heroUnit)).x;
            float pillW = Mathf.Min(nameMaxW, numW + unitW + 20f);
            Rect heroPill = new Rect(cardRect.x + padX, heroY, pillW, heroH);
            Box(heroPill, new Color(0.02f, 0.03f, 0.04f, 0.76f));
            Box(new Rect(heroPill.x, heroPill.y, 3f, heroPill.height), theme);
            EqCardLabel(new Rect(heroPill.x + 8f, heroPill.y - 1f, numW + 6f, heroPill.height), heroNum, Color.white, heroNumStyle);
            EqCardLabel(new Rect(heroPill.x + 10f + numW, heroPill.y, unitW + 8f, heroPill.height - 3f), heroUnit, new Color(0.75f, 0.82f, 0.88f), heroUnitStyle);

            // Equipped or Score Comparison chip next to Hero Pill
            float compX = heroPill.xMax + 8f;
            float compMaxW = portRect.x - compX - 4f;
            if (compMaxW > 42f)
            {
                var compStyle = new GUIStyle(label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false };
                if (equipped)
                {
                    Rect eqBadge = new Rect(compX, heroY + 3f, Mathf.Min(compMaxW, 96f), heroH - 6f);
                    Box(eqBadge, new Color(0.10f, 0.28f, 0.18f, 0.90f));
                    Box(new Rect(eqBadge.x, eqBadge.y, eqBadge.width, 1f), new Color(0.35f, 0.95f, 0.60f, 0.8f));
                    EqCardLabel(new Rect(eqBadge.x + 6f, eqBadge.y, eqBadge.width - 8f, eqBadge.height), T("✓ EQUIPPED", "✓ 装備中"), new Color(0.48f, 1f, 0.70f), compStyle);
                }
                else if (hasComparison)
                {
                    int delta = score - compareReferenceScore;
                    string cmpStr = delta > 0 ? "<color=#4DFF88>▲+" + delta + "</color>"
                                  : delta < 0 ? "<color=#FF5E5E>▼" + delta + "</color>"
                                  : "<color=#B0BEC5>＝0</color>";
                    EqCardLabel(new Rect(compX, heroY, compMaxW, heroH), cmpStr, Color.white, compStyle);
                }
                else if (item is Gun || item is MeleeWeaponItem)
                {
                    EqCardLabel(new Rect(compX, heroY, compMaxW, heroH), T("SCR ", "評価 ") + score, new Color(0.72f, 0.80f, 0.85f), compStyle);
                }
            }

            // 3. BUILD STAT CELLS & FEATURE ROWS
            BuildItemCardData(item, out var primaryStats, out var subStats, out var features);

            float cursorY = cardRect.y + headerH + 5f;

            // 4. PRIMARY STAT ICON TILE STRIP (e.g. 5 columns for guns, 4 for armor/melee, 3 for modules)
            if (primaryStats.Count > 0)
            {
                float stripH = compact ? 50f : 56f;
                Rect stripRect = new Rect(cardRect.x + 8f, cursorY, cardRect.width - 16f, stripH);
                Box(stripRect, new Color(0.03f, 0.055f, 0.078f, 0.88f));
                Box(new Rect(stripRect.x, stripRect.y, stripRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.35f));
                Box(new Rect(stripRect.x, stripRect.yMax - 1f, stripRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.35f));

                int cols = primaryStats.Count;
                float cellW = stripRect.width / cols;
                var valStyle = new GUIStyle(label)
                {
                    font = EqNumericFont() ?? this.font,
                    fontSize = cols >= 5 ? (compact ? 14 : 16) : (compact ? 16 : 18),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
                var lblStyle = new GUIStyle(label)
                {
                    font = this.font,
                    fontSize = 10,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = false
                };

                for (int i = 0; i < cols; i++)
                {
                    var st = primaryStats[i];
                    float cx = stripRect.x + i * cellW;
                    if (i > 0)
                        Box(new Rect(cx, stripRect.y + 8f, 1f, stripRect.height - 16f), new Color(0.26f, 0.44f, 0.54f, 0.55f));

                    float iconSz = compact ? 18f : 21f;
                    DrawEqCardIcon(new Rect(cx + (cellW - iconSz) * 0.5f, stripRect.y + 3f, iconSz, iconSz), st.icon, Color.Lerp(st.tint, theme, 0.22f));

                    // Fit value text if needed
                    GUIStyle vStyle = valStyle;
                    if (vStyle.CalcSize(new GUIContent(st.value)).x > cellW - 4f)
                    {
                        vStyle = new GUIStyle(valStyle) { fontSize = Mathf.Max(11, valStyle.fontSize - 3) };
                    }
                    EqCardLabel(new Rect(cx + 1f, stripRect.y + (compact ? 21f : 24f), cellW - 2f, 19f), st.value, st.tint, vStyle);
                    if (!compact)
                        EqCardLabel(new Rect(cx + 1f, stripRect.y + 41f, cellW - 2f, 13f), st.label, new Color(0.58f, 0.68f, 0.75f), lblStyle);
                }
                cursorY += stripH + 3f;
            }

            // 5. SECONDARY SUB-STAT BANNER (Crit / 30m Range / Extra Armor Bonuses — like Borderlands' skull +33% bar)
            if (subStats.Count > 0)
            {
                float subH = compact ? 25f : 28f;
                Rect subRect = new Rect(cardRect.x + 8f, cursorY, cardRect.width - 16f, subH);
                Box(subRect, new Color(0.045f, 0.08f, 0.11f, 0.84f));
                Box(new Rect(subRect.x, subRect.yMax - 1f, subRect.width, 1f), new Color(0.24f, 0.42f, 0.52f, 0.45f));

                int sCols = Mathf.Min(3, subStats.Count);
                float sCellW = subRect.width / sCols;
                var sValStyle = new GUIStyle(label) { font = EqNumericFont() ?? this.font, fontSize = compact ? 12 : 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
                for (int i = 0; i < sCols; i++)
                {
                    var st = subStats[i];
                    float sx = subRect.x + i * sCellW;
                    if (i > 0)
                        Box(new Rect(sx, subRect.y + 5f, 1f, subRect.height - 10f), new Color(0.26f, 0.44f, 0.54f, 0.45f));

                    string combined = st.label + " " + st.value;
                    float textW = sValStyle.CalcSize(new GUIContent(combined)).x;
                    float totalW = Mathf.Min(sCellW - 8f, 18f + 5f + textW);
                    float startX = sx + (sCellW - totalW) * 0.5f;
                    DrawEqCardIcon(new Rect(startX, subRect.center.y - 9f, 18f, 18f), st.icon, st.tint);
                    EqCardLabel(new Rect(startX + 22f, subRect.y, sCellW - (startX - sx) - 23f, subRect.height), combined, st.tint, sValStyle);
                }
                cursorY += subH + 4f;
            }

            // 6. SCROLLABLE FEATURE ROWS
            float bodyBottom = cardRect.yMax - footerH - 3f;
            float bodyH = Mathf.Max(48f, bodyBottom - cursorY);
            Rect scrollViewport = new Rect(cardRect.x + 8f, cursorY, cardRect.width - 16f, bodyH);

            var featStyle = new GUIStyle(label)
            {
                font = this.font,
                fontSize = MobileTouchControls.Enabled ? 18 : (compact ? 13 : 14),
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };
            featStyle.normal.textColor = new Color(0.90f, 0.93f, 0.96f);

            float contentW = scrollViewport.width - 14f;
            float iconBoxSz = compact ? 25f : 28f;
            float textColW = contentW - iconBoxSz - 14f;

            // Measure total height
            float totalH = 2f;
            var rowHeights = new float[features.Count];
            for (int i = 0; i < features.Count; i++)
            {
                float th = featStyle.CalcHeight(new GUIContent(features[i].richText), textColW);
                rowHeights[i] = Mathf.Max(iconBoxSz + 6f, th + 6f);
                totalH += rowHeights[i] + 3f;
            }
            computedDetailExtent = Mathf.Max(bodyH, totalH + 2f);

            if (Event.current != null && Event.current.type == EventType.ScrollWheel && scrollViewport.Contains(Event.current.mousePosition) && computedDetailExtent > bodyH)
            {
                detailScroll.y = Mathf.Clamp(detailScroll.y + Event.current.delta.y * 22f, 0f, computedDetailExtent - bodyH);
                Event.current.Use();
            }

            detailScroll = GUI.BeginScrollView(scrollViewport, detailScroll, new Rect(0f, 0f, contentW, computedDetailExtent));
            float ry = 1f;
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                float rh = rowHeights[i];
                Rect rowRect = new Rect(0f, ry, contentW, rh);
                Box(rowRect, new Color(0.025f, 0.045f, 0.065f, 0.68f));
                Box(new Rect(rowRect.x, rowRect.y, 2f, rowRect.height), f.borderTint);

                // Framed Icon Badge on Left
                Rect badgeRect = new Rect(rowRect.x + 5f, rowRect.y + (rh - iconBoxSz) * 0.5f, iconBoxSz, iconBoxSz);
                Box(badgeRect, f.badgeBg);
                Box(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1f), f.borderTint);
                Box(new Rect(badgeRect.x, badgeRect.yMax - 1f, badgeRect.width, 1f), f.borderTint);
                Box(new Rect(badgeRect.x, badgeRect.y, 1f, badgeRect.height), f.borderTint);
                Box(new Rect(badgeRect.xMax - 1f, badgeRect.y, 1f, badgeRect.height), f.borderTint);

                Rect icRect = new Rect(badgeRect.x + 3f, badgeRect.y + 3f, badgeRect.width - 6f, badgeRect.height - 6f);
                if (f.element != GunElement.None)
                    DrawEqElementSymbol(icRect, f.element);
                else
                    DrawEqCardIcon(icRect, f.icon, f.iconTint);

                // Rich text description on Right
                float th = featStyle.CalcHeight(new GUIContent(f.richText), textColW);
                float ty = rowRect.y + Mathf.Max(2f, (rh - th) * 0.5f);
                GUI.Label(new Rect(badgeRect.xMax + 7f, ty, textColW, th + 4f), f.richText, featStyle);

                ry += rh + 3f;
            }
            GUI.EndScrollView();

            // 7. FOOTER BAR: Manufacturer Wordmark Badge (Left) + Green Bracketed Scrap Value Box (Right)
            Rect footerRect = new Rect(cardRect.x + 4f, cardRect.yMax - footerH - 2f, cardRect.width - 8f, footerH);
            Box(footerRect, new Color(0.02f, 0.032f, 0.045f, 0.92f));
            Box(new Rect(footerRect.x, footerRect.y, footerRect.width, 1f), new Color(theme.r, theme.g, theme.b, 0.42f));

            if (!string.IsNullOrEmpty(maker))
            {
                Color mkCol = ManufacturerCatalog.Color(maker);
                Box(new Rect(footerRect.x + 6f, footerRect.y + 5f, 146f, footerRect.height - 10f), new Color(mkCol.r * 0.16f, mkCol.g * 0.16f, mkCol.b * 0.16f, 0.65f));
                ManufacturerBranding.Badge(new Rect(footerRect.x + 8f, footerRect.y + 3f, 68f, footerRect.height - 6f), maker);
                var mkStyle = new GUIStyle(label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = false };
                EqCardLabel(new Rect(footerRect.x + 78f, footerRect.y, 110f, footerRect.height), ManufacturerCatalog.Name(maker, VaultLanguage.Japanese), Color.Lerp(mkCol, Color.white, 0.35f), mkStyle);
            }

            if (price > 0)
            {
                float valW = Mathf.Min(195f, footerRect.width * 0.46f);
                Rect priceBox = new Rect(footerRect.xMax - valW - 8f, footerRect.y + 6f, valW, footerRect.height - 12f);
                Color cashGreen = new Color(0.28f, 0.92f, 0.48f, 0.95f);
                Box(priceBox, new Color(0.04f, 0.16f, 0.09f, 0.90f));
                // Left & right green vertical brackets just like Borderlands "$ 1,415,812"
                Box(new Rect(priceBox.x, priceBox.y, 3f, priceBox.height), cashGreen);
                Box(new Rect(priceBox.xMax - 3f, priceBox.y, 3f, priceBox.height), cashGreen);
                DrawEqCardIcon(new Rect(priceBox.x + 7f, priceBox.center.y - 10f, 20f, 20f), 20, cashGreen);

                var priceStyle = new GUIStyle(label)
                {
                    font = EqNumericFont() ?? this.font,
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false
                };
                string priceText = price.ToString("N0");
                while (priceStyle.fontSize > 12 && priceStyle.CalcSize(new GUIContent(priceText)).x > priceBox.width - 36f)
                    priceStyle.fontSize--;
                EqCardLabel(new Rect(priceBox.x + 28f, priceBox.y, priceBox.width - 36f, priceBox.height), priceText, new Color(0.85f, 1f, 0.90f), priceStyle);
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

            // Rarity-tinted full card background + header strip
            DrawRarityBackdrop(r, rarity, selected, true, 54f);

            // Left Item Portrait
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

            // Maker badge under portrait if tall enough
            if (r.height >= 175f && !string.IsNullOrEmpty(maker))
            {
                ManufacturerBranding.Badge(new Rect(portRect.x, portRect.yMax + 4f, 58f, 28f), maker);
                var scoreMiniStyle = new GUIStyle(label)
                {
                    font = this.font,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false
                };
                EqCardLabel(new Rect(portRect.x + 56f, portRect.yMax + 4f, portSz - 56f, 28f), score.ToString("N0"), new Color(0.86f, 0.92f, 0.96f), scoreMiniStyle);
            }

            float tx = portRect.xMax + 12f;
            float tw = r.xMax - tx - 10f;

            // Top row: Level right-aligned + Name left-aligned
            var lvStyle = new GUIStyle(label)
            {
                font = this.font,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                wordWrap = false
            };
            string lvStr = "Lv " + level;
            EqCardLabel(new Rect(r.xMax - 68f, r.y + 8f, 58f, 22f), lvStr, Color.white, lvStyle);

            var nameStyle = new GUIStyle(label)
            {
                font = this.font,
                fontSize = MobileTouchControls.Enabled ? 21 : 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            string cleanName = (name ?? "").Trim().Replace("\n ", "\n").Replace("\n　", "\n");
            float nameW = tw - 54f;
            while (nameStyle.fontSize > 13 && nameStyle.CalcHeight(new GUIContent(cleanName), nameW) > 44f)
                nameStyle.fontSize--;
            Color nameCol = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.50f);
            EqCardLabel(new Rect(tx + 1f, r.y + 8f, nameW, 44f), cleanName, new Color(0f, 0f, 0f, 0.88f), nameStyle);
            EqCardLabel(new Rect(tx, r.y + 7f, nameW, 44f), cleanName, nameCol, nameStyle);

            // Rarity chevrons + subtype
            int chev = Mathf.Clamp((int)rarity + 1, 1, 5);
            var subStyle = new GUIStyle(label) { fontSize = 13, wordWrap = false };
            EqCardLabel(new Rect(tx, r.y + 56f, tw, 20f), new string('▸', chev) + " " + subType + " · " + VaultLanguage.RarityName(rarity), Color.Lerp(theme, Color.white, 0.25f), subStyle);

            // Mini 3-stat icon strip inside list card!
            BuildItemCardData(item, out var pStats, out _, out _);
            int miniCount = Mathf.Min(3, pStats.Count);
            if (miniCount > 0 && r.height >= 145f)
            {
                Rect miniBar = new Rect(tx, r.y + 80f, tw, 42f);
                Box(miniBar, new Color(0.025f, 0.045f, 0.065f, 0.82f));
                float cw = miniBar.width / miniCount;
                var mvStyle = new GUIStyle(label)
                {
                    font = EqNumericFont() ?? this.font,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false
                };
                for (int i = 0; i < miniCount; i++)
                {
                    var st = pStats[i];
                    float cx = miniBar.x + i * cw;
                    if (i > 0) Box(new Rect(cx, miniBar.y + 6f, 1f, miniBar.height - 12f), new Color(0.25f, 0.42f, 0.52f, 0.45f));
                    DrawEqCardIcon(new Rect(cx + 5f, miniBar.center.y - 9f, 18f, 18f), st.icon, Color.Lerp(st.tint, theme, 0.2f));
                    EqCardLabel(new Rect(cx + 25f, miniBar.y, cw - 26f, miniBar.height), st.value, st.tint, mvStyle);
                }
            }

            // Bottom status line: Equipped badge / Comparison delta / Optional Price
            float botY = r.yMax - 30f;
            var botStyle = new GUIStyle(label) { fontSize = 14, richText = true, wordWrap = false, alignment = TextAnchor.MiddleLeft };
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
                var pStyle = new GUIStyle(label)
                {
                    font = this.font,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false
                };
                EqCardLabel(new Rect(r.xMax - 128f, botY, 118f, 24f), "$ " + priceBadge.ToString("N0"), new Color(0.45f, 0.96f, 0.62f), pStyle);
            }
        }

        // Returns comparable currently equipped item for side-by-side Borderlands comparison
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

            var lvStyle = new GUIStyle(label) { font = this.font, fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            EqCardLabel(new Rect(card.xMax - 68f, card.y + 8f, 58f, 22f), "Lv " + level, Color.white, lvStyle);

            var nStyle = new GUIStyle(label) { font = this.font, fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true, clipping = TextClipping.Clip };
            Color nameCol = Color.Lerp(theme, Color.white, rarity == Rarity.Common ? 0.78f : 0.45f);
            EqCardLabel(new Rect(tx, card.y + 7f, tw - 52f, 26f), name, nameCol, nStyle);

            int chev = Mathf.Clamp((int)rarity + 1, 1, 5);
            var subStyle = new GUIStyle(label) { fontSize = 12, wordWrap = false };
            EqCardLabel(new Rect(tx, card.y + 33f, tw, 18f), new string('▸', chev) + " " + subType + " · " + VaultLanguage.RarityName(rarity), Color.Lerp(theme, Color.white, 0.25f), subStyle);

            // Mini stat icons row
            BuildItemCardData(item, out var pStats, out _, out _);
            int cnt = Mathf.Min(3, pStats.Count);
            if (cnt > 0)
            {
                Rect strip = new Rect(tx, card.y + 56f, tw, 36f);
                Box(strip, new Color(0.02f, 0.04f, 0.06f, 0.82f));
                float cw = strip.width / cnt;
                var mvStyle = new GUIStyle(label) { font = this.font, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                for (int i = 0; i < cnt; i++)
                {
                    float cx = strip.x + i * cw;
                    if (i > 0) Box(new Rect(cx, strip.y + 5f, 1f, strip.height - 10f), new Color(0.25f, 0.42f, 0.52f, 0.45f));
                    DrawEqCardIcon(new Rect(cx + 4f, strip.center.y - 8f, 16f, 16f), pStats[i].icon, Color.Lerp(pStats[i].tint, theme, 0.2f));
                    EqCardLabel(new Rect(cx + 22f, strip.y, cw - 23f, strip.height), pStats[i].value, pStats[i].tint, mvStyle);
                }
            }

            // Bottom prompt bar
            Rect promptBar = new Rect(card.x + 10f, card.yMax - 36f, card.width - 20f, 28f);
            Box(promptBar, new Color(0.03f, 0.05f, 0.07f, 0.88f));
            DrawEqCardIcon(new Rect(promptBar.x + 6f, promptBar.center.y - 9f, 18f, 18f), 23, Gold);
            var scrStyle = new GUIStyle(label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            EqCardLabel(new Rect(promptBar.x + 28f, promptBar.y, 140f, promptBar.height), T("SCORE ", "評価 ") + score, Color.white, scrStyle);
            var prStyle = new GUIStyle(label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
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
