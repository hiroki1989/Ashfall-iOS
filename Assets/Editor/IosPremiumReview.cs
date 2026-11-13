using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Pure boundary checks only: no player preferences or purchase transactions.
public static class IosPremiumReview
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    public static void Run()
    {
        int checks = 0;
        GameObject obj = null;
        try
        {
            Action<bool, string> check = (ok, name) => { checks++; if (!ok) throw new Exception(name); };
            var type = typeof(VaultGame);
            var allowance = type.GetMethod("FreeVersionRepaymentAllowance", Static);
            var allowsDungeon = type.GetMethod("FreeVersionAllowsDungeonLevel", Static);
            var total = type.GetMethod("DebtTotalForLevel", Instance);
            var paid = type.GetField("debtPaid", Instance);
            obj = new GameObject("IosPremiumReview");
            var game = obj.AddComponent<VaultGame>();
            int level2 = (int)total.Invoke(game, new object[] { 2 });
            int level3 = (int)total.Invoke(game, new object[] { 3 });
            Func<int, int> remaining = value => (int)allowance.Invoke(null, new object[] { value, level2 });
            check(level2 > 0 && level3 > level2, "level thresholds ordered");
            check(remaining(0) == level2 && remaining(level2 - 1) == 1, "repayment allowance before level 2");
            check(remaining(level2) == 0 && remaining(level3) == 0, "repayment allowance at and beyond level 2");
            paid.SetValue(game, level2 - 1); check(game.DebtLevel == 1, "one before level 2");
            paid.SetValue(game, level2); check(game.DebtLevel == 2, "level 2 boundary");
            paid.SetValue(game, level3); check(game.DebtLevel == 3, "editor/unlocked level remains unrestricted");
            check((bool)allowsDungeon.Invoke(null, new object[] { 2 }), "free level 2 dungeon allowed");
            check(!(bool)allowsDungeon.Invoke(null, new object[] { 3 }), "free level 3 dungeon blocked");
            Debug.Log("IOS_PREMIUM_REVIEW_PASS checks=" + checks + " (Editor cannot execute iOS-only entitlement branches)");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("IOS_PREMIUM_REVIEW_FAIL " + e);
            EditorApplication.Exit(1);
        }
        finally { if (obj != null) UnityEngine.Object.DestroyImmediate(obj); }
    }
}
