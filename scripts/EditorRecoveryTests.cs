using System;
using System.Diagnostics;
using System.IO;
using UnityHubCustom.Services;

class EditorRecoveryTests
{
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }
    static int Main(string[] args)
    {
        if (args.Length > 0) { System.Threading.Thread.Sleep(60000); return 0; }
        var project = Path.Combine(Path.GetTempPath(), "UnityHubCustom-test-" + Guid.NewGuid(), "Project with spaces");
        Directory.CreateDirectory(Path.Combine(project, "Library"));
        var layout = Path.Combine(project, "Library", "CurrentLayout-default.dwlt");
        File.WriteAllText(layout, "keep while running");
        Process child = null;
        Process other = null;
        try
        {
            Check(EditorProcessService.MatchesProject("Unity.exe -projectPath \"" + project + "\"", project + "\\"), "Quoted path and trailing separator");
            Check(!EditorProcessService.MatchesProject("Unity.exe -projectPath \"" + project + "-other\"", project), "Exact project match");
            Check(!EditorProcessService.MatchesProject("Unity.exe -logFile \"" + project + "\"", project), "Unrelated argument ignored");
            Check(!EditorProcessService.IsUnityEditorProcess(@"C:\Program Files\Unity Hub\resources\unity.exe", "\"C:\\Program Files\\Unity Hub\\resources\\unity.exe\" serve"), "Unity Hub serve process ignored");
            Check(!EditorProcessService.IsUnityEditorProcess(@"C:\Users\sjhde\AppData\Local\Unity\bin\unity.exe", "--internal-auth-broker-serve"), "Unity Hub auth broker process ignored");
            Check(EditorProcessService.IsUnityEditorProcess(@"C:\Program Files\Unity\Hub\Editor\2023.1.22f1\Editor\Unity.exe", "-projectPath \"D:\\project\""), "Real Unity Editor recognized");

            var corruptYaml = "m_PixelRect:\r\n  serializedVersion: 2\r\n  x: 2249\r\n  y: 302.5\r\n  width: 1206\r\n  height: 715\r\nm_ShowMode: 4";
            var sanitizedYaml = LayoutResetService.SanitizeLayoutText(corruptYaml);
            Check(sanitizedYaml.Contains("x: 0") && sanitizedYaml.Contains("y: 0") && !sanitizedYaml.Contains("2249"), "Offscreen x:2249 coordinates sanitized to x:0, y:0");
            child = Process.Start(new ProcessStartInfo { FileName = typeof(EditorRecoveryTests).Assembly.Location, Arguments = "-projectPath \"" + project + "\"", UseShellExecute = false, CreateNoWindow = true });
            var service = new EditorProcessService();
            Check(service.FindEditors(project).Contains(child.Id), "Discover Editor by project");
            other = Process.Start(new ProcessStartInfo { FileName = typeof(EditorRecoveryTests).Assembly.Location, Arguments = "-projectPath \"" + project + "-other\"", UseShellExecute = false, CreateNoWindow = true });
            var blocked = new LayoutResetService().ResetLayouts(project, true, false, false);
            Check(!blocked.Success && File.Exists(layout), "Running Editor blocks reset without deleting layout");
            service.TerminateProjectEditors(project);
            Check(child.WaitForExit(5000), "Only matching test Editor terminated");
            Check(!other.HasExited, "Other project's Editor preserved");
            var global = new LayoutResetService().ResetLayouts(project, false, true, true);
            Check(global.Success && global.EditorLayoutFilesDeleted == 0 && global.RegistryValuesDeleted == 0 && global.Messages.Exists(m => m.Contains("생략")), "Global reset skipped while other Editor runs");
            var reset = new LayoutResetService().ResetLayouts(project, true, false, false);
            Check(reset.Success && !File.Exists(layout), "Reset allowed after Editor exits");
            File.WriteAllText(Path.Combine(project, "Library", "sentinel.asset"), "keep");
            new LayoutResetService().ResetLayouts(project, true, false, false);
            Check(File.Exists(Path.Combine(project, "Library", "sentinel.asset")), "Non-layout project data preserved");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            if (child != null) { if (!child.HasExited) child.Kill(); child.Dispose(); }
            if (other != null) { if (!other.HasExited) other.Kill(); other.Dispose(); }
            // Temporary test fixtures are intentionally retained for inspection.
        }
    }
}
