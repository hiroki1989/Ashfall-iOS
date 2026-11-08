using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class SkillTreeVisualReviewEditor
{
    public static void Run()
    {
        string logPath = "D:/Unity/BuildTools/skill_tree_review.log";
        using (var sw = new StreamWriter(logPath, false))
        {
            sw.WriteLine("=== Borderlands Skill Tree Review ===");

            var icons = Resources.Load<Texture2D>("Art/UI/skill_tree_icons");
            if (icons == null) throw new Exception("Art/UI/skill_tree_icons failed to load");
            sw.WriteLine($"skill_tree_icons loaded: {icons.width}x{icons.height}");

            var frames = Resources.Load<Texture2D>("Art/UI/skill_node_frames");
            if (frames == null) throw new Exception("Art/UI/skill_node_frames failed to load");
            sw.WriteLine($"skill_node_frames loaded: {frames.width}x{frames.height}");

            var holder = new GameObject("ReviewGameHolder");
            holder.SetActive(false);
            var game = holder.AddComponent<VaultGame>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            typeof(VaultGame).GetField("smoke", flags).SetValue(game, true);
            typeof(VaultGame).GetField("actionSkillLoaded", flags).SetValue(game, true);

            // Test ActionSkill icon mapping for all kinds/branches/tiers/slots
            var getActionIcon = typeof(VaultGame).GetMethod("GetActionNodeIconIndex", flags);
            for (int k = 0; k < 3; k++)
            {
                for (int b = 0; b < 3; b++)
                {
                    for (int t = 0; t < 5; t++)
                    {
                        for (int s = -1; s < 4; s++)
                        {
                            int idx = (int)getActionIcon.Invoke(game, new object[] { (ActionSkillKind)k, b, t, s });
                            if (idx < 0 || idx >= 32) throw new Exception($"Invalid action icon index {idx}");
                        }
                    }
                }
            }
            sw.WriteLine("All 225 Action Skill nodes mapped to valid custom icons!");

            // Test PetSkill icon mapping
            var getPetIcon = typeof(VaultGame).GetMethod("GetPetNodeIconIndex", flags);
            for (int b = 0; b < 3; b++)
            {
                for (int n = 0; n < 5; n++)
                {
                    int idx = (int)getPetIcon.Invoke(game, new object[] { b, n });
                    if (idx < 0 || idx >= 32) throw new Exception($"Invalid pet icon index {idx}");
                }
            }
            sw.WriteLine("All 15 Pet Skill nodes mapped to valid custom icons!");

            // Test scroll visibility helper
            var ensureScroll = typeof(VaultGame).GetMethod("EnsureActionNodeScrollVisible", flags);
            ensureScroll.Invoke(game, new object[] { 4, 3 });
            Vector2 scrollAfter = (Vector2)typeof(VaultGame).GetField("actionTreeScroll", flags).GetValue(game);
            sw.WriteLine($"Scroll offset when selecting Tier 5 Convergence: {scrollAfter.y}");
            if (scrollAfter.y <= 0) throw new Exception("Scroll did not advance to Tier 5 node");

            UnityEngine.Object.DestroyImmediate(holder);
            sw.WriteLine("ALL SKILL TREE CHECKS PASSED!");
        }
        Debug.Log("SkillTreeVisualReviewEditor completed successfully.");
    }
}
