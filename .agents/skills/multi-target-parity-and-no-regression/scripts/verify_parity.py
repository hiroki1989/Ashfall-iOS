import os
import re
import glob
import hashlib
import argparse
import subprocess
import tempfile

IOS_ROOT = "D:/Unity/Ashfall-iOS"
STEAM_ROOT = "D:/Unity/DustDebt-Steam"
DEMO_ROOT = "D:/Unity/DustDebt-Steam-Demo"

UNITY_EXE = "D:/Unity/Unity Hub/6000.6.0f1/Editor/Unity.exe"
DOTNET = "D:/Unity/Unity Hub/6000.6.0f1/Editor/Data/DotNetSdk/dotnet.exe"
CSC = "D:/Unity/Unity Hub/6000.6.0f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"

# Files that intentionally differ ONLY due to platform / ads / IAP / controller / demo limits
EXPECTED_DIFF_FILES = {
    "FirstStepsTutorial.cs",
    "GameCenterSyncManager.cs",
    "IosPremiumAccess.cs",
    "SaveSlotPrefs.cs",
    "VaultAutoAim.cs",
    "VaultDebtSkills.cs",
    "VaultEquipmentScreen.cs",
    "VaultGame.cs",
    "VaultHubWorld.cs",
    "VaultMobileAuxUI.cs",
    "VaultMobileInputBridge.cs",
    "VaultMobileUI.cs",
    "VaultNpcConversations.cs",
    "VaultOverworldUI.cs",
    "VaultPet.cs",
    "VaultRunSave.cs",
    "VaultShopSelling.cs",
    "VaultStorage.cs",
    "VaultTitleMenu.cs",
    "VaultUI.cs",
}

SHARED_PLAYERPREFS_ALLOWED_RAW = {
    "SaveSlotPrefs.cs",
    "VaultCameraSettings.cs",
    "VaultLanguage.cs",
    "DistinctEquipmentReview.cs",
    "EquipmentScreenReview.cs",
    "GameplayRevisionReview.cs",
    "GeneratedCommonBodyRig.cs",
    "GeneratedWardenPreviewAssets.cs",
    "ManufacturerExpansionReview.cs",
    "ProgressionBlockerReview.cs",
    "WeaponBlockerReview.cs",
    "GameCenterSyncManager.cs",
}

def normalize_without_steam_controller(text):
    text = re.sub(r'#if !UNITY_IOS && !UNITY_ANDROID\r?\n([^\n]*SteamController[^\n]*\r?\n)+#endif\r?\n?', '', text)
    return hashlib.md5(text.replace('\r\n', '\n').encode('utf-8')).hexdigest()

def md5(p):
    with open(p, "rb") as f:
        return hashlib.md5(f.read().replace(b"\r\n", b"\n")).hexdigest()

def check_steam_controller_isolation():
    failures = []
    for f in sorted(glob.glob(f"{IOS_ROOT}/Assets/Scripts/**/*.cs", recursive=True)):
        lines = open(f, "rb").read().decode("utf-8", errors="replace").splitlines()
        for i, l in enumerate(lines):
            if "SteamController" in l:
                failures.append(f"{os.path.basename(f)}:{i+1}: {l.strip()}")
    print(f"[1] SteamController references in Ashfall-iOS (must be 0 so Windows & iOS both compile): {len(failures)}")
    for x in failures:
        print("   ERROR:", x)
    return len(failures) == 0

def check_saveslot_aliases():
    ok = True
    for name, repo in [("iOS", IOS_ROOT), ("Steam", STEAM_ROOT), ("Demo", DEMO_ROOT)]:
        bad = []
        for f in sorted(glob.glob(f"{repo}/Assets/Scripts/**/*.cs", recursive=True)):
            bn = os.path.basename(f)
            if bn in SHARED_PLAYERPREFS_ALLOWED_RAW:
                continue
            txt = open(f, "rb").read().decode("utf-8", errors="ignore")
            if "PlayerPrefs." in txt and "using PlayerPrefs = NeonVault.SaveSlotPrefs;" not in txt:
                bad.append(bn)
        print(f"[2] Raw PlayerPrefs without SaveSlotPrefs alias ({name}): {bad}")
        if bad:
            ok = False
    return ok

