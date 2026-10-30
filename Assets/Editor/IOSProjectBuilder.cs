using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using iOSPlatformIconKind = UnityEditor.iOS.iOSPlatformIconKind;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault.Editor
{
    // Explicit actions only: opening this independent copy never switches platform.
    public static class IOSProjectBuilder
    {
        const string StartupScene = "Assets/Scenes/NeonVault.unity";
        const string BundleId = "com.neonvault.ashfall.ios";

        [MenuItem("Ashfall/iOS/Configure iOS")]
        public static void ConfigureIOS()
        {
            PlayerSettings.companyName = "NeonVault";
            PlayerSettings.productName = "Ashfall iOS";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // ARM64
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.bundleVersion = "30";
            PlayerSettings.iOS.buildNumber = "30";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            ConfigureIcons();
            // Keep a developer's signing selection on subsequent configuration runs.
            // The independent copy initially has no team or provisioning profile.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StartupScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("ASHFALL_IOS_CONFIGURED: landscape / IL2CPP ARM64 / Metal / iOS 15 / signing required on Mac.");
        }

        [MenuItem("Ashfall/iOS/Configure app icon")]
        public static void ConfigureIcons()
        {
            var kinds = new[]
            {
                iOSPlatformIconKind.Application,
                iOSPlatformIconKind.Marketing,
                iOSPlatformIconKind.Settings,
                iOSPlatformIconKind.Spotlight,
                iOSPlatformIconKind.Notification
            };
            foreach (var kind in kinds)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                for (var i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    var path = $"Assets/Art/IOS/AshfallAppIcon-{icon.width}.png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null || texture.width != icon.width || texture.height != icon.height)
                        throw new InvalidOperationException("Missing or invalid iOS icon: " + path);
                    icon.SetTexture(texture);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ASHFALL_IOS_ICONS_CONFIGURED: app icons and 1024x1024 marketing icon.");
        }

        [MenuItem("Ashfall/iOS/Build Xcode project")]
        public static void BuildXcode()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("iOS Build Support is unavailable in this Editor. Install it using Unity Hub; no platform switch was made.");
            ConfigureIOS();
            Build(BuildTarget.iOS, "Builds/iOS/Xcode");
            Debug.Log("ASHFALL_IOS_XCODE_OK: source export only; use macOS/Xcode and your Apple signing team to produce and install the app.");
        }

        [MenuItem("Ashfall/iOS/Build Xcode project", true)]
        public static bool CanBuildXcode()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS);
        }

        [MenuItem("Ashfall/iOS/Build Windows mobile preview")]
        public static void BuildMobilePreview()
        {
            ConfigureIOS();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            Build(BuildTarget.StandaloneWindows64, "Builds/MobilePreview/Ashfall-iOS-Preview.exe");
            Debug.Log("ASHFALL_MOBILE_PREVIEW_OK: Windows touch-layout preview; not an iOS app or device performance test.");
        }

        static void Build(BuildTarget target, string output)
        {
            if (!File.Exists(StartupScene)) throw new FileNotFoundException("Startup scene is missing", StartupScene);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { StartupScene }, locationPathName = output,
                target = target, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
        }
    }
}
