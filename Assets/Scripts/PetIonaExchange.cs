using PlayerPrefs = NeonVault.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string PetIonaIntroKey = "Vault.Pet.IonaIntro.v1";
        bool PetIonaIntroSeen => !smoke && PlayerPrefs.GetInt(PetIonaIntroKey, 0) != 0;
        int petExchangeCategory;
        string petExchangeSelection;
        Vector2 petExchangeScroll;

        bool PetExchangeLegendary(SaleEntry e)
        {
            if (e == null || e.equipped || e.lastRequired) return false;
            switch (e.kind)
            {
                case SaleKind.Gun: return Weapons.Exists(x => x.id == e.id && x.rarity == Rarity.Legendary);
                case SaleKind.Armor: return ArmorInventory.Exists(x => x.id == e.id && x.rarity == Rarity.Legendary);
                case SaleKind.Module: return ArtifactInventory.Exists(x => x.id == e.id && x.rarity == Rarity.Legendary);
                case SaleKind.Melee: return MeleeWeaponInventory.Exists(x => x.id == e.id && x.rarity == Rarity.Legendary);
                default: return false;
            }
        }

        bool DonatePetLegendary(SaleEntry entry)
        {
            if (!PetExchangeLegendary(entry)) return false;
            bool removed = false;
            if (entry.kind == SaleKind.Gun)
            {
                int index = Weapons.FindIndex(x => x.id == entry.id);
                if (index >= 0 && EquippedWeaponSlot(Weapons[index]) < 0 && Weapons.Count > 1)
                { DiscardWeapon(index); removed = true; }
            }
            else if (entry.kind == SaleKind.Armor)
            {
                var item = ArmorInventory.Find(x => x.id == entry.id);
                if (item != null && !IsArmorEquipped(item) && Armory.IsActiveArmor(item))
                { DiscardArmor(item); removed = true; }
            }
            else if (entry.kind == SaleKind.Module)
            {
                var item = ArtifactInventory.Find(x => x.id == entry.id);
                if (item != null && Array.IndexOf(EquippedArtifacts, item) < 0)
                { DiscardArtifact(item); removed = true; }
            }
            else if (entry.kind == SaleKind.Melee)
            {
                var item = MeleeWeaponInventory.Find(x => x.id == entry.id);
                if (item != null && item != EquippedMeleeWeapon && MeleeWeaponInventory.Count > 1)
                { MeleeWeaponInventory.Remove(item); SaveInventory(); removed = true; }
            }
            if (!removed) return false;
            LoadJunk(); junkKeys.Remove(SaleKey(entry)); SaveJunk();
            GrantPetPoint();
            PlayUserSound("skill", .35f);
            Notify("IONA / PET POINT +1", "イオナ / ペットポイント +1");
            return true;
        }

        bool DrawPetIonaPages()
        {
            if (collectorPage < 14 || collectorPage > 18) return false;
            if (collectorPage <= 16)
            {
                string[] ja = {
                    "あら、その子……ルークっていうの？ いい目をしてる。ちょっとだけ、触ってもいい？",
                    "珍しい装備には、あの子の訓練に使える部品があるの。不要なレジェンダリーを一つ譲ってくれたら、ペットポイントを一つあげる。",
                    "ポイントはインベントリのペット画面で使ってね。回復、弾薬、探索の得意分野から一つ選べるわ。ルーク、楽しみね。"
                };
                string[] en = {
                    "Oh, who's this? Rook? What a lovely pair of eyes. May I say hello?",
                    "Rare gear has parts I can use for his training. Bring me a legendary item you no longer need and I'll give you one pet point.",
                    "Spend those points in the pet screen in your inventory. Choose a specialty: rescue, ammunition, or finding equipment. Shall we get started, Rook?"
                };
                Box(new Rect(0, 0, 1440, 900), new Color(0, 0, 0, .76f));
                DrawUnifiedDialogueScene("iona", false, T("IONA / COLLECTOR", "イオナ / コレクター"), (VaultLanguage.Japanese ? ja : en)[collectorPage - 14], Gold, (collectorPage - 13) + " / 3");
                if (WorldButton(UnifiedDialoguePrimaryButton, T("NEXT", "次へ")))
                {
                    collectorPage++;
                    if (collectorPage == 17)
                    {
                        if (!smoke) { PlayerPrefs.SetInt(PetIonaIntroKey, 1); PlayerPrefs.Save(); }
                        collectorPage = collectorIntroSeen ? 12 : 0;
                    }
                }
                return true;
            }

            Box(new Rect(0, 0, 1440, 900), PanelInk);
            Text(60, 35, T("IONA / PET RESEARCH", "イオナ / ペットの研究"), heading, Gold, 1040);
            Text(64, 100, T("Trade one legendary item for one pet point. Equipped items cannot be traded.", "レジェンダリー装備1つでペットポイント1。装備中の品は渡せません。"), small, UiPaper, 1200);
            if (WorldButton(new Rect(1160, 35, 220, 60), T("BACK", "戻る"))) { collectorPage = 12; petExchangeSelection = null; return true; }
            string[] enTabs = { "GUNS", "ARMOR", "MODULES", "MELEE" }, jaTabs = { "銃", "防具", "モジュール", "近接武器" };
            for (int i = 0; i < 4; i++)
                if (Button(new Rect(65 + i * 250, 155, 235, 55), T(enTabs[i], jaTabs[i]), i == petExchangeCategory))
                { petExchangeCategory = i; petExchangeSelection = null; petExchangeScroll = Vector2.zero; }
            var all = SaleEntries(petExchangeCategory);
            var entries = new List<SaleEntry>();
            foreach (var entry in all) if (PetExchangeLegendary(entry)) entries.Add(entry);
            if (entries.Count == 0) Text(85, 265, T("No eligible legendary items.", "渡せるレジェンダリー装備がありません。"), label, Mist, 1100);
            petExchangeScroll = GUI.BeginScrollView(new Rect(65, 225, 1310, 555), petExchangeScroll, new Rect(0, 0, 1260, Mathf.Max(540, entries.Count * 104)));
            foreach (var entry in entries)
            {
                int idx = entries.IndexOf(entry); float y = idx * 104;
                Panel(new Rect(0, y, 1240, 96), Gold);
                Text(18, y + 20, entry.title, label, UiPaper, 850);
                if (WorldButton(new Rect(920, y + 16, 290, 60), T("SELECT", "選ぶ"))) { petExchangeSelection = SaleKey(entry); collectorPage = 18; }
            }
            GUI.EndScrollView();
            if (collectorPage == 18)
            {
                var selected = entries.Find(e => SaleKey(e) == petExchangeSelection);
                if (selected == null) { collectorPage = 17; petExchangeSelection = null; return true; }
                Box(new Rect(0, 0, 1440, 900), new Color(0, 0, 0, .8f)); Panel(new Rect(310, 245, 820, 390), Gold);
                Text(350, 275, T("TRADE LEGENDARY", "装備を渡す"), heading, Gold, 740);
                Text(350, 355, selected.title, new GUIStyle(label) { wordWrap = true }, UiPaper, 720);
                Text(350, 435, T("Exchange this item for 1 pet point?", "この装備を渡してペットポイントを1獲得します。"), small, Mist, 720);
                if (WorldButton(new Rect(350, 530, 310, 65), T("TRADE", "渡す"))) { DonatePetLegendary(selected); collectorPage = 17; petExchangeSelection = null; }
                if (WorldButton(new Rect(760, 530, 310, 65), T("CANCEL", "やめる"))) { collectorPage = 17; petExchangeSelection = null; }
            }
            return true;
        }
    }
}
