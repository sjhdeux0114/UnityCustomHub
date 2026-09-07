using System;
using System.Diagnostics;
using System.IO;

namespace UnityHubCustom.Services
{
    public class ProcessLauncherService
    {
        public Process LaunchUnityEditor(string editorExePath, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(editorExePath) || !File.Exists(editorExePath))
            {
                throw new FileNotFoundException("Unity Editor 실행 파일이 존재하지 않습니다.", editorExePath);
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException("프로젝트 디렉토리가 존재하지 않습니다: " + projectPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = editorExePath,
                Arguments = $"-projectPath \"{projectPath}\"",
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