def check_platform_exclusives():
    ok = True
    for ios_only in ["VaultDailyChest.cs", "VaultRewardedAds.cs"]:
        for name, repo in [("Steam", STEAM_ROOT), ("Demo", DEMO_ROOT)]:
            p = os.path.join(repo, "Assets/Scripts", ios_only)
            if os.path.exists(p):
                print(f"   ERROR: iOS-only file leaked into {name}: {ios_only}")
                ok = False
    for steam_only in ["SteamController.cs", "SteamControllerLiveReview.cs"]:
        p = os.path.join(IOS_ROOT, "Assets/Scripts", steam_only)
        if os.path.exists(p):
            print(f"   ERROR: Steam-only file leaked into iOS: {steam_only}")
            ok = False
    print(f"[3] Platform exclusive isolation: {'OK' if ok else 'FAIL'}")
    return ok

def check_unexpected_script_diffs():
    ios_s = f"{IOS_ROOT}/Assets/Scripts"
    steam_s = f"{STEAM_ROOT}/Assets/Scripts"
    demo_s = f"{DEMO_ROOT}/Assets/Scripts"
    ios_f = {os.path.relpath(os.path.join(r, fn), ios_s).replace("\\", "/"): os.path.join(r, fn)
             for r, _, fs in os.walk(ios_s) for fn in fs if fn.endswith(".cs")}
    steam_f = {os.path.relpath(os.path.join(r, fn), steam_s).replace("\\", "/"): os.path.join(r, fn)
               for r, _, fs in os.walk(steam_s) for fn in fs if fn.endswith(".cs")}
    demo_f = {os.path.relpath(os.path.join(r, fn), demo_s).replace("\\", "/"): os.path.join(r, fn)
              for r, _, fs in os.walk(demo_s) for fn in fs if fn.endswith(".cs")}

    unexpected = []
    for k in sorted(set(ios_f) & set(steam_f)):
        if k in EXPECTED_DIFF_FILES:
            continue
        if k in {"ActionSkillUI.cs", "VaultInventoryUI.cs"}:
            t1 = open(ios_f[k], "r", encoding="utf-8").read()
            t2 = open(steam_f[k], "r", encoding="utf-8").read()
            if normalize_without_steam_controller(t1) != normalize_without_steam_controller(t2):
                unexpected.append(("iOS vs Steam", k))
        elif md5(ios_f[k]) != md5(steam_f[k]):
            unexpected.append(("iOS vs Steam", k))
    for k in sorted(set(steam_f) & set(demo_f)):
        if k in {"IosPremiumAccess.cs", "VaultDebtSkills.cs", "VaultRunSave.cs"}:
            continue
        if md5(steam_f[k]) != md5(demo_f[k]):
            unexpected.append(("Steam vs Demo", k))

    print(f"[4] Unexpected shared script drift: {len(unexpected)}")
    for pair, k in unexpected:
        print(f"   DRIFT ({pair}): {k}")
    return len(unexpected) == 0

