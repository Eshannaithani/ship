// ShipAudit.cs
// Installed by the ship skill into Assets/Editor/GameTools/. Editor only.
// Reads player settings and scans scene and prefab YAML without loading anything, so the audit never
// touches the open scene. From a Unity MCP RunCommand:
//   result.Log(GameTools.ShipAudit.Run("Builds/ship_audit.json", lastIosBuildNumber, lastAndroidVersionCode));
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GameTools
{
    [Serializable] public class AuditItem { public string check = ""; public string status = ""; public string detail = ""; }

    [Serializable]
    public class AuditResult
    {
        public string product = "", company = "", version = "", iosBuildNumber = "";
        public int androidVersionCode;
        public string bundleIdIos = "", bundleIdAndroid = "";
        public bool developmentBuild;
        public string orientation = "";
        public string androidBackend = "", androidStripping = "", androidArchitectures = "", androidTargetSdk = "", androidMinSdk = "";
        public bool androidMinifyRelease;
        public string iosTargetOs = "", iosBackend = "", iosStripping = "";
        public string[] scenesInBuild = new string[0];
        public int canvasScalerVariants;
        public string canvasScalerDetail = "";
        public bool safeAreaObjectFound, safeAreaScriptFound;
        public int iosIconCount, androidIconCount;
        public List<AuditItem> items = new List<AuditItem>();
        public int fails, warns;
    }

    public static class ShipAudit
    {
        public static string Run(string outJsonPath, int lastIosBuildNumber, int lastAndroidVersionCode)
        {
            var r = new AuditResult();
            r.product = PlayerSettings.productName ?? "";
            r.company = PlayerSettings.companyName ?? "";
            r.version = PlayerSettings.bundleVersion ?? "";
            r.iosBuildNumber = PlayerSettings.iOS.buildNumber ?? "";
            r.androidVersionCode = PlayerSettings.Android.bundleVersionCode;
            r.bundleIdIos = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) ?? "";
            r.bundleIdAndroid = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) ?? "";
            r.developmentBuild = EditorUserBuildSettings.development;
            r.orientation = PlayerSettings.defaultInterfaceOrientation.ToString();
            r.androidBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString();
            r.androidStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android).ToString();
            r.androidArchitectures = PlayerSettings.Android.targetArchitectures.ToString();
            r.androidMinifyRelease = PlayerSettings.Android.minifyRelease;
            r.androidTargetSdk = PlayerSettings.Android.targetSdkVersion.ToString();
            r.androidMinSdk = PlayerSettings.Android.minSdkVersion.ToString();
            r.iosTargetOs = PlayerSettings.iOS.targetOSVersionString ?? "";
            r.iosBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS).ToString();
            r.iosStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.iOS).ToString();

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            r.scenesInBuild = scenes.ToArray();
            try
            {
                r.iosIconCount = PlayerSettings.GetIcons(NamedBuildTarget.iOS, IconKind.Application).Length;
                r.androidIconCount = PlayerSettings.GetIcons(NamedBuildTarget.Android, IconKind.Application).Length;
            }
            catch (Exception) { }

            ScanCanvasScalers(r);
            ScanSafeArea(r);

            Add(r, "Scenes in build", r.scenesInBuild.Length > 0 ? "pass" : "fail", r.scenesInBuild.Length + " enabled");
            Add(r, "Development build flag off", r.developmentBuild ? "fail" : "pass", "development=" + r.developmentBuild);
            Add(r, "Company and product set", (r.company == "DefaultCompany" || r.company.Length == 0 || r.product.Length == 0) ? "fail" : "pass", r.company + " / " + r.product);
            bool badBundle = r.bundleIdIos.Length == 0 || r.bundleIdAndroid.Length == 0 || r.bundleIdIos.Contains("DefaultCompany") || r.bundleIdAndroid.Contains("DefaultCompany");
            Add(r, "Bundle ids set", badBundle ? "fail" : "pass", r.bundleIdIos + " | " + r.bundleIdAndroid);
            Add(r, "Version is semver", Regex.IsMatch(r.version, @"^\d+\.\d+\.\d+$") ? "pass" : "warn", r.version);
            int iosNumber; int.TryParse(r.iosBuildNumber, out iosNumber);
            Add(r, "iOS build number bumped", iosNumber > lastIosBuildNumber ? "pass" : "fail", r.iosBuildNumber + " vs last shipped " + lastIosBuildNumber);
            Add(r, "Android version code bumped", r.androidVersionCode > lastAndroidVersionCode ? "pass" : "fail", r.androidVersionCode + " vs last shipped " + lastAndroidVersionCode);
            Add(r, "Android IL2CPP", r.androidBackend == "IL2CPP" ? "pass" : "fail", r.androidBackend);
            Add(r, "Android ARM64", r.androidArchitectures.Contains("ARM64") ? "pass" : "fail", r.androidArchitectures);
            Add(r, "Android minify release", r.androidMinifyRelease ? "pass" : "warn", "minifyRelease=" + r.androidMinifyRelease + "; then grep usage.txt and mapping.txt");
            Add(r, "Managed stripping enabled", (r.androidStripping == "Disabled" || r.iosStripping == "Disabled") ? "warn" : "pass", "android " + r.androidStripping + ", ios " + r.iosStripping);
            Add(r, "Orientation decided", r.orientation == "AutoRotation" ? "warn" : "pass", r.orientation);
            Add(r, "One CanvasScaler configuration", r.canvasScalerVariants <= 1 ? "pass" : "fail", r.canvasScalerDetail);
            Add(r, "SafeArea present", (r.safeAreaObjectFound || r.safeAreaScriptFound) ? "pass" : "warn", "object=" + r.safeAreaObjectFound + " script=" + r.safeAreaScriptFound);
            Add(r, "App icons set", (r.iosIconCount > 0 && r.androidIconCount > 0) ? "pass" : "warn", "ios " + r.iosIconCount + ", android " + r.androidIconCount);

            foreach (AuditItem i in r.items) { if (i.status == "fail") r.fails++; else if (i.status == "warn") r.warns++; }

            if (!string.IsNullOrEmpty(outJsonPath))
            {
                string dir = Path.GetDirectoryName(outJsonPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(outJsonPath, JsonUtility.ToJson(r, true));
            }
            var sb = new StringBuilder();
            sb.AppendFormat("{0} fails, {1} warns | {2} {3} build ios {4} / android {5}\n", r.fails, r.warns, r.product, r.version, r.iosBuildNumber, r.androidVersionCode);
            foreach (AuditItem i in r.items) sb.AppendFormat("{0}: {1} ({2})\n", i.status.ToUpperInvariant(), i.check, i.detail);
            return sb.ToString();
        }

        static void Add(AuditResult r, string check, string status, string detail)
        {
            r.items.Add(new AuditItem { check = check, status = status, detail = detail });
        }

        static void ScanCanvasScalers(AuditResult r)
        {
            var variants = new Dictionary<string, List<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                string text;
                try { text = File.ReadAllText(path); } catch (Exception) { continue; }
                int idx = 0;
                while ((idx = text.IndexOf("m_UiScaleMode:", idx, StringComparison.Ordinal)) >= 0)
                {
                    string block = text.Substring(idx, Math.Min(600, text.Length - idx));
                    string sig = "mode=" + Grab(block, @"m_UiScaleMode:\s*(\d+)")
                        + " ref=" + Grab(block, @"m_ReferenceResolution:\s*\{x:\s*([\d.]+),\s*y:\s*([\d.]+)\}", true)
                        + " match=" + Grab(block, @"m_ScreenMatchMode:\s*(\d+)")
                        + " mwh=" + Grab(block, @"m_MatchWidthOrHeight:\s*([\d.]+)");
                    if (!variants.ContainsKey(sig)) variants[sig] = new List<string>();
                    if (!variants[sig].Contains(path)) variants[sig].Add(path);
                    idx += 14;
                }
            }
            r.canvasScalerVariants = variants.Count;
            if (variants.Count == 0) { r.canvasScalerDetail = "no CanvasScaler found in scenes or prefabs"; return; }
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> kv in variants) sb.Append(kv.Key).Append(" in ").Append(string.Join(", ", kv.Value.ToArray())).Append("; ");
            r.canvasScalerDetail = sb.ToString();
        }

        static string Grab(string block, string pattern, bool pair = false)
        {
            Match m = Regex.Match(block, pattern);
            if (!m.Success) return "?";
            return pair ? m.Groups[1].Value + "x" + m.Groups[2].Value : m.Groups[1].Value;
        }

        static void ScanSafeArea(AuditResult r)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                try { if (File.ReadAllText(path).Contains("m_Name: SafeArea")) { r.safeAreaObjectFound = true; break; } } catch (Exception) { }
            }
            r.safeAreaScriptFound = AssetDatabase.FindAssets("SafeAreaFitter t:Script").Length > 0;
        }
    }
}
