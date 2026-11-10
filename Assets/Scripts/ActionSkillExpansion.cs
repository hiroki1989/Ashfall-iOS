using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Stable node rankings: 3 classes * 3 branches * 5 tiers * 14 slots = 630 slots (array size 810 for safety)
        readonly int[] actionSkillExtraRanks = new int[810];
        const int LegacyExtraSlots = 4;
        const int ExpandedExtraSlots = 14;
        readonly Dictionary<string, float> actionSkillExtraValues = new Dictionary<string, float>();
        bool actionSkillExtraValuesDirty = true;

        static int ExtraIndex(ActionSkillKind k, int b, int t, int s) =>
            s < LegacyExtraSlots
                ? (((int)k * 3 + b) * 5 + t) * LegacyExtraSlots + s
                : 180 + (((int)k * 3 + b) * 5 + t) * (ExpandedExtraSlots - LegacyExtraSlots) + (s - LegacyExtraSlots);

        static bool ValidExtra(ActionSkillKind k, int b, int t, int s) =>
            ValidActionSkill(k, b, t) && s >= 0 && s < ExpandedExtraSlots;

        public int ActionSkillExtraRank(ActionSkillKind k, int b, int t, int s)
        {
            LoadActionSkills();
            return ValidExtra(k, b, t, s) ? actionSkillExtraRanks[ExtraIndex(k, b, t, s)] : 0;
        }

        public int ActionSkillExtraTotal(ActionSkillKind k, int b, int s)
        {
            int n = 0;
            for (int t = 0; t < 5; t++) n += ActionSkillExtraRank(k, b, t, s);
            return n;
        }

        // Geometry definition per tier (T1..T5 have distinct layouts)
        public static void GetTierGeometry(int tier, out int[] row, out float[] lane, out int[] parentMap)
        {
            switch (tier)
            {
                case 0:
                    // Tier 1: 2-Wing Bifurcated Tree (Roots & Dual Flanks)
                    row       = new int[]   { 1,    2,     2,    2,     3,     3,    3,    3,    3,    3,     4,     4,    4,    4 };
                    lane      = new float[] { 0.0f, -1.8f, 1.8f, 0.0f, -2.4f, -1.2f, 1.2f, 2.4f, -0.5f, 0.5f, -2.4f, -1.2f, 1.2f, 2.4f };
                    parentMap = new int[]   { -1,   0,     0,    0,     1,     1,    2,    2,    3,    3,     4,     5,    6,    7 };
                    break;
                case 1:
                    // Tier 2: Triple Fan-Out Tree (3 specialized pillars)
                    row       = new int[]   { 1,    2,     2,    2,     3,     3,     3,    3,    3,    3,     4,     4,    4,    4 };
                    lane      = new float[] { 0.0f, -2.0f, 0.0f, 2.0f, -2.5f, -1.5f, -0.6f, 0.6f, 1.5f, 2.5f, -2.5f, -0.6f, 0.6f, 2.5f };
                    parentMap = new int[]   { -1,   0,     0,    0,     1,     1,     2,    2,    3,    3,     4,     6,    7,    9 };
                    break;
                case 2:
                    // Tier 3: Rhombus / Diamond Synergy Cluster
                    row       = new int[]   { 1,    2,     2,    2,     3,     3,     3,    3,    3,    3,     4,     4,    4,    4 };
                    lane      = new float[] { 0.0f, -2.0f, 0.0f, 2.0f, -2.5f, -1.5f, -0.6f, 0.6f, 1.5f, 2.5f, -2.5f, -0.8f, 0.8f, 2.5f };
                    parentMap = new int[]   { -1,   0,     0,    0,     1,     1,     2,    2,    3,    3,     4,     5,    7,    9 };
                    break;
                case 3:
                    // Tier 4: Parallel Deep Specialization Columns (Ladder to Keystones)
                    row       = new int[]   { 1,    2,     2,    2,     3,     3,    3,    2,    3,    3,     4,     4,    4,    4 };
                    lane      = new float[] { 0.0f, -2.2f, -0.7f, 0.7f, -2.2f, -0.7f, 0.7f, 2.2f, 2.2f, 0.0f, -2.2f, -0.7f, 0.7f, 2.2f };
                    parentMap = new int[]   { -1,   0,     0,    0,     1,     2,    3,    0,    7,    0,     4,     5,    6,    8 };
                    break;
                default:
                    // Tier 5: Capstone Crown & Apex Constellation
                    row       = new int[]   { 1,    2,     2,    2,     3,     3,     3,    3,    3,    3,     4,     4,    4,    4 };
                    lane      = new float[] { 0.0f, -2.0f, 0.0f, 2.0f, -2.5f, -1.5f, -0.6f, 0.6f, 1.5f, 2.5f, -2.0f, -0.6f, 0.6f, 2.0f };
                    parentMap = new int[]   { -1,   0,     0,    0,     1,     1,     2,    2,    3,    3,     4,     6,    7,    9 };
                    break;
            }
        }

        public static int ActionSkillExtraMaxRank(ActionSkillKind k, int b, int t, int s)
        {
            // Keystones (1 point unlocks):
            // Tier 1 slot 10 is Emergency Deployment Protocol (Turret flat shield / Grenade surge / Medic ward)
            if (t == 0 && s == 10) return 1;
            // Tier 3 and Tier 4 keystone slots (Turret Follow, Twin Turrets, Elemental Infusion, Overclock, etc.)
            if (t == 2 && (s == 10 || s == 13)) return 1;
            if (t == 3 && (s >= 10 && s <= 13)) return 1;
            if (t == 4 && (s >= 10 && s <= 13)) return 1;

            // Deep utility / mid-tier specialization (3 points):
            if (t >= 2 && (s == 3 || s == 7 || s == 8 || s == 9)) return 3;

            // Foundational stat & combat enhancements: 5 points
            return 5;
        }

        public static int ActionSkillExtraMaxRankByIndex(int index)
        {
            if (index < 180)
            {
                int rem = index;
                int s = rem % LegacyExtraSlots; rem /= LegacyExtraSlots;
                int t = rem % 5; rem /= 5;
                int b = rem % 3; rem /= 3;
                var k = (ActionSkillKind)rem;
                return ActionSkillExtraMaxRank(k, b, t, s);
            }
            else
            {
                int rem = index - 180;
                int s = LegacyExtraSlots + (rem % (ExpandedExtraSlots - LegacyExtraSlots));
                rem /= (ExpandedExtraSlots - LegacyExtraSlots);
                int t = rem % 5; rem /= 5;
                int b = rem % 3; rem /= 3;
                var k = (ActionSkillKind)rem;
                return ActionSkillExtraMaxRank(k, b, t, s);
            }
        }

        public bool ActionSkillExtraAvailable(ActionSkillKind k, int b, int t, int s)
        {
            if (!ValidExtra(k, b, t, s) || ActionSkillAvailablePoints <= 0) return false;
            int curRank = ActionSkillExtraRank(k, b, t, s);
            int maxRank = ActionSkillExtraMaxRank(k, b, t, s);
            if (curRank >= maxRank) return false;
            if (curRank > 0) return true; // Already unlocked, can invest further up to max

            // Unlocking rank 1 requires the tier's base skill and parent slot
            if (ActionSkillRank(k, b, t) < 1) return false;

            GetTierGeometry(t, out _, out _, out int[] parentMap);
            int p = parentMap[s];
            if (p < 0) return true; // Direct child of tier base skill
            return ActionSkillExtraRank(k, b, t, p) > 0;
        }

        public bool TryUpgradeActionSkillExtra(ActionSkillKind k, int b, int t, int s)
        {
            if (!ActionSkillExtraAvailable(k, b, t, s)) return false;
            int idx = ExtraIndex(k, b, t, s);
            int cur = actionSkillExtraRanks[idx];
            int max = ActionSkillExtraMaxRank(k, b, t, s);
            if (cur >= max) return false;

            actionSkillExtraRanks[idx] = cur + 1;
            actionSkillExtraValuesDirty = true;
            SaveActionSkills();
            RecalculateArmor();
            FirstStepsSkillUpgraded();
            PlayUserSound("skill", .35f);
            return true;
        }

        public static string ExtraEffect(ActionSkillKind k, int b, int t, int s)
        {
            // Mechanic (k=0)
            if (k == ActionSkillKind.Mechanic)
            {
                if (t == 0)
                {
                    // Tier 1: Foundation & Deployment
                    if (s == 0) return "reload_speed";
                    if (s == 1) return "accuracy";
                    if (s == 2) return "turret_damage";
                    if (s == 3) return "max_health";
                    if (s == 4) return "fire_rate";
                    if (s == 5) return "handgun_damage";
                    if (s == 6) return "turret_rate";
                    if (s == 7) return "turret_range";
                    if (s == 8) return "move_speed";
                    if (s == 9) return "action_charge";
                    if (s == 10) return "turret_shield_burst"; // Option A: Emergency Barrier (+15 flat shield on deploy)
                    if (s == 11) return "damage_reduction";
                    if (s == 12) return "turret_duration";
                    return "critical_chance";
                }
                if (t == 1)
                {
                    // Tier 2: Enhanced Field Mechanics
                    if (s == 0) return "turret_damage";
                    if (s == 1) return "assault_damage";
                    if (s == 2) return "turret_rate";
                    if (s == 3) return "action_charge";
                    if (s == 4) return "critical_damage";
                    if (s == 5) return "shotgun_damage";
                    if (s == 6) return "turret_near_damage";
                    if (s == 7) return "turret_near_guard";
                    if (s == 8) return "reload_speed";
                    if (s == 9) return "sniper_damage";
                    if (s == 10) return "turret_near_reload";
                    if (s == 11) return "turret_focus";
                    if (s == 12) return "turret_hit_charge";
                    return "turret_range";
                }
                if (t == 2)
                {
                    // Tier 3: Tactical Synergies & Mobile Drone
                    if (s == 0) return "turret_duration";
                    if (s == 1) return "heavy_damage";
                    if (s == 2) return "turret_damage";
                    if (s == 3) return "pet_damage";
                    if (s == 4) return "turret_rate";
                    if (s == 5) return "electric_damage";
                    if (s == 6) return "turret_near_critical";
                    if (s == 7) return "turret_near_element";
                    if (s == 8) return "pet_speed";
                    if (s == 9) return "critical_chance";
                    if (s == 10) return "turret_follow"; // Keystone: Mobile Turret Drone
                    if (s == 11) return "damage_reduction";
                    if (s == 12) return "action_charge";
                    return "turret_element"; // Keystone: Turret Infusion
                }
                if (t == 3)
                {
                    // Tier 4: Heavy Deployment & Overclock
                    if (s == 0) return "turret_damage";
                    if (s == 1) return "turret_rate";
                    if (s == 2) return "turret_duration";
                    if (s == 3) return "turret_near_damage";
                    if (s == 4) return "critical_damage";
                    if (s == 5) return "reload_speed";
                    if (s == 6) return "fire_rate";
                    if (s == 7) return "turret_range";
                    if (s == 8) return "accuracy";
                    if (s == 9) return "move_speed";
                    if (s == 10) return "turret_double"; // Keystone: Twin Turrets (2 deployable simultaneously)
                    if (s == 11) return "turret_overclock"; // Keystone: Overclock Duration Extension
                    if (s == 12) return "turret_focus";
                    return "turret_hit_charge";
                }
                // Tier 5: Apex Capstone
                if (s == 0) return "turret_damage";
                if (s == 1) return "turret_rate";
                if (s == 2) return "critical_damage";
                if (s == 3) return "action_charge";
                if (s == 4) return "turret_duration";
                if (s == 5) return "turret_near_damage";
                if (s == 6) return "electric_damage";
                if (s == 7) return "turret_range";
                if (s == 8) return "damage_reduction";
                if (s == 9) return "max_health";
                if (s == 10) return "turret_double";
                if (s == 11) return "turret_overclock";
                if (s == 12) return "turret_follow";
                return "turret_element";
            }

            // Bomber (k=1)
            if (k == ActionSkillKind.Bomber)
            {
                if (t == 0)
                {
                    if (s == 0) return "grenade_damage";
                    if (s == 1) return "grenade_radius";
                    if (s == 2) return "reload_speed";
                    if (s == 3) return "max_health";
                    if (s == 4) return "explosive_damage";
                    if (s == 5) return "handgun_damage";
                    if (s == 6) return "shotgun_damage";
                    if (s == 7) return "move_speed";
                    if (s == 8) return "action_charge";
                    if (s == 9) return "accuracy";
                    if (s == 10) return "grenade_heal";
                    if (s == 11) return "damage_reduction";
                    if (s == 12) return "fire_rate";
                    return "critical_chance";
                }
                if (t == 1)
                {
                    if (s == 0) return "grenade_radius";
                    if (s == 1) return "grenade_damage";
                    if (s == 2) return "explosive_damage";
                    if (s == 3) return "critical_damage";
                    if (s == 4) return "assault_damage";
                    if (s == 5) return "heavy_damage";
                    if (s == 6) return "action_charge";
                    if (s == 7) return "grenade_suppress";
                    if (s == 8) return "reload_speed";
                    if (s == 9) return "move_speed";
                    if (s == 10) return "grenade_hit_heal";
                    if (s == 11) return "grenade_slow";
                    if (s == 12) return "grenade_hit_charge";
                    return "poison_damage";
                }
                if (t == 2)
                {
                    if (s == 0) return "grenade_count";
                    if (s == 1) return "grenade_damage";
                    if (s == 2) return "grenade_radius";
                    if (s == 3) return "fire_damage";
                    if (s == 4) return "critical_chance";
                    if (s == 5) return "explosive_damage";
                    if (s == 6) return "grenade_suppress";
                    if (s == 7) return "damage_reduction";
                    if (s == 8) return "sniper_damage";
                    if (s == 9) return "action_charge";
                    if (s == 10) return "grenade_element";
                    if (s == 11) return "grenade_slow";
                    if (s == 12) return "grenade_hit_charge";
                    return "grenade_chain";
                }
                if (t == 3)
                {
                    if (s == 0) return "grenade_damage";
                    if (s == 1) return "grenade_count";
                    if (s == 2) return "grenade_radius";
                    if (s == 3) return "explosive_damage";
                    if (s == 4) return "critical_damage";
                    if (s == 5) return "reload_speed";
                    if (s == 6) return "fire_rate";
                    if (s == 7) return "grenade_hit_heal";
                    if (s == 8) return "move_speed";
                    if (s == 9) return "accuracy";
                    if (s == 10) return "grenade_chain";
                    if (s == 11) return "grenade_element";
                    if (s == 12) return "grenade_count";
                    return "grenade_suppress";
                }
                // Tier 5: Apex Capstone
                if (s == 0) return "grenade_damage";
                if (s == 1) return "grenade_radius";
                if (s == 2) return "grenade_count";
                if (s == 3) return "explosive_damage";
                if (s == 4) return "critical_damage";
                if (s == 5) return "action_charge";
                if (s == 6) return "grenade_chain";
                if (s == 7) return "grenade_element";
                if (s == 8) return "damage_reduction";
                if (s == 9) return "max_health";
                if (s == 10) return "grenade_chain";
                if (s == 11) return "grenade_count";
                if (s == 12) return "grenade_suppress";
                return "grenade_hit_charge";
            }

            // Medic (k=2)
            if (t == 0)
            {
                if (s == 0) return "medic_heal";
                if (s == 1) return "max_health";
                if (s == 2) return "medic_radius";
                if (s == 3) return "damage_reduction";
                if (s == 4) return "move_speed";
                if (s == 5) return "reload_speed";
                if (s == 6) return "handgun_damage";
                if (s == 7) return "medic_duration";
                if (s == 8) return "action_charge";
                if (s == 9) return "accuracy";
                if (s == 10) return "medic_guard";
                if (s == 11) return "fire_rate";
                if (s == 12) return "medic_field_damage";
                return "critical_chance";
            }
            if (t == 1)
            {
                if (s == 0) return "medic_radius";
                if (s == 1) return "medic_heal";
                if (s == 2) return "medic_duration";
                if (s == 3) return "damage_reduction";
                if (s == 4) return "max_health";
                if (s == 5) return "medic_field_reload";
                if (s == 6) return "medic_field_damage";
                if (s == 7) return "action_charge";
                if (s == 8) return "move_speed";
                if (s == 9) return "shotgun_damage";
                if (s == 10) return "medic_guard";
                if (s == 11) return "medic_damage";
                if (s == 12) return "medic_field_charge";
                return "pet_speed";
            }
            if (t == 2)
            {
                if (s == 0) return "medic_duration";
                if (s == 1) return "medic_heal";
                if (s == 2) return "medic_radius";
                if (s == 3) return "medic_damage";
                if (s == 4) return "critical_damage";
                if (s == 5) return "assault_damage";
                if (s == 6) return "medic_field_damage";
                if (s == 7) return "medic_field_reload";
                if (s == 8) return "poison_damage";
                if (s == 9) return "action_charge";
                if (s == 10) return "medic_follow"; // Keystone: Mobile Healing Drone
                if (s == 11) return "medic_debuff";
                if (s == 12) return "medic_guard";
                return "medic_purge";
            }
            if (t == 3)
            {
                if (s == 0) return "medic_heal";
                if (s == 1) return "medic_duration";
                if (s == 2) return "medic_radius";
                if (s == 3) return "damage_reduction";
                if (s == 4) return "max_health";
                if (s == 5) return "medic_field_charge";
                if (s == 6) return "medic_damage";
                if (s == 7) return "critical_chance";
                if (s == 8) return "reload_speed";
                if (s == 9) return "move_speed";
                if (s == 10) return "medic_purge";
                if (s == 11) return "medic_follow";
                if (s == 12) return "medic_debuff";
                return "medic_guard";
            }
            // Tier 5: Apex Capstone
            if (s == 0) return "medic_heal";
            if (s == 1) return "medic_duration";
            if (s == 2) return "medic_radius";
            if (s == 3) return "max_health";
            if (s == 4) return "damage_reduction";
            if (s == 5) return "medic_field_damage";
            if (s == 6) return "medic_purge";
            if (s == 7) return "medic_debuff";
            if (s == 8) return "action_charge";
            if (s == 9) return "critical_damage";
            if (s == 10) return "medic_purge";
            if (s == 11) return "medic_follow";
            if (s == 12) return "medic_debuff";
            return "medic_field_charge";
        }

        static float ExtraBaseAmount(string id, int tier)
        {
            switch (id)
            {
                // Keystones
                case "turret_shield_burst": return 15f; // Flat +15 ArtifactShield
                case "turret_follow":
                case "medic_follow":
                case "turret_double":
                case "turret_element":
                case "grenade_element":
                case "turret_overclock":
                case "grenade_chain":
                case "medic_purge":
                case "medic_debuff":
                    return 1f;

                // Tactical & Utility
                case "turret_focus":
                case "medic_guard":
                case "turret_near_critical":
                    return 0.05f;
                case "turret_near_guard":
                    return 0.06f;
                case "turret_near_damage":
                case "medic_field_damage":
                case "turret_hit_charge":
                    return 0.10f;
                case "turret_near_reload":
                case "medic_field_reload":
                case "grenade_hit_charge":
                    return 0.15f;
                case "turret_near_element":
                case "medic_field_charge":
                    return 0.12f;
                case "medic_damage":
                    return 0.02f;
                case "grenade_heal":
                    return 0.01f;
                case "grenade_hit_heal":
                    return 0.005f;
                case "grenade_suppress":
                    return 0.25f;
                case "grenade_slow":
                    return 0.50f;
                case "grenade_count":
                    return 1f;

                // Growth stats: deeper tiers get higher base values
                case "turret_damage":
                case "grenade_damage":
                case "medic_heal":
                    return 0.035f + 0.010f * tier;

                case "turret_range":
                case "medic_radius":
                case "grenade_radius":
                    return 0.25f + 0.05f * tier;

                case "turret_duration":
                case "medic_duration":
                    return 0.50f + 0.15f * tier;

                case "turret_rate":
                case "fire_rate":
                case "reload_speed":
                case "accuracy":
                case "move_speed":
                case "action_charge":
                case "critical_chance":
                case "critical_damage":
                case "damage_reduction":
                    return 0.020f + 0.005f * tier;

                case "max_health":
                    return 0.015f + 0.003f * tier;

                case "pet_damage":
                    return 0.25f;
                case "pet_speed":
                    return 0.20f;

                default:
                    // Weapon specific damage
                    return 0.025f + 0.005f * tier;
            }
        }

        public float ActionSkillExtraValue(string effectId)
        {
            LoadActionSkills();
            if (actionSkillExtraValuesDirty)
            {
                actionSkillExtraValues.Clear();
                for (int k = 0; k < 3; k++)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        for (int t = 0; t < 5; t++)
                        {
                            for (int slot = 0; slot < ExpandedExtraSlots; slot++)
                            {
                                int rank = actionSkillExtraRanks[ExtraIndex((ActionSkillKind)k, b, t, slot)];
                                if (rank <= 0) continue;
                                string id = ExtraEffect((ActionSkillKind)k, b, t, slot);
                                float unit = ExtraBaseAmount(id, t);
                                actionSkillExtraValues.TryGetValue(id, out float current);
                                actionSkillExtraValues[id] = current + unit * rank;
                            }
                        }
                    }
                }
                actionSkillExtraValuesDirty = false;
            }
            return effectId != null && actionSkillExtraValues.TryGetValue(effectId, out float value) ? value : 0f;
        }

        static string ExtraLabel(string id, bool ja)
        {
            switch (id)
            {
                case "turret_shield_burst": return ja ? "緊急防壁展開" : "Emergency Barrier";
                case "turret_overclock": return ja ? "限界解除" : "Overclock";
                case "grenade_chain": return ja ? "連鎖起爆" : "Chain Reaction";
                case "medic_purge": return ja ? "浄化フィールド" : "Purge Field";
                case "pet_damage": return ja ? "相棒の噛みつき威力" : "Companion bite damage";
                case "pet_speed": return ja ? "相棒の攻撃速度" : "Companion attack speed";
                case "turret_near_damage": return ja ? "防壁射撃援護" : "Turret cover";
                case "turret_near_reload": return ja ? "防壁補給支援" : "Supply cover";
                case "turret_near_critical": return ja ? "防壁照準支援" : "Targeting cover";
                case "turret_near_guard": return ja ? "防壁防護支援" : "Defensive cover";
                case "turret_hit_charge": return ja ? "連動蓄電" : "Linked charge";
                case "turret_near_element": return ja ? "属性増幅支援" : "Elemental cover";
                case "grenade_hit_heal": return ja ? "爆風活性化" : "Blast recovery";
                case "grenade_hit_charge": return ja ? "爆破蓄電" : "Blast charge";
                case "grenade_slow": return ja ? "衝撃波停滞" : "Concussive delay";
                case "medic_field_damage": return ja ? "治療域援護射撃" : "Field fire";
                case "medic_field_reload": return ja ? "治療域補給支援" : "Field supply";
                case "medic_field_charge": return ja ? "治療域蓄電支援" : "Field charge";
                case "turret_damage": return ja ? "タレット威力" : "Turret damage";
                case "turret_rate": return ja ? "タレット連射速度" : "Turret fire rate";
                case "turret_range": return ja ? "タレット射程" : "Turret range";
                case "turret_duration": return ja ? "タレット稼働時間" : "Turret duration";
                case "turret_focus": return ja ? "集中連射" : "Focused fire";
                case "turret_follow": return ja ? "追従ドローン化" : "Mobile turret";
                case "grenade_suppress": return ja ? "衝撃波足止め" : "Concussive blast";
                case "grenade_heal": return ja ? "戦闘吸血" : "Combat recovery";
                case "medic_damage": return ja ? "侵食フィールド" : "Hostile field";
                case "medic_guard": return ja ? "治療域防護" : "Field protection";
                case "medic_follow": return ja ? "移動診療ドローン" : "Mobile clinic";
                case "turret_double": return ja ? "ツインタレット" : "Twin turrets";
                case "turret_element": return ja ? "属性弾装填" : "Turret infusion";
                case "grenade_damage": return ja ? "グレネード威力" : "Grenade damage";
                case "grenade_radius": return ja ? "爆発半径" : "Blast radius";
                case "grenade_count": return ja ? "追加投擲数" : "Extra grenade";
                case "grenade_element": return ja ? "属性グレネード" : "Grenade infusion";
                case "medic_heal": return ja ? "回復レート" : "Healing rate";
                case "medic_radius": return ja ? "回復フィールド半径" : "Healing radius";
                case "medic_duration": return ja ? "フィールド展開時間" : "Field duration";
                case "medic_debuff": return ja ? "神経抑制域" : "Suppressive field";
                case "shotgun_damage": return ja ? "ショットガン威力" : "Shotgun damage";
                case "assault_damage": return ja ? "アサルトライフル威力" : "Assault rifle damage";
                case "heavy_damage": return ja ? "重機関銃威力" : "Heavy machine gun damage";
                case "accuracy": return ja ? "命中精度" : "Accuracy";
                case "fire_rate": return ja ? "連射速度" : "Fire rate";
                case "reload_speed": return ja ? "リロード速度" : "Reload speed";
                case "max_health": return ja ? "最大HP" : "Maximum HP";
                case "move_speed": return ja ? "移動速度" : "Movement speed";
                case "critical_chance": return ja ? "会心率" : "Critical chance";
                case "critical_damage": return ja ? "会心威力" : "Critical damage";
                case "damage_reduction": return ja ? "被ダメージ軽減" : "Damage reduction";
                case "action_charge": return ja ? "スキル蓄電効率" : "Action charge rate";
                case "handgun_damage": return ja ? "ハンドガン威力" : "Handgun damage";
                case "sniper_damage": return ja ? "スナイパー威力" : "Sniper damage";
                case "fire_damage": return ja ? "火炎威力" : "Fire damage";
                case "electric_damage": return ja ? "電撃威力" : "Shock damage";
                case "poison_damage": return ja ? "毒素威力" : "Poison damage";
                case "explosive_damage": return ja ? "爆発威力" : "Explosive damage";
                default: return id;
            }
        }

        public string ActionSkillExtraTitle(ActionSkillKind k, int b, int t, int s)
        {
            if (!ValidExtra(k, b, t, s)) return "";
            string id = ExtraEffect(k, b, t, s);
            string[] levels = { "I", "II", "III", "IV", "V" };
            return ExtraLabel(id, VaultLanguage.Japanese) + " " + levels[t];
        }

        public string ActionSkillExtraDescription(ActionSkillKind k, int b, int t, int s)
        {
            if (!ValidExtra(k, b, t, s)) return "";
            string id = ExtraEffect(k, b, t, s);
            bool ja = VaultLanguage.Japanese;
            int max = ActionSkillExtraMaxRank(k, b, t, s);
            float unit = ExtraBaseAmount(id, t);

            // Special Keystones
            if (id == "turret_shield_burst")
            {
                return ja
                    ? "タレット展開時、シールドモジュールの有無や現在値にかかわらず、即座に固定シールド " + unit.ToString("0") + " を獲得する。"
                    : "Deploying a turret immediately grants " + unit.ToString("0") + " flat shield, regardless of shield modules equipped.";
            }
            if (id == "turret_follow")
            {
                return ja
                    ? "タレットが空中ドローンとして浮遊し、プレイヤーに追従して移動しながら射撃を行う。"
                    : "The turret becomes a floating drone that follows the player while firing.";
            }
            if (id == "turret_double")
            {
                return ja
                    ? "タレットを同時に2機展開可能になる。2機目は1機目の70%の威力で射撃を行う。"
                    : "Deploy two turrets simultaneously; the second deals 70% of the first turret's damage.";
            }
            if (id == "turret_overclock")
            {
                return ja
                    ? "タレット稼働中、銃弾の命中ごとにタレットの残り持続時間が0.08秒回復する（1秒に1回まで発動）。"
                    : "Bullet hits restore 0.08s of turret duration while active (once per second).";
            }
            if (id == "turret_element")
            {
                return ja
                    ? "タレットの射撃が、現在装備中の銃の属性効果を完全に継承する。"
                    : "Turret shots inherit the element of your equipped firearm.";
            }
            if (id == "grenade_chain")
            {
                return ja
                    ? "グレネード着弾時、周囲の敵へ連鎖する副次爆発が1回発生し、基本威力の20%のダメージを与える。"
                    : "Grenade hits trigger a secondary chain blast for 20% base damage.";
            }
            if (id == "medic_purge")
            {
                return ja
                    ? "回復フィールド内で敵を撃破すると、最大HPの2%が即座に回復する（1秒に1回まで発動）。"
                    : "Defeating enemies inside the healing field restores 2% max HP (once per second).";
            }
            if (id == "medic_follow")
            {
                return ja
                    ? "回復フィールド発生器がプレイヤーに追従し、移動中も常に回復とバフを受けられる。"
                    : "The healing field emitter follows the player wherever you move.";
            }
            if (id == "medic_debuff")
            {
                return ja
                    ? "回復フィールド内の敵の攻撃力と移動速度を抑制し、被ダメージを軽減する。"
                    : "Suppresses attack power and movement speed of enemies inside the field.";
            }
            if (id == "turret_focus")
            {
                return ja
                    ? "同一ターゲットへの連続射撃ごとにタレット威力 +" + (unit * 100).ToString("0") + "%（最大5回蓄積）。"
                    : "Each consecutive hit on the same target adds +" + (unit * 100).ToString("0") + "% damage (up to 5x).";
            }
            if (id == "medic_damage")
            {
                return ja
                    ? "回復フィールド内の敵に毎秒装備武器DPSの" + (unit * 100).ToString("0") + "%の侵食ダメージを与える。"
                    : "Enemies in the field take damage per second equal to " + (unit * 100).ToString("0") + "% weapon DPS.";
            }
            if (id == "medic_guard")
            {
                return ja
                    ? "回復フィールド内で受けるダメージを " + (unit * 100).ToString("0") + "% 軽減する。"
                    : "Reduces damage taken inside the healing field by " + (unit * 100).ToString("0") + "%.";
            }
            if (id == "grenade_heal")
            {
                return ja
                    ? "グレネード命中時、敵1体ごとに最大HPの1%を即座に回復する（1投あたり最大5体）。"
                    : "Restore 1% max HP per enemy hit by grenade (up to 5 targets).";
            }
            if (id == "grenade_suppress")
            {
                return ja
                    ? "グレネード爆風を受けた敵の次回攻撃を0.25秒遅延させる。"
                    : "Grenade blasts delay enemy attacks by 0.25 seconds.";
            }
            if (id == "grenade_slow")
            {
                return ja
                    ? "グレネード爆風を受けた敵の移動と次回攻撃を0.5秒大幅に遅延させる。"
                    : "Blasts delay enemy actions and movement by 0.5 seconds.";
            }

            // Stat scaling nodes (with per-rank increment info)
            string suffix = id == "grenade_count" ? (ja ? "個" : "")
                : (id == "turret_range" || id == "medic_radius" || id == "grenade_radius") ? "m"
                : (id == "turret_duration" || id == "medic_duration") ? (ja ? "秒" : "s")
                : "%";
            float displayedUnit = suffix == "%" ? unit * 100f : unit;
            float displayedMax = displayedUnit * max;

            string line = ExtraLabel(id, ja) + " +" + displayedUnit.ToString("0.#") + suffix +
                (max > 1 ? (ja ? "（最大 +" + displayedMax.ToString("0.#") + suffix + " / " + max + "段階）" : " (Max +" + displayedMax.ToString("0.#") + suffix + " / " + max + " ranks)") : "");

            return line;
        }
    }
}
