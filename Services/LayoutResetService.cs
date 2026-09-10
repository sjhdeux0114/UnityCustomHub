using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace UnityHubCustom.Services
{
    public class ResetResult
    {
        public bool Success { get; set; } = true;
        public int ProjectLayoutFilesDeleted { get; set; }
        public int EditorLayoutFilesDeleted { get; set; }
        public int RegistryValuesDeleted { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();

        public string SummaryText =>
            $"초기화 완료 (프로젝트 파일: {ProjectLayoutFilesDeleted}개, 에디터 캐시: {EditorLayoutFilesDeleted}개, 레지스트리: {RegistryValuesDeleted}개 항목 정리됨)";
    }

    public class LayoutResetService
    {
        public ResetResult ResetLayouts(string projectPath, bool resetProject = true, bool resetEditor = true, bool resetRegistry = true)
        {
            var result = new ResetResult();

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                result.Success = false;
                result.Errors.Add("프로젝트 경로가 유효하지 않습니다.");
                return result;
            }

            // 1. Reset Project-specific Layouts
            if (resetProject)
            {
                ResetProjectLayout(projectPath, result);
            }

            // 2. Reset Unity Editor Global Layout Cache
            if (resetEditor)
            {
                ResetGlobalEditorLayout(result);
            }

            // 3. Reset Windows Registry Layout Entries
            if (resetRegistry)
            {
                ResetRegistryLayoutEntries(result);
            }

            return result;
        }

        private void ResetProjectLayout(string projectPath, ResetResult result)
        {
            try
            {
                // A. UserSettings/Layouts
                var userSettingsLayouts = Path.Combine(projectPath, "UserSettings", "Layouts");
                if (Directory.Exists(userSettingsLayouts))
                {
                    var dwltFiles = Directory.GetFiles(userSettingsLayouts, "*.*", SearchOption.AllDirectories);
                    foreach (var file in dwltFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            result.ProjectLayoutFilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"프로젝트 레이아웃 파일 삭제 실패 ({Path.GetFileName(file)}): {ex.Message}");
                        }
                    }

                    // Attempt to delete empty layouts folder
                    try
                    {
                        Directory.Delete(userSettingsLayouts, true);
                    }
                    catch { }
                }

                // Clean up any existing Layouts_Backup_* folders in UserSettings
                var userSettingsDir = Path.Combine(projectPath, "UserSettings");
                if (Directory.Exists(userSettingsDir))
                {
                    try
                    {
                        var backupDirs = Directory.GetDirectories(userSettingsDir, "Layouts_Backup_*");
                        foreach (var bDir in backupDirs)
                        {
                            try
                            {
                                var bFiles = Directory.GetFiles(bDir, "*.*", SearchOption.AllDirectories);
                                result.ProjectLayoutFilesDeleted += bFiles.Length;
                                Directory.Delete(bDir, true);
                            }
                            catch { }
                        }
                    }
                    catch { }

                    try
                    {
                        var looseDwltFiles = Directory.GetFiles(userSettingsDir, "*.dwlt", SearchOption.TopDirectoryOnly);
                        foreach (var file in looseDwltFiles)
                        {
                            try
                            {
                                File.Delete(file);
                                result.ProjectLayoutFilesDeleted++;
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add($"UserSettings 레이아웃 파일 삭제 실패 ({Path.GetFileName(file)}): {ex.Message}");
                            }
                        }
                    }
                    catch { }
                }

                // B. Library/CurrentLayout*.dwlt
                var libraryDir = Path.Combine(projectPath, "Library");
                if (Directory.Exists(libraryDir))
                {
                    var libraryLayoutPatterns = new[] { "CurrentLayout*.dwlt", "CurrentMaximizeLayout.dwlt", "*.dwlt" };
                    var deletedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pattern in libraryLayoutPatterns)
                    {
                        try
                        {
                            var files = Directory.GetFiles(libraryDir, pattern, SearchOption.TopDirectoryOnly);
                            foreach (var file in files)
                            {
                                if (deletedPaths.Add(file))
                                {
                                    try
                                    {
                                        File.Delete(file);
                                        result.ProjectLayoutFilesDeleted++;
                                    }
                                    catch (Exception ex)
                                    {
                                        result.Errors.Add($"Library 레이아웃 파일 삭제 실패 ({Path.GetFileName(file)}): {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                result.Messages.Add($"프로젝트 레이아웃 초기화 완료 ({result.ProjectLayoutFilesDeleted}개 파일 삭제)");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"프로젝트 레이아웃 초기화 중 오류: {ex.Message}");
            }
        }

        private void ResetGlobalEditorLayout(ResetResult result)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var currentLayoutDir = Path.Combine(appData, "Unity", "Editor-5.x", "Preferences", "Layouts", "current");

                if (Directory.Exists(currentLayoutDir))
                {
                    var files = Directory.GetFiles(currentLayoutDir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            result.EditorLayoutFilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"에디터 레이아웃 캐시 삭제 실패 ({Path.GetFileName(file)}): {ex.Message}");
                        }
                    }

                    try
                    {
                        Directory.Delete(currentLayoutDir, true);
                    }
                    catch { }
                }

                // Clean up any existing backup_current_* folders in Editor Layouts
                var editorLayoutsDir = Path.Combine(appData, "Unity", "Editor-5.x", "Preferences", "Layouts");
                if (Directory.Exists(editorLayoutsDir))
                {
                    try
                    {
                        var backupDirs = Directory.GetDirectories(editorLayoutsDir, "backup_current_*");
                        foreach (var bDir in backupDirs)
                        {
                            try
                            {
                                var bFiles = Directory.GetFiles(bDir, "*.*", SearchOption.AllDirectories);
                                result.EditorLayoutFilesDeleted += bFiles.Length;
                                Directory.Delete(bDir, true);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                result.Messages.Add($"에디터 전역 레이아웃 캐시 초기화 완료 ({result.EditorLayoutFilesDeleted}개 파일 삭제)");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"에디터 전역 레이아웃 초기화 중 오류: {ex.Message}");
            }
        }

        private void ResetRegistryLayoutEntries(ResetResult result)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 5.x", true))
                {
                    if (key != null)
                    {
                        var valueNames = key.GetValueNames();
                        foreach (var name in valueNames)
                        {
                            var lower = name.ToLowerInvariant();
                            // Check if name is related to window layouts or geometry
                            if (lower.Contains("savewindowlayout") ||
                                lower.Contains("restoredmainwindowsize") ||
                                lower.Contains("ismainwindowmaximized") ||
                                lower.StartsWith("unityeditor.popupwindow") ||
                                lower.StartsWith("unityeditor.preferencesettingswindow") ||
                                lower.StartsWith("unityeditor.imgui.controls.advanceddropdownwindow"))
                            {
                                try
                                {
                                    key.DeleteValue(name, false);
                                    result.RegistryValuesDeleted++;
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"레지스트리 키 삭제 실패 ({name}): {ex.Message}");
                                }
                            }
                        }
                    }
                }

                result.Messages.Add($"레지스트리 윈도우 레이아웃 설정 초기화 완료 ({result.RegistryValuesDeleted}개 항목 정리)");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"레지스트리 초기화 중 오류: {ex.Message}");
            }
        }
    }
}
