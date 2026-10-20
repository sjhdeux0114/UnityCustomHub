using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace UnityHubCustom.Services
{
    public class ResetResult
    {
        public bool Success { get; set; } = true;
        public int ProjectLayoutFilesDeleted { get; set; }
        public int EditorLayoutFilesDeleted { get; set; }
        public int RegistryValuesDeleted { get; set; }
        public bool SafeLayoutRestored { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();

        public string SummaryText =>
            $"초기화 완료 (프로젝트 파일: {ProjectLayoutFilesDeleted}개, 에디터 캐시: {EditorLayoutFilesDeleted}개, 레지스트리: {RegistryValuesDeleted}개 항목 정리됨, 단일 창 안전 레이아웃 복원: {(SafeLayoutRestored ? "성공" : "생략")})";
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

            try
            {
                var processes = new EditorProcessService();
                processes.EnsureProjectClosed(projectPath);
                if ((resetEditor || resetRegistry) && processes.FindEditors().Count > 0)
                {
                    resetEditor = false;
                    resetRegistry = false;
                    result.Messages.Add("다른 Unity Editor가 실행 중이므로 전역 캐시/레지스트리 초기화는 생략합니다.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
                return result;
            }

            // 1. Reset Project-specific Layouts
            if (resetProject)
            {
                ResetProjectLayout(projectPath, result);
            }

            // 2. Reset Unity Editor Global Layout Cache & Sanitize Default.wlt
            if (resetEditor)
            {
                ResetGlobalEditorLayout(result);
            }

            // 3. Reset Windows Registry Layout Entries
            if (resetRegistry)
            {
                ResetRegistryLayoutEntries(result);
            }

            result.Success = result.Errors.Count == 0;
            return result;
        }

        public static string SanitizeLayoutText(string yamlText)
        {
            if (string.IsNullOrWhiteSpace(yamlText)) return yamlText;

            // Fix off-screen or corrupted coordinates (e.g., Unity 2023.1 shipped with x: 2249, y: 302.5)
            yamlText = Regex.Replace(
                yamlText,
                @"(m_PixelRect:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*x:\s*)[^\r\n]+",
                "${1}0"
            );

            yamlText = Regex.Replace(
                yamlText,
                @"(m_PixelRect:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*x:\s*0\s*\r?\n\s*y:\s*)[^\r\n]+",
                "${1}0"
            );

            yamlText = Regex.Replace(
                yamlText,
                @"(m_PixelRect:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*x:\s*0\s*\r?\n\s*y:\s*0\s*\r?\n\s*width:\s*)[^\r\n]+",
                "${1}1920"
            );

            yamlText = Regex.Replace(
                yamlText,
                @"(m_PixelRect:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*x:\s*0\s*\r?\n\s*y:\s*0\s*\r?\n\s*width:\s*1920\s*\r?\n\s*height:\s*)[^\r\n]+",
                "${1}1040"
            );

            return yamlText;
        }

        private string GetSafeDefaultLayoutContent()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var defaultWltPath = Path.Combine(appData, "Unity", "Editor-5.x", "Preferences", "Layouts", "default", "Default.wlt");
                if (File.Exists(defaultWltPath))
                {
                    var text = File.ReadAllText(defaultWltPath);
                    return SanitizeLayoutText(text);
                }
            }
            catch { }

            return null;
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

                // C. Seed safe single-window layout to prevent 2-window and offscreen white screen freeze
                try
                {
                    var safeContent = GetSafeDefaultLayoutContent();
                    if (!string.IsNullOrWhiteSpace(safeContent))
                    {
                        if (!Directory.Exists(userSettingsLayouts))
                            Directory.CreateDirectory(userSettingsLayouts);

                        var targetPath = Path.Combine(userSettingsLayouts, "default-2023.dwlt");
                        File.WriteAllText(targetPath, safeContent);
                        result.SafeLayoutRestored = true;
                        result.Messages.Add("안전한 단일 창 기본 레이아웃(default-2023.dwlt) 복원 완료 (창 2개 열림 및 백화 방지)");
                    }
                }
                catch (Exception ex)
                {
                    result.Messages.Add($"안전 레이아웃 사전 생성 생략 ({ex.Message})");
                }

                result.Messages.Add($"프로젝트 레이아웃 초기화 완료 ({result.ProjectLayoutFilesDeleted}개 파일 정리)");
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

                    try
                    {
                        var looseDefault = Path.Combine(editorLayoutsDir, "default-2023.dwlt");
                        if (File.Exists(looseDefault))
                        {
                            File.Delete(looseDefault);
                            result.EditorLayoutFilesDeleted++;
                        }
                    }
                    catch { }
                }

                // Sanitize Default.wlt to eliminate offscreen x: 2249 coordinates baked into Unity 2023.1
                try
                {
                    var defaultWltPath = Path.Combine(editorLayoutsDir, "default", "Default.wlt");
                    if (File.Exists(defaultWltPath))
                    {
                        var original = File.ReadAllText(defaultWltPath);
                        var sanitized = SanitizeLayoutText(original);
                        if (!string.Equals(original, sanitized, StringComparison.Ordinal))
                        {
                            File.WriteAllText(defaultWltPath, sanitized);
                            result.Messages.Add("전역 Default.wlt 오프스크린 좌표(x:2249) 정상화 완료 (x:0, y:0)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Default.wlt 좌표 수정 실패: {ex.Message}");
                }

                result.Messages.Add($"에디터 전역 레이아웃 캐시 초기화 완료 ({result.EditorLayoutFilesDeleted}개 파일 정리)");
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
                            // Check if name is related to window layouts, coordinates, or geometry
                            if (lower.Contains("savewindowlayout") ||
                                lower.Contains("restoredmainwindowsize") ||
                                lower.Contains("ismainwindowmaximized") ||
                                lower.Contains("currentlayout") ||
                                lower.StartsWith("unityeditor.mainview") ||
                                lower.StartsWith("defaultwindow") ||
                                lower.StartsWith("unityeditor.popupwindow") ||
                                lower.StartsWith("unityeditor.preferencesettingswindow") ||
                                lower.StartsWith("unityeditor.imgui.controls.advanceddropdownwindow") ||
                                lower.Contains("windowx_") ||
                                lower.Contains("windowy_") ||
                                lower.Contains("windoww_") ||
                                lower.Contains("windowh_") ||
                                lower.Contains("windowz_"))
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
