using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace UnityHubCustom.Services
{
    public class EditorProcessService
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string commandLine, out int count);
        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        private readonly string _targetProcessName;

        public EditorProcessService(string targetProcessName = null)
        {
            if (targetProcessName != null)
            {
                _targetProcessName = targetProcessName;
            }
            else
            {
                string entry = null;
                try
                {
                    entry = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "");
                }
                catch { }

                if (!string.IsNullOrEmpty(entry) &&
                    (entry.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     entry.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _targetProcessName = entry;
                }
                else
                {
                    _targetProcessName = "Unity.exe";
                }
            }
        }

        public static bool IsUnityEditorProcess(string executablePath, string commandLine, string targetProcessName = "Unity.exe")
        {
            // Unity Hub background CLI / auth broker helper processes should not be counted as Editors
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                if (commandLine.IndexOf("serve", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    commandLine.IndexOf("--internal-auth-broker-serve", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                // Processes inside Unity Hub resource directories are helper services, not Editors
                if (executablePath.IndexOf("Unity Hub", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    executablePath.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                var fileName = Path.GetFileName(executablePath);
                if (string.Equals(fileName, targetProcessName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "Unity.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // If the command line contains -projectPath, treat as editor or test process
            if (!string.IsNullOrWhiteSpace(commandLine) &&
                commandLine.IndexOf("-projectPath", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static bool MatchesProject(string commandLine, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return false;
            int count;
            var argv = CommandLineToArgvW(commandLine, out count);
            if (argv == IntPtr.Zero) throw new InvalidOperationException("Editor 실행 인수를 읽을 수 없습니다.");
            try
            {
                for (int i = 0; i < count - 1; i++)
                {
                    var arg = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size));
                    if (!string.Equals(arg, "-projectPath", StringComparison.OrdinalIgnoreCase)) continue;
                    var path = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, (i + 1) * IntPtr.Size));
                    return string.Equals(Normalize(path), Normalize(projectPath), StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
            finally { LocalFree(argv); }
        }

        private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd('\\', '/');

        public List<int> FindEditors(string projectPath = null)
        {
            var ids = new List<int>();
            var query = string.Equals(_targetProcessName, "Unity.exe", StringComparison.OrdinalIgnoreCase)
                ? "SELECT ProcessId, CommandLine, ExecutablePath, Name FROM Win32_Process WHERE Name='Unity.exe'"
                : $"SELECT ProcessId, CommandLine, ExecutablePath, Name FROM Win32_Process WHERE Name='Unity.exe' OR Name='{_targetProcessName}'";

            using (var search = new ManagementObjectSearcher(query))
            using (var rows = search.Get())
            {
                foreach (ManagementObject row in rows)
                using (row)
                {
                    var commandLine = row["CommandLine"] as string;
                    var exePath = row["ExecutablePath"] as string;

                    if (!IsUnityEditorProcess(exePath, commandLine, _targetProcessName))
                        continue;

                    var pid = Convert.ToInt32(row["ProcessId"]);
                    if (pid == Process.GetCurrentProcess().Id)
                        continue;

                    if (projectPath != null && commandLine == null)
                        throw new InvalidOperationException("Unity 실행 정보를 읽을 수 없습니다. Editor를 종료한 후 다시 시도하세요.");
                    if (projectPath == null || MatchesProject(commandLine, projectPath))
                        ids.Add(pid);
                }
            }
            return ids;
        }

        public void EnsureProjectClosed(string projectPath)
        {
            var ids = FindEditors(projectPath);
            if (ids.Count != 0)
                throw new InvalidOperationException("이 프로젝트의 Editor가 아직 실행 중입니다 (PID: " + string.Join(", ", ids) + "). 프로젝트 메뉴의 '남아 있는 Editor 종료'를 사용하세요.");
            var lockPath = Path.Combine(projectPath, "Temp", "UnityLockfile");
            if (File.Exists(lockPath))
                using (File.Open(lockPath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
        }

        public void TerminateProjectEditors(string projectPath)
        {
            foreach (var id in FindEditors(projectPath))
            {
                // Re-query immediately before killing so a recycled PID is not targeted.
                using (var row = new ManagementObject("Win32_Process.Handle='" + id + "'"))
                {
                    row.Get();
                    var commandLine = row["CommandLine"] as string;
                    var exePath = row["ExecutablePath"] as string;
                    if (!IsUnityEditorProcess(exePath, commandLine, _targetProcessName) ||
                        !MatchesProject(commandLine, projectPath)) continue;
                    var status = Convert.ToUInt32(row.InvokeMethod("Terminate", new object[] { (uint)1 }));
                    if (status != 0) throw new InvalidOperationException("Editor 종료 실패 (PID: " + id + ", 코드: " + status + ")");
                }
                try
                {
                    using (var process = Process.GetProcessById(id))
                        if (!process.WaitForExit(5000)) throw new InvalidOperationException("Editor 종료 대기 시간이 초과되었습니다.");
                }
                catch (ArgumentException) { /* Already exited. */ }
            }
            EnsureProjectClosed(projectPath);
        }
    }
}
