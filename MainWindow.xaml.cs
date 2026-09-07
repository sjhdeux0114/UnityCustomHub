using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using UnityHubCustom.Helpers;
using UnityHubCustom.Models;
using UnityHubCustom.Services;

namespace UnityHubCustom
{
    public partial class MainWindow : Window
    {
        private readonly UnityDetectionService _detectionService = new UnityDetectionService();
        private readonly ProjectService _projectService = new ProjectService();
        private readonly LayoutResetService _resetService = new LayoutResetService();
        private readonly ProcessLauncherService _launcherService = new ProcessLauncherService();

        private AppSettings _settings = new AppSettings();
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UnityHubCustom", "settings.json");

        private readonly List<UnityProject> _allProjects = new List<UnityProject>();
        private readonly ObservableCollection<UnityProject> _filteredProjects = new ObservableCollection<UnityProject>();
        private readonly ObservableCollection<UnityEditorInstall> _installedEditors = new ObservableCollection<UnityEditorInstall>();

        public MainWindow()
        {
            InitializeComponent();
            ProjectsItemsControl.ItemsSource = _filteredProjects;
            InstallsItemsControl.ItemsSource = _installedEditors;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            RefreshAll();
            LogMessage("Unity Hub+ 초기화 완료. 환영합니다!");
        }

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonHelper.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        _settings = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading settings: " + ex.Message);
            }

            // Sync UI with settings
            ChkResetProjectLayout.IsChecked = _settings.ResetProjectLayout;
            ChkResetEditorLayout.IsChecked = _settings.ResetEditorLayout;
            ChkResetRegistry.IsChecked = _settings.ResetRegistry;
            ChkBackupBeforeReset.IsChecked = _settings.BackupBeforeReset;
            ChkSyncUnityHub.IsChecked = _settings.AutoSyncWithUnityHub;

