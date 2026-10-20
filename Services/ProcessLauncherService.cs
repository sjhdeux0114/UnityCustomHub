using System;
using System.Diagnostics;
using System.IO;

namespace UnityHubCustom.Services
{
    public class ProcessLauncherService
    {
        public string LastLogPath { get; private set; }

        public static string GetGraphicsArguments(string graphicsApi)
        {
            switch (graphicsApi)
            {
                case "DirectX11": return " -force-d3d11";
                case "DirectX12": return " -force-d3d12";
                case "OpenGL": return " -force-glcore";
                case "Default": return "";
                default: throw new ArgumentException("지원하지 않는 그래픽 API입니다.", nameof(graphicsApi));
            }
        }

        public Process LaunchUnityEditor(string editorExePath, string projectPath, bool useOpenGL = false)
            => LaunchUnityEditor(editorExePath, projectPath, useOpenGL ? "OpenGL" : "Default");

        public Process LaunchUnityEditor(string editorExePath, string projectPath, string graphicsApi)
        {
            var graphicsArguments = GetGraphicsArguments(graphicsApi);
            if (string.IsNullOrWhiteSpace(editorExePath) || !File.Exists(editorExePath))
            {
                throw new FileNotFoundException("Unity Editor 실행 파일이 존재하지 않습니다.", editorExePath);
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException("프로젝트 디렉토리가 존재하지 않습니다: " + projectPath);
            }

            new EditorProcessService().EnsureProjectClosed(projectPath);
            var logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnityHubCustom", "EditorLogs");
            Directory.CreateDirectory(logs);
            LastLogPath = Path.Combine(logs, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".log");
            var startInfo = new ProcessStartInfo
            {
                FileName = editorExePath,
                Arguments = $"-projectPath \"{Path.GetFullPath(projectPath).TrimEnd('\\', '/')}\" -logFile \"{LastLogPath}\"" + graphicsArguments,
                WorkingDirectory = Path.GetDirectoryName(editorExePath),
                UseShellExecute = true
            };

            return Process.Start(startInfo);
        }

        public void OpenInExplorer(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error opening explorer: " + ex.Message);
            }
        }
    }
}