def compile_all():
    if not (os.path.exists(DOTNET) and os.path.exists(CSC)):
        print("[5] Skipping Roslyn compile check (Unity dotnet/csc not found).")
        return True
    ok = True
    with tempfile.TemporaryDirectory() as tmp:
        def run_rsp(repo, rsp_rel, tag, extra_defs=None, extra_ref=None):
            rsp_path = os.path.join(repo, rsp_rel)
            if not os.path.exists(rsp_path):
                print(f"   SKIP {tag}: {rsp_rel} not found")
                return None, True
            lines = open(rsp_path, "r", encoding="utf-8").read().splitlines()
            out_dll = os.path.join(tmp, f"{tag}.dll").replace("\\", "/")
            existing = set()
            new_lines = []
            for l in lines:
                if l.startswith("-out:") or l.startswith("-refout:") or l.startswith("-pdb:"):
                    continue
                if extra_ref and l.startswith('-r:"Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp'):
                    new_lines.append(f'-r:"{extra_ref}"')
                    continue
                if l.strip().endswith('.cs"'):
                    existing.add(l.strip().strip('"').replace("\\", "/"))
                new_lines.append(l)
            new_lines.append(f'-out:"{out_dll}"')
            if extra_defs:
                for d in extra_defs:
                    new_lines.append(f"-define:{d}")
            if "Editor" not in rsp_rel:
                for p in glob.glob(os.path.join(repo, "Assets/Scripts/*.cs")):
                    rel = os.path.relpath(p, repo).replace("\\", "/")
                    if rel not in existing:
                        new_lines.append(f'"{rel}"')
            tmp_rsp = os.path.join(tmp, f"{tag}.rsp")
            open(tmp_rsp, "w", encoding="utf-8").write("\n".join(new_lines))
            proc = subprocess.run([DOTNET, "exec", CSC, f"@{tmp_rsp}"], cwd=repo, capture_output=True, text=True)
            errs = [l for l in proc.stdout.splitlines() if ": error " in l]
            print(f"   [{tag}] exit={proc.returncode} errors={len(errs)}")
            for e in errs[:15]:
                print("     ", e)
            return out_dll, proc.returncode == 0

        print("[5] Roslyn compile verification (all 8 configurations):")
        ios_win_dll, o0 = run_rsp(IOS_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp", "iOS_win_runtime")
        _, o0e = run_rsp(IOS_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp", "iOS_win_editor", extra_ref=ios_win_dll)
        ios_dll, o1 = run_rsp(IOS_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp", "iOS_ios_runtime", ["UNITY_IOS"])
        _, o2 = run_rsp(IOS_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp", "iOS_ios_editor", ["UNITY_IOS"], ios_dll)
        steam_dll, o3 = run_rsp(STEAM_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp", "Steam_runtime")
        _, o4 = run_rsp(STEAM_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp", "Steam_editor", extra_ref=steam_dll)
        demo_dll, o5 = run_rsp(DEMO_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp", "Demo_runtime")
        _, o6 = run_rsp(DEMO_ROOT, "Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp", "Demo_editor", extra_ref=demo_dll)
        ok = all([o0, o0e, o1, o2, o3, o4, o5, o6])
    return ok

def check_unity_batchmode():
    if not os.path.exists(UNITY_EXE):
        print("[6] Unity.exe not found; skipping batchmode verify.")
        return True
    ok = True
    print("[6] Unity Editor live / batchmode compile verification:")
    for name, repo in [("iOS", IOS_ROOT), ("Steam", STEAM_ROOT), ("Demo", DEMO_ROOT)]:
        lock = os.path.join(repo, "Temp/UnityLockfile")
        if os.path.exists(lock):
            log_path = os.path.join(repo, "Logs/Editor.log")
            if os.path.exists(log_path):
                lines = open(log_path, "rb").read().decode("utf-8", errors="replace").splitlines()
                last_reload = max([i for i, l in enumerate(lines) if "Reloading assemblies" in l or "Begin MonoManager ReloadAssembly" in l] + [0])
                tail_errs = [l for l in lines[last_reload:] if ": error " in l or "Scripts have compiler errors" in l]
                print(f"   [{name}] Editor is OPEN (checked Editor.log after last reload): errors={len(tail_errs)}")
                for e in tail_errs[:10]:
                    print("     ", e)
                if tail_errs:
                    ok = False
            else:
                print(f"   [{name}] Editor is OPEN (no Editor.log found)")
            continue

        log_out = f"D:/Unity/verify_{name.lower()}_unity.log"
        proc = subprocess.run([UNITY_EXE, "-batchmode", "-quit", "-projectPath", repo, "-logFile", log_out])
        errs = []
        if os.path.exists(log_out):
            lines = open(log_out, "rb").read().decode("utf-8", errors="replace").splitlines()
            errs = [l for l in lines if ": error " in l or "Scripts have compiler errors" in l or "Compilation failed" in l]
        print(f"   [{name}] Unity batchmode exit={proc.returncode} errors={len(errs)} (log={log_out})")
        for e in errs[:15]:
            print("     ", e)
        if proc.returncode != 0 or errs:
            ok = False
    return ok

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity", action="store_true", help="Also run Unity.exe -batchmode -quit on closed projects and check open Editor logs")
    args = parser.parse_args()

    c1 = check_steam_controller_isolation()
    c2 = check_saveslot_aliases()
    c3 = check_platform_exclusives()
    c4 = check_unexpected_script_diffs()
    c5 = compile_all()
    c6 = check_unity_batchmode() if args.unity else True
    passed = all([c1, c2, c3, c4, c5, c6])
    print("PARITY & NO-REGRESSION VERIFICATION:", "PASS" if passed else "FAIL")
    raise SystemExit(0 if passed else 1)

if __name__ == "__main__":
    main()