            _detectionService.SetCustomPaths(_settings.CustomEditorPaths);
        }

        private void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                _settings.ResetProjectLayout = ChkResetProjectLayout.IsChecked ?? true;
                _settings.ResetEditorLayout = ChkResetEditorLayout.IsChecked ?? true;
                _settings.ResetRegistry = ChkResetRegistry.IsChecked ?? true;
                _settings.BackupBeforeReset = ChkBackupBeforeReset.IsChecked ?? true;
                _settings.AutoSyncWithUnityHub = ChkSyncUnityHub.IsChecked ?? true;

                var json = JsonHelper.Serialize(_settings);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving settings: " + ex.Message);
            }
        }

        #endregion

        #region Data Refresh & Matching

        private void RefreshAll()
        {
            RefreshEditors();
            RefreshProjects();
            UpdateStatusSummary();
        }

        private void RefreshEditors()
        {
            _installedEditors.Clear();
            var editors = _detectionService.DetectInstalledEditors();
            foreach (var ed in editors)
            {
                _installedEditors.Add(ed);
            }
            LogMessage($"설치된 Unity 에디터 {_installedEditors.Count}개 감지됨.");
        }

        private void RefreshProjects()
        {
            _allProjects.Clear();
            var projects = _projectService.LoadProjects(_settings.AutoSyncWithUnityHub);

            // Match each project with installed editors
            var editorList = _installedEditors.ToList();
            foreach (var proj in projects)
            {
                var matched = _detectionService.FindBestMatchingEditor(proj.Version, editorList);
                if (matched != null)
                {
                    proj.IsEditorInstalled = true;
                    proj.MatchedEditorPath = matched.ExecutablePath;
                }
                else
                {
                    proj.IsEditorInstalled = false;
                    proj.MatchedEditorPath = null;
                }

                _allProjects.Add(proj);
            }

            // Sort by LastModified descending
            _allProjects.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));

            ApplyProjectFilter();
            LogMessage($"프로젝트 {_allProjects.Count}개 로드 완료.");
        }

        private void ApplyProjectFilter()
        {
            var query = TxtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            _filteredProjects.Clear();

            foreach (var p in _allProjects)
            {
                if (string.IsNullOrEmpty(query) ||
                    (!string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(query)) ||
                    (!string.IsNullOrEmpty(p.Path) && p.Path.ToLowerInvariant().Contains(query)))
                {
                    _filteredProjects.Add(p);
                }
            }

            TxtProjectCount.Text = $"{_filteredProjects.Count}개 프로젝트";
        }

        private void UpdateStatusSummary()
        {
            TxtStatus.Text = $"프로젝트 {_allProjects.Count}개 등록됨 | 감지된 Unity Editor {_installedEditors.Count}개";
        }

        #endregion

        #region Navigation Tabs

        private void TabRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewProjects == null || ViewInstalls == null || ViewSettings == null) return;

            ViewProjects.Visibility = TabProjectsRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ViewInstalls.Visibility = TabInstallsRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ViewSettings.Visibility = TabSettingsRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // Update text colors for active tab
            TabProjectsRadio.Foreground = TabProjectsRadio.IsChecked == true ? (Brush)FindResource("TextPrimaryBrush") : (Brush)FindResource("TextSecondaryBrush");
            TabInstallsRadio.Foreground = TabInstallsRadio.IsChecked == true ? (Brush)FindResource("TextPrimaryBrush") : (Brush)FindResource("TextSecondaryBrush");
            TabSettingsRadio.Foreground = TabSettingsRadio.IsChecked == true ? (Brush)FindResource("TextPrimaryBrush") : (Brush)FindResource("TextSecondaryBrush");

            // Toggle top Add Project button visibility
            BtnAddProject.Visibility = TabProjectsRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Search

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchPlaceholder != null)
            {
                TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
            ApplyProjectFilter();
        }

        #endregion

        #region Project Open Actions (Core Requirements)

        // 1. 일반 열기 (Normal Open)
        private void BtnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is UnityProject project)
            {
                OpenProjectInternal(project, resetLayout: false);
            }
        }

        // 2. 리셋후 열기 (Reset & Open)
        private void BtnResetAndOpenProject_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is UnityProject project)
            {
                OpenProjectInternal(project, resetLayout: true);
            }
        }

        private void OpenProjectInternal(UnityProject project, bool resetLayout)
        {
            if (!Directory.Exists(project.Path))
            {
                MessageBox.Show(this, $"프로젝트 디렉토리를 찾을 수 없습니다:\n{project.Path}", "프로젝트 열기 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Find matching editor
            var editorPath = project.MatchedEditorPath;
            if (string.IsNullOrEmpty(editorPath) || !File.Exists(editorPath))
            {
                var matched = _detectionService.FindBestMatchingEditor(project.Version, _installedEditors.ToList());
                if (matched != null)
                {
                    editorPath = matched.ExecutablePath;
                }
            }

            if (string.IsNullOrEmpty(editorPath) || !File.Exists(editorPath))
            {
                var result = MessageBox.Show(
                    this,
                    $"프로젝트 버전({project.Version})에 맞는 Unity Editor를 찾을 수 없습니다.\n[설치된 에디터] 탭에서 Unity.exe를 수동 등록하시겠습니까?",
                    "Unity Editor 미발견",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    TabInstallsRadio.IsChecked = true;
                }
                return;
            }

            if (resetLayout)
            {
                // Reset layouts before opening
                var resetProj = ChkResetProjectLayout.IsChecked ?? true;
                var resetEd = ChkResetEditorLayout.IsChecked ?? true;
                var resetReg = ChkResetRegistry.IsChecked ?? true;
                var backup = ChkBackupBeforeReset.IsChecked ?? true;

                LogMessage($"[{project.Name}] 레이아웃 초기화 작업 시작...");

                var resetResult = _resetService.ResetLayouts(project.Path, resetProj, resetEd, resetReg, backup);

                foreach (var msg in resetResult.Messages)
                {
                    LogMessage($"  ✓ {msg}");
                }
                foreach (var err in resetResult.Errors)
                {
                    LogMessage($"  ⚠ {err}");
                }

                LogMessage($"[{project.Name}] 레이아웃 초기화 완료: {resetResult.SummaryText}");
                TxtLastAction.Text = $"[{DateTime.Now:HH:mm:ss}] {project.Name} 레이아웃 초기화 후 실행 중...";
            }
            else
            {
                LogMessage($"[{project.Name}] 일반 실행 시작...");
                TxtLastAction.Text = $"[{DateTime.Now:HH:mm:ss}] {project.Name} 실행 중...";
            }

            try
            {
                var proc = _launcherService.LaunchUnityEditor(editorPath, project.Path);
                LogMessage($"Unity Editor 실행 성공 (PID: {proc.Id}, 버전: {project.Version}, 에디터: {editorPath})");
            }
            catch (Exception ex)
            {
                LogMessage($"Unity Editor 실행 오류: {ex.Message}");
                MessageBox.Show(this, $"Unity 실행 중 오류가 발생했습니다:\n{ex.Message}", "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Context Menu & More Options

        private void BtnMoreOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void MenuOpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (GetProjectFromMenuItem(sender) is UnityProject project)
            {
                _launcherService.OpenInExplorer(project.Path);
                LogMessage($"탐색기에서 열기: {project.Path}");
            }
        }

        private void MenuResetLayoutOnly_Click(object sender, RoutedEventArgs e)
        {
            if (GetProjectFromMenuItem(sender) is UnityProject project)
            {
                var confirm = MessageBox.Show(
                    this,
                    $"[{project.Name}]의 프로젝트 Layout 정보와 Unity Editor의 전역 Layout 정보를 초기화하시겠습니까?\n(열지 않고 초기화만 수행합니다)",
                    "레이아웃 초기화 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    var resetResult = _resetService.ResetLayouts(
                        project.Path,
                        ChkResetProjectLayout.IsChecked ?? true,
                        ChkResetEditorLayout.IsChecked ?? true,
                        ChkResetRegistry.IsChecked ?? true,
                        ChkBackupBeforeReset.IsChecked ?? true);

                    foreach (var msg in resetResult.Messages)
                    {
                        LogMessage($"  ✓ {msg}");
                    }
                    LogMessage($"[{project.Name}] 레이아웃만 초기화 완료: {resetResult.SummaryText}");
                    MessageBox.Show(this, $"레이아웃 초기화가 완료되었습니다.\n{resetResult.SummaryText}", "초기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void MenuRemoveProject_Click(object sender, RoutedEventArgs e)
        {
            if (GetProjectFromMenuItem(sender) is UnityProject project)
            {
                var confirm = MessageBox.Show(
                    this,
                    $"'{project.Name}' 프로젝트를 목록에서 제거하시겠습니까?\n(실제 디렉토리의 파일은 삭제되지 않습니다)",
                    "프로젝트 목록에서 제거",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    _allProjects.Remove(project);
                    _projectService.SaveCustomProjects(_allProjects);
                    ApplyProjectFilter();
                    UpdateStatusSummary();
                    LogMessage($"프로젝트 제거됨: {project.Name}");
                }
            }
        }

        private UnityProject GetProjectFromMenuItem(object sender)
        {
            if (sender is MenuItem mi && mi.Parent is ContextMenu cm && cm.PlacementTarget is Button btn)
            {
                return btn.Tag as UnityProject;
            }
            return null;
        }

        #endregion

        #region Toolbar Buttons (Add Project, Refresh)

        private void BtnAddProject_Click(object sender, RoutedEventArgs e)
        {
            // Use WinForms FolderBrowserDialog or OpenFileDialog
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Unity 프로젝트 루트 폴더를 선택하세요 (Assets 폴더가 위치한 디렉토리)",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var newProject = _projectService.AddProjectFromFolder(dialog.SelectedPath);

                    // Check if already in list
                    if (_allProjects.Any(p => string.Equals(p.Path, newProject.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(this, "이미 목록에 등록되어 있는 프로젝트입니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Match editor
                    var matched = _detectionService.FindBestMatchingEditor(newProject.Version, _installedEditors.ToList());
                    if (matched != null)
                    {
                        newProject.IsEditorInstalled = true;
                        newProject.MatchedEditorPath = matched.ExecutablePath;
                    }

                    _allProjects.Insert(0, newProject);
                    _projectService.SaveCustomProjects(_allProjects);
                    ApplyProjectFilter();
                    UpdateStatusSummary();
                    LogMessage($"새 프로젝트 추가됨: {newProject.Name} (버전: {newProject.Version})");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "프로젝트 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAll();
            LogMessage("새로고침 완료.");
        }

        #endregion

        #region Editor Management Actions

        private void BtnAddEditor_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Unity Editor 실행 파일(Unity.exe)을 선택하세요",
                Filter = "Unity Editor (Unity.exe)|Unity.exe|모든 실행 파일 (*.exe)|*.exe",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog(this) == true)
            {
                var selectedPath = openFileDialog.FileName;
                if (!_settings.CustomEditorPaths.Contains(selectedPath))
                {
                    _settings.CustomEditorPaths.Add(selectedPath);
                    SaveSettings();
                    _detectionService.SetCustomPaths(_settings.CustomEditorPaths);
                    RefreshAll();
                    LogMessage($"수동 Unity Editor 추가됨: {selectedPath}");
                }
                else
                {
                    MessageBox.Show(this, "이미 등록된 에디터 경로입니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnOpenEditorFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is UnityEditorInstall editor)
            {
                _launcherService.OpenInExplorer(Path.GetDirectoryName(editor.ExecutablePath));
            }
        }

        private void BtnRemoveCustomEditor_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is UnityEditorInstall editor)
            {
                if (_settings.CustomEditorPaths.Remove(editor.ExecutablePath))
                {
                    SaveSettings();
                    _detectionService.SetCustomPaths(_settings.CustomEditorPaths);
                    RefreshAll();
                    LogMessage($"수동 에디터 등록 해제: {editor.DisplayName}");
                }
            }
        }

        #endregion

        #region Settings & Logs

        private void ChkSyncUnityHub_Changed(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLogs.Clear();
        }

        private void LogMessage(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            TxtLogs.AppendText(line + Environment.NewLine);
            TxtLogs.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }

        #endregion
    }
}
