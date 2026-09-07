using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityHubCustom.Helpers;
using UnityHubCustom.Models;

namespace UnityHubCustom.Services
{
    public class ProjectService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnityHubCustom");
        private static readonly string CustomProjectsFile = Path.Combine(AppDataFolder, "projects.json");

        public List<UnityProject> LoadProjects(bool syncWithUnityHub = true)
        {
            var projectDict = new Dictionary<string, UnityProject>(StringComparer.OrdinalIgnoreCase);

            // 1. Load from official Unity Hub if requested
            if (syncWithUnityHub)
            {
                LoadFromUnityHub(projectDict);
            }

            // 2. Load from our custom persisted list (takes priority or merges)
            LoadFromCustomFile(projectDict);

            var list = new List<UnityProject>(projectDict.Values);

            // Verify paths and detect versions if missing
            foreach (var proj in list)
            {
                if (Directory.Exists(proj.Path))
                {
                    var detectedVersion = DetectProjectVersion(proj.Path);
                    if (!string.IsNullOrEmpty(detectedVersion))
                    {
                        proj.Version = detectedVersion;
                    }

                    if (proj.LastModified == DateTime.MinValue)
                    {
                        try
                        {
                            proj.LastModified = Directory.GetLastWriteTime(proj.Path);
                        }
                        catch { }
                    }
                }
            }

            return list;
        }

        private void LoadFromUnityHub(Dictionary<string, UnityProject> projectDict)
        {
            try
            {
                var hubProjectsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "UnityHub", "projects-v1.json");

                if (!File.Exists(hubProjectsPath)) return;

                var json = File.ReadAllText(hubProjectsPath);
                var root = JsonHelper.ParseDictionary(json);

                if (root.TryGetValue("data", out var dataObj) && dataObj is Dictionary<string, object> dataDict)
                {
                    foreach (var kvp in dataDict)
                    {
                        if (kvp.Value is Dictionary<string, object> item)
                        {
                            var path = item.ContainsKey("path") ? item["path"]?.ToString() : kvp.Key;
                            if (string.IsNullOrWhiteSpace(path)) continue;

                            var title = item.ContainsKey("title") ? item["title"]?.ToString() : Path.GetFileName(path);
                            var version = item.ContainsKey("version") ? item["version"]?.ToString() : "";
                            var lastModifiedMillis = 0L;
                            if (item.ContainsKey("lastModified") && long.TryParse(item["lastModified"]?.ToString(), out var lm))
                            {
                                lastModifiedMillis = lm;
                            }

                            DateTime lastModified = DateTime.MinValue;
                            if (lastModifiedMillis > 0)
                            {
                                try
                                {
                                    lastModified = DateTimeOffset.FromUnixTimeMilliseconds(lastModifiedMillis).LocalDateTime;
                                }
                                catch { }
                            }

                            if (Directory.Exists(path) && lastModified == DateTime.MinValue)
                            {
                                try { lastModified = Directory.GetLastWriteTime(path); } catch { }
                            }

                            var project = new UnityProject
                            {
                                Name = !string.IsNullOrEmpty(title) ? title : Path.GetFileName(path),
                                Path = path,
                                Version = version,
                                LastModified = lastModified
                            };

                            projectDict[path] = project;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading from UnityHub: " + ex.Message);
            }
        }

        private void LoadFromCustomFile(Dictionary<string, UnityProject> projectDict)
        {
            try
            {
                if (!File.Exists(CustomProjectsFile)) return;

                var json = File.ReadAllText(CustomProjectsFile);
                var list = JsonHelper.Deserialize<List<UnityProject>>(json);
                if (list != null)
                {
                    foreach (var p in list)
                    {
                        if (!string.IsNullOrWhiteSpace(p.Path))
                        {
                            projectDict[p.Path] = p;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading custom projects: " + ex.Message);
            }
        }

        public void SaveCustomProjects(IEnumerable<UnityProject> projects)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var list = new List<UnityProject>(projects);
                var json = JsonHelper.Serialize(list);
                File.WriteAllText(CustomProjectsFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving custom projects: " + ex.Message);
            }
        }

        public UnityProject AddProjectFromFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                throw new ArgumentException("유효하지 않은 디렉토리 경로입니다.");
            }

            // Check if it's a Unity project
            var projectVersionFile = Path.Combine(folderPath, "ProjectSettings", "ProjectVersion.txt");
            var assetsDir = Path.Combine(folderPath, "Assets");

            if (!File.Exists(projectVersionFile) && !Directory.Exists(assetsDir))
            {
                throw new InvalidOperationException("선택한 폴더가 유효한 Unity 프로젝트 디렉토리가 아닙니다. (Assets 또는 ProjectSettings 폴더 없음)");
            }

            var name = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var version = DetectProjectVersion(folderPath);

            return new UnityProject
            {
                Name = name,
                Path = folderPath,
                Version = version ?? "Unknown",
                LastModified = Directory.GetLastWriteTime(folderPath)
            };
        }

        public string DetectProjectVersion(string projectPath)
        {
            try
            {
                var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(versionFile))
                {
                    var lines = File.ReadAllLines(versionFile);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("m_EditorVersion:"))
                        {
                            return line.Substring("m_EditorVersion:".Length).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error detecting project version: " + ex.Message);
            }
            return null;
        }
    }
}
