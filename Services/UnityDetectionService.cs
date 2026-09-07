using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityHubCustom.Models;

namespace UnityHubCustom.Services
{
    public class UnityDetectionService
    {
        private readonly List<string> _customEditorPaths = new List<string>();

        public void SetCustomPaths(IEnumerable<string> paths)
        {
            _customEditorPaths.Clear();
            if (paths != null)
            {
                _customEditorPaths.AddRange(paths);
            }
        }

        public List<UnityEditorInstall> DetectInstalledEditors()
        {
            var result = new List<UnityEditorInstall>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Check Unity Hub standard Editor locations
            var standardHubPaths = new List<string>
            {
                @"C:\Program Files\Unity\Hub\Editor",
                @"D:\Program Files\Unity\Hub\Editor",
                @"E:\Program Files\Unity\Hub\Editor"
            };

            foreach (var hubDir in standardHubPaths)
            {
                if (Directory.Exists(hubDir))
                {
                    try
                    {
                        var versionDirs = Directory.GetDirectories(hubDir);
                        foreach (var vDir in versionDirs)
                        {
                            var unityExe = Path.Combine(vDir, "Editor", "Unity.exe");
                            if (File.Exists(unityExe) && seenPaths.Add(unityExe))
                            {
                                var folderVersion = Path.GetFileName(vDir);
                                var fileVersion = GetVersionFromFile(unityExe) ?? folderVersion;
                                result.Add(new UnityEditorInstall
                                {
                                    Version = !string.IsNullOrEmpty(folderVersion) ? folderVersion : fileVersion,
                                    ExecutablePath = unityExe,
                                    IsCustom = false
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error scanning {hubDir}: {ex.Message}");
                    }
                }
            }

            // 2. Check legacy standalone Unity locations
            var legacyPaths = new List<string>
            {
                @"C:\Program Files\Unity\Editor\Unity.exe",
                @"C:\Program Files (x86)\Unity\Editor\Unity.exe"
            };

            foreach (var legacyExe in legacyPaths)
            {
                if (File.Exists(legacyExe) && seenPaths.Add(legacyExe))
                {
                    var version = GetVersionFromFile(legacyExe) ?? "Legacy";
                    result.Add(new UnityEditorInstall
                    {
                        Version = version,
                        ExecutablePath = legacyExe,
                        IsCustom = false
                    });
                }
            }

            // 3. Check custom user paths
            foreach (var customPath in _customEditorPaths)
            {
                if (File.Exists(customPath) && seenPaths.Add(customPath))
                {
                    var version = GetVersionFromFile(customPath) ?? Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(customPath))) ?? "Custom";
                    result.Add(new UnityEditorInstall
                    {
                        Version = version,
                        ExecutablePath = customPath,
                        IsCustom = true
                    });
                }
            }

            // Sort versions descending (newest first)
            return result.OrderByDescending(e => e.Version).ToList();
        }

        public UnityEditorInstall FindBestMatchingEditor(string targetVersion, List<UnityEditorInstall> installedEditors)
        {
            if (installedEditors == null || installedEditors.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(targetVersion)) return installedEditors.FirstOrDefault();

            var trimmedTarget = targetVersion.Trim();

            // 1. Exact match
            var exact = installedEditors.FirstOrDefault(e => string.Equals(e.Version, trimmedTarget, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2. Match without patch release letter/digit (e.g. 2023.1.22 vs 2023.1.22f1)
            var baseVersion = trimmedTarget.Split('f', 'b', 'a', 'p', 'c')[0];
            var baseMatch = installedEditors.FirstOrDefault(e => e.Version.StartsWith(baseVersion, StringComparison.OrdinalIgnoreCase));
            if (baseMatch != null) return baseMatch;

            // 3. Match major.minor (e.g. 2023.1)
            var parts = trimmedTarget.Split('.');
            if (parts.Length >= 2)
            {
                var minorPrefix = $"{parts[0]}.{parts[1]}.";
                var minorMatch = installedEditors.FirstOrDefault(e => e.Version.StartsWith(minorPrefix, StringComparison.OrdinalIgnoreCase));
                if (minorMatch != null) return minorMatch;
            }

            // 4. Match major (e.g. 2023)
            if (parts.Length >= 1)
            {
                var majorPrefix = $"{parts[0]}.";
                var majorMatch = installedEditors.FirstOrDefault(e => e.Version.StartsWith(majorPrefix, StringComparison.OrdinalIgnoreCase));
                if (majorMatch != null) return majorMatch;
            }

            // Fallback: first available
            return installedEditors.FirstOrDefault();
        }

        private string GetVersionFromFile(string exePath)
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(exePath);
                return fvi.FileVersion?.Trim();
            }
            catch
            {
                return null;
            }
        }
    }
}
